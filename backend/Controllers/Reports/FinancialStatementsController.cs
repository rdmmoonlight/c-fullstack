namespace AumoBackend.Controllers.Reports;

using AumoBackend.Data;
using AumoBackend.Extensions;
using AumoBackend.Helpers;
using AumoBackend.Services.Reports.FinancialStatements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("reports")]
[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
public class FinancialStatementsController : ControllerBase
{
    private const string NoPeriodMessage = "No accounting period selected.";

    private readonly AppDbContext _db;
    private readonly IFinancialStatementsService _statements;

    public FinancialStatementsController(AppDbContext db, IFinancialStatementsService statements)
    {
        _db = db;
        _statements = statements;
    }

    // GET /api/v1/reports/income-statement
    [HttpGet("income-statement")]
    public async Task<IActionResult> GetIncomeStatement()
    {
        var userId = User.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        var period = await SelectedPeriodHelper.GetSelectedPeriodAsync(_db, userId);
        if (period == null)
        {
            return Ok(new
            {
                success = true,
                hasPeriodSelected = false,
                message = NoPeriodMessage,
                selectedPeriodName = (string?)null,
                revenueAccounts = Array.Empty<object>(),
                expenseAccounts = Array.Empty<object>(),
                otherIncomeAccounts = Array.Empty<object>(),
                otherExpenseAccounts = Array.Empty<object>(),
            });
        }

        var statement = await _statements.BuildIncomeStatementAsync(userId, period);

        return Ok(new
        {
            success = true,
            hasPeriodSelected = true,
            selectedPeriodName = period.PeriodName,
            asOfDate = statement.AsOfDate,
            revenueAccounts = statement.Revenues,
            totalRevenue = statement.TotalRevenue,
            expenseAccounts = statement.OperatingExpenses,
            totalExpenses = statement.TotalOperatingExpenses,
            operatingIncome = statement.OperatingIncome,
            otherIncomeAccounts = statement.OtherIncome,
            otherExpenseAccounts = statement.OtherExpenses,
            totalOtherIncome = statement.TotalOtherIncome,
            totalOtherExpenses = statement.TotalOtherExpenses,
            netIncome = statement.NetIncome,
        });
    }

    // GET /api/v1/reports/retained-earnings
    [HttpGet("retained-earnings")]
    public async Task<IActionResult> GetRetainedEarnings()
    {
        var userId = User.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        var period = await SelectedPeriodHelper.GetSelectedPeriodAsync(_db, userId);
        if (period == null)
        {
            return Ok(new
            {
                success = true,
                hasPeriodSelected = false,
                message = NoPeriodMessage,
                selectedPeriodName = (string?)null,
                accountName = "Retained Earnings",
                startDate = (DateTime?)null,
                endDate = (DateTime?)null,
                beginningRetainedEarnings = 0m,
                netIncome = 0m,
                dividendsOrDraws = 0m,
                endingRetainedEarnings = 0m,
            });
        }

        var statement = await _statements.BuildRetainedEarningsAsync(userId, period);

        return Ok(new
        {
            success = true,
            hasPeriodSelected = true,
            selectedPeriodName = period.PeriodName,
            accountName = statement.AccountName,
            startDate = statement.StartDate,
            endDate = statement.EndDate,
            beginningRetainedEarnings = statement.BeginningBalance,
            netIncome = statement.NetIncome,
            dividendsOrDraws = statement.Dividends,
            endingRetainedEarnings = statement.EndingBalance,
        });
    }

    // GET /api/v1/reports/statement-of-cash-flow
    [HttpGet("statement-of-cash-flow")]
    public async Task<IActionResult> GetCashFlowStatement()
    {
        var userId = User.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        var period = await SelectedPeriodHelper.GetSelectedPeriodAsync(_db, userId);
        if (period == null)
        {
            return Ok(new
            {
                success = true,
                hasPeriodSelected = false,
                message = NoPeriodMessage,
                selectedPeriodName = (string?)null,
                operatingActivities = Array.Empty<object>(),
                netCashFromOperating = 0m,
                investingActivities = Array.Empty<object>(),
                netCashFromInvesting = 0m,
                financingActivities = Array.Empty<object>(),
                netCashFromFinancing = 0m,
                netChangeInCash = 0m,
                beginningCash = 0m,
                endingCash = 0m,
            });
        }

        var statement = await _statements.BuildCashFlowAsync(userId, period);

        return Ok(new
        {
            success = true,
            hasPeriodSelected = true,
            selectedPeriodName = period.PeriodName,
            operatingActivities = statement.OperatingActivities,
            netCashFromOperating = statement.NetCashFromOperating,
            investingActivities = statement.InvestingActivities,
            netCashFromInvesting = statement.NetCashFromInvesting,
            financingActivities = statement.FinancingActivities,
            netCashFromFinancing = statement.NetCashFromFinancing,
            netChangeInCash = statement.NetChangeInCash,
            beginningCash = statement.BeginningCash,
            endingCash = statement.EndingCash,
        });
    }

    // GET /api/v1/reports/statement-of-financial-position
    [HttpGet("statement-of-financial-position")]
    public async Task<IActionResult> GetStatementOfFinancialPosition([FromQuery] bool isPostClosing = false)
    {
        var userId = User.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        var period = await SelectedPeriodHelper.GetSelectedPeriodAsync(_db, userId);
        if (period == null)
        {
            return Ok(new
            {
                success = true,
                hasPeriodSelected = false,
                message = NoPeriodMessage,
                selectedPeriodName = (string?)null,
                assetAccounts = Array.Empty<object>(),
                totalAssets = 0m,
                liabilityAccounts = Array.Empty<object>(),
                totalLiabilities = 0m,
                equityAccounts = Array.Empty<object>(),
                totalEquity = 0m,
                totalLiabilitiesAndEquity = 0m,
                isBalanced = true,
            });
        }

        var sofp = await _statements.BuildSofpAsync(userId, period, isPostClosing);

        var equityAccounts = sofp.EquityExcludingRetainedEarnings
            .Select(e => new { accountId = 0, referenceNumber = e.ReferenceNumber, accountName = e.AccountName, amount = e.Amount })
            .Append(new { accountId = 0, referenceNumber = "0", accountName = "Retained Earnings", amount = sofp.RetainedEarningsEnding })
            .ToList();

        return Ok(new
        {
            success = true,
            hasPeriodSelected = true,
            selectedPeriodName = period.PeriodName,
            asOfDate = sofp.AsOfDate,
            isPostClosing = sofp.IsPostClosing,
            assetAccounts = sofp.Assets.Select(a => new { accountId = 0, referenceNumber = a.ReferenceNumber, accountName = a.AccountName, amount = a.Amount }),
            totalAssets = sofp.TotalAssets,
            liabilityAccounts = sofp.Liabilities.Select(l => new { accountId = 0, referenceNumber = l.ReferenceNumber, accountName = l.AccountName, amount = l.Amount }),
            totalLiabilities = sofp.TotalLiabilities,
            equityAccounts,
            totalEquity = sofp.TotalEquity,
            totalLiabilitiesAndEquity = sofp.TotalLiabilitiesAndEquity,
            isBalanced = sofp.IsBalanced,
        });
    }
}

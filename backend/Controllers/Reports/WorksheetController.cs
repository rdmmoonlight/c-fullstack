namespace AumoBackend.Controllers.Reports;

using AumoBackend.Extensions;
using AumoBackend.Services.Reports.Worksheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("reports/worksheet")]
[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
public class WorksheetController : ControllerBase
{
    private readonly IWorksheetService _worksheetService;
    private readonly ILogger<WorksheetController> _logger;

    public WorksheetController(IWorksheetService worksheetService, ILogger<WorksheetController> logger)
    {
        _worksheetService = worksheetService;
        _logger = logger;
    }

    // GET /api/v1/reports/worksheet
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var userId = User.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        var result = await _worksheetService.GetWorksheetAsync(userId);
        if (!result.HasPeriodSelected)
        {
            return Ok(new
            {
                success = true,
                hasPeriodSelected = false,
                message = "No accounting period selected.",
                selectedPeriodName = (string?)null,
                rows = Array.Empty<object>(),
                totals = new { },
            });
        }

        // Snapshot ke tabel Worksheet bersifat pelengkap: kegagalan menyimpan tidak boleh
        // menggagalkan kertas kerja yang sudah berhasil dihitung.
        try
        {
            await _worksheetService.SaveSnapshotAsync(userId, result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gagal menyimpan snapshot worksheet periode {PeriodId}.", result.PeriodId);
        }

        var t = result.Totals;

        // Nama field mengikuti kontrak yang dipakai web dan mobile.
        return Ok(new
        {
            success = true,
            hasPeriodSelected = true,
            selectedPeriodName = result.SelectedPeriodName,
            rows = result.Rows.Select(r => new
            {
                accountId = r.AccountId,
                referenceNumber = r.ReferenceNumber,
                accountName = r.AccountName,
                type = r.Type,
                normalBalanceIsDebit = r.NormalBalanceIsDebit,
                tbDebit = r.UnadjustedDebit,
                tbCredit = r.UnadjustedCredit,
                adjDebit = r.AdjustmentDebit,
                adjCredit = r.AdjustmentCredit,
                adjTbDebit = r.AdjustedDebit,
                adjTbCredit = r.AdjustedCredit,
                isDebit = r.IncomeStatementDebit,
                isCredit = r.IncomeStatementCredit,
                bsDebit = r.FinancialPositionDebit,
                bsCredit = r.FinancialPositionCredit,
            }),
            totals = new
            {
                tbDebit = t.UnadjustedDebit,
                tbCredit = t.UnadjustedCredit,
                adjDebit = t.AdjustmentDebit,
                adjCredit = t.AdjustmentCredit,
                adjTbDebit = t.AdjustedDebit,
                adjTbCredit = t.AdjustedCredit,
                isDebit = t.IncomeStatementDebit,
                isCredit = t.IncomeStatementCredit,
                bsDebit = t.FinancialPositionDebit,
                bsCredit = t.FinancialPositionCredit,
                netIncome = t.NetIncome,
            },
        });
    }
}

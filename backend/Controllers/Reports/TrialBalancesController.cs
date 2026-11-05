using System.Security.Claims;
using AumoBackend.Data;
using AumoBackend.Helpers;
using AumoBackend.Models;
using AumoBackend.Services.Reports.TrialBalances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AumoBackend.Controllers.Reports;

[ApiController]
[Route("reports/trial-balances")]
[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
public class TrialBalancesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITrialBalancesService _trialBalancesService;
    private readonly ILogger<TrialBalancesController> _logger;

    public TrialBalancesController(
        AppDbContext db,
        ITrialBalancesService trialBalancesService,
        ILogger<TrialBalancesController> logger)
    {
        _db = db;
        _trialBalancesService = trialBalancesService;
        _logger = logger;
    }

    // GET /reports/trial-balances?type=unadjusted|adjusted|post-closing
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? type = "unadjusted")
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { success = false, message = "User identity invalid." });
        }

        var reportType = (type ?? "unadjusted").Trim().ToLowerInvariant().Replace("_", "-");
        if (reportType == "postclosing")
        {
            reportType = "post-closing";
        }

        var title = reportType switch
        {
            "unadjusted" => "Neraca Saldo",
            "adjusted" => "Neraca Saldo Disesuaikan",
            "post-closing" => "Neraca Saldo Pasca-Penutupan",
            _ => null
        };

        if (title == null)
        {
            return BadRequest(new { success = false, message = "type harus unadjusted, adjusted, atau post-closing." });
        }

        var period = await ResolveSelectedPeriodAsync(userId);
        if (period == null)
        {
            return Ok(new
            {
                success = true,
                hasPeriodSelected = false,
                selectedPeriodName = (string?)null,
                reportTitle = title,
                type = reportType,
                totalDebit = 0m,
                totalCredit = 0m,
                isBalanced = true,
                rows = Array.Empty<object>()
            });
        }

        var rows = await _trialBalancesService.BuildTrialBalancesRowsAsync(
            userId,
            period,
            includeAdjusting: reportType != "unadjusted",
            reportType: reportType);

        // Snapshot ke tabel TrialBalance* bersifat pelengkap: kegagalan menyimpan tidak boleh
        // menggagalkan laporan yang sudah berhasil dihitung.
        try
        {
            await _trialBalancesService.SaveSnapshotAsync(reportType, userId, period, rows);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gagal menyimpan snapshot neraca saldo {ReportType} periode {PeriodId}.", reportType, period.Id);
        }

        var totalDebit = rows.Sum(r => r.Debit);
        var totalCredit = rows.Sum(r => r.Credit);

        return Ok(new
        {
            success = true,
            hasPeriodSelected = true,
            selectedPeriodName = period.PeriodName,
            reportTitle = title,
            type = reportType,
            totalDebit,
            totalCredit,
            isBalanced = Math.Round(totalDebit - totalCredit, 2) == 0,
            rows = rows.Select(r => new
            {
                accountId = r.AccountId,
                referenceNumber = r.ReferenceNumber,
                accountName = r.AccountName,
                type = r.Type,
                role = r.Role ?? string.Empty,
                normalBalanceIsDebit = r.NormalBalanceIsDebit,
                netBalance = r.NetBalance,
                debit = r.Debit,
                credit = r.Credit
            })
        });
    }

    private async Task<Period?> ResolveSelectedPeriodAsync(Guid userId)
    {
        var selected = await _db.Periods.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.IsSelected);
        if (selected != null)
        {
            return selected;
        }

        var helper = await SelectedPeriodHelper.GetSelectedPeriodAsync(_db, userId);
        if (helper == null)
        {
            return null;
        }

        return await _db.Periods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == helper.Id && x.UserId == userId);
    }

    private Guid GetCurrentUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }
}

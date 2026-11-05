namespace AumoBackend.Services.Reports.Worksheet;

using AumoBackend.Data;
using AumoBackend.DTOs.Reports;
using AumoBackend.Helpers;
using AumoBackend.Models;
using AumoBackend.Services.Reports.TrialBalances;
using Microsoft.EntityFrameworkCore;
using WorksheetEntity = AumoBackend.Models.Worksheet; // alias: "Worksheet" juga nama namespace service ini

public class WorksheetService : IWorksheetService
{
    private readonly AppDbContext _db;
    private readonly ITrialBalancesService _trialBalancesService;

    public WorksheetService(AppDbContext db, ITrialBalancesService trialBalancesService)
    {
        _db = db;
        _trialBalancesService = trialBalancesService;
    }

    public async Task<WorksheetResult> GetWorksheetAsync(Guid userId)
    {
        var period = await SelectedPeriodHelper.GetSelectedPeriodAsync(_db, userId);
        if (period == null)
        {
            return new WorksheetResult { HasPeriodSelected = false };
        }

        var unadjusted = await _trialBalancesService.BuildTrialBalancesRowsAsync(userId, period, false, "unadjusted");
        var adjusted = await _trialBalancesService.BuildTrialBalancesRowsAsync(userId, period, true, "adjusted");

        // Mengambil ChartOfAccounts ber-key Guid
        var accounts = await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.IsActive && a.UserId == userId)
            .ToDictionaryAsync(a => a.Id); // a.Id pastikan bertipe Guid

        var unadjustedById = unadjusted.ToDictionary(r => r.AccountId); // r.AccountId bertipe Guid
        var adjustedById = adjusted.ToDictionary(r => r.AccountId);   // r.AccountId bertipe Guid

        var rows = new List<WorksheetRowApiResponse>();
        foreach (var accountId in unadjustedById.Keys.Union(adjustedById.Keys))
        {
            if (!accounts.TryGetValue(accountId.GetHashCode(), out var account))
            {
                continue;
            }

            unadjustedById.TryGetValue(accountId, out var u);
            adjustedById.TryGetValue(accountId, out var a);

            var uDebit = u?.Debit ?? 0m;
            var uCredit = u?.Credit ?? 0m;
            var aDebit = a?.Debit ?? 0m;
            var aCredit = a?.Credit ?? 0m;

            // Kolom penyesuaian = selisih saldo neto sesudah dan sebelum jurnal penyesuaian.
            var adjNet = (aDebit - aCredit) - (uDebit - uCredit);

            var row = new WorksheetRowApiResponse
            {
                AccountId = accountId.GetHashCode(),
                ReferenceNumber = account.ReferenceNumber,
                AccountName = account.AccountName,
                Type = account.Type,
                NormalBalanceIsDebit = AccountClassificationHelper.NormalBalanceIsDebit(account.Type),
                UnadjustedDebit = uDebit,
                UnadjustedCredit = uCredit,
                AdjustmentDebit = adjNet > 0m ? adjNet : 0m,
                AdjustmentCredit = adjNet < 0m ? -adjNet : 0m,
                AdjustedDebit = aDebit,
                AdjustedCredit = aCredit,
            };

            // Akun temporer masuk Laba Rugi, akun permanen masuk Posisi Keuangan.
            if (AccountClassificationHelper.IsTemporary(account.Type))
            {
                row.IncomeStatementDebit = aDebit;
                row.IncomeStatementCredit = aCredit;
            }
            else
            {
                row.FinancialPositionDebit = aDebit;
                row.FinancialPositionCredit = aCredit;
            }

            rows.Add(row);
        }

        rows = rows.OrderBy(r => r.ReferenceNumber).ToList();

        var totals = new WorksheetTotalsApiResponse
        {
            UnadjustedDebit = rows.Sum(r => r.UnadjustedDebit),
            UnadjustedCredit = rows.Sum(r => r.UnadjustedCredit),
            AdjustmentDebit = rows.Sum(r => r.AdjustmentDebit),
            AdjustmentCredit = rows.Sum(r => r.AdjustmentCredit),
            AdjustedDebit = rows.Sum(r => r.AdjustedDebit),
            AdjustedCredit = rows.Sum(r => r.AdjustedCredit),
            IncomeStatementDebit = rows.Sum(r => r.IncomeStatementDebit),
            IncomeStatementCredit = rows.Sum(r => r.IncomeStatementCredit),
            FinancialPositionDebit = rows.Sum(r => r.FinancialPositionDebit),
            FinancialPositionCredit = rows.Sum(r => r.FinancialPositionCredit),
        };
        totals.NetIncome = totals.IncomeStatementCredit - totals.IncomeStatementDebit;

        return new WorksheetResult
        {
            HasPeriodSelected = true,
            PeriodId = period.Id,
            SelectedPeriodName = period.PeriodName,
            Rows = rows,
            Totals = totals,
        };
    }

    public async Task SaveSnapshotAsync(Guid userId, WorksheetResult result)
    {
        if (!result.HasPeriodSelected)
        {
            return;
        }

        var periodId = result.PeriodId;
        var savedAt = DateTime.UtcNow; // jam aplikasi, sama dengan CreatedAt/UpdatedAt jurnal
        var fresh = result.Rows
            .Select(r => new WorksheetEntity
            {
                UserId = userId,
                PeriodId = periodId,
                AccountId = r.AccountId,
                AccountNumber = Truncate(r.ReferenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), 50),
                AccountName = Truncate(r.AccountName, 255),
                AccountType = Truncate(r.Type, 50),
                NormalBalanceIsDebit = r.NormalBalanceIsDebit,
                UnadjustedDebit = Math.Round(r.UnadjustedDebit, 2),
                UnadjustedCredit = Math.Round(r.UnadjustedCredit, 2),
                AdjustmentDebit = Math.Round(r.AdjustmentDebit, 2),
                AdjustmentCredit = Math.Round(r.AdjustmentCredit, 2),
                AdjustedDebit = Math.Round(r.AdjustedDebit, 2),
                AdjustedCredit = Math.Round(r.AdjustedCredit, 2),
                IncomeStatementDebit = Math.Round(r.IncomeStatementDebit, 2),
                IncomeStatementCredit = Math.Round(r.IncomeStatementCredit, 2),
                FinancialPositionDebit = Math.Round(r.FinancialPositionDebit, 2),
                FinancialPositionCredit = Math.Round(r.FinancialPositionCredit, 2),
                IsTotalRow = false,
                CreatedAt = savedAt,
            })
            .ToList();

        var t = result.Totals;
        fresh.Add(new WorksheetEntity
        {
            UserId = userId,
            PeriodId = periodId,
            AccountId = null,
            AccountNumber = string.Empty,
            AccountName = "Total",
            AccountType = string.Empty,
            NormalBalanceIsDebit = false,
            UnadjustedDebit = Math.Round(t.UnadjustedDebit, 2),
            UnadjustedCredit = Math.Round(t.UnadjustedCredit, 2),
            AdjustmentDebit = Math.Round(t.AdjustmentDebit, 2),
            AdjustmentCredit = Math.Round(t.AdjustmentCredit, 2),
            AdjustedDebit = Math.Round(t.AdjustedDebit, 2),
            AdjustedCredit = Math.Round(t.AdjustedCredit, 2),
            IncomeStatementDebit = Math.Round(t.IncomeStatementDebit, 2),
            IncomeStatementCredit = Math.Round(t.IncomeStatementCredit, 2),
            FinancialPositionDebit = Math.Round(t.FinancialPositionDebit, 2),
            FinancialPositionCredit = Math.Round(t.FinancialPositionCredit, 2),
            IsTotalRow = true,
            CreatedAt = savedAt,
        });

        var old = await _db.Worksheet
            .Where(x => x.UserId == userId && x.PeriodId == periodId)
            .ToListAsync();

        _db.Worksheet.RemoveRange(old);
        await _db.Worksheet.AddRangeAsync(fresh);
        await _db.SaveChangesAsync();
    }

    private static string Truncate(string? value, int max)
    {
        value ??= string.Empty;
        return value.Length <= max ? value : value[..max];
    }
}

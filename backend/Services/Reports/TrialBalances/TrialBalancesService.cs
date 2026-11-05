using System.Globalization;
using System.Linq.Expressions;
using AumoBackend.Data;
using AumoBackend.DTOs.Reports;
using AumoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AumoBackend.Services.Reports.TrialBalances;

public class TrialBalancesService : ITrialBalancesService
{
    private const string Unadjusted = "unadjusted";
    private const string Adjusted = "adjusted";
    private const string PostClosing = "post-closing";

    private readonly AppDbContext _db;

    public TrialBalancesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TrialBalancesRow>> BuildTrialBalancesRowsAsync(
        Guid userId,
        Period period,
        bool includeAdjusting = false,
        string reportType = Unadjusted)
    {
        var type = NormalizeType(reportType);

        // Jika meminta post-closing tetapi periode belum ditutup, jangan hitung post-closing.
        var isPostClosingRequested = type == PostClosing && period != null && period.IsClosed;

        var rows = await ComputeRowsAsync(
            userId,
            period,
            includeAdjustingLines: includeAdjusting || type == Adjusted || isPostClosingRequested,
            permanentOnly: isPostClosingRequested);

        if (isPostClosingRequested)
        {
            await ApplyClosingToRetainedEarningsAsync(userId, period, rows);
        }

        return rows;
    }

    public async Task<decimal> ComputeRetainedEarningsEndingAsync(Guid userId, Period period)
    {
        var rows = await ComputeRowsAsync(userId, period, includeAdjustingLines: true, permanentOnly: false);

        var totalRevenue = rows
            .Where(r => !r.NormalBalanceIsDebit && IsTemporaryType(r.Type))
            .Sum(r => r.NetBalance);

        var totalExpense = rows
            .Where(r => r.NormalBalanceIsDebit && IsTemporaryType(r.Type))
            .Sum(r => r.NetBalance);

        var netIncome = totalRevenue - totalExpense;

        var reRow = rows.FirstOrDefault(r => IsRetainedEarnings(r.Role));
        var initialRe = reRow?.NetBalance ?? 0m;

        return initialRe + netIncome;
    }

    public async Task SaveSnapshotAsync(string reportType, Guid userId, Period period, IReadOnlyList<TrialBalancesRow> rows)
    {
        var type = NormalizeType(reportType);
        var periodId = period.Id;
        var lines = BuildSnapshotLines(rows);

        switch (type)
        {
            case Adjusted:
                await ReplaceSnapshotAsync(
                    _db.AdjustedTrialBalance,
                    x => x.UserId == userId && x.PeriodId == periodId,
                    lines.Select(l => new AdjustedTrialBalance
                    {
                        UserId = userId,
                        PeriodId = periodId,
                        AccountNumber = l.Number,
                        AccountName = l.Name,
                        DebitBalance = l.Debit,
                        CreditBalance = l.Credit,
                        IsTotalRow = l.IsTotal,
                    }));
                break;

            case PostClosing:
                await ReplaceSnapshotAsync(
                    _db.PostClosingTrialBalance,
                    x => x.UserId == userId && x.PeriodId == periodId,
                    lines.Select(l => new PostClosingTrialBalance
                    {
                        UserId = userId,
                        PeriodId = periodId,
                        AccountNumber = l.Number,
                        AccountName = l.Name,
                        DebitBalance = l.Debit,
                        CreditBalance = l.Credit,
                        IsTotalRow = l.IsTotal,
                    }));
                break;

            default:
                await ReplaceSnapshotAsync(
                    _db.UnadjustedTrialBalance,
                    x => x.UserId == userId && x.PeriodId == periodId,
                    lines.Select(l => new UnadjustedTrialBalance
                    {
                        UserId = userId,
                        PeriodId = periodId,
                        AccountNumber = l.Number,
                        AccountName = l.Name,
                        DebitBalance = l.Debit,
                        CreditBalance = l.Credit,
                        IsTotalRow = l.IsTotal,
                    }));
                break;
        }
    }

    public async Task<List<UnadjustedTrialBalanceRow>> BuildUnadjustedTrialBalanceAsync(Guid userId, Period period)
    {
        var rows = await BuildTrialBalancesRowsAsync(userId, period, false, Unadjusted);
        await SaveSnapshotAsync(Unadjusted, userId, period, rows);
        return ToSnapshotRows<UnadjustedTrialBalanceRow>(userId, period, rows);
    }

    public async Task<List<AdjustedTrialBalanceRow>> BuildAdjustedTrialBalanceAsync(Guid userId, Period period)
    {
        var rows = await BuildTrialBalancesRowsAsync(userId, period, true, Adjusted);
        await SaveSnapshotAsync(Adjusted, userId, period, rows);
        return ToSnapshotRows<AdjustedTrialBalanceRow>(userId, period, rows);
    }

    public async Task<List<PostClosingTrialBalanceRow>> BuildPostClosingTrialBalanceAsync(Guid userId, Period period)
    {
        // Proteksi Opsi 2: Mengembalikan list kosong jika periode belum ditutup
        if (period == null || !period.IsClosed)
        {
            return new List<PostClosingTrialBalanceRow>();
        }

        var rows = await BuildTrialBalancesRowsAsync(userId, period, true, PostClosing);
        await SaveSnapshotAsync(PostClosing, userId, period, rows);
        return ToSnapshotRows<PostClosingTrialBalanceRow>(userId, period, rows);
    }

    // ------------------------------------------------------------------
    // Perhitungan saldo dari jurnal umum
    // ------------------------------------------------------------------
    private async Task<List<TrialBalancesRow>> ComputeRowsAsync(
        Guid userId,
        Period period,
        bool includeAdjustingLines,
        bool permanentOnly)
    {
        var accounts = await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.IsActive && a.UserId == userId)
            .OrderBy(a => a.ReferenceNumber)
            .ToListAsync();

        var accountIds = accounts.Select(a => a.Id).ToList();

        var startUtc = DateTime.SpecifyKind(period.StartDate.Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(period.EndDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var linesQuery = _db.GeneralJournals
            .AsNoTracking()
            .Where(l => accountIds.Contains(l.AccountId)
                     && l.UserId == userId
                     && l.EntryDate >= startUtc
                     && l.EntryDate <= endUtc);

        var lines = includeAdjustingLines
            ? await linesQuery.Where(l => l.JournalType == "General" || l.JournalType == "Adjusting").ToListAsync()
            : await linesQuery.Where(l => l.JournalType == "General").ToListAsync();

        var linesByAccount = lines
            .GroupBy(l => l.AccountId)
            .ToDictionary(g => g.Key, g => (Debit: g.Sum(x => x.Debit), Credit: g.Sum(x => x.Credit)));

        var rows = new List<TrialBalancesRow>();
        foreach (var account in accounts)
        {
            var isPermanent = !string.IsNullOrEmpty(account.Type) && AccountClassificationHelper.IsPermanent(account.Type);
            if (permanentOnly && !isPermanent)
            {
                continue;
            }

            if (!linesByAccount.TryGetValue(account.Id, out var totals))
            {
                continue;
            }

            var normalDebit = !string.IsNullOrEmpty(account.Type) && AccountClassificationHelper.NormalBalanceIsDebit(account.Type);

            var netBalance = normalDebit
                ? totals.Debit - totals.Credit
                : totals.Credit - totals.Debit;

            var debit = totals.Debit >= totals.Credit ? totals.Debit - totals.Credit : 0m;
            var credit = totals.Debit < totals.Credit ? totals.Credit - totals.Debit : 0m;

            rows.Add(new TrialBalancesRow
            {
                AccountId = new Guid(account.Id, 0, 0, new byte[8]),
                ReferenceNumber = account.ReferenceNumber.ToString(CultureInfo.InvariantCulture),
                AccountName = account.AccountName,
                Type = account.Type,
                Role = account.Role,
                NormalBalanceIsDebit = normalDebit,
                NetBalance = netBalance,
                Debit = debit,
                Credit = credit,
            });
        }

        return rows;
    }

    // Pasca-penutupan: akun temporer sudah dikeluarkan; saldo Laba Ditahan harus memuat
    // efek Closing (saldo awal + laba bersih), kalau tidak debit dan kredit tidak seimbang.
    private async Task ApplyClosingToRetainedEarningsAsync(Guid userId, Period period, List<TrialBalancesRow> rows)
    {
        var endingRe = await ComputeRetainedEarningsEndingAsync(userId, period);

        var reRow = rows.FirstOrDefault(r => IsRetainedEarnings(r.Role));
        if (reRow == null)
        {
            if (endingRe == 0m)
            {
                return;
            }

            var account = await _db.ChartOfAccounts
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.IsActive && a.Role == "RetainedEarnings")
                .OrderBy(a => a.ReferenceNumber)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                return;
            }

            reRow = new TrialBalancesRow
            {
                AccountId = new Guid(account.Id, 0, 0, new byte[8]),
                ReferenceNumber = account.ReferenceNumber.ToString(CultureInfo.InvariantCulture),
                AccountName = account.AccountName,
                Type = account.Type,
                Role = account.Role,
                NormalBalanceIsDebit = false,
            };
            rows.Add(reRow);
            rows.Sort((a, b) => string.CompareOrdinal(a.ReferenceNumber.PadLeft(10, '0'), b.ReferenceNumber.PadLeft(10, '0')));
        }

        reRow.NetBalance = endingRe;
        reRow.Debit = endingRe < 0m ? -endingRe : 0m;
        reRow.Credit = endingRe > 0m ? endingRe : 0m;
    }

    // ------------------------------------------------------------------
    // Snapshot ke tiga tabel neraca saldo
    // ------------------------------------------------------------------
    private readonly record struct SnapshotLine(string Number, string Name, decimal Debit, decimal Credit, bool IsTotal);

    private static List<SnapshotLine> BuildSnapshotLines(IReadOnlyList<TrialBalancesRow> rows)
    {
        var lines = rows
            .Select(r => new SnapshotLine(
                Truncate(r.ReferenceNumber, 50),
                Truncate(r.AccountName, 255),
                Math.Round(r.Debit, 2),
                Math.Round(r.Credit, 2),
                false))
            .ToList();

        lines.Add(new SnapshotLine(
            string.Empty,
            "Total",
            Math.Round(rows.Sum(r => r.Debit), 2),
            Math.Round(rows.Sum(r => r.Credit), 2),
            true));

        return lines;
    }

    private static List<T> ToSnapshotRows<T>(Guid userId, Period period, IReadOnlyList<TrialBalancesRow> rows)
        where T : TrialBalancesRow, new()
    {
        var now = DateTimeOffset.UtcNow;

        return BuildSnapshotLines(rows)
            .Select(l => new T
            {
                UserId = userId,
                PeriodId = period.Id,
                AccountNumber = l.Number,
                AccountName = l.Name,
                DebitBalance = l.Debit,
                CreditBalance = l.Credit,
                IsTotalRow = l.IsTotal,
                CreatedAt = now,
            })
            .ToList();
    }

    private async Task ReplaceSnapshotAsync<T>(DbSet<T> set, Expression<Func<T, bool>> predicate, IEnumerable<T> fresh)
        where T : class
    {
        var old = await set.Where(predicate).ToListAsync();
        set.RemoveRange(old);
        await set.AddRangeAsync(fresh);
        await _db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------------
    private static string NormalizeType(string? reportType)
    {
        var type = (reportType ?? Unadjusted).Trim().ToLowerInvariant().Replace('_', '-');
        return type switch
        {
            Adjusted => Adjusted,
            PostClosing or "postclosing" => PostClosing,
            _ => Unadjusted,
        };
    }

    private static bool IsRetainedEarnings(string? role)
        => string.Equals(role, "RetainedEarnings", StringComparison.OrdinalIgnoreCase);

    private static bool IsTemporaryType(string? type)
        => !string.IsNullOrEmpty(type) && AccountClassificationHelper.IsTemporary(type);

    private static string Truncate(string? value, int max)
        {
        value ??= string.Empty;
        return value.Length <= max ? value : value[..max];
    }
}

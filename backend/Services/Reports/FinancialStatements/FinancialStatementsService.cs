namespace AumoBackend.Services.Reports.FinancialStatements;

using System.Globalization;
using AumoBackend.Data;
using AumoBackend.DTOs.Reports;
using AumoBackend.Models;
using AumoBackend.Services.Reports.Worksheet;
using Microsoft.EntityFrameworkCore;

public class FinancialStatementsService : IFinancialStatementsService
{
    private readonly AppDbContext _db;
    private readonly IWorksheetService _worksheetService;
    private readonly ILogger<FinancialStatementsService> _logger;

    public FinancialStatementsService(
        AppDbContext db,
        IWorksheetService worksheetService,
        ILogger<FinancialStatementsService> logger)
    {
        _db = db;
        _worksheetService = worksheetService;
        _logger = logger;
    }

    // Satu baris akun dari kolom "Neraca Saldo Disesuaikan" kertas kerja. NetBalance berada di sisi
    // normal akun (debit - kredit untuk akun bersaldo normal debit, sebaliknya untuk kredit).
    private sealed record AccountLine(
        int AccountId,
        string ReferenceNumber,
        string AccountName,
        string Type,
        string Role,
        decimal NetBalance);

    public async Task<IncomeStatementApiResponse> BuildIncomeStatementAsync(Guid userId, Period period)
    {
        var lines = await LoadLinesAsync(userId, period);
        return BuildIncomeStatement(lines, period);
    }

    public async Task<RetainedEarningsApiResponse> BuildRetainedEarningsAsync(Guid userId, Period period)
    {
        var lines = await LoadLinesAsync(userId, period);
        return BuildRetainedEarnings(lines, period);
    }

    public async Task<StatementOfCashFlowApiResponse> BuildCashFlowAsync(Guid userId, Period period)
    {
        var lines = await LoadLinesAsync(userId, period);
        var incomeStatement = BuildIncomeStatement(lines, period);

        decimal endingCash = lines.Where(r => r.Role == "CashAndEquivalents").Sum(r => r.NetBalance);

        var operating = new List<StatementOfCashFlowLineResponse>
        {
            new() { Description = "Net Income per Income Statement", Amount = incomeStatement.NetIncome },
        };
        var investing = new List<StatementOfCashFlowLineResponse>();
        var financing = new List<StatementOfCashFlowLineResponse>();

        foreach (var r in lines)
        {
            if (r.NetBalance == 0 || r.Role == "CashAndEquivalents" || r.Role == "RetainedEarnings")
            {
                continue;
            }

            int.TryParse(r.ReferenceNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var refNum);

            if (AccountClassificationHelper.IsTemporary(r.Type) || refNum >= 400)
            {
                continue;
            }

            if (r.Type == "Assets" || (refNum >= 100 && refNum <= 199))
            {
                bool isFixedAsset = refNum >= 150
                    || Contains(r.AccountName, "Equipment")
                    || Contains(r.AccountName, "Depreciation")
                    || Contains(r.AccountName, "Asset");

                if (isFixedAsset)
                {
                    investing.Add(new() { Description = $"Capital expenditure / Sale of {r.AccountName}", Amount = -r.NetBalance });
                }
                else
                {
                    operating.Add(new() { Description = $"Change in {r.AccountName}", Amount = -r.NetBalance });
                }
            }
            else if (r.Type == "Liabilities" || (refNum >= 200 && refNum <= 299))
            {
                bool isLongTermDebt = refNum >= 250
                    || Contains(r.AccountName, "Bank Loan")
                    || Contains(r.AccountName, "Long Term");

                if (isLongTermDebt)
                {
                    financing.Add(new() { Description = $"Change in {r.AccountName}", Amount = r.NetBalance });
                }
                else
                {
                    operating.Add(new() { Description = $"Change in {r.AccountName}", Amount = r.NetBalance });
                }
            }
            else if (r.Type == "Equity" || (refNum >= 300 && refNum <= 399))
            {
                financing.Add(new() { Description = $"Change in {r.AccountName}", Amount = r.NetBalance });
            }
            else
            {
                operating.Add(new() { Description = $"Adjustment for {r.AccountName}", Amount = -r.NetBalance });
            }
        }

        decimal netOperating = operating.Sum(a => a.Amount);
        decimal netInvesting = investing.Sum(a => a.Amount);
        decimal netFinancing = financing.Sum(a => a.Amount);
        decimal netChange = netOperating + netInvesting + netFinancing;

        return new StatementOfCashFlowApiResponse
        {
            OperatingActivities = operating,
            NetCashFromOperating = netOperating,
            InvestingActivities = investing,
            NetCashFromInvesting = netInvesting,
            FinancingActivities = financing,
            NetCashFromFinancing = netFinancing,
            NetChangeInCash = netChange,
            BeginningCash = endingCash - netChange,
            EndingCash = endingCash,
        };
    }

    public async Task<StatementOfFinancialPositionApiResponse> BuildSofpAsync(Guid userId, Period period, bool isPostClosing)
    {
        var lines = await LoadLinesAsync(userId, period);
        var re = BuildRetainedEarnings(lines, period);

        FinancialPositionLineApiResponse ToLine(AccountLine r) => new()
        {
            ReferenceNumber = r.ReferenceNumber,
            AccountName = r.AccountName,
            Amount = r.NetBalance,
        };

        var assets = lines.Where(r => r.Type == "Assets").Select(ToLine).ToList();
        var liabilities = lines.Where(r => r.Type == "Liabilities").Select(ToLine).ToList();
        var equityExcludingRe = lines.Where(r => r.Type == "Equity" && r.Role != "RetainedEarnings").Select(ToLine).ToList();

        decimal totalAssets = assets.Sum(a => a.Amount);
        decimal totalLiabilities = liabilities.Sum(l => l.Amount);
        decimal totalEquity = equityExcludingRe.Sum(e => e.Amount) + re.EndingBalance;
        decimal totalLiabilitiesAndEquity = totalLiabilities + totalEquity;

        return new StatementOfFinancialPositionApiResponse
        {
            PeriodName = period.PeriodName,
            AsOfDate = period.EndDate,
            IsPostClosing = isPostClosing,
            Assets = assets,
            TotalAssets = totalAssets,
            Liabilities = liabilities,
            TotalLiabilities = totalLiabilities,
            EquityExcludingRetainedEarnings = equityExcludingRe,
            RetainedEarningsEnding = re.EndingBalance,
            TotalEquity = totalEquity,
            TotalLiabilitiesAndEquity = totalLiabilitiesAndEquity,
            IsBalanced = Math.Round(totalAssets - totalLiabilitiesAndEquity, 2) == 0,
        };
    }

    // ------------------------------------------------------------------
    // Pembangun laporan dari baris kertas kerja
    // ------------------------------------------------------------------
    private static IncomeStatementApiResponse BuildIncomeStatement(List<AccountLine> lines, Period period)
    {
        IncomeStatementLineApiResponse ToLine(AccountLine r) => new()
        {
            ReferenceNumber = r.ReferenceNumber,
            AccountName = r.AccountName,
            Amount = r.NetBalance,
        };

        var revenues = lines.Where(r => r.Type == "OperatingIncome").Select(ToLine).ToList();
        var operatingExpenses = lines.Where(r => r.Type == "OperatingExpenses").Select(ToLine).ToList();
        var otherIncome = lines.Where(r => r.Type == "OtherIncome").Select(ToLine).ToList();
        var otherExpenses = lines.Where(r => r.Type == "OtherExpenses").Select(ToLine).ToList();

        decimal totalRevenue = revenues.Sum(r => r.Amount);
        decimal totalOperatingExpenses = operatingExpenses.Sum(e => e.Amount);
        decimal operatingIncome = totalRevenue - totalOperatingExpenses;
        decimal totalOtherIncome = otherIncome.Sum(i => i.Amount);
        decimal totalOtherExpenses = otherExpenses.Sum(e => e.Amount);

        return new IncomeStatementApiResponse
        {
            PeriodName = period.PeriodName,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            AsOfDate = period.EndDate,
            Revenues = revenues,
            TotalRevenue = totalRevenue,
            TotalRevenues = totalRevenue,
            OperatingExpenses = operatingExpenses,
            Expenses = operatingExpenses,
            TotalOperatingExpenses = totalOperatingExpenses,
            TotalExpenses = totalOperatingExpenses,
            OperatingIncome = operatingIncome,
            OtherIncome = otherIncome,
            TotalOtherIncome = totalOtherIncome,
            OtherExpenses = otherExpenses,
            TotalOtherExpenses = totalOtherExpenses,
            NetIncome = operatingIncome + totalOtherIncome - totalOtherExpenses,
        };
    }

    private static RetainedEarningsApiResponse BuildRetainedEarnings(List<AccountLine> lines, Period period)
    {
        var incomeStatement = BuildIncomeStatement(lines, period);
        var reAccount = lines.Find(r => r.Role == "RetainedEarnings");

        decimal beginningBalance = reAccount?.NetBalance ?? 0m;
        decimal dividends = 0m;

        return new RetainedEarningsApiResponse
        {
            PeriodName = period.PeriodName,
            AccountName = reAccount?.AccountName ?? "Retained Earnings",
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            BeginningBalance = beginningBalance,
            NetIncome = incomeStatement.NetIncome,
            Dividends = dividends,
            EndingBalance = beginningBalance + incomeStatement.NetIncome - dividends,
        };
    }

    // ------------------------------------------------------------------
    // Sumber data: tabel Worksheet
    // ------------------------------------------------------------------
    private async Task<List<AccountLine>> LoadLinesAsync(Guid userId, Period period)
    {
        // Role akun (RetainedEarnings, CashAndEquivalents) tidak disimpan di Worksheet; diambil dari ChartOfAccounts.
        var roles = (await _db.ChartOfAccounts
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .Select(a => new { a.Id, a.Role })
                .ToListAsync())
            .ToDictionary(a => a.Id, a => a.Role ?? "Default");

        List<AccountLine>? lines = null;

        try
        {
            var snapshot = await _db.Worksheet
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.PeriodId == period.Id)
                .ToListAsync();

            var total = snapshot.FirstOrDefault(x => x.IsTotalRow);
            if (total != null && !await HasChangesAfterAsync(userId, total.CreatedAt))
            {
                lines = snapshot
                    .Where(x => !x.IsTotalRow && x.AccountId.HasValue)
                    .Select(x => ToLine(
                        x.AccountId!.Value,
                        x.AccountNumber,
                        x.AccountName,
                        x.AccountType,
                        x.NormalBalanceIsDebit,
                        x.AdjustedDebit,
                        x.AdjustedCredit,
                        roles))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            // Tabel Worksheet belum dibuat di database: laporan tetap dihitung langsung.
            _logger.LogWarning(ex, "Tabel Worksheet tidak dapat dibaca; laporan keuangan dihitung langsung.");
        }

        lines ??= await RebuildAsync(userId, period, roles);

        return lines
            .OrderBy(r => int.TryParse(r.ReferenceNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : int.MaxValue)
            .ThenBy(r => r.ReferenceNumber, StringComparer.Ordinal)
            .ToList();
    }

    // Snapshot kosong atau basi: hitung kertas kerja periode terpilih, simpan ke tabel Worksheet,
    // lalu pakai hasilnya (tetap terpakai walaupun penyimpanan gagal).
    private async Task<List<AccountLine>> RebuildAsync(Guid userId, Period period, Dictionary<int, string> roles)
    {
        var result = await _worksheetService.GetWorksheetAsync(userId);
        if (!result.HasPeriodSelected)
        {
            return new List<AccountLine>();
        }

        if (result.PeriodId == period.Id)
        {
            try
            {
                await _worksheetService.SaveSnapshotAsync(userId, result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal menyimpan snapshot worksheet periode {PeriodId}.", period.Id);
            }
        }

        return result.Rows
            .Select(r => ToLine(
                r.AccountId,
                r.ReferenceNumber.ToString(CultureInfo.InvariantCulture),
                r.AccountName,
                r.Type,
                r.NormalBalanceIsDebit,
                r.AdjustedDebit,
                r.AdjustedCredit,
                roles))
            .ToList();
    }

    // Snapshot basi jika ada jurnal dibuat atau diubah sesudah snapshot disimpan. Jurnal yang dihapus dan
    // akun yang diubah tidak meninggalkan jejak waktu; keduanya membuang snapshot (WorksheetSnapshot.InvalidateAsync).
    private async Task<bool> HasChangesAfterAsync(Guid userId, DateTime? snapshotAt)
    {
        if (snapshotAt == null)
        {
            return true;
        }

        var since = snapshotAt.Value;
        return await _db.GeneralJournals
            .AsNoTracking()
            .AnyAsync(g => g.UserId == userId && (g.CreatedAt > since || (g.UpdatedAt != null && g.UpdatedAt > since)));
    }

    private static AccountLine ToLine(
        int accountId,
        string referenceNumber,
        string accountName,
        string type,
        bool normalBalanceIsDebit,
        decimal adjustedDebit,
        decimal adjustedCredit,
        Dictionary<int, string> roles)
    {
        var net = normalBalanceIsDebit ? adjustedDebit - adjustedCredit : adjustedCredit - adjustedDebit;
        roles.TryGetValue(accountId, out var role);
        return new AccountLine(accountId, referenceNumber ?? string.Empty, accountName ?? string.Empty, type ?? string.Empty, role ?? "Default", net);
    }

    private static bool Contains(string text, string value)
        => text.Contains(value, StringComparison.OrdinalIgnoreCase);
}

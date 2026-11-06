using System.Globalization;
using AumoBackend.DTOs.Reports;
using AumoBackend.ViewModels.Reports;

namespace AumoFinance.Services;

/// <summary>
/// Memetakan hasil service backend ke ViewModel tampilan. Hanya penyalinan field:
/// tidak ada perhitungan akuntansi di frontend.
/// </summary>
public static class ReportMapper
{
    public static IncomeStatementViewModel ToViewModel(IncomeStatementApiResponse r) => new()
    {
        AsOfDate = r.AsOfDate,
        Revenues = r.Revenues.Select(l => new IncomeStatementLine { ReferenceNumber = l.ReferenceNumber, AccountName = l.AccountName, Amount = l.Amount }).ToList(),
        OperatingExpenses = r.OperatingExpenses.Select(l => new IncomeStatementLine { ReferenceNumber = l.ReferenceNumber, AccountName = l.AccountName, Amount = l.Amount }).ToList(),
        OtherIncome = r.OtherIncome.Select(l => new IncomeStatementLine { ReferenceNumber = l.ReferenceNumber, AccountName = l.AccountName, Amount = l.Amount }).ToList(),
        OtherExpenses = r.OtherExpenses.Select(l => new IncomeStatementLine { ReferenceNumber = l.ReferenceNumber, AccountName = l.AccountName, Amount = l.Amount }).ToList(),
    };

    public static CashFlowStatementViewModel ToViewModel(StatementOfCashFlowApiResponse r) => new()
    {
        BeginningCash = r.BeginningCash,
        OperatingActivities = r.OperatingActivities.Select(l => new CashFlowLine { Description = l.Description, Amount = l.Amount }).ToList(),
        InvestingActivities = r.InvestingActivities.Select(l => new CashFlowLine { Description = l.Description, Amount = l.Amount }).ToList(),
        FinancingActivities = r.FinancingActivities.Select(l => new CashFlowLine { Description = l.Description, Amount = l.Amount }).ToList(),
    };

    public static StatementOfFinancialPositionViewModel ToViewModel(StatementOfFinancialPositionApiResponse r) => new()
    {
        AsOfDate = r.AsOfDate,
        IsPostClosing = r.IsPostClosing,
        Assets = Positions(r.Assets),
        Liabilities = Positions(r.Liabilities),
        EquityExcludingRetainedEarnings = Positions(r.EquityExcludingRetainedEarnings),
        RetainedEarningsEnding = r.RetainedEarningsEnding,
    };

    public static RetainedEarningsViewModel ToViewModel(RetainedEarningsApiResponse r) => new()
    {
        AccountName = string.IsNullOrWhiteSpace(r.AccountName) ? "Retained Earnings" : r.AccountName,
        StartDate = r.StartDate ?? DateTime.Today,
        EndDate = r.EndDate ?? DateTime.Today,
        BeginningBalance = r.BeginningBalance,
        NetIncome = r.NetIncome,
        Dividends = r.Dividends,
    };

    public static WorksheetViewModel ToViewModel(WorksheetResult r) => new()
    {
        NetIncome = r.Totals.NetIncome,
        Rows = r.Rows.Select(x => new WorksheetRow
        {
            AccountId = x.AccountId,
            ReferenceNumber = x.ReferenceNumber.ToString(CultureInfo.InvariantCulture),
            AccountName = x.AccountName,
            Type = x.Type,
            NormalBalanceIsDebit = x.NormalBalanceIsDebit,
            UnadjustedDebit = x.UnadjustedDebit,
            UnadjustedCredit = x.UnadjustedCredit,
            AdjustmentDebit = x.AdjustmentDebit,
            AdjustmentCredit = x.AdjustmentCredit,
            AdjustedDebit = x.AdjustedDebit,
            AdjustedCredit = x.AdjustedCredit,
            IncomeStatementDebit = x.IncomeStatementDebit,
            IncomeStatementCredit = x.IncomeStatementCredit,
            FinancialPositionDebit = x.FinancialPositionDebit,
            FinancialPositionCredit = x.FinancialPositionCredit,
        }).ToList(),
    };

    public static ClosingJournalViewModel ToViewModel(ClosingJournalReportResult r) => new()
    {
        NetIncome = r.NetIncome,
        RetainedEarningsAccountName = r.RetainedEarningsAccountName,
        Groups = r.Groups.Select(g => new ClosingJournalEntryGroup
        {
            Description = string.IsNullOrWhiteSpace(g.Description) ? g.Title : g.Description,
            Lines = g.Lines.Select(l => new ClosingJournalLine
            {
                ReferenceNumber = l.ReferenceNumber,
                AccountName = l.AccountName,
                Debit = l.Debit,
                Credit = l.Credit,
            }).ToList(),
        }).ToList(),
    };

    public static List<LedgerAccountViewModel> ToViewModels(IEnumerable<LedgerAccountResponse> accounts) =>
        accounts.Select(a => new LedgerAccountViewModel
        {
            AccountId = a.AccountId,
            ReferenceNumber = a.ReferenceNumber.ToString(CultureInfo.InvariantCulture),
            AccountName = a.AccountName,
            Type = string.IsNullOrWhiteSpace(a.Type) ? a.AccountType : a.Type,
            NormalBalanceIsDebit = a.NormalBalanceIsDebit,
            EndingBalance = a.EndingBalance,
            Lines = a.Lines.Select(l => new LedgerLineViewModel
            {
                EntryDate = DateTime.TryParse(l.EntryDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : default,
                Description = l.Description,
                Debit = l.Debit,
                Credit = l.Credit,
                RunningBalance = l.RunningBalance,
            }).ToList(),
        }).ToList();

    private static List<FinancialPositionLine> Positions(IEnumerable<FinancialPositionLineApiResponse> src) =>
        src.Select(l => new FinancialPositionLine { ReferenceNumber = l.ReferenceNumber, AccountName = l.AccountName, Balance = l.Amount, Amount = l.Amount }).ToList();
}

/// <summary>Format tampilan saja.</summary>
public static class ViewFormat
{
    /// <summary>Tanggal dari backend berformat yyyy-MM-dd; ditampilkan dd-MM-yyyy.</summary>
    public static string Date(string? isoDate)
        => DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
            : isoDate ?? string.Empty;
}

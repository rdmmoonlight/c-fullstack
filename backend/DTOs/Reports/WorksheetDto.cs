namespace AumoBackend.DTOs.Reports;

/// <summary>Satu baris kertas kerja (10 kolom): neraca saldo, penyesuaian, disesuaikan, laba rugi, posisi keuangan.</summary>
public class WorksheetRowApiResponse
{
    public int AccountId { get; set; }

    public int ReferenceNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool NormalBalanceIsDebit { get; set; }

    public decimal UnadjustedDebit { get; set; }

    public decimal UnadjustedCredit { get; set; }

    public decimal AdjustmentDebit { get; set; }

    public decimal AdjustmentCredit { get; set; }

    public decimal AdjustedDebit { get; set; }

    public decimal AdjustedCredit { get; set; }

    public decimal IncomeStatementDebit { get; set; }

    public decimal IncomeStatementCredit { get; set; }

    public decimal FinancialPositionDebit { get; set; }

    public decimal FinancialPositionCredit { get; set; }
}

public class WorksheetTotalsApiResponse
{
    public decimal UnadjustedDebit { get; set; }

    public decimal UnadjustedCredit { get; set; }

    public decimal AdjustmentDebit { get; set; }

    public decimal AdjustmentCredit { get; set; }

    public decimal AdjustedDebit { get; set; }

    public decimal AdjustedCredit { get; set; }

    public decimal IncomeStatementDebit { get; set; }

    public decimal IncomeStatementCredit { get; set; }

    public decimal FinancialPositionDebit { get; set; }

    public decimal FinancialPositionCredit { get; set; }

    /// <summary>Laba bersih = total kredit laba rugi dikurangi total debit laba rugi.</summary>
    public decimal NetIncome { get; set; }
}

public class WorksheetResult
{
    public bool HasPeriodSelected { get; set; }

    public int PeriodId { get; set; }

    public string? SelectedPeriodName { get; set; }

    public List<WorksheetRowApiResponse> Rows { get; set; } = new();

    public WorksheetTotalsApiResponse Totals { get; set; } = new();
}

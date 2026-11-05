namespace AumoBackend.DTOs.Reports;

public class TrialBalancesRow
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public int PeriodId { get; set; }

    // Properti Identifikasi & Klasifikasi Akun
    public Guid AccountId { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool NormalBalanceIsDebit { get; set; }

    // Properti Saldo
    public decimal NetBalance { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public decimal DebitBalance { get; set; }

    public decimal CreditBalance { get; set; }

    public bool IsTotalRow { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class UnadjustedTrialBalanceRow : TrialBalancesRow
{
}

public class AdjustedTrialBalanceRow : TrialBalancesRow
{
}

public class PostClosingTrialBalanceRow : TrialBalancesRow
{
}
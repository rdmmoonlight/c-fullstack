namespace AumoBackend.Models;

public partial class AdjustedTrialBalance
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public int PeriodId { get; set; }

    public string AccountNumber { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public decimal DebitBalance { get; set; }

    public decimal CreditBalance { get; set; }

    public bool IsTotalRow { get; set; }

    public DateTime? CreatedAt { get; set; }
}

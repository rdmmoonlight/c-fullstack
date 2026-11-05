namespace AumoBackend.Models;

/// <summary>
/// Kertas kerja (worksheet) akuntansi per periode: satu baris per akun ditambah satu baris total
/// (<see cref="IsTotalRow"/>). Kolom mengikuti tabel yang tampil di web dan mobile: Neraca Saldo,
/// Penyesuaian, Neraca Saldo Disesuaikan, Laba Rugi, dan Posisi Keuangan, masing-masing Debit/Kredit.
/// Isi tabel adalah snapshot yang dihitung dari jurnal; diganti utuh setiap periode dihitung ulang.
/// </summary>
public partial class Worksheet
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public int PeriodId { get; set; }

    /// <summary>Id akun pada ChartOfAccounts; null pada baris total.</summary>
    public int? AccountId { get; set; }

    public string AccountNumber { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public string AccountType { get; set; } = null!;

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

    public bool IsTotalRow { get; set; }

    public DateTime? CreatedAt { get; set; }
}

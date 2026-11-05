namespace AumoBackend.DTOs.Reports;

/// <summary>Hasil laporan jurnal (umum, penyesuaian, penutup) untuk periode terpilih.</summary>
public class JournalReportResult
{
    public bool HasPeriodSelected { get; set; }

    public string? SelectedPeriodName { get; set; }

    public bool IsPeriodClosed { get; set; }

    public List<JournalReportEntryDto> Entries { get; set; } = new();
}

public class JournalReportEntryDto
{
    public int Id { get; set; }

    public string TransactionNumber { get; set; } = string.Empty;

    public string JournalType { get; set; } = string.Empty;

    public string EntryDate { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<JournalReportLineDto> Lines { get; set; } = new();
}

public class JournalReportLineDto
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string ReferenceNumber { get; set; } = "0";

    public string? LineDescription { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public int LineOrder { get; set; }
}

/// <summary>
/// Hasil jurnal penutup. Tidak ada tabel fisik: seluruh isinya dihitung dari GeneralJournals
/// (General + Adjusting) periode terpilih melalui trial balance.
/// </summary>
public class ClosingJournalReportResult
{
    public bool HasPeriodSelected { get; set; }

    public string? SelectedPeriodName { get; set; }

    public bool IsPeriodClosed { get; set; }

    public decimal NetIncome { get; set; }

    public string RetainedEarningsAccountName { get; set; } = "Retained Earnings";

    public List<ClosingJournalEntryGroupApiResponse> Groups { get; set; } = new();
}

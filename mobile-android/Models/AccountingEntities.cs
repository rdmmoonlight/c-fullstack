namespace AumoFinance.Models;

public class Period : AccountingPeriod
{
    public Guid UserId { get; set; }
    public bool IsSelected { get; set; }
}

public class ChartOfAccount
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int ReferenceNumber { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class JournalEntry
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string JournalType { get; set; } = "General";
    public string ReferenceNumber { get; set; } = string.Empty;
    public bool NeedsClassification { get; set; }
    public string? Source { get; set; }
    public string? MobileNote { get; set; }
    public List<JournalEntryLine> Lines { get; set; } = new();
}

public class JournalEntryLine
{
    public int Id { get; set; }
    public int JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public int AccountId { get; set; }
    public ChartOfAccount? Account { get; set; }
    public string LineDescription { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public int LineOrder { get; set; }
}

// SelectedPeriod dihapus — tabel tidak ada di Neon.
// Seleksi periode kini memakai kolom Period.IsSelected (lihat SelectedPeriodHelper).
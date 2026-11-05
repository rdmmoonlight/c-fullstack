using System.Text.Json.Serialization;

namespace AumoBackend.DTOs.Reports;

// ==========================================
// RESPONSE DTOs
// ==========================================

public class GeneralJournalResponseDto
{
    public int Id { get; set; }

    [JsonIgnore]
    public int JournalEntryId
    {
        get => this.Id;
        set => this.Id = value;
    }

    public string JournalType { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; }

    public string TransactionNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsLocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<GeneralJournalLineDto> Lines { get; set; } = new();

    public decimal TotalDebit => this.Lines.Sum(l => l.Debit);

    public decimal TotalCredit => this.Lines.Sum(l => l.Credit);
}

public class CreateGeneralJournalResponseDto : GeneralJournalResponseDto
{
}

public class GeneralJournalLineDto
{
    public int Id { get; set; }

    [JsonIgnore]
    public int LineId
    {
        get => this.Id;
        set => this.Id = value;
    }

    public int AccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string ReferenceNumber { get; set; } = "0";

    public string? LineDescription { get; set; }

    [JsonIgnore]
    public string? Description
    {
        get => this.LineDescription;
        set => this.LineDescription = value;
    }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public int LineOrder { get; set; }
}

public class FlatGeneralJournalResponseDto
{
    public int LineId { get; set; }

    public int JournalEntryId { get; set; }

    public string JournalType { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; }

    public string TransactionNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int AccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string ReferenceNumber { get; set; } = "0";

    public string? LineDescription { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public int LineOrder { get; set; }
}

// ==========================================
// CLOSING JOURNAL DTOs
// ==========================================

public class ClosingJournalEntryGroupApiResponse
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<ClosingJournalLineApiResponse> Lines { get; set; } = new();

    public decimal TotalDebit => this.Lines.Sum(l => l.Debit);

    public decimal TotalCredit => this.Lines.Sum(l => l.Credit);
}

public class ClosingJournalLineApiResponse
{
    public int AccountId { get; set; }

    public string ReferenceNumber { get; set; } = "0";

    public string AccountName { get; set; } = string.Empty;

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }
}

// ==========================================
// REQUEST DTOs
// ==========================================

public class CreateGeneralJournalRequest
{
    public string JournalType { get; set; } = "General";

    public DateTime EntryDate { get; set; }

    public string TransactionNumber { get; set; } = string.Empty;

    public string Status { get; set; } = "Posted";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CreateGeneralJournalLineRequest> Lines { get; set; } = new();
}

public class CreateGeneralJournalLineRequest
{
    public int AccountId { get; set; }

    public string? LineDescription { get; set; }

    [JsonIgnore]
    public string? Description
    {
        get => this.LineDescription;
        set => this.LineDescription = value;
    }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public int LineOrder { get; set; }
}

public class UpdateGeneralJournalRequest
{
    public string JournalType { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; }

    public string TransactionNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CreateGeneralJournalLineRequest> Lines { get; set; } = new();
}
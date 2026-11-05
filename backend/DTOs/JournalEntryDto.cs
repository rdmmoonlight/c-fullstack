namespace AumoBackend.DTOs;

using System;

public class JournalEntryDto
{
    public DateTime Date { get; set; }

    public decimal TotalDebit { get; set; }

    public decimal TotalCredit { get; set; }
}

public class JournalEntryLineDto
{
    public int AccountId { get; set; }

    public string LineDescription { get; set; } = string.Empty;

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public int LineOrder { get; set; }
}

public class JournalEntryResponseDto
{
    public int Id { get; set; }

    public string TransactionNumber { get; set; } = string.Empty;

    public string JournalType { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsLocked { get; set; }

    public List<JournalEntryLineResponseDto> Lines { get; set; } = new();
}

public class JournalEntryLineResponseDto
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public string? LineDescription { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public int LineOrder { get; set; }
}

public class CreateJournalEntryRequest
{
    public string JournalType { get; set; } = "General";

    public DateTime EntryDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<JournalEntryLineDto> Lines { get; set; } = new();
}

public class CreateJournalEntryResponseDto
{
    public int EntryId { get; set; }

    public string TransactionNumber { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public class UpdateJournalEntryRequest
{
    public string JournalType { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<JournalEntryLineDto> Lines { get; set; } = new();
}

public class CreateJournalEntryLineRequest
{
    public int AccountId { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public string? LineDescription { get; set; }

    public int LineOrder { get; set; }
}

public class JournalEntryLineRequest
{
    public int AccountId { get; set; }

    public string LineDescription { get; set; } = string.Empty;

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public int LineOrder { get; set; }
}

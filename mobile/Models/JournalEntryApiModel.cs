using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AumoFinance.Models;

public class GeneralJournalEnvelopeModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("isPeriodClosed")]
    public bool IsPeriodClosed { get; set; }

    [JsonPropertyName("entries")]
    public List<JournalEntryApiModel> Entries { get; set; } = new();
}

public class JournalEntryApiModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("referenceNumber")]
    public string ReferenceNumber { get; set; } = string.Empty;

    [JsonPropertyName("journalType")]
    public string JournalType { get; set; } = string.Empty;

    [JsonPropertyName("entryDate")]
    public DateTime EntryDate { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lines")]
    public List<JournalEntryLineApiModel> Lines { get; set; } = new();
}

public class JournalEntryLineApiModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("lineDescription")]
    public string? LineDescription { get; set; }

    [JsonPropertyName("debit")]
    public decimal Debit { get; set; }

    [JsonPropertyName("credit")]
    public decimal Credit { get; set; }

    [JsonPropertyName("lineOrder")]
    public int LineOrder { get; set; }
}

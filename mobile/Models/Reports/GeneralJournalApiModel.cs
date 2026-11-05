using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AumoFinance.Models.Reports;

public class GeneralJournalReportApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hasPeriodSelected")]
    public bool HasPeriodSelected { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("isPeriodClosed")]
    public bool IsPeriodClosed { get; set; }

    [JsonPropertyName("entries")]
    public List<GeneralJournalEntryReportDto> Entries { get; set; } = new();
}

public class GeneralJournalEntryReportDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("transactionNumber")]
    public string? TransactionNumber { get; set; }

    [JsonPropertyName("entryDate")]
    public DateTime EntryDate { get; set; }

    // Timestamp asli pembuatan entry di server (tidak berubah walau entry
    // diedit) — dipakai untuk urutan jam:menit:detik yang sesungguhnya,
    // karena EntryDate cuma tanggal pilihan user dari DatePicker (jam selalu 00:00:00).
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    // Last time this entry was edited — null as long as it has never been edited.
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("journalType")]
    public string? JournalType { get; set; }

    [JsonPropertyName("lines")]
    public List<GeneralJournalLineReportDto> Lines { get; set; } = new();

    public decimal TotalDebit => Lines.Sum(l => l.Debit);
    public decimal TotalCredit => Lines.Sum(l => l.Credit);
}

public class GeneralJournalLineReportDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    [JsonPropertyName("accountName")]
    public string? AccountName { get; set; }

    [JsonPropertyName("referenceNumber")]
    public int? ReferenceNumber { get; set; } // REVISI: Dibuat nullable (int?) agar aman jika bernilai null

    [JsonPropertyName("lineDescription")]
    public string? LineDescription { get; set; }

    [JsonPropertyName("debit")]
    public decimal Debit { get; set; }

    [JsonPropertyName("credit")]
    public decimal Credit { get; set; }

    [JsonPropertyName("lineOrder")]
    public int LineOrder { get; set; }
}

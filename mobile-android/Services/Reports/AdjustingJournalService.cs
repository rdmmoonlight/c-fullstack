using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services.Reports;

public class AdjustingJournalService : BaseApiService
{
    // ==========================================
    // GET ADJUSTING JOURNAL REPORT
    // ==========================================
    public async Task<(AdjustingJournalReportApiResponse? data, string? errorDetail)> GetAdjustingJournalReportAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, "/api/mobile/reports/adjusting-journal");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<AdjustingJournalReportApiResponse>(content, JsonOptions);
                return (result, null);
            }

            var snippet = content.Length > 150 ? content[..150] : content;
            return (null, $"HTTP {(int)response.StatusCode} ({response.StatusCode}) — {snippet}");
        }
        catch (TaskCanceledException)
        {
            return (null, "Timeout — server tidak merespons dalam 15 detik (kemungkinan cold start Railway).");
        }
        catch (Exception ex)
        {
            return (null, $"{ex.GetType().Name}: {ex.Message}");
        }
    }
}

// ==========================================
// DTO / MODEL RESPONSE ADJUSTING JOURNAL
// ==========================================
public class AdjustingJournalReportApiResponse
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
    public List<AdjustingJournalEntryDto> Entries { get; set; } = new();
}

public class AdjustingJournalEntryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("transactionNumber")]
    public string? TransactionNumber { get; set; }

    [JsonPropertyName("entryDate")]
    public DateTime EntryDate { get; set; }

    // Timestamp asli pembuatan entry di server (tidak berubah walau entry
    // diedit) — dipakai untuk tampilan tanggal+jam input yang sesungguhnya,
    // karena EntryDate cuma tanggal pilihan user dari DatePicker (jam selalu 00:00:00).
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    // Last time this entry was edited — null as long as it has never been edited.
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("journalType")]
    public string? JournalType { get; set; }

    [JsonPropertyName("lines")]
    public List<AdjustingJournalLineDto> Lines { get; set; } = new();

    public decimal TotalDebit => Lines.Sum(l => l.Debit);
    public decimal TotalCredit => Lines.Sum(l => l.Credit);
}

public class AdjustingJournalLineDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    [JsonPropertyName("accountName")]
    public string? AccountName { get; set; }

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("lineDescription")]
    public string? LineDescription { get; set; }

    [JsonPropertyName("debit")]
    public decimal Debit { get; set; }

    [JsonPropertyName("credit")]
    public decimal Credit { get; set; }
}

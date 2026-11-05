using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services.Reports;

public class GeneralLedgerService : BaseApiService
{
    // ==========================================
    // GET GENERAL LEDGER REPORT
    // isTemporary: false -> Akun Riil (Assets, Liabilities, Equity)
    // isTemporary: true  -> Akun Nominal (Revenue, Expenses)
    // ==========================================
    public async Task<(GeneralLedgerReportApiResponse? data, string? errorDetail)> GetGeneralLedgerReportAsync(bool isTemporary = false)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            string requestUri = $"/api/mobile/reports/general-ledger?isTemporary={isTemporary.ToString().ToLower()}";

            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUri);
            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<GeneralLedgerReportApiResponse>(content, JsonOptions);
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
// DTO / MODEL RESPONSE GENERAL LEDGER
// ==========================================
public class GeneralLedgerReportApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hasPeriodSelected")]
    public bool HasPeriodSelected { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("isTemporary")]
    public bool IsTemporary { get; set; }

    // Backend (GeneralLedgerControllers.GetGeneralLedger) returns the account list under
    // the "ledgers" key, not "accounts".
    [JsonPropertyName("ledgers")]
    public List<GeneralLedgerAccountDto> Accounts { get; set; } = new();
}

public class GeneralLedgerAccountDto
{
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    // Account classification, e.g. Asset/Liability/Equity/Revenue/Expense — shown as the chip.
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    // Backend sends a bool, not the "Debit"/"Credit" string this DTO previously expected.
    [JsonPropertyName("normalBalanceIsDebit")]
    public bool NormalBalanceIsDebit { get; set; }

    // Backend (LedgerAccountApiResponse.Lines) returns the line list under "lines", not "entries".
    [JsonPropertyName("lines")]
    public List<GeneralLedgerEntryDto> Entries { get; set; } = new();

    [JsonPropertyName("endingBalance")]
    public decimal EndingBalance { get; set; }
}

public class GeneralLedgerEntryDto
{
    [JsonPropertyName("entryDate")]
    public DateTime EntryDate { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("debit")]
    public decimal Debit { get; set; }

    [JsonPropertyName("credit")]
    public decimal Credit { get; set; }

    [JsonPropertyName("runningBalance")]
    public decimal RunningBalance { get; set; }
}

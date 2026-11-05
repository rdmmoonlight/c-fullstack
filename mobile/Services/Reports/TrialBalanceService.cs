using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services.Reports;

public class TrialBalanceService : BaseApiService
{
    // =========================================================================
    // GET TRIAL BALANCE REPORT (Unadjusted, Adjusted, & Post-Closing)
    // reportType: "unadjusted" | "adjusted" | "post-closing"
    // =========================================================================
    public async Task<(TrialBalanceReportApiResponse? data, string? errorDetail)> GetTrialBalanceReportAsync(string reportType = "unadjusted")
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            string requestUri = $"/api/mobile/reports/trial-balance?type={Uri.EscapeDataString(reportType)}";

            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUri);
            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TrialBalanceReportApiResponse>(content, JsonOptions);
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
// DTO / MODEL RESPONSE ALL TRIAL BALANCE
// ==========================================
public class TrialBalanceReportApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hasPeriodSelected")]
    public bool HasPeriodSelected { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("reportTitle")]
    public string ReportTitle { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("totalDebit")]
    public decimal TotalDebit { get; set; }

    [JsonPropertyName("totalCredit")]
    public decimal TotalCredit { get; set; }

    [JsonPropertyName("isBalanced")]
    public bool IsBalanced { get; set; }

    [JsonPropertyName("rows")]
    public List<TrialBalanceRowDto> Rows { get; set; } = new();
}

public class TrialBalanceRowDto
{
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("debit")]
    public decimal Debit { get; set; }

    [JsonPropertyName("credit")]
    public decimal Credit { get; set; }

    [JsonPropertyName("netBalance")]
    public decimal NetBalance { get; set; }

    [JsonPropertyName("normalBalanceIsDebit")]
    public bool NormalBalanceIsDebit { get; set; }
}

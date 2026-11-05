using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services.Reports;

public class RetainedEarningsService : BaseApiService
{
    // ==========================================
    // GET RETAINED EARNINGS REPORT
    // ==========================================
    public async Task<(RetainedEarningsReportApiResponse? data, string? errorDetail)> GetRetainedEarningsReportAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, "/api/mobile/reports/retained-earnings");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<RetainedEarningsReportApiResponse>(content, JsonOptions);
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
// DTO / MODEL RESPONSE RETAINED EARNINGS
// ==========================================
public class RetainedEarningsReportApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hasPeriodSelected")]
    public bool HasPeriodSelected { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("beginningRetainedEarnings")]
    public decimal BeginningRetainedEarnings { get; set; }

    [JsonPropertyName("netIncome")]
    public decimal NetIncome { get; set; }

    [JsonPropertyName("dividendsOrDraws")]
    public decimal DividendsOrDraws { get; set; }

    [JsonPropertyName("endingRetainedEarnings")]
    public decimal EndingRetainedEarnings { get; set; }
}

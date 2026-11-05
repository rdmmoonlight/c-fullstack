using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services.Reports;

public class StatementOfCashFlowsService : BaseApiService
{
    // ==========================================
    // GET STATEMENT OF CASH FLOWS REPORT
    // ==========================================
    public async Task<(StatementOfCashFlowsReportApiResponse? data, string? errorDetail)> GetStatementOfCashFlowsReportAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            // Updated endpoint to match backend route
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, "/api/mobile/reports/cash-flow");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<StatementOfCashFlowsReportApiResponse>(content, JsonOptions);
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
// DTO / MODEL RESPONSE STATEMENT OF CASH FLOWS
// ==========================================
public class StatementOfCashFlowsReportApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hasPeriodSelected")]
    public bool HasPeriodSelected { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    // 1. Operating Activities
    [JsonPropertyName("operatingActivities")]
    public List<CashFlowItemDto> OperatingActivities { get; set; } = new();

    [JsonPropertyName("netCashFromOperating")]
    public decimal NetCashFromOperating { get; set; }

    // 2. Investing Activities
    [JsonPropertyName("investingActivities")]
    public List<CashFlowItemDto> InvestingActivities { get; set; } = new();

    [JsonPropertyName("netCashFromInvesting")]
    public decimal NetCashFromInvesting { get; set; }

    // 3. Financing Activities
    [JsonPropertyName("financingActivities")]
    public List<CashFlowItemDto> FinancingActivities { get; set; } = new();

    [JsonPropertyName("netCashFromFinancing")]
    public decimal NetCashFromFinancing { get; set; }

    // Summary Reconciliation
    [JsonPropertyName("netChangeInCash")]
    public decimal NetChangeInCash { get; set; }

    [JsonPropertyName("beginningCash")]
    public decimal BeginningCash { get; set; }

    [JsonPropertyName("endingCash")]
    public decimal EndingCash { get; set; }
}

public class CashFlowItemDto
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

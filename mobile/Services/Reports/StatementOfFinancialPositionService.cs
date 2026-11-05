using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services.Reports;

public class StatementOfFinancialPositionService : BaseApiService
{
    // ==========================================
    // GET STATEMENT OF FINANCIAL POSITION (BALANCE SHEET) REPORT
    // ==========================================
    public async Task<(StatementOfFinancialPositionReportApiResponse? data, string? errorDetail)> GetStatementOfFinancialPositionReportAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, "/api/mobile/reports/statement-of-financial-position");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<StatementOfFinancialPositionReportApiResponse>(content, JsonOptions);
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
// DTO / MODEL RESPONSE STATEMENT OF FINANCIAL POSITION
// ==========================================
public class StatementOfFinancialPositionReportApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hasPeriodSelected")]
    public bool HasPeriodSelected { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("assetAccounts")]
    public List<FinancialPositionAccountDto> AssetAccounts { get; set; } = new();

    [JsonPropertyName("totalAssets")]
    public decimal TotalAssets { get; set; }

    [JsonPropertyName("liabilityAccounts")]
    public List<FinancialPositionAccountDto> LiabilityAccounts { get; set; } = new();

    [JsonPropertyName("totalLiabilities")]
    public decimal TotalLiabilities { get; set; }

    [JsonPropertyName("equityAccounts")]
    public List<FinancialPositionAccountDto> EquityAccounts { get; set; } = new();

    [JsonPropertyName("totalEquity")]
    public decimal TotalEquity { get; set; }

    [JsonPropertyName("totalLiabilitiesAndEquity")]
    public decimal TotalLiabilitiesAndEquity { get; set; }

    [JsonPropertyName("isBalanced")]
    public bool IsBalanced { get; set; }
}

public class FinancialPositionAccountDto
{
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

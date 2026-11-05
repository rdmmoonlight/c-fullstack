using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services.Reports;

public class IncomeStatementService : BaseApiService
{
    // ==========================================
    // GET INCOME STATEMENT REPORT
    // ==========================================
    public async Task<(IncomeStatementReportApiResponse? data, string? errorDetail)> GetIncomeStatementReportAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, "/api/mobile/reports/income-statement");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<IncomeStatementReportApiResponse>(content, JsonOptions);
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
// DTO / MODEL RESPONSE INCOME STATEMENT
// ==========================================
public class IncomeStatementReportApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hasPeriodSelected")]
    public bool HasPeriodSelected { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("revenueAccounts")]
    public List<IncomeStatementAccountDto> RevenueAccounts { get; set; } = new();

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("expenseAccounts")]
    public List<IncomeStatementAccountDto> ExpenseAccounts { get; set; } = new();

    [JsonPropertyName("totalExpenses")]
    public decimal TotalExpenses { get; set; }

    [JsonPropertyName("operatingIncome")]
    public decimal OperatingIncome { get; set; }

    [JsonPropertyName("otherIncomeAccounts")]
    public List<IncomeStatementAccountDto> OtherIncomeAccounts { get; set; } = new();

    [JsonPropertyName("otherExpenseAccounts")]
    public List<IncomeStatementAccountDto> OtherExpenseAccounts { get; set; } = new();

    [JsonPropertyName("totalOtherIncome")]
    public decimal TotalOtherIncome { get; set; }

    [JsonPropertyName("totalOtherExpenses")]
    public decimal TotalOtherExpenses { get; set; }

    [JsonPropertyName("netIncome")]
    public decimal NetIncome { get; set; }
}

public class IncomeStatementAccountDto
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

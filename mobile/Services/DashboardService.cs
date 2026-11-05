using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services;

public class DashboardService : BaseApiService
{
    // ==========================================
    // GET DASHBOARD DATA
    // ==========================================
    public async Task<(DashboardApiResponse? data, string? errorDetail)> GetDashboardAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, "/api/mobile/dashboard");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<DashboardApiResponse>(content, JsonOptions);
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
// DTO / MODEL RESPONSE DASHBOARD
// ==========================================
public class DashboardApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hasPeriodSelected")]
    public bool HasPeriodSelected { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("isPeriodClosed")]
    public bool IsPeriodClosed { get; set; }

    [JsonPropertyName("totalAssets")]
    public decimal TotalAssets { get; set; }

    [JsonPropertyName("totalLiabilities")]
    public decimal TotalLiabilities { get; set; }

    [JsonPropertyName("totalEquity")]
    public decimal TotalEquity { get; set; }

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalExpenses")]
    public decimal TotalExpenses { get; set; }

    [JsonPropertyName("netIncome")]
    public decimal NetIncome { get; set; }

    [JsonPropertyName("cashAccounts")]
    public List<CashAndBankAccountDto> CashAccounts { get; set; } = new();

    [JsonPropertyName("totalCashOnHand")]
    public decimal TotalCashOnHand { get; set; }

    [JsonPropertyName("bankAccounts")]
    public List<CashAndBankAccountDto> BankAccounts { get; set; } = new();

    [JsonPropertyName("totalBankBalance")]
    public decimal TotalBankBalance { get; set; }

    [JsonPropertyName("recentEntries")]
    public List<RecentEntryDto> RecentEntries { get; set; } = new();
}

public class CashAndBankAccountDto
{
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string? AccountName { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }
}

public class RecentEntryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("referenceNumber")]
    public string? ReferenceNumber { get; set; }

    [JsonPropertyName("entryDate")]
    public DateTime EntryDate { get; set; }

    [JsonPropertyName("journalType")]
    public string? JournalType { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("lines")]
    public List<RecentEntryLineDto> Lines { get; set; } = new();
}

public class RecentEntryLineDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

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

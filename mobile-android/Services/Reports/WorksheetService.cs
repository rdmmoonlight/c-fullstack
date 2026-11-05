using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services.Reports;

public class WorksheetService : BaseApiService
{
    // ==========================================
    // GET WORKSHEET (10-COLUMN SHEET) REPORT
    // ==========================================
    public async Task<(WorksheetReportApiResponse? data, string? errorDetail)> GetWorksheetReportAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, "/api/mobile/reports/worksheet");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<WorksheetReportApiResponse>(content, JsonOptions);
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
// DTO / MODEL RESPONSE WORKSHEET
// ==========================================
public class WorksheetReportApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hasPeriodSelected")]
    public bool HasPeriodSelected { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("rows")]
    public List<WorksheetRowDto> Rows { get; set; } = new();

    [JsonPropertyName("totals")]
    public WorksheetTotalsDto Totals { get; set; } = new();
}

public class WorksheetRowDto
{
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    // Unadjusted Trial Balance
    [JsonPropertyName("tbDebit")]
    public decimal TbDebit { get; set; }

    [JsonPropertyName("tbCredit")]
    public decimal TbCredit { get; set; }

    // Adjustments (AJE)
    [JsonPropertyName("adjDebit")]
    public decimal AdjDebit { get; set; }

    [JsonPropertyName("adjCredit")]
    public decimal AdjCredit { get; set; }

    // Adjusted Trial Balance
    [JsonPropertyName("adjTbDebit")]
    public decimal AdjTbDebit { get; set; }

    [JsonPropertyName("adjTbCredit")]
    public decimal AdjTbCredit { get; set; }

    // Income Statement
    [JsonPropertyName("isDebit")]
    public decimal IsDebit { get; set; }

    [JsonPropertyName("isCredit")]
    public decimal IsCredit { get; set; }

    // Balance Sheet / SOFP
    [JsonPropertyName("bsDebit")]
    public decimal BsDebit { get; set; }

    [JsonPropertyName("bsCredit")]
    public decimal BsCredit { get; set; }
}

public class WorksheetTotalsDto
{
    [JsonPropertyName("tbDebit")]
    public decimal TbDebit { get; set; }

    [JsonPropertyName("tbCredit")]
    public decimal TbCredit { get; set; }

    [JsonPropertyName("adjDebit")]
    public decimal AdjDebit { get; set; }

    [JsonPropertyName("adjCredit")]
    public decimal AdjCredit { get; set; }

    [JsonPropertyName("adjTbDebit")]
    public decimal AdjTbDebit { get; set; }

    [JsonPropertyName("adjTbCredit")]
    public decimal AdjTbCredit { get; set; }

    [JsonPropertyName("isDebit")]
    public decimal IsDebit { get; set; }

    [JsonPropertyName("isCredit")]
    public decimal IsCredit { get; set; }

    [JsonPropertyName("bsDebit")]
    public decimal BsDebit { get; set; }

    [JsonPropertyName("bsCredit")]
    public decimal BsCredit { get; set; }

    [JsonPropertyName("netIncome")]
    public decimal NetIncome { get; set; }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AumoFinance.Models;

namespace AumoFinance.Services;

public class PeriodService : BaseApiService
{
    private const string BaseEndpoint = "/api/mobile/periods";

    public async Task<(List<PeriodApiModel>? periods, string? selectedPeriodId, string? errorDetail)> GetPeriodsAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, BaseEndpoint);

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<GetPeriodsApiResponse>(content, JsonOptions);
                return (result?.Periods ?? new(), result?.SelectedPeriodId?.ToString(), null);
            }

            var snippet = content.Length > 150 ? content[..150] : content;
            return (null, null, $"HTTP {(int)response.StatusCode} — {snippet}");
        }
        catch (Exception ex)
        {
            return (null, null, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> SelectPeriodAsync(string periodId)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, $"{BaseEndpoint}/select/{periodId}");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return (true, ExtractMessage(content, response.StatusCode, "Period selected successfully."));
            }

            return (false, ExtractMessage(content, response.StatusCode, "Failed to select period."));
        }
        catch (Exception ex)
        {
            return (false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> ClosePeriodAsync(int id)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, $"{BaseEndpoint}/close/{id}");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return (true, ExtractMessage(content, response.StatusCode, "Period closed successfully."));
            }

            return (false, ExtractMessage(content, response.StatusCode, "Failed to close period."));
        }
        catch (Exception ex)
        {
            return (false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> CreatePeriodAsync(string name, DateTime startDate, DateTime endDate)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, $"{BaseEndpoint}/create");

            var dto = new CreatePeriodRequest { PeriodName = name, StartDate = startDate, EndDate = endDate };
            var jsonBody = JsonSerializer.Serialize(dto, JsonOptions);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return (true, ExtractMessage(content, response.StatusCode, "Period created successfully."));
            }

            return (false, ExtractMessage(content, response.StatusCode, "Failed to create period."));
        }
        catch (Exception ex)
        {
            return (false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Membaca pesan dari body JSON secara aman. Jika body kosong atau bukan JSON valid
    /// (mis. cold-start server, 401 kosong dari token kedaluwarsa, atau error gateway),
    /// kembalikan pesan fallback beserta status code alih-alih melempar JsonException.
    /// </summary>
    private static string ExtractMessage(string content, HttpStatusCode statusCode, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<BasicPeriodResponse>(content, JsonOptions);
                if (!string.IsNullOrWhiteSpace(parsed?.Message))
                {
                    return parsed.Message;
                }
            }
            catch (JsonException)
            {
                // Body bukan JSON valid — abaikan dan pakai fallback di bawah.
            }
        }

        return statusCode == HttpStatusCode.Unauthorized
            ? "Your session has expired. Please log in again."
            : $"{fallback} (HTTP {(int)statusCode})";
    }
}

public class GetPeriodsApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("selectedPeriodId")]
    public int? SelectedPeriodId { get; set; }

    [JsonPropertyName("periods")]
    public List<PeriodApiModel> Periods { get; set; } = new();
}

public class BasicPeriodResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class CreatePeriodRequest
{
    [JsonPropertyName("periodName")]
    public string PeriodName { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }
}

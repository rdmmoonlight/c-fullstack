using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AumoFinance.Services;

public class CoaService : BaseApiService
{
    // ==========================================
    // GET ALL ACCOUNTS (CHART OF ACCOUNTS)
    // ==========================================
    public async Task<(List<CoaAccountDto> accounts, string? errorDetail)> GetAccountsAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, "/api/mobile/chart-of-accounts");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CoaListApiResponse>(content, JsonOptions);
                return (result?.Accounts ?? new(), null);
            }

            var snippet = content.Length > 150 ? content[..150] : content;
            return (new(), $"HTTP {(int)response.StatusCode} ({response.StatusCode}) — {snippet}");
        }
        catch (TaskCanceledException)
        {
            return (new(), "Timeout — server tidak merespons dalam 15 detik.");
        }
        catch (Exception ex)
        {
            return (new(), $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    // ==========================================
    // CREATE NEW ACCOUNT
    // ==========================================
    public async Task<(bool success, string message)> CreateAccountAsync(CreateAccountDto dto)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, "/api/mobile/chart-of-accounts");

            var jsonBody = JsonSerializer.Serialize(dto, JsonOptions);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Account created successfully.");
            }

            return (false, $"Failed to create account: {content}");
        }
        catch (Exception ex)
        {
            return (false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    // ==========================================
    // UPDATE ACCOUNT
    // ==========================================
    public async Task<(bool success, string message)> UpdateAccountAsync(int id, UpdateAccountDto dto)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Put, $"/api/mobile/chart-of-accounts/{id}");

            var jsonBody = JsonSerializer.Serialize(dto, JsonOptions);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Account updated successfully.");
            }

            return (false, $"Failed to update account: {content}");
        }
        catch (Exception ex)
        {
            return (false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    // ==========================================
    // DELETE ACCOUNT
    // ==========================================
    public async Task<(bool success, string message)> DeleteAccountAsync(int id)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Delete, $"/api/mobile/chart-of-accounts/{id}");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Account deleted successfully.");
            }

            return (false, $"Failed to delete account: {content}");
        }
        catch (Exception ex)
        {
            return (false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }
}

// ==========================================
// DTOs / MODELS COA
// ==========================================
public class CoaListApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("accounts")]
    public List<CoaAccountDto> Accounts { get; set; } = new();
}

public class CoaAccountDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("balance")]
    public decimal CurrentBalance { get; set; }
}

public class CreateAccountDto
{
    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public class UpdateAccountDto
{
    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace AumoFinance.Services;

public class AuthService : BaseApiService
{
    // ==========================================
    // 1. LOGIN
    // ==========================================
    public async Task<(bool success, string message, string? userId, string? fullName)> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Email dan password wajib diisi.", null, null);
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var payload = new { email = email.Trim(), password = password };

            var response = await HttpClient.PostAsJsonAsync("/api/mobile/auth/login", payload, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            var result = JsonSerializer.Deserialize<LoginApiResponse>(content, JsonOptions);

            if (response.IsSuccessStatusCode && result != null && result.Success)
            {
                if (!string.IsNullOrEmpty(result.Token))
                {
                    await SecureStorage.Default.SetAsync(AuthTokenKey, result.Token);
                }
                return (true, result.Message ?? "Login berhasil.", result.UserId, result.FullName);
            }

            return (false, result?.Message ?? "Email atau password salah.", null, null);
        }
        catch (TaskCanceledException)
        {
            return (false, "Koneksi RTO (Timeout). Periksa koneksi internet Anda.", null, null);
        }
        catch (Exception ex)
        {
            return (false, $"Gagal terhubung ke server: {ex.Message}", null, null);
        }
    }

    // ==========================================
    // 2. GET CURRENT USER (GET ME)
    // ==========================================
    public async Task<(bool isAuthenticated, string? userId, string? email, string? fullName, string? errorDetail)> GetMeAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, "/api/mobile/auth/me");

            using var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<GetMeApiResponse>(content, JsonOptions);
                if (result != null && result.Success)
                {
                    return (true, result.UserId, result.Email, result.FullName, null);
                }
            }

            return (false, null, null, null, $"HTTP {(int)response.StatusCode} ({response.StatusCode})");
        }
        catch (TaskCanceledException)
        {
            return (false, null, null, null, "Timeout saat memverifikasi sesi login.");
        }
        catch (Exception ex)
        {
            return (false, null, null, null, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    // ==========================================
    // 3. LOGOUT
    // ==========================================
    public async Task LogoutAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, "/api/mobile/auth/logout");
            await HttpClient.SendAsync(request, cts.Token);
        }
        catch
        {
            // Abaikan error koneksi saat logout
        }
        finally
        {
            SecureStorage.Default.Remove(AuthTokenKey);
        }
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var token = await SecureStorage.Default.GetAsync(AuthTokenKey);
        return !string.IsNullOrEmpty(token);
    }

    private class LoginApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
    }

    private class GetMeApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
    }
}

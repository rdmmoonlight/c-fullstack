using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace AumoFinance.Services;

public abstract class BaseApiService
{
    // REVISI: Menggunakan host Render
    public const string BaseUrl = "https://aumo.onrender.com";
    protected const string AuthTokenKey = "auth_token_jwt";

    protected static readonly HttpClient HttpClient = new HttpClient
    {
        BaseAddress = new Uri(BaseUrl),
        Timeout = TimeSpan.FromSeconds(45) // REVISI: Waktu tunggu 45 detik
    };

    protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        // REVISI: Mengizinkan pembacaan angka jika sewaktu-waktu dikirim dalam bentuk string
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    protected async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        var token = await SecureStorage.Default.GetAsync(AuthTokenKey);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    /// <summary>
    /// Mengekstrak pesan error dari body respons non-sukses tanpa pernah
    /// melempar JsonException. Server bisa saja mengirim body kosong (mis.
    /// unhandled exception 500 sebelum ada exception-handler middleware,
    /// atau timeout proxy) — mem-parse body itu langsung sebagai JSON akan
    /// crash dengan pesan yang tidak jelas ("ExpectedJsonTokens..."). Selalu
    /// pakai method ini, jangan Deserialize&lt;BasicApiResponse&gt; langsung.
    /// </summary>
    protected static string ExtractErrorMessage(System.Net.HttpStatusCode statusCode, string? content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                var err = JsonSerializer.Deserialize<BasicApiResponse>(content, JsonOptions);
                if (!string.IsNullOrEmpty(err?.Message))
                {
                    return err.Message;
                }
            }
            catch (JsonException)
            {
                // Body bukan JSON valid — abaikan, jatuh ke pesan generik di bawah.
            }
        }

        return $"HTTP {(int)statusCode}";
    }
}

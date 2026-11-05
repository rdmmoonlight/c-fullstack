// <copyright file="HomeService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Home;

using System.Globalization;
using System.Text.Json;
using AumoBackend.DTOs.Home;

public class HomeService : IHomeService
{
    private readonly IHttpClientFactory httpClientFactory;

    // Fix: Inject IHttpClientFactory lewat constructor
    public HomeService(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<List<MarketIndicatorDto>> GetMarketIndicatorsAsync()
    {
        var client = this.httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        var usdTask = FetchUsdRateAsync(client);
        var ihsgTask = FetchIhsgAsync(client);

        await Task.WhenAll(usdTask, ihsgTask);

        var usdResult = await usdTask;
        var ihsgResult = await ihsgTask;

        var resultData = new List<MarketIndicatorDto>();

        if (usdResult != null)
        {
            resultData.Add(usdResult);
        }

        if (ihsgResult != null)
        {
            resultData.Add(ihsgResult);
        }

        return resultData;
    }

    private static async Task<MarketIndicatorDto?> FetchUsdRateAsync(HttpClient client)
    {
        foreach (var host in new[] { "query1", "query2" })
        {
            var yahooUrl = $"https://{host}.finance.yahoo.com/v8/finance/chart/IDR=X";
            var yahooResult = await FetchYahooChartDataAsync(client, yahooUrl, "USD/IDR", "Dolar AS / Rupiah");
            if (yahooResult != null)
            {
                return yahooResult;
            }
        }

        try
        {
            const string fallbackUrl = "https://open.er-api.com/v6/latest/USD";
            var response = await client.GetAsync(fallbackUrl);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var jsonDoc = await JsonDocument.ParseAsync(stream);

            if (jsonDoc.RootElement.TryGetProperty("rates", out var rates) &&
                rates.TryGetProperty("IDR", out var idrElement))
            {
                decimal rate = idrElement.GetDecimal();
                return new MarketIndicatorDto
                {
                    Symbol = "USD/IDR",
                    Name = "Dolar AS / Rupiah",
                    Price = rate,
                    Change = 0m,
                };
            }
        }
        catch
        {
            // Abaikan error fallback
        }

        return null;
    }

    // Cache hasil IDX (data penutupan harian) agar tidak membebani server IDX
    private static MarketIndicatorDto? ihsgCache;
    private static DateTime ihsgCacheAtUtc = DateTime.MinValue;
    private static readonly TimeSpan IhsgCacheTtl = TimeSpan.FromMinutes(5);

    private static async Task<MarketIndicatorDto?> FetchIhsgAsync(HttpClient client)
    {
        // 1. Sumber utama: API resmi IDX (GetIndexSummary, kode indeks COMPOSITE = IHSG)
        if (ihsgCache != null && DateTime.UtcNow - ihsgCacheAtUtc < IhsgCacheTtl)
        {
            return ihsgCache;
        }

        var idxResult = await FetchIhsgFromIdxAsync(client);
        if (idxResult != null)
        {
            ihsgCache = idxResult;
            ihsgCacheAtUtc = DateTime.UtcNow;
            return idxResult;
        }

        // 2. Cadangan: Yahoo Finance
        foreach (var host in new[] { "query1", "query2" })
        {
            var yahooUrl = $"https://{host}.finance.yahoo.com/v8/finance/chart/%5EJKSE";
            var result = await FetchYahooChartDataAsync(client, yahooUrl, "IHSG", "Indeks Harga Saham Gabungan");
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static async Task<MarketIndicatorDto?> FetchIhsgFromIdxAsync(HttpClient client)
    {
        // Hari bursa terakhir: mundur maksimal 7 hari dari hari ini (WIB) untuk melewati akhir pekan dan libur
        var todayWib = DateTime.UtcNow.AddHours(7).Date;

        for (var i = 0; i < 7; i++)
        {
            var date = todayWib.AddDays(-i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            try
            {
                var url = $"https://www.idx.co.id/primary/TradingSummary/GetIndexSummary?date={date:yyyyMMdd}&start=0&length=9999";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
                request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
                request.Headers.TryAddWithoutValidation("Referer", "https://www.idx.co.id/");

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null; // diblokir atau error: pakai cadangan
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                if (!doc.RootElement.TryGetProperty("data", out var dataArray) ||
                    dataArray.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                foreach (var item in dataArray.EnumerateArray())
                {
                    if (!item.TryGetProperty("IndexCode", out var codeEl) ||
                        !string.Equals(codeEl.GetString(), "COMPOSITE", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!TryReadDecimal(item, "Close", out var close) || close <= 0)
                    {
                        break;
                    }

                    TryReadDecimal(item, "Previous", out var previous);
                    var changePercent = previous > 0 ? ((close - previous) / previous) * 100 : 0m;

                    return new MarketIndicatorDto
                    {
                        Symbol = "IHSG",
                        Name = "Indeks Harga Saham Gabungan",
                        Price = close,
                        Change = Math.Round(changePercent, 2),
                    };
                }

                // Tidak ada data COMPOSITE pada tanggal ini (libur bursa): coba hari sebelumnya
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static bool TryReadDecimal(JsonElement element, string property, out decimal value)
    {
        value = 0m;
        if (!element.TryGetProperty(property, out var prop))
        {
            return false;
        }

        switch (prop.ValueKind)
        {
            case JsonValueKind.Number:
                return prop.TryGetDecimal(out value);
            case JsonValueKind.String:
                return decimal.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            default:
                return false;
        }
    }

    private static async Task<MarketIndicatorDto?> FetchYahooChartDataAsync(
        HttpClient client,
        string url,
        string symbol,
        string name)
    {
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Request URL wajib menggunakan protokol HTTPS.");
        }

        try
        {
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var jsonDoc = await JsonDocument.ParseAsync(stream);

            var root = jsonDoc.RootElement;
            if (!root.TryGetProperty("chart", out var chart) ||
                !chart.TryGetProperty("result", out var result) ||
                result.GetArrayLength() == 0)
            {
                return null;
            }

            var meta = result[0].GetProperty("meta");

            if (!meta.TryGetProperty("regularMarketPrice", out var priceElement))
            {
                return null;
            }

            decimal price = priceElement.GetDecimal();
            decimal prevClose = price;

            if (meta.TryGetProperty("chartPreviousClose", out var prevCloseElement) ||
                meta.TryGetProperty("previousClose", out prevCloseElement))
            {
                prevClose = prevCloseElement.GetDecimal();
            }

            decimal changePercent = prevClose != 0 ? ((price - prevClose) / prevClose) * 100 : 0m;

            return new MarketIndicatorDto
            {
                Symbol = symbol,
                Name = name,
                Price = price,
                Change = Math.Round(changePercent, 2),
            };
        }
        catch
        {
            return null;
        }
    }
}

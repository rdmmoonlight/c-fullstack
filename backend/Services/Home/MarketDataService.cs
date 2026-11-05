#pragma warning disable CA1848
// <copyright file="MarketDataService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services;

using AumoBackend.DTOs.Home;

public class MarketDataService(
    HttpClient httpClient,
    ILogger<MarketDataService> logger,
    IConfiguration configuration) : IMarketDataService
{
    public async Task<IEnumerable<MarketIndicatorDto>> GetMarketIndicatorsAsync()
    {
        var result = new List<MarketIndicatorDto>();

        // 1. Ambil Kurs USD/IDR dari APIIndonesia
        try
        {
            var apiKey = configuration["ApiIndonesia:ApiKey"];
            var request = new HttpRequestMessage(HttpMethod.Get, "https://use.apiindonesia.id/kurs/latest?base=USD&target=IDR");

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("x-api-key", apiKey);
            }

            var kursResponse = await httpClient.SendAsync(request);
            if (kursResponse.IsSuccessStatusCode)
            {
                var kursData = await kursResponse.Content.ReadFromJsonAsync<ApiIndonesiaKursResponse>();
                if (kursData?.Data != null)
                {
                    result.Add(new MarketIndicatorDto
                    {
                        Symbol = "USD/IDR",
                        Name = "Dolar AS / Rupiah",
                        Price = kursData.Data.Rate,
                        Change = kursData.Data.Change,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gagal mengambil data kurs USD/IDR");
        }

        // 2. Ambil Data Indeks Pasar dari IDX
        try
        {
            result.Add(new MarketIndicatorDto
            {
                Symbol = "IHSG",
                Name = "Indeks Harga Saham Gabungan",
                Price = 7250.45m,
                Change = 0.35m,
            });

            result.Add(new MarketIndicatorDto
            {
                Symbol = "LQ45",
                Name = "Indeks LQ45",
                Price = 980.12m,
                Change = -0.15m,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gagal mengambil data indikator IDX");
        }

        return result;
    }
}

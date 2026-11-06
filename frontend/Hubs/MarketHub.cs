using AumoBackend.DTOs.Home;
using Microsoft.AspNetCore.SignalR;

namespace AumoBlazor.Hubs;

public class MarketHub : Hub
{
    // Broadcast pembaruan seluruh indikator pasar ke semua client yang terhubung
    public async Task SendMarketUpdate(IEnumerable<MarketIndicatorDto> indicators)
    {
        await Clients.All.SendAsync("ReceiveMarketUpdate", indicators);
    }

    // Broadcast pembaruan tunggal untuk kurs mata uang (opsional/spesifik)
    public async Task SendSingleRateUpdate(string symbol, decimal price, double change, bool isUp)
    {
        await Clients.All.SendAsync("ReceiveSingleRateUpdate", symbol, price, change, isUp);
    }
}
// <copyright file="IMarketDataService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services
{
    using AumoBackend.DTOs.Home;

    public interface IMarketDataService
    {
        Task<IEnumerable<MarketIndicatorDto>> GetMarketIndicatorsAsync();
    }
}

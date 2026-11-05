// <copyright file="IHealthService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Health
{
    using AumoBackend.DTOs;

    public interface IHealthService
    {
        Task<HealthStatusDto> GetHealthStatusAsync();
    }
}

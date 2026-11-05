// <copyright file="IDashboardService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Dashboard;

using AumoBackend.DTOs;

public interface IDashboardService
{
    Task<DashboardDataDto> GetDashboardDataAsync(Guid userId, string period);
}

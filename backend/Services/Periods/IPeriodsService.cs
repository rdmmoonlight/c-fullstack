// <copyright file="IPeriodsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Periods;

using AumoBackend.DTOs;

public interface IPeriodsService
{
    Task<GetPeriodsResponse> GetPeriodsAsync(Guid userId);

    Task<OpenPeriodInfoResponse> GetOpenPeriodInfoAsync(Guid userId);

    Task<CreatePeriodResult> CreatePeriodAsync(Guid userId, CreatePeriodRequest request);

    Task<SelectPeriodResult?> SelectPeriodAsync(Guid userId, int periodId);

    Task<BaseServiceResult> ClearSelectionAsync(Guid userId);

    Task<BaseServiceResult> ClosePeriodAsync(Guid userId, int periodId);
}

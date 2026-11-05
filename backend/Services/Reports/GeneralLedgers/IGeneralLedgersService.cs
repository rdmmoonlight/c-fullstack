// <copyright file="IGeneralLedgersService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Reports.GeneralLedgers;

using AumoBackend.DTOs;
using AumoBackend.DTOs.Reports;

public interface IGeneralLedgersService
{
    Task<BaseServiceResult> RefreshGeneralLedgersAsync(Guid userId);

    Task<BaseServiceResult> ClearSelectedPeriodLedgersAsync(Guid userId);

    // Baru - semua bisnis GET pindah kesini
    Task<BaseServiceResult<PermanentLedgerGroupedResponse>> GetPermanentLedgersAsync(Guid userId);

    Task<BaseServiceResult<TemporaryLedgerGroupedResponse>> GetTemporaryLedgersAsync(Guid userId);
}

// Kalau BaseServiceResult kamu belum generic, tambahkan ini:
public class BaseServiceResult<T> : BaseServiceResult
{
    public T? Data { get; set; }
}

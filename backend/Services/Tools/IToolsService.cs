// <copyright file="IToolsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Tools;

using AumoBackend.DTOs;
using AumoBackend.ViewModels;

public interface IToolsService
{
    byte[] GenerateJournalTemplate();

    Task<PreviewJournalImportViewModel> PreviewJournalImportAsync(Guid userId, JournalImportRequestDto request);

    Task<ImportJournalResultViewModel> ImportJournalEntriesAsync(Guid userId, JournalImportRequestDto request);
}

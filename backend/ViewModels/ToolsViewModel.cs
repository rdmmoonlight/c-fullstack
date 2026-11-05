// <copyright file="ToolsViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.ViewModels;

using System.Collections.Generic;
using AumoBackend.DTOs;

public class PreviewJournalImportViewModel
{
    public List<JournalTransactionDto> Transactions { get; set; } = new();

    public List<AccountMappingDetailDto> AccountMappings { get; set; } = new();

    public PreviewJournalSummaryViewModel Summary { get; set; } = new();
}

public class PreviewJournalSummaryViewModel
{
    public int TotalUniqueAccounts { get; set; }

    public int ExactMatchCount { get; set; }

    public int ReallocatedCount { get; set; }

    public int UnmappedCount { get; set; }

    public bool IsPerfectMatch { get; set; }
}

public class ImportJournalResultViewModel
{
    public string Message { get; set; } = string.Empty;

    public int ImportedEntriesCount { get; set; }
}

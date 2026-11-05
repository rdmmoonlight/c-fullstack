// <copyright file="GeneralLedgerViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.ViewModels.Reports;

using System;
using System.Collections.Generic;

public class LedgerAccountViewModel
{
    public int AccountId { get; set; }

    public string? ReferenceNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool NormalBalanceIsDebit { get; set; }

    public List<LedgerLineViewModel> Lines { get; set; } = new();

    public decimal EndingBalance { get; set; }
}

public class LedgerLineViewModel
{
    public DateTime EntryDate { get; set; }

    public string? Description { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public decimal RunningBalance { get; set; }
}

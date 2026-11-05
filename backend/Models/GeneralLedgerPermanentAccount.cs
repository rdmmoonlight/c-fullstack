// <copyright file="GeneralLedgerPermanentAccount.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Models;

using System;

public partial class GeneralLedgerPermanentAccount
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public int PeriodId { get; set; }

    public int AccountId { get; set; }

    public int JournalEntryId { get; set; }

    public int JournalEntryLineId { get; set; }

    public DateTime EntryDate { get; set; }

    public string? TransactionNumber { get; set; }

    public string? LineDescription { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public decimal RunningBalance { get; set; }

    public virtual ChartOfAccount Account { get; set; } = null!;

    public virtual GeneralJournal JournalEntryLine { get; set; } = null!;

    public virtual Period Period { get; set; } = null!;
}

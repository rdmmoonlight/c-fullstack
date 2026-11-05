// <copyright file="ChartOfAccount.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Models;

using System;
using System.Collections.Generic;

public partial class ChartOfAccount
{
    public int Id { get; set; }

    public int ReferenceNumber { get; set; }

    public string AccountName { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public Guid UserId { get; set; }

    public virtual ICollection<GeneralJournal> GeneralJournals { get; set; } = new List<GeneralJournal>();

    public virtual ICollection<GeneralLedgerPermanentAccount> GeneralLedgerPermanentAccounts { get; set; } = new List<GeneralLedgerPermanentAccount>();

    public virtual ICollection<GeneralLedgerTemporaryAccount> GeneralLedgerTemporaryAccounts { get; set; } = new List<GeneralLedgerTemporaryAccount>();

    // Jika Balance dihitung dari posisi Debit/Kredit atau disimpan di DTO
    public decimal Balance { get; set; }

    // DisplayLabel helper untuk tampilan UI/Dropdown
    public string DisplayLabel => $"{this.ReferenceNumber} - {this.AccountName}";
}

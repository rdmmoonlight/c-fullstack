// <copyright file="CreateJournalEntryRequestValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.JournalEntries;

using AumoBackend.DTOs;

public class CreateJournalEntryRequestValidator : JournalEntryValidatorBase<CreateJournalEntryRequest>
{
    public CreateJournalEntryRequestValidator()
        : base(x => x.EntryDate, x => x.JournalType, x => x.Lines)
    {
    }
}

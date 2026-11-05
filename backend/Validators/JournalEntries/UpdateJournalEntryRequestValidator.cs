// <copyright file="UpdateJournalEntryRequestValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.JournalEntries;

using AumoBackend.DTOs;

public class UpdateJournalEntryRequestValidator : JournalEntryValidatorBase<UpdateJournalEntryRequest>
{
    public UpdateJournalEntryRequestValidator()
        : base(x => x.EntryDate, x => x.JournalType, x => x.Lines)
    {
    }
}

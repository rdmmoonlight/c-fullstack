// <copyright file="NextTransactionNumberQueryValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.Queries;

using AumoBackend.DTOs;
using AumoBackend.Validators.JournalEntries;
using FluentValidation;

public class NextTransactionNumberQueryValidator : AbstractValidator<NextTransactionNumberQuery>
{
    private static readonly DateTime MinEntryDate = new(2000, 1, 1);
    private static readonly DateTime MaxEntryDate = new(2100, 12, 31);

    public NextTransactionNumberQueryValidator()
    {
        this.RuleFor(x => x.JournalType)
            .Must(t => JournalTypes.IsValidOrEmpty(t))
            .WithMessage("Journal type must be 'General' or 'Adjusting'.");

        this.RuleFor(x => x.EntryDate)
            .Must(d => d!.Value >= MinEntryDate && d.Value <= MaxEntryDate)
            .When(x => x.EntryDate.HasValue)
            .WithMessage("Entry date is invalid.");
    }
}

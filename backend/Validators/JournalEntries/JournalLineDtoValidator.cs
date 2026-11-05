// <copyright file="JournalLineDtoValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.JournalEntries;

using AumoBackend.DTOs;
using FluentValidation;

public class JournalLineDtoValidator : AbstractValidator<JournalEntryLineDto>
{
    private const int MaxLineDescriptionLength = 250; // selaras HasMaxLength(250) di AppDbContext

    public JournalLineDtoValidator()
    {
        this.RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("Each journal line must have a valid account.");

        this.RuleFor(x => x.Debit)
            .GreaterThanOrEqualTo(0m).WithMessage("Debit amount cannot be negative.")
            .PrecisionScale(18, 2, true).WithMessage("Amounts support at most 2 decimal places and 16 digits before the decimal point.");

        this.RuleFor(x => x.Credit)
            .GreaterThanOrEqualTo(0m).WithMessage("Credit amount cannot be negative.")
            .PrecisionScale(18, 2, true).WithMessage("Amounts support at most 2 decimal places and 16 digits before the decimal point.");

        this.RuleFor(x => x)
            .Must(l => !(l.Debit > 0 && l.Credit > 0))
            .WithMessage("A journal line cannot have both a debit and a credit amount.");

        this.RuleFor(x => x.LineDescription)
            .MaximumLength(MaxLineDescriptionLength)
            .WithMessage($"Line description must not exceed {MaxLineDescriptionLength} characters.");
    }
}

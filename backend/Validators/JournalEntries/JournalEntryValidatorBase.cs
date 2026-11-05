// <copyright file="JournalEntryValidatorBase.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.JournalEntries;

using System.Linq.Expressions;
using AumoBackend.DTOs;
using FluentValidation;

public abstract class JournalEntryValidatorBase<T> : AbstractValidator<T>
{
    private const int MaxLines = 200;
    private static readonly DateTime MinEntryDate = new(2000, 1, 1);
    private static readonly DateTime MaxEntryDate = new(2100, 12, 31);

    protected JournalEntryValidatorBase(
        Expression<Func<T, DateTime>> entryDate,
        Expression<Func<T, string>> journalType,
        Expression<Func<T, IEnumerable<JournalEntryLineDto>>> lines)
    {
        var getLines = lines.Compile();

        this.RuleFor(entryDate)
            .Must(d => d >= MinEntryDate && d <= MaxEntryDate)
            .WithMessage("Entry date is invalid.");

        // Kosong diperbolehkan (Create => General, Edit => jenis jurnal semula).
        this.RuleFor(journalType)
            .Must(t => JournalTypes.IsValidOrEmpty(t))
            .WithMessage("Journal type must be 'General' or 'Adjusting'.");

        this.RuleFor(lines)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Journal lines are required.")
            .Must(l => l.Count() <= MaxLines).WithMessage($"A journal entry cannot have more than {MaxLines} lines.")
            .Must(l => Effective(l).Count() >= 2).WithMessage("A journal entry must have at least two line items.")
            .Must(HaveBalancedTotals).WithMessage("Total debit must equal total credit before posting.");

        this.RuleForEach(lines)
            .Where(IsEffective)
            .SetValidator(new JournalLineDtoValidator())
            .When(x => getLines(x) != null);
    }

    private static bool IsEffective(JournalEntryLineDto line)
        => line != null && line.AccountId != 0 && (line.Debit != 0 || line.Credit != 0);

    private static IEnumerable<JournalEntryLineDto> Effective(IEnumerable<JournalEntryLineDto> lines)
        => lines.Where(IsEffective);

    private static bool HaveBalancedTotals(IEnumerable<JournalEntryLineDto> lines)
    {
        var effective = Effective(lines).ToList();
        var totalDebit = effective.Sum(l => l.Debit);
        var totalCredit = effective.Sum(l => l.Credit);
        return totalDebit == totalCredit && totalDebit > 0;
    }
}

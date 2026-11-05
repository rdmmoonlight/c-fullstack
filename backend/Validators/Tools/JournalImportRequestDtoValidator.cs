// <copyright file="JournalImportRequestDtoValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using AumoBackend.DTOs;
using AumoBackend.Validators.JournalEntries;
using FluentValidation;

namespace AumoBackend.Validators.Tools;

public class JournalImportRequestDtoValidator : AbstractValidator<JournalImportRequestDto>
{
    private const int MaxTransactions = 5000;
    private const int MaxMappings = 5000;

    public JournalImportRequestDtoValidator()
    {
        this.RuleFor(x => x.TargetMonth)
            .InclusiveBetween(1, 12)
            .WithMessage("Invalid period parameters provided.");

        this.RuleFor(x => x.TargetYear)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Invalid period parameters provided.");

        this.RuleFor(x => x.Transactions)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("No transaction data provided.")
            .Must(t => t.Count <= MaxTransactions).WithMessage($"A single import cannot exceed {MaxTransactions} transactions.");

        this.RuleForEach(x => x.Transactions)
            .SetValidator(new JournalTransactionDtoValidator())
            .When(x => x.Transactions != null);

        this.RuleFor(x => x.CustomMappings)
            .Must(m => m!.Count <= MaxMappings)
            .When(x => x.CustomMappings != null)
            .WithMessage($"Custom mappings cannot exceed {MaxMappings} entries.");

        this.RuleForEach(x => x.CustomMappings)
            .SetValidator(new AccountMappingDetailDtoValidator())
            .When(x => x.CustomMappings != null);
    }
}

public class JournalTransactionDtoValidator : AbstractValidator<JournalTransactionDto>
{
    private const int MaxLinesPerTransaction = 500;

    public JournalTransactionDtoValidator()
    {
        this.RuleFor(x => x.Date)
            .Must(d => DateTime.TryParse(d, out _))
            .WithMessage(t => $"Transaction '{t.TransactionNumber}' has an invalid date.");

        // Import memperlakukan "Adjusting" tanpa membedakan huruf besar/kecil; selain itu dianggap General.
        this.RuleFor(x => x.JournalType)
            .Must(t => !string.IsNullOrWhiteSpace(t) && JournalTypes.IsValidOrEmpty(t, ignoreCase: true))
            .WithMessage(t => $"Transaction '{t.TransactionNumber}' has an invalid journal type (use 'General' or 'Adjusting').");

        this.RuleFor(x => x.Lines)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(t => $"Transaction '{t.TransactionNumber}' has no lines.")
            .Must(l => l.Count >= 2).WithMessage(t => $"Transaction '{t.TransactionNumber}' must have at least two lines.")
            .Must(l => l.Count <= MaxLinesPerTransaction).WithMessage(t => $"Transaction '{t.TransactionNumber}' has too many lines.")
            .Must(HaveBalancedTotals).WithMessage(t => $"Transaction '{t.TransactionNumber}': total debit must equal total credit.");

        this.RuleForEach(x => x.Lines)
            .SetValidator(new JournalImportLineValidator())
            .When(x => x.Lines != null);
    }

    // Dibulatkan 2 desimal untuk menoleransi selisih floating point dari Excel.
    private static bool HaveBalancedTotals(List<JournalLineDto> lines)
    {
        var totalDebit = Math.Round(lines.Sum(l => l.Debit ?? 0m), 2);
        var totalCredit = Math.Round(lines.Sum(l => l.Credit ?? 0m), 2);
        return totalDebit == totalCredit && totalDebit > 0;
    }
}

public class JournalImportLineValidator : AbstractValidator<JournalLineDto>
{
    private const int MaxDescriptionLength = 250; // selaras HasMaxLength(250) di AppDbContext
    private const int MaxAccountNameLength = 200;

    public JournalImportLineValidator()
    {
        this.RuleFor(x => x.Debit)
            .GreaterThanOrEqualTo(0m).When(x => x.Debit.HasValue)
            .WithMessage("Debit amount cannot be negative.");

        this.RuleFor(x => x.Credit)
            .GreaterThanOrEqualTo(0m).When(x => x.Credit.HasValue)
            .WithMessage("Credit amount cannot be negative.");

        this.RuleFor(x => x)
            .Must(l => !((l.Debit ?? 0m) > 0 && (l.Credit ?? 0m) > 0))
            .WithMessage("A journal line cannot have both a debit and a credit amount.");

        this.RuleFor(x => x.Description)
            .MaximumLength(MaxDescriptionLength)
            .WithMessage($"Line description must not exceed {MaxDescriptionLength} characters.");

        this.RuleFor(x => x.AccountName)
            .MaximumLength(MaxAccountNameLength)
            .WithMessage($"Account name must not exceed {MaxAccountNameLength} characters.");
    }
}

public class AccountMappingDetailDtoValidator : AbstractValidator<AccountMappingDetailDto>
{
    private const int MaxAccountNameLength = 200;

    public AccountMappingDetailDtoValidator()
    {
        this.RuleFor(x => x.MappedRef)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Mapped account reference cannot be negative.");

        // Dipakai sebagai kunci mapping (.Trim()), sehingga tidak boleh null.
        this.RuleFor(x => x.ExcelAccountName)
            .NotNull().WithMessage("Excel account name is required in mappings.")
            .MaximumLength(MaxAccountNameLength).WithMessage($"Account name must not exceed {MaxAccountNameLength} characters.");
    }
}

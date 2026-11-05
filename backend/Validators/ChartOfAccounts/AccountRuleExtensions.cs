// <copyright file="AccountRuleExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.ChartOfAccounts;

using AumoBackend.Models;
using FluentValidation;

internal static class AccountRuleExtensions
{
    public const int MaxAccountNameLength = 100; // selaras HasMaxLength(100) di AppDbContext

    public static IRuleBuilderOptions<T, string> MustBeValidAccountName<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(MaxAccountNameLength).WithMessage($"Account name must not exceed {MaxAccountNameLength} characters.");

    public static IRuleBuilderOptions<T, string> MustBeValidAccountType<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Account category type is required.")
            .Must(AccountClassificationHelper.IsKnownAccountType).WithMessage("Account category type is invalid.");

    public static IRuleBuilderOptions<T, string> MustBeValidAccountRole<T>(this IRuleBuilder<T, string> rule)
        => rule
            .Must(role => string.IsNullOrWhiteSpace(role) || AccountClassificationHelper.AccountRoles.Contains(role))
            .WithMessage("Account role is invalid.");
}

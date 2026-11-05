// <copyright file="CreateAccountRequestValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.ChartOfAccounts;

using AumoBackend.DTOs;
using AumoBackend.Models;
using FluentValidation;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        this.RuleFor(x => x.AccountName).MustBeValidAccountName();
        this.RuleFor(x => x.Type).MustBeValidAccountType();
        this.RuleFor(x => x.Role).MustBeValidAccountRole();

        this.RuleFor(x => x.ReferenceNumber)
            .Must((r, n) => AccountClassificationHelper.ValidateReferenceNumber(r.Type, n))
            .When(x => AccountClassificationHelper.IsKnownAccountType(x.Type))
            .WithMessage(r => $"Invalid reference number {r.ReferenceNumber} for category {r.Type}.");
    }
}

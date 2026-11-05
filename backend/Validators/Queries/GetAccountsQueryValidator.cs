// <copyright file="GetAccountsQueryValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.Queries;

using AumoBackend.DTOs;
using AumoBackend.Models;
using FluentValidation;

public class GetAccountsQueryValidator : AbstractValidator<GetAccountsQuery>
{
    private const int MaxSearchLength = 100;

    public GetAccountsQueryValidator()
    {
        this.RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .WithMessage($"Search keyword must not exceed {MaxSearchLength} characters.");

        this.RuleFor(x => x.Category)
            .Must(AccountClassificationHelper.IsKnownAccountType)
            .When(x => !string.IsNullOrWhiteSpace(x.Category))
            .WithMessage("Account category is invalid.");
    }
}

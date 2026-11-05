// <copyright file="SearchDescriptionsQueryValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.Queries;

using AumoBackend.DTOs;
using FluentValidation;

public class SearchDescriptionsQueryValidator : AbstractValidator<SearchDescriptionsQuery>
{
    private const int MaxKeywordLength = 100;

    public SearchDescriptionsQueryValidator()
    {
        this.RuleFor(x => x.Q)
            .MaximumLength(MaxKeywordLength)
            .WithMessage($"Search keyword must not exceed {MaxKeywordLength} characters.");
    }
}

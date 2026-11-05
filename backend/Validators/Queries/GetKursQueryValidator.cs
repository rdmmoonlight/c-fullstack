// <copyright file="GetKursQueryValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.Queries;

using System.Text.RegularExpressions;
using AumoBackend.DTOs;
using FluentValidation;

public partial class GetKursQueryValidator : AbstractValidator<GetKursQuery>
{
    public GetKursQueryValidator()
    {
        // Nilai ini disisipkan ke URL layanan eksternal, jadi hanya kode ISO 4217 (3 huruf) yang diterima.
        this.RuleFor(x => x.BaseCurrency)
            .Must(BeCurrencyCodeOrEmpty)
            .WithMessage("Base currency must be a 3-letter currency code.");

        this.RuleFor(x => x.TargetCurrency)
            .Must(BeCurrencyCodeOrEmpty)
            .WithMessage("Target currency must be a 3-letter currency code.");
    }

    [GeneratedRegex("^[A-Za-z]{3}$")]
    private static partial Regex CurrencyCodeRegex();

    private static bool BeCurrencyCodeOrEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) || CurrencyCodeRegex().IsMatch(value);
}

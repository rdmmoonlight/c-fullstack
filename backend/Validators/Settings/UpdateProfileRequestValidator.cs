// <copyright file="UpdateProfileRequestValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.Settings;

using System.Text.RegularExpressions;
using AumoBackend.DTOs;
using FluentValidation;

public partial class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    private const int MaxFullNameLength = 100;
    private const int MaxUserNameLength = 256; // batas bawaan ASP.NET Identity
    private const int MaxBioLength = 500;
    private const int MaxAvatarUrlLength = 2048;

    public UpdateProfileRequestValidator()
    {
        this.RuleFor(x => x.FullName)
            .MaximumLength(MaxFullNameLength)
            .WithMessage($"Full name must not exceed {MaxFullNameLength} characters.");

        // Karakter yang diizinkan diperiksa oleh Identity saat SetUserNameAsync; di sini hanya batas panjang.
        this.RuleFor(x => x.UserName)
            .MaximumLength(MaxUserNameLength)
            .WithMessage($"Username must not exceed {MaxUserNameLength} characters.");

        this.RuleFor(x => x.PhoneNumber)
            .Must(BePhoneNumber)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Phone number format is invalid.");

        this.RuleFor(x => x.Bio)
            .MaximumLength(MaxBioLength)
            .WithMessage($"Bio must not exceed {MaxBioLength} characters.");

        this.RuleFor(x => x.AvatarUrl)
            .MaximumLength(MaxAvatarUrlLength).WithMessage("Avatar URL is too long.")
            .Must(BeAbsoluteHttpUrl).WithMessage("Avatar URL must be a valid http or https address.")
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
    }

    // Digit, spasi, +, -, titik, dan tanda kurung; 5-20 karakter.
    [GeneratedRegex(@"^\+?[0-9\s\-().]{5,20}$")]
    private static partial Regex PhoneRegex();

    private static bool BePhoneNumber(string? value)
        => value != null && PhoneRegex().IsMatch(value.Trim());

    private static bool BeAbsoluteHttpUrl(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

// <copyright file="GoogleLoginRequestValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.Auth;

using AumoBackend.DTOs;
using FluentValidation;

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    // Batas wajar untuk Google ID Token (JWT) agar payload abnormal ditolak sebelum diverifikasi ke Google.
    private const int MaxIdTokenLength = 4096;

    public GoogleLoginRequestValidator()
    {
        this.RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token is required.")
            .MaximumLength(MaxIdTokenLength).WithMessage("Google ID Token is too long.")
            .Must(BeJwtShape).WithMessage("Google ID Token format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.IdToken));
    }

    // JWT = tiga segmen dipisah titik (header.payload.signature), tanpa spasi.
    private static bool BeJwtShape(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var parts = token.Split('.');
        return parts.Length == 3 && parts.All(p => p.Length > 0);
    }
}

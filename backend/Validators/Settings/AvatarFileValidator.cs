// <copyright file="AvatarFileValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.Settings;

using FluentValidation;
using Microsoft.AspNetCore.Http;

public class AvatarFileValidator : AbstractValidator<IFormFile>
{
    public const long MaxFileSizeBytes = 2 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };

    public AvatarFileValidator()
    {
        this.RuleFor(x => x.Length)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File size exceeds limit (Max 2MB).");

        this.RuleFor(x => x.FileName)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Invalid file type. Only JPG, PNG, GIF, and WEBP are allowed.");

        this.RuleFor(x => x.ContentType)
            .Must(type => AllowedContentTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Invalid file type. Only JPG, PNG, GIF, and WEBP are allowed.");
    }
}

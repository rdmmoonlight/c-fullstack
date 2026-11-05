// <copyright file="LoginRequestValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.Auth
{
    using AumoBackend.DTOs;
    using FluentValidation;

    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            // 1. Validasi Email / Username
            this.RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email or username is required.")
                .MaximumLength(256).WithMessage("Email or username must not exceed 256 characters.");

            // 2. Validasi Password
            this.RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
                .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");

            // 3. Validasi UserAgent Optional
            this.When(x => !string.IsNullOrWhiteSpace(x.UserAgent), () =>
            {
                this.RuleFor(x => x.UserAgent)
                    .MaximumLength(512).WithMessage("UserAgent payload is too long.");
            });

            // 4. Validasi OperatingSystem Optional
            this.When(x => !string.IsNullOrWhiteSpace(x.OperatingSystem), () =>
            {
                this.RuleFor(x => x.OperatingSystem)
                    .MaximumLength(100).WithMessage("Operating system identifier is too long.");
            });
        }
    }
}

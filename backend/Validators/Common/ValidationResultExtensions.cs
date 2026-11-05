// <copyright file="ValidationResultExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Validators.Common;

using FluentValidation.Results;

public static class ValidationResultExtensions
{
    public static object ToErrorResponse(this ValidationResult result)
    {
        var errors = result.Errors
            .Select(e => e.ErrorMessage)
            .Distinct()
            .ToArray();

        return new
        {
            success = false,
            message = errors.FirstOrDefault() ?? "Validation failed.",
            errors,
        };
    }
}

// <copyright file="AuthModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Models;

using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }
}

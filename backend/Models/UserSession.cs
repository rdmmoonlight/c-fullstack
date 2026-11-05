// <copyright file="UserSession.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Models;

using System;

public partial class UserSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string DeviceName { get; set; } = null!;

    public string Browser { get; set; } = null!;

    public string OperatingSystem { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string UserAgent { get; set; } = null!;

    public string RefreshTokenHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsCurrent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastActivityAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}

// <copyright file="TrustedDevice.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Models;

using System;

public partial class TrustedDevice
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string DeviceName { get; set; } = null!;

    public string DeviceIdentifier { get; set; } = null!;

    public string Browser { get; set; } = null!;

    public string OperatingSystem { get; set; } = null!;

    public bool IsTrusted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastUsedAt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}

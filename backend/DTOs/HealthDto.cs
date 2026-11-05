namespace AumoBackend.DTOs;

using System;

public class HealthStatusDto
{
    public string Status { get; set; } = string.Empty;

    public string Database { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public string? Message { get; set; }
}

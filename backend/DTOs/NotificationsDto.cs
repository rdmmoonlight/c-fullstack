namespace AumoBackend.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public string? TargetUrl { get; set; }

    public string CreatedAt { get; set; } = string.Empty;
}

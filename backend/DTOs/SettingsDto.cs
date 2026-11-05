namespace AumoBackend.DTOs;

public class UpdateProfileRequest
{
    public string? FullName { get; set; }

    public string? UserName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

public class ServiceResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public object? Data { get; set; }

    public int StatusCode { get; set; } = 200;

    public static ServiceResult Ok(string message = "", object? data = null) =>
        new() { Success = true, Message = message, Data = data, StatusCode = 200 };

    public static ServiceResult BadRequest(string message) =>
        new() { Success = false, Message = message, StatusCode = 400 };

    public static ServiceResult Unauthorized(string message = "Unauthorized access.") =>
        new() { Success = false, Message = message, StatusCode = 401 };

    public static ServiceResult InternalServerError(string message) =>
        new() { Success = false, Message = message, StatusCode = 500 };
}

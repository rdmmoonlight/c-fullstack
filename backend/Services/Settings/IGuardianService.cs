// <copyright file="IGuardianService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Settings
{
    using AumoBackend.DTOs;

    public interface IGuardianService
    {
        Task<bool> RevokeSessionAsync(string sessionId);

        Task<bool> RevokeSessionAsync(string sessionId, string reason);

        Task<bool> RevokeSessionAsync(Guid sessionId);

        Task<bool> RevokeSessionAsync(Guid sessionId, string reason);

        Task<bool> RevokeSessionAsync(object a1, object a2 = null!);

        Task<bool> RevokeAllSessionsAsync(string userId);

        Task<bool> RevokeAllSessionsAsync(Guid userId);

        Task<List<UserSessionDto>> GetActiveSessionsAsync(string userId);

        Task<List<UserSessionDto>> GetActiveSessionsAsync(Guid userId);

        Task<List<LoginActivityDto>> GetLoginActivitiesAsync(string userId);

        Task<List<LoginActivityDto>> GetLoginActivitiesAsync(Guid userId);

        Task CreateLoginActivityAsync(string userId, string activity = "", string ipAddress = "", string operatingSystem = "", string browser = "", string userAgent = "", object a7 = null!, object a8 = null!, object a9 = null!);

        Task CreateLoginActivityAsync(Guid userId, string activity = "", string ipAddress = "", string operatingSystem = "", string browser = "", string userAgent = "", object a7 = null!, object a8 = null!, object a9 = null!);

        Task CreateSessionAsync(string userId, string deviceName = "", string operatingSystem = "", string browser = "", string country = "", string ipAddress = "", string userAgent = "", string refreshTokenHash = "", object a9 = null!);

        Task CreateSessionAsync(Guid userId, string deviceName = "", string operatingSystem = "", string browser = "", string country = "", string ipAddress = "", string userAgent = "", string refreshTokenHash = "", object a9 = null!);
    }
}

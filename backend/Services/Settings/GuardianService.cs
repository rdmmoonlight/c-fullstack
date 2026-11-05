// <copyright file="GuardianService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Settings
{
    using AumoBackend.DTOs;

    public class GuardianService : IGuardianService
    {
        public Task<bool> RevokeSessionAsync(string sessionId) => Task.FromResult(true);
        public Task<bool> RevokeSessionAsync(string sessionId, string reason) => Task.FromResult(true);
        public Task<bool> RevokeSessionAsync(Guid sessionId) => Task.FromResult(true);
        public Task<bool> RevokeSessionAsync(Guid sessionId, string reason) => Task.FromResult(true);
        public Task<bool> RevokeSessionAsync(object a1, object a2 = null!) => Task.FromResult(true);

        public Task<bool> RevokeAllSessionsAsync(string userId) => Task.FromResult(true);
        public Task<bool> RevokeAllSessionsAsync(Guid userId) => Task.FromResult(true);

        public Task<List<UserSessionDto>> GetActiveSessionsAsync(string userId) => Task.FromResult(new List<UserSessionDto>());
        public Task<List<UserSessionDto>> GetActiveSessionsAsync(Guid userId) => Task.FromResult(new List<UserSessionDto>());

        public Task<List<LoginActivityDto>> GetLoginActivitiesAsync(string userId) => Task.FromResult(new List<LoginActivityDto>());
        public Task<List<LoginActivityDto>> GetLoginActivitiesAsync(Guid userId) => Task.FromResult(new List<LoginActivityDto>());

        public Task CreateLoginActivityAsync(string userId, string activity = "", string ipAddress = "", string operatingSystem = "", string browser = "", string userAgent = "", object a7 = null!, object a8 = null!, object a9 = null!) => Task.CompletedTask;
        public Task CreateLoginActivityAsync(Guid userId, string activity = "", string ipAddress = "", string operatingSystem = "", string browser = "", string userAgent = "", object a7 = null!, object a8 = null!, object a9 = null!) => Task.CompletedTask;

        Task IGuardianService.CreateSessionAsync(string userId, string deviceName, string operatingSystem, string browser, string country, string ipAddress, string userAgent, string refreshTokenHash, object a9) => Task.CompletedTask;
        Task IGuardianService.CreateSessionAsync(Guid userId, string deviceName, string operatingSystem, string browser, string country, string ipAddress, string userAgent, string refreshTokenHash, object a9) => Task.CompletedTask;
    }
}

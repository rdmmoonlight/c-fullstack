// <copyright file="ISettingsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Settings
{
    using System.Security.Claims;
    using AumoBackend.DTOs;

    public interface ISettingsService
    {
        Task<ServiceResult> UpdateProfileAsync(ClaimsPrincipal userPrincipal, UpdateProfileRequest request);

        Task<ServiceResult> UploadAvatarAsync(ClaimsPrincipal userPrincipal, IFormFile? file, IFormFile? avatar, IFormFileCollection? files);

        Task<ServiceResult> ChangePasswordAsync(ClaimsPrincipal userPrincipal, ChangePasswordRequest request);

        Task<ServiceResult> DeleteAccountAsync(ClaimsPrincipal userPrincipal);

        Task<ServiceResult> GetGuardianDashboardAsync(ClaimsPrincipal userPrincipal);

        Task<ServiceResult> RevokeSessionAsync(ClaimsPrincipal userPrincipal, Guid sessionId);

        Task<ServiceResult> RevokeAllSessionsAsync(ClaimsPrincipal userPrincipal);
    }
}

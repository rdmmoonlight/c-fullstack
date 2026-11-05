#pragma warning disable CA1826
// <copyright file="SettingsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Settings
{
    using System.Security.Claims;
    using AumoBackend.DTOs;
    using AumoBackend.Models;
    using AumoBackend.Services.Storage;
    using AumoBackend.ViewModels;
    using Microsoft.AspNetCore.Identity;

    public class SettingsService : ISettingsService
    {
        private readonly UserManager<ApplicationUser>? userManager;
        private readonly SignInManager<ApplicationUser>? signInManager;
        private readonly IGuardianService? guardianService;
        private readonly IAvatarStorage? avatarStorage;

        public async Task<ServiceResult> UpdateProfileAsync(ClaimsPrincipal userPrincipal, UpdateProfileRequest request)
        {
            var user = await this.userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return ServiceResult.Unauthorized();
            }

            if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
            {
                var setUserNameResult = await this.userManager.SetUserNameAsync(user, request.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    var errors = string.Join(", ", setUserNameResult.Errors.Select(e => e.Description));
                    return ServiceResult.BadRequest(errors);
                }
            }

            if (request.PhoneNumber != user.PhoneNumber)
            {
                var setPhoneResult = await this.userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    var errors = string.Join(", ", setPhoneResult.Errors.Select(e => e.Description));
                    return ServiceResult.BadRequest(errors);
                }
            }

            user.FullName = request.FullName;
            user.Bio = request.Bio;
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            var updateResult = await this.userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return ServiceResult.BadRequest(errors);
            }

            return ServiceResult.Ok("Profile updated successfully.");
        }

        public async Task<ServiceResult> UploadAvatarAsync(ClaimsPrincipal userPrincipal, IFormFile? file, IFormFile? avatar, IFormFileCollection? files)
        {
            var user = await this.userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return ServiceResult.Unauthorized();
            }

            var uploadFile = file ?? avatar ?? files?.FirstOrDefault();
            if (uploadFile == null || uploadFile.Length == 0)
            {
                return ServiceResult.BadRequest("No file uploaded. Ensure the form-data key is named 'file' or 'avatar'.");
            }

            if (uploadFile.Length > 2 * 1024 * 1024)
            {
                return ServiceResult.BadRequest("File size exceeds limit (Max 2MB).");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(uploadFile.FileName);

            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult.BadRequest("Invalid file type. Only JPG, PNG, GIF, and WEBP are allowed.");
            }

            if (!this.avatarStorage.IsConfigured)
            {
                return ServiceResult.InternalServerError("Neon bucket belum dikonfigurasi di server.");
            }

            try
            {
                var fileName = $"avatar_{user.Id}_{Guid.NewGuid()}{extension.ToLowerInvariant()}";

                using var memoryStream = new MemoryStream();
                await uploadFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var publicUrl = await this.avatarStorage.UploadAvatarAsync(fileName, memoryStream);

                user.AvatarUrl = publicUrl;
                await this.userManager.UpdateAsync(user);

                return ServiceResult.Ok("Avatar uploaded successfully.", new { avatarUrl = publicUrl });
            }
            catch (Exception ex)
            {
                return ServiceResult.InternalServerError($"Avatar upload error: {ex.Message}");
            }
        }

        public async Task<ServiceResult> ChangePasswordAsync(ClaimsPrincipal userPrincipal, ChangePasswordRequest request)
        {
            var user = await this.userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return ServiceResult.Unauthorized();
            }

            var result = await this.userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.BadRequest(errors);
            }

            return ServiceResult.Ok("Password successfully updated.");
        }

        public async Task<ServiceResult> DeleteAccountAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await this.userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return ServiceResult.Unauthorized();
            }

            await this.guardianService.RevokeAllSessionsAsync(user.Id);

            var result = await this.userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.BadRequest(errors);
            }

            await this.signInManager.SignOutAsync();
            return ServiceResult.Ok("Account successfully deleted.");
        }

        public async Task<ServiceResult> GetGuardianDashboardAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await this.userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return ServiceResult.Unauthorized();
            }

            var activeSessions = await this.guardianService.GetActiveSessionsAsync(user.Id);
            var loginActivities = await this.guardianService.GetLoginActivitiesAsync(user.Id);

            var failedAttempts = loginActivities.Count(a => !a.IsSuccess && a.CreatedAt >= DateTime.UtcNow.AddDays(-1));
            var lastSuccess = loginActivities.FirstOrDefault(a => a.IsSuccess)?.CreatedAt;

            var dashboard = new GuardianDashboardViewModel
            {
                SecurityStatus = new SecurityStatusViewModel
                {
                    StatusLevel = failedAttempts > 3 ? "Warning" : "Good",
                    ActiveSessionsCount = activeSessions.Count,
                    FailedAttemptsLast24Hours = failedAttempts,
                    LastSuccessfulLogin = lastSuccess,
                },
                RecentActivities = loginActivities.Select(a => new LoginActivityViewModel
                {
                    Id = a.Id,
                    ActivityType = a.ActivityType,
                    Device = a.Device,
                    OperatingSystem = a.OperatingSystem,
                    Browser = a.Browser,
                    IpAddress = a.IpAddress,
                    Country = a.Country,
                    IsSuccess = a.IsSuccess,
                    CreatedAt = a.CreatedAt,
                }).ToList(),
                ActiveSessions = activeSessions.Select(s => new ActiveSessionViewModel
                {
                    Id = s.Id,
                    DeviceName = s.DeviceName,
                    OperatingSystem = s.OperatingSystem,
                    Browser = s.Browser,
                    IpAddress = s.IpAddress,
                    Country = s.Country,
                    IsCurrent = s.IsCurrent,
                    LastActivityAt = s.LastActivityAt
                }).ToList(),
            };

            return ServiceResult.Ok(data: dashboard);
        }

        public async Task<ServiceResult> RevokeSessionAsync(ClaimsPrincipal userPrincipal, Guid sessionId)
        {
            var user = await this.userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return ServiceResult.Unauthorized();
            }

            await this.guardianService.RevokeSessionAsync(sessionId, user.Id);
            return ServiceResult.Ok("Session revoked.");
        }

        public async Task<ServiceResult> RevokeAllSessionsAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await this.userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return ServiceResult.Unauthorized();
            }

            await this.guardianService.RevokeAllSessionsAsync(user.Id);
            return ServiceResult.Ok("All other sessions revoked.");
        }
    }
}

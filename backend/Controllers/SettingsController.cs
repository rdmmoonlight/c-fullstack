// <copyright file="SettingsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Controllers
{
    using AumoBackend.DTOs;
    using AumoBackend.Models;
    using AumoBackend.Services.Settings;
    using AumoBackend.Services.Storage;
    using AumoBackend.Validators.Common;
    using AumoBackend.ViewModels;
    using FluentValidation;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("/settings")]
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]

    public class SettingsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IGuardianService guardianService;
        private readonly IAvatarStorage avatarStorage;
        private readonly IValidator<ChangePasswordRequest> changePasswordValidator;
        private readonly IValidator<UpdateProfileRequest> updateProfileValidator;
        private readonly IValidator<IFormFile> avatarFileValidator;

        public SettingsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IGuardianService guardianService,
            IAvatarStorage avatarStorage,
            IValidator<ChangePasswordRequest> changePasswordValidator,
            IValidator<UpdateProfileRequest> updateProfileValidator,
            IValidator<IFormFile> avatarFileValidator)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.guardianService = guardianService;
            this.avatarStorage = avatarStorage;
            this.changePasswordValidator = changePasswordValidator;
            this.updateProfileValidator = updateProfileValidator;
            this.avatarFileValidator = avatarFileValidator;
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var validation = await this.updateProfileValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                return this.BadRequest(validation.ToErrorResponse());
            }

            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.Unauthorized();
            }

            if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
            {
                var setUserNameResult = await this.userManager.SetUserNameAsync(user, request.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    var errors = string.Join(", ", setUserNameResult.Errors.Select(e => e.Description));
                    return this.BadRequest(new { success = false, message = errors });
                }
            }

            if (request.PhoneNumber != user.PhoneNumber)
            {
                var setPhoneResult = await this.userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    var errors = string.Join(", ", setPhoneResult.Errors.Select(e => e.Description));
                    return this.BadRequest(new { success = false, message = errors });
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
                return this.BadRequest(new { success = false, message = errors });
            }

            return this.Ok(new { success = true, message = "Profile updated successfully." });
        }

        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile? file, [FromForm] IFormFile? avatar)
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.Unauthorized();
            }

            // Flexible Binding
            var uploadFile = file ?? avatar ?? (this.Request.Form.Files.Count > 0 ? this.Request.Form.Files[0] : null);

            if (uploadFile == null || uploadFile.Length == 0)
            {
                return this.BadRequest(new { success = false, message = "No file uploaded. Ensure the form-data key is named 'file' or 'avatar'." });
            }

            var fileValidation = await this.avatarFileValidator.ValidateAsync(uploadFile);
            if (!fileValidation.IsValid)
            {
                return this.BadRequest(fileValidation.ToErrorResponse());
            }

            var extension = Path.GetExtension(uploadFile.FileName);

            if (!this.avatarStorage.IsConfigured)
            {
                return this.StatusCode(500, new { success = false, message = "Neon bucket belum dikonfigurasi di server." });
            }

            try
            {
                var fileName = $"avatar_{user.Id}_{Guid.NewGuid()}{extension.ToLowerInvariant()}";

                using var memoryStream = new MemoryStream();
                await uploadFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var publicUrl = await this.avatarStorage.UploadAvatarAsync(fileName, memoryStream);

                // Simpan URL avatar ke database user
                user.AvatarUrl = publicUrl;
                await this.userManager.UpdateAsync(user);

                return this.Ok(new
                {
                    success = true,
                    message = "Avatar uploaded successfully.",
                    avatarUrl = publicUrl,
                });
            }
            catch (Exception ex)
            {
                return this.StatusCode(500, new { success = false, message = $"Avatar upload error: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var validation = await this.changePasswordValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                return this.BadRequest(validation.ToErrorResponse());
            }

            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.Unauthorized();
            }

            var result = await this.userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return this.BadRequest(new { success = false, message = errors });
            }

            return this.Ok(new { success = true, message = "Password successfully updated." });
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.Unauthorized();
            }

            await this.guardianService.RevokeAllSessionsAsync(user.Id);

            var result = await this.userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return this.BadRequest(new { success = false, message = errors });
            }

            await this.signInManager.SignOutAsync();
            return this.Ok(new { success = true, message = "Account successfully deleted." });
        }

        [HttpGet("guardian/dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.Unauthorized();
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

            return this.Ok(new { success = true, data = dashboard });
        }

        [HttpPost("guardian/revoke-session/{sessionId}")]
        public async Task<IActionResult> RevokeSession(Guid sessionId)
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.Unauthorized();
            }

            await this.guardianService.RevokeSessionAsync(sessionId, user.Id);
            return this.Ok(new { success = true, message = "Session revoked." });
        }

        [HttpPost("guardian/revoke-all-sessions")]
        public async Task<IActionResult> RevokeAllSessions()
        {
            var user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.Unauthorized();
            }

            await this.guardianService.RevokeAllSessionsAsync(user.Id);
            return this.Ok(new { success = true, message = "All other sessions revoked." });
        }
    }
}

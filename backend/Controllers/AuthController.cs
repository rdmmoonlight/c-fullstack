namespace AumoBackend.Controllers;

using AumoBackend.DTOs;
using AumoBackend.Helpers;
using AumoBackend.Models;
using AumoBackend.Services.Auth;
using AumoBackend.Validators.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("/auth")]
[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
[EnableRateLimiting("auth-strict")]
public class AuthController : ControllerBase
{
    private readonly IAuthService authService;
    private readonly SignInManager<ApplicationUser> signInManager;

    public AuthController(IAuthService authService, SignInManager<ApplicationUser> signInManager)
    {
        this.authService = authService;
        this.signInManager = signInManager;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await this.authService.ValidateLoginAsync(request);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        var ip = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var headerUserAgent = this.Request.Headers["User-Agent"].ToString();

        var result = await this.authService.ProcessLoginAsync(request, ip, headerUserAgent);

        if (result == null)
        {
            return this.Unauthorized(new { success = false, message = "Invalid email/username or password." });
        }

        if (result.IsLockedOut)
        {
            return this.StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                success = false,
                message = result.Message,
                lockoutEnd = result.LockoutEnd,
            });
        }

        if (request.IsMobileClient)
        {
            return this.Ok(new
            {
                success = true,
                message = result.Message,
                userId = result.UserId,
                fullName = result.FullName,
                avatarUrl = result.AvatarUrl,
                token = result.Token,
            });
        }

        return this.Ok(new
        {
            success = true,
            message = result.Message,
            userId = result.UserId,
            fullName = result.FullName,
            avatarUrl = result.AvatarUrl,
        });
    }

    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var validation = await this.authService.ValidateGoogleLoginAsync(request);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        var result = await this.authService.ProcessGoogleLoginAsync(request);

        if (result == null)
        {
            return this.Unauthorized(new { success = false, message = "Invalid Google ID Token or account could not be created." });
        }

        if (request.IsMobileClient)
        {
            return this.Ok(new
            {
                success = true,
                message = result.Message,
                userId = result.UserId,
                fullName = result.FullName,
                avatarUrl = result.AvatarUrl,
                token = result.Token,
            });
        }

        return this.Ok(new
        {
            success = true,
            message = result.Message,
            userId = result.UserId,
            fullName = result.FullName,
            avatarUrl = result.AvatarUrl,
        });
    }

    [HttpGet("google-login")]
    [AllowAnonymous]
    public IActionResult GoogleLoginRedirect(string redirectTo = "/home")
    {
        // Validasi URL awal sebelum dilewatkan ke Google Challenge
        var safeRedirectUrl = this.Url.GetSafeRedirectUrl(redirectTo);

        var redirectUrl = this.Url.Action(nameof(this.GoogleCallback), new { redirectTo = safeRedirectUrl });
        var properties = this.authService.ConfigureGoogleRedirectProperties(redirectUrl!);

        return this.Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(string redirectTo = "/home", string? remoteError = null)
    {
        if (remoteError != null)
        {
            return this.BadRequest(new { success = false, message = $"Error from external provider: {remoteError}" });
        }

        var info = await this.signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return this.BadRequest(new { success = false, message = "Error loading external login information." });
        }

        var error = await this.authService.ProcessGoogleCallbackAsync(info.Principal, info.LoginProvider, info.ProviderKey);
        if (error != null)
        {
            return this.BadRequest(new { success = false, message = error });
        }

        // Validasi ulang URL sebelum eksekusi Redirect (cegah open redirect)
        return this.Redirect(this.Url.GetSafeRedirectUrl(redirectTo));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await this.authService.GetUserProfileAsync(this.User);

        if (profile == null)
        {
            return this.Unauthorized(new { success = false, message = "User session expired or user not found." });
        }

        var profileData = new
        {
            id = profile.Id,
            userId = profile.UserId,
            email = profile.Email,
            userName = profile.UserName,
            fullName = profile.FullName,
            phoneNumber = profile.PhoneNumber,
            avatarUrl = profile.AvatarUrl,
            bio = profile.Bio,
            roles = profile.Roles,
            customClaims = profile.CustomClaims,
        };

        return this.Ok(new
        {
            success = true,
            data = profileData,
            id = profile.Id,
            userId = profile.UserId,
            email = profile.Email,
            userName = profile.UserName,
            fullName = profile.FullName,
            phoneNumber = profile.PhoneNumber,
            avatarUrl = profile.AvatarUrl,
            bio = profile.Bio,
            roles = profile.Roles,
            customClaims = profile.CustomClaims,
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await this.authService.LogoutAsync();
        return this.Ok(new { success = true, message = "Logged out successfully." });
    }
}

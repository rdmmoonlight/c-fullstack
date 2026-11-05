namespace AumoBackend.Services.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AumoBackend.DTOs;
using AumoBackend.Extensions;
using AumoBackend.Models;
using AumoBackend.Services.Settings;
using AumoBackend.ViewModels;
using FluentValidation;
using FluentValidation.Results;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IGuardianService guardianService,
    IConfiguration configuration,
    IValidator<LoginRequest> loginValidator,
    IValidator<GoogleLoginRequest> googleLoginValidator) : IAuthService
{
    public async Task<ValidationResult> ValidateLoginAsync(LoginRequest request)
    {
        return await loginValidator.ValidateAsync(request);
    }

    public async Task<ValidationResult> ValidateGoogleLoginAsync(GoogleLoginRequest request)
    {
        return await googleLoginValidator.ValidateAsync(request);
    }

    private async Task<AuthResponseDto> BuildLockedOutResponseAsync(ApplicationUser user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
        return new AuthResponseDto
        {
            Success = false,
            IsLockedOut = true,
            Message = "Account is locked due to multiple failed login attempts.",
            LockoutEnd = lockoutEnd?.UtcDateTime,
        };
    }

    public async Task<AuthResponseDto?> ProcessLoginAsync(LoginRequest request, string ipAddress, string headerUserAgent)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
                   ?? await userManager.FindByNameAsync(request.Email);

        if (user == null)
        {
            return null;
        }

        var safeUserAgent = !string.IsNullOrWhiteSpace(request.UserAgent)
            ? request.UserAgent
            : (!string.IsNullOrWhiteSpace(headerUserAgent) ? headerUserAgent : "Aumo Client");

        var isMobile = request.IsMobileClient;
        string deviceCategory = isMobile ? "Mobile" : "Web";
        string osValue = !string.IsNullOrWhiteSpace(request.OperatingSystem) ? request.OperatingSystem : deviceCategory;

        if (await userManager.IsLockedOutAsync(user))
        {
            return await this.BuildLockedOutResponseAsync(user);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            await guardianService.CreateLoginActivityAsync(
                user.Id, "Locked Out Login Attempt", deviceCategory,
                isMobile ? "Mobile App" : "Web Browser", ipAddress, "ID", false,
                osValue, safeUserAgent);

            return await this.BuildLockedOutResponseAsync(user);
        }

        if (!signInResult.Succeeded)
        {
            await guardianService.CreateLoginActivityAsync(
                user.Id, "Failed Login", deviceCategory,
                isMobile ? "Mobile App" : "Web Browser", ipAddress, "ID", false,
                osValue, safeUserAgent);

            return null;
        }

        string? jwtToken = null;

        if (!isMobile)
        {
            // ✅ Gunakan PasswordSignInAsync langsung agar Cookie Identity ter-set sempurna dengan Claims bawaan
            await signInManager.SignInAsync(user, isPersistent: request.RememberMe);
        }
        else
        {
            jwtToken = await this.GenerateJwtTokenAsync(user);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        await guardianService.CreateSessionAsync(
            user.Id,
            deviceName: deviceCategory,
            osValue,
            isMobile ? "Mobile App" : "Web Browser",
            ipAddress,
            "ID",
            isMobile ? "JWT_BEARER" : "COOKIE_SESSION",
            safeUserAgent);

        await guardianService.CreateLoginActivityAsync(
            user.Id, "Interactive Login", deviceCategory,
            isMobile ? "Mobile App" : "Web Browser", ipAddress, "ID", true,
            osValue, safeUserAgent);

        return new AuthResponseDto
        {
            Success = true,
            Message = isMobile ? "Mobile login successful." : "Web login successful.",
            UserId = user.Id.ToString(),
            FullName = user.FullName ?? user.UserName ?? "User",
            AvatarUrl = user.AvatarUrl,
            Token = jwtToken,
        };
    }

    public async Task<AuthResponseDto?> ProcessGoogleLoginAsync(GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return null;
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
        }
        catch
        {
            return null;
        }

        var info = new UserLoginInfo("Google", payload.Subject, "Google");
        var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

        if (user == null)
        {
            user = await userManager.FindByEmailAsync(payload.Email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = payload.Email,
                    Email = payload.Email,
                    EmailConfirmed = true,
                    FullName = payload.Name,
                    AvatarUrl = payload.Picture,
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return null;
                }

                await userManager.AddToRoleAsync(user, "User");
            }

            await userManager.AddLoginAsync(user, info);
        }

        if (!request.IsMobileClient)
        {
            await signInManager.SignInAsync(user, isPersistent: true);
            return new AuthResponseDto
            {
                Success = true,
                Message = "Google login successful (Cookie session established).",
                UserId = user.Id.ToString(),
                FullName = user.FullName ?? user.UserName ?? "User",
                AvatarUrl = user.AvatarUrl,
            };
        }

        var token = await this.GenerateJwtTokenAsync(user);
        return new AuthResponseDto
        {
            Success = true,
            Message = "Google login successful.",
            UserId = user.Id.ToString(),
            FullName = user.FullName ?? user.UserName ?? "User",
            AvatarUrl = user.AvatarUrl,
            Token = token,
        };
    }

    public AuthenticationProperties ConfigureGoogleRedirectProperties(string redirectUrl)
    {
        return signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
    }

    public async Task<string?> ProcessGoogleCallbackAsync(ClaimsPrincipal principal, string loginProvider, string providerKey)
    {
        var result = await signInManager.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent: true, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            return null;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return "Email claim not received from Google.";
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = principal.FindFirstValue(ClaimTypes.Name) ?? email,
                AvatarUrl = principal.FindFirstValue("picture"),
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return "Failed to create user from Google callback.";
            }

            await userManager.AddToRoleAsync(user, "User");
        }

        await userManager.AddLoginAsync(user, new UserLoginInfo(loginProvider, providerKey, loginProvider));
        await signInManager.SignInAsync(user, isPersistent: true);

        return null;
    }

    public async Task<UserProfileViewModel?> GetUserProfileAsync(ClaimsPrincipal userPrincipal)
    {
        // 1. Coba ambil user via SignInManager / ClaimsPrincipal bawaan
        var user = await userManager.GetUserAsync(userPrincipal);

        // 2. Fallback pencarian manual jika claims diparsing beda oleh Jwt/Cookie middleware
        if (user == null)
        {
            var userIdStr = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? userPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                         ?? userPrincipal.FindFirstValue("sub")
                         ?? userPrincipal.FindFirstValue("id");

            if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userGuid))
            {
                user = await userManager.FindByIdAsync(userGuid.ToString());
            }
        }

        if (user == null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var userClaims = await userManager.GetClaimsAsync(user);

        return new UserProfileViewModel
        {
            Id = user.Id,
            UserId = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Roles = roles,
            CustomClaims = userClaims,
        };
    }

    public async Task LogoutAsync()
    {
        await signInManager.SignOutAsync();
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var jwt = JwtSettings.From(configuration);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName ?? user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var userClaims = await userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

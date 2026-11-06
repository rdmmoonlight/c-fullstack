// <copyright file="IAuthService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Auth;

using System.Security.Claims;
using System.Threading.Tasks;
using AumoBackend.DTOs;
using AumoBackend.ViewModels;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication;

public interface IAuthService
{
    Task<ValidationResult> ValidateLoginAsync(LoginRequest request);

    Task<ValidationResult> ValidateGoogleLoginAsync(GoogleLoginRequest request);

    Task<AuthResponseDto?> ProcessLoginAsync(LoginRequest request, string ipAddress, string headerUserAgent);

    Task<AuthResponseDto?> ProcessGoogleLoginAsync(GoogleLoginRequest request);

    /// <summary>Membuat akun baru. Mengembalikan daftar error (kosong bila berhasil).</summary>
    Task<IReadOnlyList<string>> RegisterAsync(RegisterRequest request);

    AuthenticationProperties ConfigureGoogleRedirectProperties(string redirectUrl);

    Task<string?> ProcessGoogleCallbackAsync(ClaimsPrincipal principal, string loginProvider, string providerKey);

    Task<UserProfileViewModel?> GetUserProfileAsync(ClaimsPrincipal userPrincipal);

    Task LogoutAsync();
}

using System.Text;
using AumoBackend.Data;
using AumoBackend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AumoBackend.Extensions;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityAndAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Registrasi ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // 2. Pengaturan JWT: sumber yang sama dengan pembuat token (AuthService). Tanpa kunci bawaan.
        var jwt = JwtSettings.From(configuration);

        // 3. Konfigurasi Authentication Schemes (Cookie default + JwtBearer)
        services.AddAuthentication(options =>
        {
            // Set Cookie sebagai skema default untuk browser/web
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
        })
        // 🔑 DAFTARKAN HANDLER 'Bearer' SECARA EKSPLISIT DAN SELALU DILAKUKAN
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudiences = new[] { jwt.Audience, jwt.Issuer }.Distinct().ToArray(),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
            };
        });

        // 4. Konfigurasi Cookie HTTP-Only untuk Web Client
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        return services;
    }
}

using System.Globalization;
using System.Security.Claims;
using AumoBackend.Data;
using AumoBackend.Extensions;
using AumoBackend.Middlewares;
using AumoBackend.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace AumoBackend;

public partial class Program
{
    private static readonly string[] RootMethods = ["GET", "HEAD"];

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Failed to run automatic database migration or seeding.")]
    private static partial void LogMigrationFailed(Microsoft.Extensions.Logging.ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Unhandled exception on {Path}")]
    private static partial void LogUnhandledException(Microsoft.Extensions.Logging.ILogger logger, Exception exception, PathString path);

    public static async Task Main(string[] args)
    {
        // Bootstrap Logger dengan ANSI Theme agar log saat startup di Render langsung berwarna
        Serilog.Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(
                theme: CustomLogThemes.VividTheme,
                applyThemeToRedirectedOutput: true,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateBootstrapLogger();

        try
        {
            Serilog.Log.Information("Starting AumoBackend web application...");
            var builder = WebApplication.CreateBuilder(args);

            // Konfigurasi Host Serilog Utama
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Code,
                    applyThemeToRedirectedOutput: true,
                    outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}"));

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    var redirectUriBuilder = new UriBuilder(context.RedirectUri) { Port = -1 };
                    context.Response.Redirect(redirectUriBuilder.Uri.ToString());
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                    var redirectUriBuilder = new UriBuilder(context.RedirectUri) { Port = -1 };
                    context.Response.Redirect(redirectUriBuilder.Uri.ToString());
                    return Task.CompletedTask;
                };
            });

            builder.Services.AddHttpClient();
            builder.Services.AddDatabaseServices(builder.Configuration);
            builder.Services.AddIdentityAndAuthServices(builder.Configuration);
            builder.Services.AddCacheAndInfrastructure(builder.Configuration);
            builder.Services.AddApplicationServices();

            builder.Services.AddControllers(options =>
            {
                options.Conventions.Add(new ApiVersionPrefixConvention());
            });

            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
                    var serverUrl = configuration["Scalar:ServerUrl"] ?? "https://aumoweb.onrender.com";
                    document.Servers = [new OpenApiServer { Url = serverUrl, Description = "Production Server" }];

                    var enableAuth = configuration.GetValue<bool>("Jwt:EnableScalarAuth", true);
                    if (enableAuth)
                    {
                        var schemeName = configuration["Jwt:SchemeName"] ?? JwtBearerDefaults.AuthenticationScheme;
                        var description = configuration["Jwt:Description"] ?? "Masukkan JWT Token dengan format: Bearer {token}";
                        var securityScheme = new OpenApiSecurityScheme
                        {
                            Name = "Authorization",
                            Description = description,
                            In = ParameterLocation.Header,
                            Type = SecuritySchemeType.Http,
                            Scheme = schemeName.ToLowerInvariant(),
                            BearerFormat = "JWT",
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = schemeName }
                        };
                        document.Components ??= new OpenApiComponents();
                        document.Components.SecuritySchemes[schemeName] = securityScheme;
                        document.SecurityRequirements.Add(new OpenApiSecurityRequirement { [securityScheme] = [] });
                    }
                    return Task.CompletedTask;
                });
            });

            builder.Services.AddHealthChecks();
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            var app = builder.Build();

            app.UseForwardedHeaders();
            if (!app.Environment.IsDevelopment())
            {
                app.Use(async (context, next) =>
                {
                    context.Request.Scheme = "https";
                    await next();
                });
            }

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    await context.Database.MigrateAsync();

                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                    string[] defaultRoles = ["Admin", "User", "Manager"];
                    foreach (var roleName in defaultRoles)
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            var role = new IdentityRole<Guid> { Name = roleName };
                            await roleManager.CreateAsync(role);
                            if (roleName == "Admin")
                            {
                                await roleManager.AddClaimAsync(role, new Claim("Permission", "Transaction.Approve"));
                                await roleManager.AddClaimAsync(role, new Claim("Permission", "User.Manage"));
                            }
                        }
                    }
                    var initialAdminEmail = app.Configuration["AdminSettings:InitialAdminEmail"];
                    if (!string.IsNullOrWhiteSpace(initialAdminEmail))
                    {
                        var adminUser = await userManager.FindByEmailAsync(initialAdminEmail);
                        if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
                        {
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
                    LogMigrationFailed(logger, ex);
                }
            }

            if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
            else
            {
                app.UseHsts();
                app.UseExceptionHandler(exceptionHandlerApp =>
                {
                    exceptionHandlerApp.Run(async context =>
                    {
                        var logger = context.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
                        var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                        if (exceptionHandlerFeature?.Error is { } error) LogUnhandledException(logger, error, context.Request.Path);
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new { success = false, message = "A server error occurred. Please try again in a moment." });
                    });
                });
            }

            app.UseStaticFiles();

            app.UseCustomRequestLogging();

            app.UseRouting();
            app.UseCors("AllowFrontend");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapOpenApi();
            var scalarTitle = app.Configuration["Scalar:Title"] ?? "Aumo Web API Reference";
            var scalarRoutePrefix = app.Configuration["Scalar:RoutePrefix"] ?? "/reference";
            app.MapScalarApiReference(scalarRoutePrefix, options => { options.WithTitle(scalarTitle).WithTheme(ScalarTheme.Moon).WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch); });

            app.MapMethods("/", RootMethods, () => Results.Ok(new { service = "AumoFinance API", status = "Online", timestamp = DateTime.UtcNow })).RequireRateLimiting("global");
            app.MapHealthChecks("/health");
            app.MapControllers();

            await app.RunAsync();
        }
        catch (Exception ex) { Serilog.Log.Fatal(ex, "AumoBackend terminated unexpectedly!"); }
        finally { Serilog.Log.CloseAndFlush(); }
    }
}

using AumoBackend.Services.ChartOfAccounts;
using AumoBackend.Services.Settings;
using AumoBackend.Services.Tools;
using AumoFinance.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

namespace AumoBlazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =====================================
            // 1. AUTHENTICATION & AUTHORIZATION (Cookie-Based)
            // =====================================
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "AumoFinance.Session";
                    options.LoginPath = "/auth/login";
                    options.LogoutPath = "/auth/logout";
                    options.AccessDeniedPath = "/auth/login";
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                });

            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();

            // =====================================
            // 2. BLAZOR CORE & CONTROLLERS
            // =====================================
            builder.Services.AddControllers();
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // =====================================
            // 3. FRONTEND SERVICES & HTTP CLIENTS
            // =====================================
            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache();
            builder.Services.AddHealthChecks();

            // Registered Frontend API/Service Layer Integrations
            builder.Services.AddHttpClient("MarketApiClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AumoFinance/1.0");
            });

            // Service Registrations (Connecting to Backend API)
            builder.Services.AddScoped<Services.ChartOfAccounts.IChartOfAccountsService, Services.ChartOfAccounts.ChartOfAccountsService>();
            builder.Services.AddScoped<Services.JournalEntry.IJournalEntryService, Services.JournalEntry.JournalEntryService>();
            builder.Services.AddScoped<Services.Settings.ISettingsService, Services.Settings.SettingsService>();
            builder.Services.AddScoped<Services.Tools.IJournalImportService, Services.Tools.JournalImportService>();
            builder.Services.AddScoped<Services.Tools.IToolsService, Services.Tools.ToolsService>();

            // =====================================
            // 4. FORWARDED HEADERS (Render / Reverse Proxy)
            // =====================================
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // =====================================
            // BUILD APPLICATION
            // =====================================
            var app = builder.Build();

            // =====================================
            // 5. HTTP PIPELINE MIDDLEWARE
            // =====================================
            app.UseForwardedHeaders();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.Use(async (context, next) =>
                {
                    try
                    {
                        await next();
                    }
                    catch (Exception ex)
                    {
                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);

                        if (!context.Response.HasStarted)
                        {
                            context.Response.Clear();
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(new
                            {
                                success = false,
                                message = "Terjadi kesalahan pada aplikasi frontend. Silakan coba lagi."
                            });
                        }
                    }
                });
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAntiforgery();
            app.UseAuthentication();
            app.UseAuthorization();

            // =====================================
            // 6. ENDPOINTS & ROUTING
            // =====================================
            app.MapHealthChecks("/health");
            app.MapControllers();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // =====================================
            // 7. RUN APPLICATION
            // =====================================
            app.Run();
        }
    }
}
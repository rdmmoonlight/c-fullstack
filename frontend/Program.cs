using AumoBackend.Extensions;
using AumoBackend.Services.Auth;
using AumoFinance.Hubs;
using AumoFinance.Services;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;

namespace AumoFinance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =====================================
            // 1. BACKEND (SSOT) & SERVICES
            // =====================================
            builder.Services.AddHttpClient();
            builder.Services.AddDatabaseServices(builder.Configuration);
            builder.Services.AddIdentityAndAuthServices(builder.Configuration);
            builder.Services.AddCacheAndInfrastructure(builder.Configuration);
            builder.Services.AddApplicationServices();
            builder.Services.AddMudServices();
            builder.Services.AddValidatorsFromAssemblyContaining<AumoBackend.Program>();

            // Cookie sesi untuk UI
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/auth/login";
                options.LogoutPath = "/auth/logout";
                options.AccessDeniedPath = "/auth/login";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            // =====================================
            // 2. BLAZOR SERVER & SIGNALR HUB SERVICES
            // =====================================
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Diperlukan untuk custom SignalR Hubs (MarketHub)
            builder.Services.AddSignalR();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddHttpContextAccessor();

            // Glue khusus UI (State & Workflow)
            builder.Services.AddScoped<UserContext>();
            builder.Services.AddScoped<SelectedPeriodProvider>();
            builder.Services.AddScoped<JournalImportWorkflow>();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();

            // =====================================
            // 3. PIPELINE & MIDDLEWARE
            // =====================================
            app.UseForwardedHeaders();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();

            // =====================================
            // 4. ENDPOINTS MAPPING
            // =====================================
            app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

            app.MapPost("/auth/logout", async (IAuthService auth) =>
            {
                await auth.LogoutAsync();
                return Results.Redirect("/auth/login");
            }).DisableAntiforgery();

            // Custom SignalR Hub Endpoint
            app.MapHub<MarketHub>("/markethub");

            // Blazor Interactive Server Rendering
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
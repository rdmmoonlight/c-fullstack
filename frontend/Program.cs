using AumoBackend.Extensions;
using AumoBackend.Services.Auth;
using AumoBlazor.Hubs;
using AumoFinance;
using AumoFinance.Services;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;

namespace AumoBlazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =====================================
            // 1. BACKEND (SSOT): database, Identity, cache, dan seluruh service bisnis
            //    dipakai langsung dari project /backend. Frontend tidak menulis ulang logika apa pun.
            // =====================================
            builder.Services.AddHttpClient();
            builder.Services.AddDatabaseServices(builder.Configuration);
            builder.Services.AddIdentityAndAuthServices(builder.Configuration);
            builder.Services.AddCacheAndInfrastructure(builder.Configuration);
            builder.Services.AddApplicationServices();
            builder.Services.AddMudServices();
            builder.Services.AddValidatorsFromAssemblyContaining<AumoBackend.Program>();

            // Cookie sesi untuk UI: pengguna yang belum login diarahkan ke halaman login (bukan 401 seperti API).
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
            // 2. BLAZOR
            // =====================================
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSignalR();

            // Glue khusus UI (bukan logika bisnis)
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
            // 3. PIPELINE
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

            app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

            app.MapPost("/auth/logout", async (IAuthService auth) =>
            {
                await auth.LogoutAsync();
                return Results.Redirect("/auth/login");
            }).DisableAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Map Hub URL endpoint SignalR
            app.MapHub<MarketHub>("/markethub");
            app.Run();
        }
    }
}

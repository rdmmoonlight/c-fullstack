using System.Threading.RateLimiting;
using AumoBackend.Services;
using AumoBackend.Services.Auth;
using AumoBackend.Services.ChartOfAccounts;
using AumoBackend.Services.Dashboard;
using AumoBackend.Services.Health;
using AumoBackend.Services.Home;
using AumoBackend.Services.JournalEntries;
using AumoBackend.Services.Notifications;
using AumoBackend.Services.Periods;
using AumoBackend.Services.Reports.FinancialStatements;
using AumoBackend.Services.Reports.GeneralLedgers;
using AumoBackend.Services.Reports.Journals;
using AumoBackend.Services.Reports.Summary;
using AumoBackend.Services.Reports.TrialBalances;
using AumoBackend.Services.Reports.Worksheet;
using AumoBackend.Services.Settings;
using AumoBackend.Services.Storage;
using AumoBackend.Services.Tools;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

namespace AumoBackend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCacheAndInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks();
        services.AddMemoryCache();

        var redisUrl = configuration["Redis:Url"];
        if (!string.IsNullOrWhiteSpace(redisUrl))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                if (redisUrl.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                    redisUrl.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(redisUrl);
                    var isSsl = uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase);
                    var userInfo = uri.UserInfo.Split(':');
                    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : Uri.UnescapeDataString(userInfo[0]);
                    var port = uri.Port > 0 ? uri.Port : (isSsl ? 6380 : 6379);

                    options.ConfigurationOptions = new ConfigurationOptions
                    {
                        EndPoints = { { uri.Host, port } },
                        Password = password,
                        Ssl = isSsl,
                        AbortOnConnectFail = false,
                    };
                }
                else
                {
                    options.Configuration = redisUrl;
                }

                options.InstanceName = "AumoFinance_";
            });

            Log.Information("Distributed Caching (Redis) terkonfigurasi.");
        }
        else
        {
            services.AddDistributedMemoryCache();
            Log.Information("Redis:Url tidak dikonfigurasi. Menggunakan Fallback Distributed Memory Cache.");
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("global", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy("auth-strict", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddSingleton<IAvatarStorage, NeonBucketStorage>();

        services.AddScoped<IGuardianService, GuardianService>();
        services.AddScoped<ITransactionNumberService, TransactionNumberService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChartOfAccountsService, ChartOfAccountsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IHomeService, HomeService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
        services.AddScoped<INotificationsService, NotificationsService>();
        services.AddScoped<IPeriodsService, PeriodsService>();
        services.AddScoped<IGeneralLedgersService, GeneralLedgersService>();
        services.AddScoped<IFinancialStatementsService, FinancialStatementsService>();
        services.AddScoped<IJournalsService, JournalsService>();
        services.AddScoped<ITrialBalancesService, TrialBalancesService>();
        services.AddScoped<IWorksheetService, WorksheetService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ISummaryService, SummaryService>();
        services.AddScoped<IToolsService, ToolsService>();

        services.AddHttpClient<IMarketDataService, MarketDataService>();

        return services;
    }
}

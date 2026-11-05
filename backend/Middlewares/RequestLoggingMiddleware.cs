using System.Diagnostics;

namespace AumoBackend.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            LogRequest(context, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static void LogRequest(HttpContext context, double elapsedMs)
    {
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;
        var statusCode = context.Response.StatusCode;

        const string reset = "\x1b[0m";
        const string bold = "\x1b[1m";

        // Warna HTTP Method
        var methodColor = method switch
        {
            "GET" => "\x1b[96m",     // Cyan
            "POST" => "\x1b[92m",    // Hijau
            "PUT" => "\x1b[93m",     // Kuning
            "PATCH" => "\x1b[33m",   // Oranye/Kuning Gelap
            "DELETE" => "\x1b[91m",  // Merah
            _ => "\x1b[97m"
        };

        // Warna Status Code
        var statusColor = statusCode switch
        {
            >= 200 and < 300 => "\x1b[92m", // Hijau
            >= 300 and < 400 => "\x1b[96m", // Cyan
            >= 400 and < 500 => "\x1b[93m", // Kuning
            >= 500 => "\x1b[91m",           // Merah
            _ => "\x1b[97m"
        };

        // Format string langsung menyertakan ANSI code agar tidak di-escape Serilog
        var formattedLog = $"[INF] HTTP {methodColor}{bold}{method}{reset} {path} responded {statusColor}{bold}{statusCode}{reset} in {elapsedMs:0.0000} ms";

        // Tulis langsung ke Console Standard Output
        Console.WriteLine(formattedLog);
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}

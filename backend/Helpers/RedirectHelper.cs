using Microsoft.AspNetCore.Mvc;

namespace AumoBackend.Helpers;

public static class RedirectHelper
{
    public static string GetSafeRedirectUrl(this IUrlHelper urlHelper, string? targetUrl)
    {
        const string defaultUrl = "/home";

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return defaultUrl;
        }

        if (urlHelper.IsLocalUrl(targetUrl))
        {
            return targetUrl;
        }

        if (Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri) && AllowedOrigins.IsAllowed(uri))
        {
            return targetUrl;
        }

        return defaultUrl;
    }
}

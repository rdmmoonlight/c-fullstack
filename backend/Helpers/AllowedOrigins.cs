namespace AumoBackend.Helpers;

public static class AllowedOrigins
{
    public static readonly string[] All =
    {
        "http://localhost:3000",
        "https://aumoweb.vercel.app",
        "https://aumoweb.onrender.com"
    };

    public static bool IsAllowed(Uri uri)
    {
        var origin = $"{uri.Scheme}://{uri.Authority}";
        return All.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}

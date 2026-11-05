namespace AumoBackend.Extensions;

/// <summary>
/// Satu-satunya sumber pengaturan JWT. Dipakai oleh pembuat token (AuthService) dan validator
/// (JwtBearer) agar kunci, issuer, dan audience selalu sama.
/// </summary>
public sealed record JwtSettings(string SigningKey, string Issuer, string Audience)
{
    public static JwtSettings From(IConfiguration configuration)
    {
        // "Jwt:SigningKey" adalah nama utama; "Jwt:Key" diterima sebagai nama lama.
        var signingKey = configuration["Jwt:SigningKey"] ?? configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing. Set environment variable Jwt__SigningKey (minimal 32 karakter).");
        }

        var issuer = configuration["Jwt:Issuer"] ?? "AumoFinanceApp";

        // Token lama memakai issuer sebagai audience, jadi itu tetap menjadi default.
        var audience = configuration["Jwt:Audience"] ?? issuer;

        return new JwtSettings(signingKey, issuer, audience);
    }
}

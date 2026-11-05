// <copyright file="EmailTemplates.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Helpers;

public static class EmailTemplates
{
    private const string AccentColor = "#0d6efd";
    private const string DarkColor = "#181818";
    private const string MutedColor = "#6c757d";

    public static string EmailConfirmation(string? fullName, string confirmUrl) => Layout(
        "Confirm your email to activate your Aumo Finance account.",
        "Confirm your email",
        $@"<p>Hi {(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName)},</p><p>Thanks for signing up for Aumo Finance. Confirm your email address to activate your account.</p>",
        "Confirm Email Address",
        confirmUrl);

    public static string PasswordReset(string? fullName, string resetUrl) => Layout(
        "Reset the password for your Aumo Finance account.",
        "Reset your password",
        $@"<p>Hi {(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName)},</p><p>We received a request to reset the password for your Aumo Finance account.</p>",
        "Reset Password",
        resetUrl);

    private static string Layout(string previewText, string heading, string bodyHtml, string buttonText, string buttonUrl)
    {
        return $@"<!DOCTYPE html><html><body style=""background-color:#f2f3f5;font-family:Segoe UI,Helvetica,Arial,sans-serif;""><div style=""display:none;"">{previewText}</div><table width=""100%"" style=""padding:32px 16px;""><tr><td align=""center""><table style=""max-width:480px;background:#fff;border-radius:8px;""><tr><td style=""background:{DarkColor};padding:20px 32px;color:#fff;font-weight:700;"">Aumo Finance</td></tr><tr><td style=""padding:32px;""><h1>{heading}</h1>{bodyHtml}<a href=""{buttonUrl}"" style=""display:inline-block;padding:12px 28px;background:{AccentColor};color:#fff;text-decoration:none;border-radius:6px;"">{buttonText}</a><p style=""color:{MutedColor};font-size:12px;"">Or copy: <a href=""{buttonUrl}"">{buttonUrl}</a></p></td></tr><tr><td style=""padding:20px 32px;background:#f8f9fa;font-size:12px;color:{MutedColor};"">&copy; {DateTime.UtcNow.Year} Aumo Finance</td></tr></table></td></tr></table></body></html>";
    }
}

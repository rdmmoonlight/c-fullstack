using Serilog.Sinks.SystemConsole.Themes;

public static class CustomLogThemes
{
    // Definisi Kode Warna ANSI
    private const string Reset = "\x1b[0m";
    private const string BrightRed = "\x1b[91m";
    private const string BrightGreen = "\x1b[92m";
    private const string BrightYellow = "\x1b[93m";
    private const string BrightCyan = "\x1b[96m";
    private const string BrightMagenta = "\x1b[95m";
    private const string WhiteBold = "\x1b[1;37m";
    private const string Gray = "\x1b[90m";

    public static AnsiConsoleTheme VividTheme { get; } = new AnsiConsoleTheme(
        new Dictionary<ConsoleThemeStyle, string>
        {
            // Level Log
            [ConsoleThemeStyle.LevelVerbose] = Gray,
            [ConsoleThemeStyle.LevelDebug] = Gray,
            [ConsoleThemeStyle.LevelInformation] = BrightCyan,  // [INF] Biru Cyan Terang
            [ConsoleThemeStyle.LevelWarning] = BrightYellow,   // [WRN] Kuning Terang
            [ConsoleThemeStyle.LevelError] = BrightRed,        // [ERR] Merah Terang
            [ConsoleThemeStyle.LevelFatal] = WhiteBold + "\x1b[41m", // Teks Putih Bold, background Merah

            // Token & Parameter Message (HTTP Verb, Status Code, Path, dll.)
            [ConsoleThemeStyle.Text] = WhiteBold,              // Teks log umum
            [ConsoleThemeStyle.SecondaryText] = Gray,          // Karakter pemisah / simbol
            [ConsoleThemeStyle.String] = BrightGreen,          // Parameter bertipe String
            [ConsoleThemeStyle.Number] = BrightMagenta,        // Parameter Angka (seperti 200 OK, ms)
            [ConsoleThemeStyle.Boolean] = BrightYellow,        // Parameter True/False
            [ConsoleThemeStyle.Null] = Gray,                   // Parameter Null
            [ConsoleThemeStyle.Scalar] = BrightGreen,          // Value scalar lainnya
        });
}
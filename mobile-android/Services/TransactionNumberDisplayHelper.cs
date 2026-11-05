using System.Text.RegularExpressions;

namespace AumoFinance.Services;

/// <summary>
/// Backend menyimpan &amp; mengirim TransactionNumber tanpa separator
/// (mis. "GJ26080001"). UI menampilkannya dengan separator supaya lebih
/// enak dibaca ("GJ-2608-0001"). Nilai yang dikirim balik ke API (kalau
/// ada) tetap harus pakai nilai mentah dari server, bukan hasil format ini.
/// </summary>
public static partial class TransactionNumberDisplayHelper
{
    [GeneratedRegex(@"^([A-Z]+)(\d{4})(\d{4})$")]
    private static partial Regex NewFormat();

    public static string Format(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;

        var match = NewFormat().Match(raw);
        if (!match.Success)
        {
            // Data lama (format pra-migrasi, mis. "GJ-000001") atau bentuk
            // tak dikenal: tampilkan apa adanya, jangan dipaksa.
            return raw;
        }

        return $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}";
    }
}

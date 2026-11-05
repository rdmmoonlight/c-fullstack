using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace AumoFinance.Services;

// Menyimpan riwayat deskripsi baris jurnal secara lokal di perangkat (Preferences),
// lalu memberi saran auto-fill berdasarkan huruf awal (trigger prefix) yang diketik.
// Contoh: pernah input "Dari Bapak" -> ketik "Dari" -> muncul saran "Dari Bapak".
public static class DescriptionSuggestionService
{
    private const string PrefsKey = "journal_line_description_history";
    private const int MaxItems = 150;
    private const int MaxSuggestions = 5;

    public static List<string> GetSuggestions(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return new List<string>();

        return LoadAll()
            .Where(d => d.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(d, prefix, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSuggestions)
            .ToList();
    }

    public static void Remember(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return;
        description = description.Trim();

        var all = LoadAll();
        all.RemoveAll(d => string.Equals(d, description, StringComparison.OrdinalIgnoreCase));
        all.Insert(0, description); // paling baru dipakai naik ke atas

        if (all.Count > MaxItems)
            all = all.Take(MaxItems).ToList();

        SaveAll(all);
    }

    private static List<string> LoadAll()
    {
        try
        {
            var json = Preferences.Default.Get(PrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static void SaveAll(List<string> items)
    {
        try
        {
            Preferences.Default.Set(PrefsKey, JsonSerializer.Serialize(items));
        }
        catch
        {
            // Gagal simpan riwayat saran tidak boleh mengganggu alur simpan jurnal.
        }
    }
}

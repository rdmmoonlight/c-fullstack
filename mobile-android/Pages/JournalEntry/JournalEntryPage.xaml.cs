using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.ViewModels;

namespace AumoFinance.Pages.JournalEntry;

[QueryProperty(nameof(EntryId), "entryId")]
public partial class JournalEntryPage : ContentPage
{
    private readonly JournalEntryViewModel _viewModel;
    private static readonly CultureInfo IdCulture = new("id-ID");

    // Guard anti-rekursi: mencegah Entry.Text yang kita set sendiri di
    // bawah memicu ulang handler ini. Dikunci per instance Entry (bukan
    // satu flag global) supaya baris jurnal lain (Debit/Kredit baris lain)
    // tidak ikut terpengaruh saat satu baris sedang diformat.
    private readonly HashSet<Entry> _formattingInProgress = new();

    public string? EntryId { get; set; }

    public JournalEntryPage(JournalEntryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Hubungkan event UI (DisplayAlert & Navigation) dari ViewModel
        _viewModel.RequestAlert += DisplayAlertAsync;
        _viewModel.RequestNavigationPop += SafePopAsync;
    }

    // Pop relatif (".."). Kalau Shell tetap melempar "Ambiguous routes matched"
    // (mis. sisa cache rute lama sebelum rebuild), fallback ke navigasi absolut
    // langsung ke GeneralJournalPage supaya tombol Cancel/Update tidak "diam saja".
    private async Task SafePopAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Relative pop failed, falling back to absolute route: {ex}");
            try
            {
                await Shell.Current.GoToAsync($"//{nameof(Pages.Home.HomePage)}/{nameof(Pages.Reports.GeneralJournal.GeneralJournalPage)}");
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"Absolute fallback pop also failed: {fallbackEx}");
                await DisplayAlertAsync("Navigation Error", "Couldn't return to the previous page. Please use the app menu.", "OK");
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.ApplyPeriodHeaderAsync(TopHeader);
        await _viewModel.InitializeAsync(EntryId);
    }

    // ==========================================
    // Pemisah ribuan Debit/Kredit.
    //
    // Percobaan sebelumnya (Behavior terpisah, lalu handler sinkron biasa)
    // masih salah menempatkan digit (237500 -> 237.005 / 237.050). Root
    // cause sebenarnya: di Android, Entry.CursorPosition yang di-set pada
    // tick sinkron YANG SAMA dengan Entry.Text sering diabaikan native
    // EditText karena buffer teksnya belum selesai diperbarui saat
    // SetSelection dipanggil. Fix-nya: tetap set CursorPosition langsung
    // (best effort), TAPI juga tunda satu tick lewat Dispatcher supaya
    // benar-benar diterapkan setelah native selesai me-render teks baru.
    // ==========================================

    private void OnDebitTextChanged(object? sender, TextChangedEventArgs e)
        => HandleAmountTextChanged(sender, e, _formattingInProgress);

    private void OnCreditTextChanged(object? sender, TextChangedEventArgs e)
        => HandleAmountTextChanged(sender, e, _formattingInProgress);

    private static void HandleAmountTextChanged(object? sender, TextChangedEventArgs e, HashSet<Entry> formattingInProgress)
    {
        if (sender is not Entry entry) return;
        if (formattingInProgress.Contains(entry)) return;

        string digitsOnly = Regex.Replace(e.NewTextValue ?? string.Empty, @"[^\d]", "");
        string formatted = string.Empty;

        if (digitsOnly.Length > 0 && decimal.TryParse(digitsOnly, NumberStyles.None, CultureInfo.InvariantCulture, out decimal value))
        {
            formatted = value.ToString("N0", IdCulture);
        }

        if (entry.Text != formatted)
        {
            formattingInProgress.Add(entry);
            try
            {
                entry.Text = formatted;
            }
            finally
            {
                formattingInProgress.Remove(entry);
            }
        }

        // Android tidak selalu menerapkan CursorPosition kalau di-set pada
        // tick sinkron yang sama dengan Text — EditText native belum
        // selesai memperbarui buffernya, jadi SetSelection bisa diabaikan
        // atau dipatok balik ke posisi lama. Set sekali langsung (best
        // effort untuk device yang tidak kena masalah ini), lalu tunda
        // satu tick lewat Dispatcher supaya benar-benar diterapkan setelah
        // native selesai me-render teks barunya.
        entry.CursorPosition = formatted.Length;
        entry.Dispatcher.Dispatch(() =>
        {
            if (entry.Text == formatted)
            {
                entry.CursorPosition = formatted.Length;
            }
        });
    }
}

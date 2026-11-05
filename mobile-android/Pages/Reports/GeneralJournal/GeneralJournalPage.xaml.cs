using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using AumoFinance.Models.Reports;
using AumoFinance.Pages.JournalEntry;
using AumoFinance.Services;
using AumoFinance.Services.Reports;

namespace AumoFinance.Pages.Reports.GeneralJournal;

public partial class GeneralJournalPage : ContentPage
{
    private readonly GeneralJournalService _generalJournalService;
    private readonly JournalEntryService _journalEntryService;
    private readonly CultureInfo _idrCulture;

    public GeneralJournalPage(GeneralJournalService generalJournalService, JournalEntryService journalEntryService)
    {
        InitializeComponent();
        _generalJournalService = generalJournalService;
        _journalEntryService = journalEntryService;

        // Memberikan spasi antara 'Rp' dan nominal angka (Rp 1.000.000)
        _idrCulture = (CultureInfo)CultureInfo.GetCultureInfo("id-ID").Clone();
        _idrCulture.NumberFormat.CurrencySymbol = "Rp ";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadGeneralJournalAsync();
    }

    private async Task LoadGeneralJournalAsync()
    {
        SetLoadingState(true);

        try
        {
            var (data, errorDetail) = await _generalJournalService.GetGeneralJournalReportAsync();

            if (!string.IsNullOrEmpty(errorDetail))
            {
                await this.DisplayAlertAsync("Error", errorDetail, "OK");
                return;
            }

            if (data != null)
            {
                TopHeader.PeriodText = string.IsNullOrWhiteSpace(data.SelectedPeriodName)
                    ? "No Active Period"
                    : data.SelectedPeriodName;

                var viewModels = (data.Entries ?? new List<GeneralJournalEntryReportDto>()).Select(e => new GeneralJournalEntryViewModel
                {
                    Id = e.Id,
                    EntryDate = e.EntryDate,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    // Tetap diset jika dibutuhkan logic internal, namun tidak ditampilkan lagi di Header XAML
                    JournalType = e.JournalType ?? "General",
                    TransactionNumber = e.TransactionNumber ?? string.Empty,
                    Lines = (e.Lines ?? new List<GeneralJournalLineReportDto>()).Select(l => new GeneralJournalLineViewModel
                    {
                        AccountReferenceNumber = l.ReferenceNumber ?? 0,
                        AccountName = l.AccountName ?? string.Empty,
                        LineDescription = l.LineDescription,
                        Debit = l.Debit,
                        Credit = l.Credit,
                        IdrCulture = _idrCulture
                    }).ToList(),
                    IdrCulture = _idrCulture
                }).ToList();

                // Grup per tanggal (kalo tanggalnya sama, satu header), tapi urutan
                // di dalam & antar grup tetap pakai timestamp penuh (jam:menit:detik)
                // dari yang paling lama -> paling baru.
                var grouped = viewModels
                    .OrderBy(v => v.EntryDate.Date)
                    .ThenBy(v => v.CreatedAt)
                    .ThenBy(v => v.Id)
                    .GroupBy(v => v.EntryDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new GeneralJournalDateGroup(g.Key.ToString("dd MMMM yyyy", _idrCulture), g))
                    .ToList();

                JournalCollectionView.ItemsSource = grouped;
                EmptyStateView.IsVisible = !viewModels.Any();
                JournalCollectionView.IsVisible = viewModels.Any();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadGeneralJournalAsync error: {ex}");
            await this.DisplayAlertAsync("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
        finally
        {
            SetLoadingState(false);
            JournalRefreshView.IsRefreshing = false;
        }
    }

    public async void OnRefreshViewRefreshing(object? sender, EventArgs e)
    {
        await LoadGeneralJournalAsync();
    }

    private async void OnNewEntryClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(JournalEntryPage));
    }

    public static readonly BindableProperty IsEditModeProperty =
        BindableProperty.Create(nameof(IsEditMode), typeof(bool), typeof(GeneralJournalPage), false);

    public bool IsEditMode
    {
        get => (bool)GetValue(IsEditModeProperty);
        set => SetValue(IsEditModeProperty, value);
    }

    private void OnToggleEditModeClicked(object? sender, EventArgs e)
    {
        IsEditMode = !IsEditMode;
        EditModeButton.Text = IsEditMode ? " Done" : " Edit";
        EditModeButton.ImageSource = new FontImageSource
        {
            FontFamily = "TablerIcons",
            Glyph = IsEditMode ? "\uea67" : "\uea98",
            Color = Color.FromArgb("#FFFFFF"),
            Size = 14
        };
        EditModeButton.BackgroundColor = IsEditMode ? Color.FromArgb("#523363") : Color.FromArgb("#1E121F");
        EditModeButton.TextColor = Color.FromArgb("#FFFFFF");
    }

    private async void OnEditEntryClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is GeneralJournalEntryViewModel entry)
        {
            await Shell.Current.GoToAsync($"{nameof(JournalEntryPage)}?entryId={entry.Id}");
        }
    }

    private async void OnDeleteEntryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not GeneralJournalEntryViewModel entry)
            return;

        bool confirm = await this.DisplayAlertAsync(
            "Delete Journal Entry",
            $"Delete transaction {entry.FormattedTransactionNumber}? This action cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        var (success, message) = await _journalEntryService.DeleteJournalEntryAsync(entry.Id);

        if (success)
        {
            await LoadGeneralJournalAsync();
        }
        else
        {
            await this.DisplayAlertAsync("Delete Failed", message, "OK");
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;
    }
}

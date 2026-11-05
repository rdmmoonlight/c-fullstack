using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.Models;
using AumoFinance.Services;
using AumoFinance.Services.Reports;
using AumoFinance.Pages.JournalEntry;

namespace AumoFinance.Pages.Reports.AdjustingJournal;

public partial class AdjustingJournalPage : ContentPage
{
    private readonly AdjustingJournalService _adjustingJournalService;
    private readonly JournalEntryService _journalEntryService;
    private readonly CultureInfo _idrCulture;

    public AdjustingJournalPage(AdjustingJournalService adjustingJournalService, JournalEntryService journalEntryService)
    {
        InitializeComponent();
        _adjustingJournalService = adjustingJournalService;
        _journalEntryService = journalEntryService;

        // Memberikan spasi antara 'Rp' dan nominal angka (Rp 1.000.000)
        _idrCulture = (CultureInfo)CultureInfo.GetCultureInfo("id-ID").Clone();
        _idrCulture.NumberFormat.CurrencySymbol = "Rp ";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAdjustingEntriesAsync();
    }

    private async Task LoadAdjustingEntriesAsync()
    {
        SetLoadingState(true);
        AdjustingJournalCollectionView.IsVisible = false;
        EmptyStateContainer.IsVisible = false;

        try
        {
            var (response, errorDetail) = await _adjustingJournalService.GetAdjustingJournalReportAsync();

            if (response == null || !response.Success)
            {
                TopHeader.PeriodText = "No Active Period";
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = errorDetail ?? "Failed to load adjusting journal data.";
                return;
            }

            if (!response.HasPeriodSelected)
            {
                TopHeader.PeriodText = "No Active Period";
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = "No active period selected.";
                return;
            }

            // Topbar sinkron dengan General Journal — nama periode ditampilkan di sana,
            // status closed ditandai lewat ikon gembok terpisah (bukan emoji ditempel ke teks).
            var periodName = string.IsNullOrWhiteSpace(response.SelectedPeriodName)
                ? "No Active Period"
                : response.SelectedPeriodName;
            TopHeader.PeriodText = periodName;
            TopHeader.IsPeriodLocked = response.IsPeriodClosed;

            var entries = response.Entries;

            if (entries == null || !entries.Any())
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = $"No adjusting entries found for period {response.SelectedPeriodName}.";
            }
            else
            {
                var displayList = entries.Select(e => new JournalEntryDisplayModel
                {
                    Id = e.Id,
                    TransactionNumber = e.TransactionNumber ?? string.Empty,
                    EntryDate = e.EntryDate,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    Lines = e.Lines.Select(l => new JournalEntryLineDisplayModel
                    {
                        AccountName = l.AccountName ?? "-",
                        RefNumber = l.ReferenceNumber.ToString(),
                        LineDescription = l.LineDescription,
                        Debit = l.Debit,
                        Credit = l.Credit,
                        IdrCulture = _idrCulture
                    }).ToList()
                }).ToList();

                // Grup per tanggal, sama pola dengan General Journal.
                var grouped = displayList
                    .OrderBy(v => v.EntryDate.Date)
                    .ThenBy(v => v.CreatedAt)
                    .ThenBy(v => v.Id)
                    .GroupBy(v => v.EntryDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new JournalEntryDateGroup(g.Key.ToString("dd MMMM yyyy", _idrCulture), g))
                    .ToList();

                AdjustingJournalCollectionView.ItemsSource = grouped;
                AdjustingJournalCollectionView.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadAdjustingEntriesAsync error: {ex}");
            await this.DisplayAlertAsync("Error", $"Failed to load data: {ex.Message}", "OK");
        }
        finally
        {
            SetLoadingState(false);
            AdjustingJournalRefreshView.IsRefreshing = false;
        }
    }

    private async void OnAddAdjustingEntryClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(JournalEntryPage)}?type=Adjusting");
    }

    public async void OnRefreshViewRefreshing(object? sender, EventArgs e)
    {
        await LoadAdjustingEntriesAsync();
    }

    private async void OnEditEntryClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int entryId)
        {
            await Shell.Current.GoToAsync($"{nameof(JournalEntryPage)}?entryId={entryId}");
        }
    }

    private async void OnDeleteEntryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not JournalEntryDisplayModel entry)
            return;

        var label = string.IsNullOrWhiteSpace(entry.TransactionNumber)
            ? entry.Id.ToString()
            : entry.FormattedTransactionNumber;

        bool confirm = await this.DisplayAlertAsync(
            "Delete Adjusting Entry",
            $"Delete transaction {label}? This action cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        var (success, message) = await _journalEntryService.DeleteJournalEntryAsync(entry.Id);

        if (success)
        {
            await LoadAdjustingEntriesAsync();
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

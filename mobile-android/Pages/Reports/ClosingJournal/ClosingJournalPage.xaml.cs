using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.Services.Reports;

namespace AumoFinance.Pages.Reports.ClosingJournal;

public partial class ClosingJournalPage : ContentPage
{
    private readonly ClosingJournalService _closingJournalService;

    public ClosingJournalPage(ClosingJournalService closingJournalService)
    {
        InitializeComponent();
        _closingJournalService = closingJournalService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReportAsync();
    }

    private async void OnRefreshViewRefreshing(object? sender, EventArgs e)
    {
        await LoadReportAsync();
        JournalRefreshView.IsRefreshing = false;
    }

    private async Task LoadReportAsync()
    {
        SetLoadingState(true);

        var (data, errorDetail) = await _closingJournalService.GetClosingJournalReportAsync();

        SetLoadingState(false);

        // Sync period name to the top bar instead of an in-page period banner
        // (same pattern as Worksheet/Income Statement/Adjusted Trial Balance).
        if (TopHeader != null)
        {
            TopHeader.PeriodText = string.IsNullOrWhiteSpace(data?.SelectedPeriodName)
                ? "No Active Period"
                : data.SelectedPeriodName;
        }

        if (errorDetail != null)
        {
            await this.DisplayAlertAsync("Error Loading Report", errorDetail, "OK");
            ShowEmptyState(true);
            return;
        }

        if (data == null || !data.Success || data.ClosingJournal == null || data.ClosingJournal.Groups.Count == 0)
        {
            ShowEmptyState(true);
            return;
        }

        ShowEmptyState(false);

        // 1. Set Banner Laba Bersih (Net Income) & Akun Laba Ditahan
        var culture = new CultureInfo("id-ID");
        NetIncomeCard.IsVisible = true;
        NetIncomeLabel.Text = $"Rp {data.ClosingJournal.NetIncome.ToString("N0", culture)}";
        RetainedEarningsAccountLabel.Text = $"Tujuan: {data.ClosingJournal.RetainedEarningsAccountName ?? "Retained Earnings"}";

        // 2. Set List Groups (Revenue, Expense, Income Summary)
        var groupViewModels = data.ClosingJournal.Groups
            .Select(g => new ClosingJournalGroupViewModel(g))
            .ToList();

        JournalCollectionView.ItemsSource = groupViewModels;
    }

    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;

        if (isLoading)
        {
            EmptyStateView.IsVisible = false;
            NetIncomeCard.IsVisible = false;
        }
    }

    private void ShowEmptyState(bool show)
    {
        EmptyStateView.IsVisible = show;
        JournalCollectionView.IsVisible = !show;
    }
}

// =========================================================================
// VIEW MODELS / DISPLAY WRAPPERS UNTUK MAPPING UI
// =========================================================================

public class ClosingJournalGroupViewModel
{
    private readonly ClosingJournalGroupDto _dto;

    public ClosingJournalGroupViewModel(ClosingJournalGroupDto dto)
    {
        _dto = dto;
        Lines = dto.Lines.Select(l => new ClosingJournalLineViewModel(l)).ToList();
    }

    public string? Description => _dto.Description;
    public List<ClosingJournalLineViewModel> Lines { get; }

    public string FormattedTotalDebit => _dto.TotalDebit == 0 ? "-" : _dto.TotalDebit.ToString("N0", new CultureInfo("id-ID"));
    public string FormattedTotalCredit => _dto.TotalCredit == 0 ? "-" : _dto.TotalCredit.ToString("N0", new CultureInfo("id-ID"));
}

public class ClosingJournalLineViewModel
{
    private readonly ClosingJournalLineDto _dto;

    public ClosingJournalLineViewModel(ClosingJournalLineDto dto)
    {
        _dto = dto;
    }

    public string FormattedRef => _dto.ReferenceNumber == 0 ? "-" : _dto.ReferenceNumber.ToString();
    public string? AccountName => _dto.AccountName;

    public string FormattedDebit => _dto.Debit == 0 ? "-" : _dto.Debit.ToString("N0", new CultureInfo("id-ID"));
    public string FormattedCredit => _dto.Credit == 0 ? "-" : _dto.Credit.ToString("N0", new CultureInfo("id-ID"));
}

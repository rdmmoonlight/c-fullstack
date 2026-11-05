using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.Models.Reports;
using AumoFinance.Services.Reports;
using AumoFinance.Pages.Reports.GeneralJournal;

namespace AumoFinance.Pages.Reports.GeneralLedgerPermanent;

public partial class GeneralLedgerPermanentPage : ContentPage
{
    private readonly GeneralLedgerService _generalLedgerService;
    private readonly CultureInfo _idrCulture;

    public GeneralLedgerPermanentPage(GeneralLedgerService generalLedgerService)
    {
        InitializeComponent();
        _generalLedgerService = generalLedgerService;

        _idrCulture = (CultureInfo)CultureInfo.GetCultureInfo("id-ID").Clone();
        _idrCulture.NumberFormat.CurrencySymbol = "Rp ";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLedgerDataAsync();
    }

    private async Task LoadLedgerDataAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LedgersCollectionView.IsVisible = false;
        EmptyStateContainer.IsVisible = false;

        try
        {
            // isTemporary = false for General Ledger Permanent (Real Accounts: Assets, Liabilities, Equity)
            var (response, errorDetail) = await _generalLedgerService.GetGeneralLedgerReportAsync(isTemporary: false);

            if (response == null || !response.Success)
            {
                EmptyStateContainer.IsVisible = true;
                return;
            }

            if (!response.HasPeriodSelected)
            {
                EmptyStateContainer.IsVisible = true;
                return;
            }

            TopHeader.PeriodText = string.IsNullOrWhiteSpace(response.SelectedPeriodName)
                ? "No Active Period"
                : response.SelectedPeriodName;

            var ledgers = response.Accounts;

            if (ledgers == null || !ledgers.Any())
            {
                EmptyStateContainer.IsVisible = true;
            }
            else
            {
                var viewModels = ledgers.Select(a => new GeneralLedgerAccountViewModel
                {
                    AccountId = a.AccountId,
                    ReferenceNumber = a.ReferenceNumber,
                    AccountName = a.AccountName,
                    Type = a.Type,
                    EndingBalance = a.EndingBalance,
                    Lines = (a.Entries ?? new List<GeneralLedgerEntryDto>()).Select(en => new GeneralLedgerLineViewModel
                    {
                        EntryDate = en.EntryDate,
                        Description = en.Description ?? string.Empty,
                        Debit = en.Debit,
                        Credit = en.Credit,
                        RunningBalance = en.RunningBalance,
                        IdrCulture = _idrCulture
                    }).ToList(),
                    IdrCulture = _idrCulture
                }).ToList();

                LedgersCollectionView.ItemsSource = viewModels;
                LedgersCollectionView.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await this.DisplayAlertAsync("Error", $"Failed to connect to the database: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnGeneralJournalClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GeneralJournalPage));
    }
}

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using AumoFinance.Services.Reports;

namespace AumoFinance.Pages.Reports.TrialBalance;

[QueryProperty(nameof(IncludeAdjustingStr), "includeAdjusting")]
public partial class TrialBalancePage : ContentPage
{
    private readonly TrialBalanceService _trialBalanceService;
    private bool _includeAdjusting;
    private bool _isDataLoaded;

    public string IncludeAdjustingStr
    {
        set 
        { 
            _includeAdjusting = bool.TryParse(value, out var result) && result; 
        }
    }

    public TrialBalancePage(TrialBalanceService trialBalanceService)
    {
        InitializeComponent();
        _trialBalanceService = trialBalanceService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Update Judul Halaman berdasarkan query parameter
        if (_includeAdjusting)
        {
            PageTitleLabel.Text = "Adjusted Trial Balance";
            PageSubtitleLabel.Text = "Account balances after adjusting entries.";
            Title = "Adjusted Trial Balance";
        }
        else
        {
            PageTitleLabel.Text = "Trial Balance";
            PageSubtitleLabel.Text = "Account balances before adjusting entries.";
            Title = "Trial Balance";
        }

        // Muat data jika belum dimuat
        if (!_isDataLoaded)
        {
            await LoadTrialBalanceAsync();
        }
    }

    private async Task LoadTrialBalanceAsync()
    {
        SetLoadingState(true);

        try
        {
            string reportType = _includeAdjusting ? "adjusted" : "unadjusted";
            
            // Panggil API Trial Balance
            var (response, errorDetail) = await _trialBalanceService.GetTrialBalanceReportAsync(reportType);

            if (response == null || !response.Success)
            {
                ShowEmptyState(errorDetail ?? "Failed to load trial balance report.");
                return;
            }

            // Update TopHeader dengan nama periode dari API segera setelah respons
            // diterima (sama seperti pada General Journal), agar top bar selalu
            // sinkron walaupun belum ada periode aktif yang dipilih.
            if (TopHeader != null)
            {
                TopHeader.PeriodText = string.IsNullOrWhiteSpace(response.SelectedPeriodName)
                    ? "No Active Period"
                    : response.SelectedPeriodName;
            }

            // Validasi apakah periode aktif sudah dipilih di sistem
            if (!response.HasPeriodSelected)
            {
                ShowEmptyState("No accounting period is currently selected.");
                return;
            }

            var rows = response.Rows;

            if (rows == null || !rows.Any())
            {
                ShowEmptyState("No account transaction history found for this period.");
            }
            else
            {
                decimal totalDebit = response.TotalDebit;
                decimal totalCredit = response.TotalCredit;
                bool isBalanced = response.IsBalanced;

                // Format total saldo ke Rupiah
                var culture = new CultureInfo("id-ID");
                TotalDebitLabel.Text = totalDebit.ToString("N0", culture);
                TotalCreditLabel.Text = totalCredit.ToString("N0", culture);

                // Update Status Keseimbangan (Balanced Alert Card)
                BalanceStatusCard.IsVisible = true;
                BalanceStatusCard.BackgroundColor = Color.Parse(isBalanced ? "#1E121F" : "#1E121F");
                BalanceStatusCard.Stroke = Color.Parse(isBalanced ? "#4FA36A" : "#D7192F");

                var accentColor = Color.Parse(isBalanced ? "#4FA36A" : "#D7192F");
                BalanceStatusIcon.Text = isBalanced ? "" : "";
                BalanceStatusIcon.TextColor = accentColor;
                BalanceStatusText.TextColor = accentColor;
                
                BalanceStatusText.Text = isBalanced
                    ? "Trial balance is balanced; total Debits equal Credits."
                    : "Trial balance is unbalanced! Please check your journal entries.";

                // Binding data baris ke CollectionView
                TrialBalanceCollectionView.ItemsSource = rows;
                TableContainer.IsVisible = true;
                _isDataLoaded = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load Trial Balance: {ex.Message}", "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;

        if (isLoading)
        {
            TableContainer.IsVisible = false;
            EmptyStateContainer.IsVisible = false;
            BalanceStatusCard.IsVisible = false;
        }
    }

    private void ShowEmptyState(string message)
    {
        EmptyStateContainer.IsVisible = true;
        EmptyStateLabel.Text = message;
        TableContainer.IsVisible = false;
        BalanceStatusCard.IsVisible = false;
    }
}

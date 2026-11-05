using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.Services.Reports;

namespace AumoFinance.Pages.Reports.RetainedEarnings;

public partial class RetainedEarningsPage : ContentPage
{
    private readonly RetainedEarningsService _retainedEarningsService;

    public RetainedEarningsPage(RetainedEarningsService retainedEarningsService)
    {
        InitializeComponent();
        _retainedEarningsService = retainedEarningsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRetainedEarningsAsync();
    }

    private async Task LoadRetainedEarningsAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        StatementContainer.IsVisible = false;
        EmptyStateContainer.IsVisible = false;

        try
        {
            var (response, errorDetail) = await _retainedEarningsService.GetRetainedEarningsReportAsync();

            if (response == null || !response.Success)
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = errorDetail ?? "Failed to load retained earnings report.";
                return;
            }

            // Sync period name to the top bar instead of an in-page period card
            // (same pattern as Worksheet/Income Statement/Adjusted Trial Balance).
            if (TopHeader != null)
            {
                TopHeader.PeriodText = string.IsNullOrWhiteSpace(response.SelectedPeriodName)
                    ? "No Active Period"
                    : response.SelectedPeriodName;
            }

            if (!response.HasPeriodSelected)
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = "No active period selected.";
                return;
            }

            decimal beginningBalance = response.BeginningRetainedEarnings;
            decimal netIncome = response.NetIncome;
            decimal dividends = response.DividendsOrDraws;
            decimal endingBalance = response.EndingRetainedEarnings;

            var culture = new System.Globalization.CultureInfo("id-ID");

            // Update UI Bindings
            AccountNameTitleLabel.Text = "Retained Earnings";
            BeginningLabel.Text = "Retained earnings, beginning";
            BeginningBalanceLabel.Text = beginningBalance.ToString("N0", culture);

            NetIncomeLabel.Text = netIncome.ToString("N0", culture);
            NetIncomeLabel.TextColor = netIncome >= 0 ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");

            if (dividends != 0)
            {
                DividendsLabel.Text = $"({dividends.ToString("N0", culture)})";
                DividendsRowContainer.IsVisible = true;
            }
            else
            {
                DividendsRowContainer.IsVisible = false;
            }

            EndingLabel.Text = "Retained earnings, ending";
            EndingBalanceLabel.Text = endingBalance.ToString("N0", culture);
            EndingBalanceLabel.TextColor = endingBalance >= 0 ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");

            StatementContainer.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load Retained Earnings: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}

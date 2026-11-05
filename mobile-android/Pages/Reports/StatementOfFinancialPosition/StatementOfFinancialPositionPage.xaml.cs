using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.Services.Reports;

namespace AumoFinance.Pages.Reports.StatementOfFinancialPosition;

public partial class StatementOfFinancialPositionPage : ContentPage
{
    private readonly StatementOfFinancialPositionService _statementOfFinancialPositionService;

    public StatementOfFinancialPositionPage(StatementOfFinancialPositionService statementOfFinancialPositionService)
    {
        InitializeComponent();
        _statementOfFinancialPositionService = statementOfFinancialPositionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPageDataAsync();
    }

    private async Task LoadPageDataAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        SheetContainer.IsVisible = false;
        EmptyStateContainer.IsVisible = false;

        try
        {
            var (response, errorDetail) = await _statementOfFinancialPositionService.GetStatementOfFinancialPositionReportAsync();

            if (response == null || !response.Success)
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = errorDetail ?? "Failed to load statement of financial position report.";
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

            var assets = response.AssetAccounts?
                .Select(a => new FinancialPositionLineModel { ReferenceNumber = a.ReferenceNumber.ToString(), AccountName = a.AccountName, Amount = a.Amount }).ToList() ?? new();

            var liabilities = response.LiabilityAccounts?
                .Select(l => new FinancialPositionLineModel { ReferenceNumber = l.ReferenceNumber.ToString(), AccountName = l.AccountName, Amount = l.Amount }).ToList() ?? new();

            var equities = response.EquityAccounts?
                .Select(e => new FinancialPositionLineModel { ReferenceNumber = e.ReferenceNumber.ToString(), AccountName = e.AccountName, Amount = e.Amount }).ToList() ?? new();

            if (!assets.Any() && !liabilities.Any() && !equities.Any())
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = $"No report data available for period {response.SelectedPeriodName}.";
                return;
            }

            var culture = new System.Globalization.CultureInfo("id-ID");

            // Update UI Collections
            AssetsCollectionView.ItemsSource = assets;
            TotalAssetsLabel.Text = $"Rp {response.TotalAssets.ToString("N0", culture)}";

            LiabilitiesCollectionView.ItemsSource = liabilities;
            TotalLiabilitiesLabel.Text = $"Rp {response.TotalLiabilities.ToString("N0", culture)}";

            EquityCollectionView.ItemsSource = equities;
            TotalEquityLabel.Text = $"Rp {response.TotalEquity.ToString("N0", culture)}";

            TotalLiabilitiesAndEquityLabel.Text = $"Rp {response.TotalLiabilitiesAndEquity.ToString("N0", culture)}";

            SheetContainer.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load balance sheet: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}

public class FinancialPositionLineModel
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    private static readonly System.Globalization.CultureInfo Idr = new("id-ID");

    public string FormattedAmount => Amount.ToString("N0", Idr);
}

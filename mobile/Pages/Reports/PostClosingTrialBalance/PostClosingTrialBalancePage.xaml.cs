using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.Services.Reports;

namespace AumoFinance.Pages.Reports.PostClosingTrialBalance;

public partial class PostClosingTrialBalancePage : ContentPage
{
    private readonly TrialBalanceService _trialBalanceService;

    public PostClosingTrialBalancePage(TrialBalanceService trialBalanceService)
    {
        InitializeComponent();
        _trialBalanceService = trialBalanceService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPostClosingDataAsync();
    }

    private async Task LoadPostClosingDataAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        TableContainer.IsVisible = false;
        EmptyStateContainer.IsVisible = false;
        BalanceStatusCard.IsVisible = false;

        try
        {
            // Call TrialBalanceService with "post-closing" parameter
            var (response, errorDetail) = await _trialBalanceService.GetTrialBalanceReportAsync("post-closing");

            if (response == null || !response.Success)
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = errorDetail ?? "Failed to load post-closing trial balance report.";
                return;
            }

            if (!response.HasPeriodSelected)
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = "No active period selected.";
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
            SubtitleLabel.Text = response.ReportTitle;

            var rows = new List<PostClosingRowModel>();
            var culture = new System.Globalization.CultureInfo("id-ID");

            if (response.Rows != null)
            {
                foreach (var r in response.Rows)
                {
                    rows.Add(new PostClosingRowModel
                    {
                        ReferenceNumber = r.ReferenceNumber > 0 ? r.ReferenceNumber.ToString() : "-",
                        AccountName = r.AccountName,
                        TypeLabel = r.Type,
                        DebitAmount = r.Debit,
                        CreditAmount = r.Credit
                    });
                }
            }

            if (!rows.Any())
            {
                EmptyStateContainer.IsVisible = true;
                return;
            }

            decimal totalDebit = response.TotalDebit;
            decimal totalCredit = response.TotalCredit;
            bool isBalanced = response.IsBalanced;

            TotalDebitLabel.Text = totalDebit.ToString("N0", culture);
            TotalCreditLabel.Text = totalCredit.ToString("N0", culture);

            // Status Alert
            BalanceStatusCard.IsVisible = true;
            BalanceStatusCard.BackgroundColor = isBalanced ? Color.FromArgb("#1E121F") : Color.FromArgb("#1E121F");
            BalanceStatusCard.Stroke = isBalanced ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");
            BalanceStatusIcon.Text = isBalanced ? "" : "";
            BalanceStatusIcon.TextColor = isBalanced ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");
            BalanceStatusText.TextColor = BalanceStatusIcon.TextColor;
            BalanceStatusText.Text = isBalanced
                ? "Post-closing trial balance is balanced; books are ready for the next period."
                : "Post-closing trial balance is unbalanced! Please review closing entries.";

            TrialBalanceCollectionView.ItemsSource = rows;
            TableContainer.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load post-closing trial balance: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}

public class PostClosingRowModel
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }

    private static readonly System.Globalization.CultureInfo Idr = new("id-ID");

    public string FormattedDebit => DebitAmount > 0 ? DebitAmount.ToString("N0", Idr) : "-";
    public string FormattedCredit => CreditAmount > 0 ? CreditAmount.ToString("N0", Idr) : "-";
}

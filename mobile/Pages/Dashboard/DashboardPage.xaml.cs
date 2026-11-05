using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using AumoFinance.Services;
using AumoFinance.Pages.JournalEntry;

namespace AumoFinance.Pages.Dashboard;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardService _dashboardService;
    private readonly IServiceProvider _serviceProvider;
    private readonly CultureInfo _idCulture = new("id-ID");

    public DashboardPage(DashboardService dashboardService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _dashboardService = dashboardService;
        _serviceProvider = serviceProvider;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardDataAsync();
    }

    private async void OnRefreshViewRefreshing(object? sender, EventArgs e)
    {
        await LoadDashboardDataAsync();
        DashboardRefreshView.IsRefreshing = false;
    }

    private async Task LoadDashboardDataAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        DashboardContent.IsVisible = false;

        try
        {
            var (data, errorDetail) = await _dashboardService.GetDashboardAsync();

            // Sync period name to the top bar instead of an in-page period banner
            // (same pattern as the Reports pages).
            if (TopHeader != null)
            {
                TopHeader.PeriodText = string.IsNullOrWhiteSpace(data?.SelectedPeriodName)
                    ? "No Active Period"
                    : data.SelectedPeriodName;
            }

            if (data != null && data.Success)
            {
                // Format ke Rupiah tanpa desimal (N0) dengan simbol "Rp " di depannya
                CashLabel.Text = string.Format(_idCulture, "Rp {0:N0}", data.TotalAssets);
                NetIncomeLabel.Text = string.Format(_idCulture, "Rp {0:N0}", data.NetIncome);
                RevenueLabel.Text = string.Format(_idCulture, "Rp {0:N0}", data.TotalRevenue);
                ExpenseLabel.Text = string.Format(_idCulture, "Rp {0:N0}", data.TotalExpenses);

                RenderCashAndBankBreakdown(data.CashAccounts, CashCard, CashItemsLayout);
                RenderCashAndBankBreakdown(data.BankAccounts, BankCard, BankItemsLayout);
            }
            else
            {
                string detail = string.IsNullOrWhiteSpace(errorDetail) ? "Unknown error occurred." : errorDetail;
                await DisplayAlertAsync("Connection Failed", $"Failed to retrieve dashboard data from the server.\n\nDetails: {detail}", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadDashboardDataAsync error: {ex}");
            await DisplayAlertAsync("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            DashboardContent.IsVisible = true;
        }
    }

    private void RenderCashAndBankBreakdown(List<CashAndBankAccountDto>? accounts, Border card, VerticalStackLayout itemsLayout)
    {
        itemsLayout.Children.Clear();

        if (accounts == null || accounts.Count == 0)
        {
            card.IsVisible = false;
            return;
        }

        foreach (var account in accounts)
        {
            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            row.Children.Add(new Label
            {
                Text = account.AccountName,
                FontSize = 13,
                TextColor = Color.FromArgb("#FFFFFF"),
                LineBreakMode = LineBreakMode.TailTruncation,
                VerticalOptions = LayoutOptions.Center
            });

            var balanceLabel = new Label
            {
                Text = string.Format(_idCulture, "Rp {0:N0}", account.Balance),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#FFFFFF"),
                HorizontalTextAlignment = TextAlignment.End,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(balanceLabel, 1);
            row.Children.Add(balanceLabel);

            itemsLayout.Children.Add(row);
        }

        card.IsVisible = true;
    }

    private async void OnPrimaryFabClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement button)
        {
            button.IsEnabled = false; // Mencegah double tap
        }

        try
        {
            var journalEntryPage = _serviceProvider.GetService<JournalEntryPage>();
            if (journalEntryPage != null)
            {
                await Navigation.PushAsync(journalEntryPage);
            }
            else
            {
                await DisplayAlertAsync("Error", "Page could not be loaded.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Navigation error: {ex}");
            await DisplayAlertAsync("Error", $"Failed to navigate to Journal Entry page: {ex.Message}", "OK");
        }
        finally
        {
            if (sender is VisualElement fabButton)
            {
                fabButton.IsEnabled = true;
            }
        }
    }
}
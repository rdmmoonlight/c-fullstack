using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.Services.Reports;

namespace AumoFinance.Pages.Reports.StatementOfCashFlows;

public partial class StatementOfCashFlowsPage : ContentPage
{
    private readonly StatementOfCashFlowsService _cashFlowsService;
    private readonly CultureInfo _culture = new("id-ID");

    public StatementOfCashFlowsPage(StatementOfCashFlowsService cashFlowsService)
    {
        InitializeComponent();
        _cashFlowsService = cashFlowsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReportAsync();
    }

    private async void OnRefreshViewRefreshing(object? sender, EventArgs e)
    {
        await LoadReportAsync();
        CashFlowRefreshView.IsRefreshing = false;
    }

    private async Task LoadReportAsync()
    {
        SetLoadingState(true);

        var (data, errorDetail) = await _cashFlowsService.GetStatementOfCashFlowsReportAsync();

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

        if (data == null || !data.Success || !data.HasPeriodSelected)
        {
            ShowEmptyState(true);
            return;
        }

        ShowEmptyState(false);

        // Render Activity Categories
        RenderActivitySection(OperatingItemsLayout, data.OperatingActivities, NetOperatingLabel, data.NetCashFromOperating);
        RenderActivitySection(InvestingItemsLayout, data.InvestingActivities, NetInvestingLabel, data.NetCashFromInvesting);
        RenderActivitySection(FinancingItemsLayout, data.FinancingActivities, NetFinancingLabel, data.NetCashFromFinancing);

        // Render Summary Reconciliation
        NetChangeCashLabel.Text = FormatAmount(data.NetChangeInCash);
        NetChangeCashLabel.TextColor = data.NetChangeInCash >= 0 ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");

        BeginningCashLabel.Text = $"Rp {data.BeginningCash.ToString("N0", _culture)}";
        EndingCashLabel.Text = $"Rp {data.EndingCash.ToString("N0", _culture)}";
    }

    private void RenderActivitySection(
        VerticalStackLayout container,
        List<CashFlowItemDto> items,
        Label netLabel,
        decimal netAmount)
    {
        container.Children.Clear();

        if (items.Count == 0)
        {
            container.Children.Add(new Label
            {
                Text = "No activities recorded",
                FontSize = 13,
                TextColor = Color.FromArgb("#D8D8D8"),
                FontAttributes = FontAttributes.Italic
            });
        }
        else
        {
            foreach (var item in items)
            {
                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Padding = new Thickness(0, 2)
                };

                grid.Children.Add(new Label
                {
                    Text = item.Description,
                    FontSize = 13,
                    TextColor = Color.FromArgb("#D8D8D8"),
                    LineBreakMode = LineBreakMode.TailTruncation
                });

                var amountLabel = new Label
                {
                    Text = FormatAmount(item.Amount),
                    FontSize = 13,
                    HorizontalTextAlignment = TextAlignment.End
                };
                amountLabel.TextColor = item.Amount >= 0 ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");
                Grid.SetColumn(amountLabel, 1);
                grid.Children.Add(amountLabel);

                container.Children.Add(grid);
            }
        }

        netLabel.Text = FormatAmount(netAmount);
        netLabel.TextColor = netAmount >= 0 ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");
    }

    private string FormatAmount(decimal amount)
    {
        if (amount == 0) return "Rp 0";
        return amount < 0 ? $"(Rp {Math.Abs(amount).ToString("N0", _culture)})" : $"Rp {amount.ToString("N0", _culture)}";
    }

    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;

        if (isLoading)
        {
            EmptyStateView.IsVisible = false;
            MainContentLayout.IsVisible = false;
        }
    }

    private void ShowEmptyState(bool show)
    {
        EmptyStateView.IsVisible = show;
        MainContentLayout.IsVisible = !show;
    }
}

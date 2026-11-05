using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.Services.Reports;

namespace AumoFinance.Pages.Reports.IncomeStatement;

public partial class IncomeStatementPage : ContentPage
{
    private readonly IncomeStatementService _incomeStatementService;

    public IncomeStatementPage(IncomeStatementService incomeStatementService)
    {
        InitializeComponent();
        _incomeStatementService = incomeStatementService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadIncomeStatementAsync();
    }

    private async Task LoadIncomeStatementAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        StatementContainer.IsVisible = false;
        EmptyStateContainer.IsVisible = false;

        try
        {
            var (response, errorDetail) = await _incomeStatementService.GetIncomeStatementReportAsync();

            if (response == null || !response.Success)
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = errorDetail ?? "Failed to load income statement report.";
                return;
            }

            // Update nama periode aktif di TopHeader segera setelah respons diterima
            // (sama seperti pada Worksheet/Adjusted Trial Balance), agar top bar
            // selalu sinkron dan tidak perlu ditampilkan ulang di badan halaman.
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

            AsOfDateLabel.Text = "Statement of Profit or Loss (IAS 1)";

            var revenues = response.RevenueAccounts?
                .Select(r => new IncomeStatementLineModel { ReferenceNumber = r.ReferenceNumber.ToString(), AccountName = r.AccountName, Amount = r.Amount }).ToList() ?? new();

            var opExpenses = response.ExpenseAccounts?
                .Select(r => new IncomeStatementLineModel { ReferenceNumber = r.ReferenceNumber.ToString(), AccountName = r.AccountName, Amount = r.Amount }).ToList() ?? new();

            var otherIncome = response.OtherIncomeAccounts?
                .Select(r => new IncomeStatementLineModel { ReferenceNumber = r.ReferenceNumber.ToString(), AccountName = r.AccountName, Amount = r.Amount, IsExpense = false }).ToList() ?? new();

            var otherExpenses = response.OtherExpenseAccounts?
                .Select(r => new IncomeStatementLineModel { ReferenceNumber = r.ReferenceNumber.ToString(), AccountName = r.AccountName, Amount = r.Amount, IsExpense = true }).ToList() ?? new();

            decimal totalRevenue = response.TotalRevenue;
            decimal totalOpExpense = response.TotalExpenses;
            decimal operatingIncome = response.OperatingIncome;
            decimal netIncome = response.NetIncome;

            var culture = new System.Globalization.CultureInfo("id-ID");

            RevenueCollectionView.ItemsSource = revenues;
            TotalRevenueLabel.Text = totalRevenue.ToString("N0", culture);

            OpExpenseCollectionView.ItemsSource = opExpenses;
            TotalOpExpenseLabel.Text = $"({totalOpExpense.ToString("N0", culture)})";

            OperatingIncomeLabel.Text = operatingIncome.ToString("N0", culture);
            OperatingIncomeLabel.TextColor = operatingIncome >= 0 ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");

            // Seksi Other Income & Expenses hanya tampil jika ada datanya, sama seperti
            // IncomeStatementPage.razor di repo web (vm.OtherIncome.Any() || vm.OtherExpenses.Any()).
            var otherLines = otherIncome.Concat(otherExpenses).ToList();
            OtherSectionContainer.IsVisible = otherLines.Any();
            OtherCollectionView.ItemsSource = otherLines;

            NetIncomeLabel.Text = netIncome.ToString("N0", culture);
            NetIncomeLabel.TextColor = netIncome >= 0 ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");

            StatementContainer.IsVisible = true;
        }
        catch (Exception ex)
        {
            await this.DisplayAlertAsync("Error", $"Failed to load income statement: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}

public class IncomeStatementLineModel
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsExpense { get; set; }

    private static readonly System.Globalization.CultureInfo Idr = new("id-ID");

    public string FormattedAmount => IsExpense
        ? $"({Amount.ToString("N0", Idr)})"
        : Amount.ToString("N0", Idr);
    public string FormattedAmountBracket => $"({Amount.ToString("N0", Idr)})";
    public Microsoft.Maui.Graphics.Color AmountColor => IsExpense
        ? Microsoft.Maui.Graphics.Color.FromArgb("#D7192F")
        : Microsoft.Maui.Graphics.Colors.White;
}

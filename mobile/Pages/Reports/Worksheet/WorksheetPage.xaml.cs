using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AumoFinance.Services.Reports;

namespace AumoFinance.Pages.Reports.Worksheet;

public partial class WorksheetPage : ContentPage
{
    private readonly WorksheetService _worksheetService;

    public WorksheetPage(WorksheetService worksheetService)
    {
        InitializeComponent();
        _worksheetService = worksheetService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadWorksheetAsync();
    }

    private async Task LoadWorksheetAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        WorksheetContainer.IsVisible = false;
        EmptyStateContainer.IsVisible = false;
        NetIncomeInfoCard.IsVisible = false;

        try
        {
            var (response, errorDetail) = await _worksheetService.GetWorksheetReportAsync();

            if (response == null || !response.Success)
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = errorDetail ?? "Failed to load worksheet report.";
                return;
            }

            // Update nama periode aktif di TopHeader segera setelah respons diterima
            // (sama seperti pada Adjusted Trial Balance/General Journal), agar top bar
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

            var rows = response.Rows;

            if (rows == null || !rows.Any())
            {
                EmptyStateContainer.IsVisible = true;
                EmptyStateLabel.Text = $"No worksheet data available for period {response.SelectedPeriodName}.";
                return;
            }

            var culture = new System.Globalization.CultureInfo("id-ID");
            var worksheetRows = new List<WorksheetRowDisplayModel>();

            foreach (var r in rows)
            {
                worksheetRows.Add(new WorksheetRowDisplayModel
                {
                    ReferenceNumber = r.ReferenceNumber > 0 ? r.ReferenceNumber.ToString() : "-",
                    AccountName = r.AccountName,
                    UnadjustedDebit = r.TbDebit,
                    UnadjustedCredit = r.TbCredit,
                    AdjustmentDebit = r.AdjDebit,
                    AdjustmentCredit = r.AdjCredit,
                    AdjustedDebit = r.AdjTbDebit,
                    AdjustedCredit = r.AdjTbCredit,
                    IncomeStatementDebit = r.IsDebit,
                    IncomeStatementCredit = r.IsCredit,
                    FinancialPositionDebit = r.BsDebit,
                    FinancialPositionCredit = r.BsCredit
                });
            }

            var totals = response.Totals ?? new WorksheetTotalsDto();

            decimal netIncome = totals.NetIncome;
            NetIncomeLabel.Text = $"Rp {netIncome.ToString("N0", culture)}";
            NetIncomeLabel.TextColor = netIncome >= 0 ? Color.FromArgb("#4FA36A") : Color.FromArgb("#D7192F");

            TotUnadjDr.Text = totals.TbDebit.ToString("N0", culture);
            TotUnadjCr.Text = totals.TbCredit.ToString("N0", culture);
            TotAdjDr.Text = totals.AdjDebit.ToString("N0", culture);
            TotAdjCr.Text = totals.AdjCredit.ToString("N0", culture);
            TotAdjTbDr.Text = totals.AdjTbDebit.ToString("N0", culture);
            TotAdjTbCr.Text = totals.AdjTbCredit.ToString("N0", culture);
            TotIncDr.Text = totals.IsDebit.ToString("N0", culture);
            TotIncCr.Text = totals.IsCredit.ToString("N0", culture);
            TotBsDr.Text = totals.BsDebit.ToString("N0", culture);
            TotBsCr.Text = totals.BsCredit.ToString("N0", culture);

            // Baris ke-2 & ke-3 (kaidah worksheet 10-kolom): Laba/Rugi Bersih di-plug dari
            // kolom Income Statement ke kolom Balance Sheet sampai Total (Setelah Plug)
            // Debit = Kredit di kedua kolom tersebut. Rumus sama persis dengan WorksheetPage.razor.
            if (netIncome >= 0)
            {
                PlugIncDr.Text = netIncome.ToString("N0", culture);
                PlugIncCr.Text = "-";
                PlugBsDr.Text = "-";
                PlugBsCr.Text = netIncome.ToString("N0", culture);
            }
            else
            {
                PlugIncDr.Text = "-";
                PlugIncCr.Text = (-netIncome).ToString("N0", culture);
                PlugBsDr.Text = (-netIncome).ToString("N0", culture);
                PlugBsCr.Text = "-";
            }

            TotAfterIncDr.Text = (totals.IsDebit + (netIncome >= 0 ? netIncome : 0)).ToString("N0", culture);
            TotAfterIncCr.Text = (totals.IsCredit + (netIncome < 0 ? -netIncome : 0)).ToString("N0", culture);
            TotAfterBsDr.Text = (totals.BsDebit + (netIncome < 0 ? -netIncome : 0)).ToString("N0", culture);
            TotAfterBsCr.Text = (totals.BsCredit + (netIncome >= 0 ? netIncome : 0)).ToString("N0", culture);

            WorksheetCollectionView.ItemsSource = worksheetRows;
            WorksheetContainer.IsVisible = true;
            NetIncomeInfoCard.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load worksheet: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}

public class WorksheetRowDisplayModel
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal UnadjustedDebit { get; set; }
    public decimal UnadjustedCredit { get; set; }
    public decimal AdjustmentDebit { get; set; }
    public decimal AdjustmentCredit { get; set; }
    public decimal AdjustedDebit { get; set; }
    public decimal AdjustedCredit { get; set; }
    public decimal IncomeStatementDebit { get; set; }
    public decimal IncomeStatementCredit { get; set; }
    public decimal FinancialPositionDebit { get; set; }
    public decimal FinancialPositionCredit { get; set; }

    private static readonly System.Globalization.CultureInfo Idr = new("id-ID");

    public string FormattedUnadjustedDebit => UnadjustedDebit > 0 ? UnadjustedDebit.ToString("N0", Idr) : "-";
    public string FormattedUnadjustedCredit => UnadjustedCredit > 0 ? UnadjustedCredit.ToString("N0", Idr) : "-";
    public string FormattedAdjustmentDebit => AdjustmentDebit > 0 ? AdjustmentDebit.ToString("N0", Idr) : "-";
    public string FormattedAdjustmentCredit => AdjustmentCredit > 0 ? AdjustmentCredit.ToString("N0", Idr) : "-";
    public string FormattedAdjustedDebit => AdjustedDebit > 0 ? AdjustedDebit.ToString("N0", Idr) : "-";
    public string FormattedAdjustedCredit => AdjustedCredit > 0 ? AdjustedCredit.ToString("N0", Idr) : "-";
    public string FormattedIncomeStatementDebit => IncomeStatementDebit > 0 ? IncomeStatementDebit.ToString("N0", Idr) : "-";
    public string FormattedIncomeStatementCredit => IncomeStatementCredit > 0 ? IncomeStatementCredit.ToString("N0", Idr) : "-";
    public string FormattedFinancialPositionDebit => FinancialPositionDebit > 0 ? FinancialPositionDebit.ToString("N0", Idr) : "-";
    public string FormattedFinancialPositionCredit => FinancialPositionCredit > 0 ? FinancialPositionCredit.ToString("N0", Idr) : "-";
}

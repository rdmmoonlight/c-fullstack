using AumoBackend.DTOs;
using AumoBackend.ViewModels;

namespace AumoFinance.Services;

/// <summary>
/// Memetakan DashboardDataDto (dari IDashboardService, SSOT) ke DashboardViewModel yang dipakai komponen UI.
/// Hanya penyalinan/penyusunan field; angka laporan tetap berasal dari backend.
/// </summary>
public static class DashboardMapper
{
    public static DashboardViewModel ToViewModel(Guid userId, DashboardDataDto d)
    {
        var cash = d.TotalCashOnHand + d.TotalBankBalance;

        // Metrik turunan untuk tampilan (belum disediakan DashboardDataDto):
        // burn rate = beban periode berjalan, runway = kas / burn rate.
        var burn = d.TotalExpenses;
        var runway = burn > 0 ? (double)(cash / burn) : 0d;

        return new DashboardViewModel
        {
            UserId = userId,
            HasSelectedPeriod = d.HasPeriodSelected,
            IsSelectedPeriodClosed = d.IsPeriodClosed,
            ActivePeriodName = d.SelectedPeriodName,
            TotalCashAndEquivalents = cash,
            TotalAssets = d.TotalAssets,
            TotalLiabilities = d.TotalLiabilities,
            RevenueThisPeriod = d.TotalRevenue,
            OperatingExpenses = d.TotalExpenses,
            NetIncome = d.NetIncome,
            MonthlyBurnRate = burn,
            CashRunwayMonths = runway,
            FinancialHealthScore = HealthScore(d),
            ChartLabels = d.ChartTrend.Select(t => string.IsNullOrWhiteSpace(t.Label) ? t.Period : t.Label).ToList(),
            ChartRevenue = d.ChartTrend.Select(t => t.Revenue).ToList(),
            ChartExpenses = d.ChartTrend.Select(t => t.Expense != 0 ? t.Expense : t.Expenses).ToList(),
            ExpenseCategoryLabels = d.ExpenseAccountsList.Select(e => string.IsNullOrWhiteSpace(e.AccountName) ? e.Name : e.AccountName).ToList(),
            ExpenseCategoryValues = d.ExpenseAccountsList.Select(e => e.Amount != 0 ? e.Amount : e.Balance).ToList(),
            MainCoaBalances = d.Accounts,
            RecentJournals = new(),
        };
    }

    /// <summary>Skor 0-100 dari rasio kewajiban terhadap aset (tampilan saja).</summary>
    private static int HealthScore(DashboardDataDto d)
    {
        if (d.TotalAssets <= 0) return 0;
        var ratio = (double)(d.TotalLiabilities / d.TotalAssets);
        return (int)Math.Clamp(Math.Round((1 - ratio) * 100), 0, 100);
    }
}

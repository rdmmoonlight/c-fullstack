using System.Globalization;
// <copyright file="DashboardService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Dashboard;

using AumoBackend.Data;
using AumoBackend.DTOs;
using AumoBackend.Helpers;
using Microsoft.EntityFrameworkCore;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext? db;

    public async Task<DashboardDataDto> GetDashboardDataAsync(Guid userId, string period)
    {
        var activePeriod = await SelectedPeriodHelper.GetSelectedPeriodAsync(this.db, userId);

        int year = activePeriod?.StartDate.Year ?? DateTime.UtcNow.Year;

        DateTime monthlyStart = activePeriod?.StartDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime monthlyEnd = activePeriod?.EndDate ?? monthlyStart.AddMonths(1).AddDays(-1).Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        DateTime annualStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime annualEnd = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        bool isAnnual = string.Equals(period, "annual", StringComparison.OrdinalIgnoreCase);
        DateTime reqStart = isAnnual ? annualStart : monthlyStart;
        DateTime reqEnd = isAnnual ? annualEnd : monthlyEnd;
        string displayPeriodName = isAnnual ? $"Annual {year}" : activePeriod?.PeriodName ?? "Current Period";

        // Scope per tab
        var monthlyLines = await this.db.GeneralJournals
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.EntryDate >= monthlyStart && l.EntryDate <= monthlyEnd)
            .Select(l => new { l.AccountId, l.Debit, l.Credit, l.EntryDate })
            .ToListAsync();

        var annualLines = await this.db.GeneralJournals
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.EntryDate >= annualStart && l.EntryDate <= annualEnd)
            .Select(l => new { l.AccountId, l.Debit, l.Credit, l.EntryDate })
            .ToListAsync();

        var periodLines = isAnnual ? annualLines : monthlyLines;

        // Kumulatif
        var cumulativeLines = await this.db.GeneralJournals
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.EntryDate <= reqEnd)
            .Select(l => new { l.AccountId, l.Debit, l.Credit })
            .ToListAsync();

        // 1. Kas & Bank
        var cashBankAccounts = await this.db.ChartOfAccounts
            .Where(a => a.UserId == userId && a.IsActive && a.Role == "CashAndEquivalents")
            .OrderBy(a => a.ReferenceNumber)
            .Select(a => new { a.Id, a.ReferenceNumber, a.AccountName })
            .ToListAsync();

        var monthlyCashBreakdown = cashBankAccounts.Select(a =>
        {
            var bal = monthlyLines.Where(l => l.AccountId == a.Id).Sum(l => l.Debit - l.Credit);
            bool isBank = a.AccountName.Contains("Bank", StringComparison.OrdinalIgnoreCase) || a.AccountName.Contains("Rekening", StringComparison.OrdinalIgnoreCase);
            return new CashAccountItemDto { AccountId = a.Id.ToString(CultureInfo.InvariantCulture), ReferenceNumber = a.ReferenceNumber.ToString(CultureInfo.InvariantCulture), AccountName = a.AccountName, Balance = bal, IsBank = isBank };
        }).ToList();

        var annualCashBreakdown = cashBankAccounts.Select(a =>
        {
            var bal = annualLines.Where(l => l.AccountId == a.Id).Sum(l => l.Debit - l.Credit);
            bool isBank = a.AccountName.Contains("Bank", StringComparison.OrdinalIgnoreCase) || a.AccountName.Contains("Rekening", StringComparison.OrdinalIgnoreCase);
            return new CashAccountItemDto { AccountId = a.Id.ToString(CultureInfo.InvariantCulture), ReferenceNumber = a.ReferenceNumber.ToString(CultureInfo.InvariantCulture), AccountName = a.AccountName, Balance = bal, IsBank = isBank };
        }).ToList();

        var cumulativeCashBreakdown = cashBankAccounts.Select(a =>
        {
            var bal = cumulativeLines.Where(l => l.AccountId == a.Id).Sum(l => l.Debit - l.Credit);
            bool isBank = a.AccountName.Contains("Bank", StringComparison.OrdinalIgnoreCase) || a.AccountName.Contains("Rekening", StringComparison.OrdinalIgnoreCase);
            return new CashAccountItemDto { AccountId = a.Id.ToString(CultureInfo.InvariantCulture), ReferenceNumber = a.ReferenceNumber.ToString(CultureInfo.InvariantCulture), AccountName = a.AccountName, Balance = bal, IsBank = isBank };
        }).ToList();

        var totalCashOnHandMonthly = monthlyCashBreakdown.Where(x => !x.IsBank).Sum(x => x.Balance);
        var totalBankBalanceMonthly = monthlyCashBreakdown.Where(x => x.IsBank).Sum(x => x.Balance);
        var totalAssetsMonthly = monthlyCashBreakdown.Sum(x => x.Balance);

        var totalCashOnHandAnnual = annualCashBreakdown.Where(x => !x.IsBank).Sum(x => x.Balance);
        var totalBankBalanceAnnual = annualCashBreakdown.Where(x => x.IsBank).Sum(x => x.Balance);
        var totalAssetsAnnual = annualCashBreakdown.Sum(x => x.Balance);

        var totalCashOnHand = isAnnual ? totalCashOnHandAnnual : totalCashOnHandMonthly;
        var totalBankBalance = isAnnual ? totalBankBalanceAnnual : totalBankBalanceMonthly;
        var totalAssets = isAnnual ? totalAssetsAnnual : totalAssetsMonthly;

        // 2. Pendapatan & Beban
        var incomeTypes = new[] { "OperatingIncome", "Income", "Revenue" };
        var incomeIds = await this.db.ChartOfAccounts.Where(a => a.UserId == userId && a.IsActive && incomeTypes.Contains(a.Type)).Select(a => a.Id).ToListAsync();
        var totalIncome = periodLines.Where(l => incomeIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);

        var expenseTypes = new[] { "OperatingExpenses", "Expense", "Expenses" };
        var expenseAccountsMeta = await this.db.ChartOfAccounts
            .Where(a => a.UserId == userId && a.IsActive && expenseTypes.Contains(a.Type))
            .OrderBy(a => a.ReferenceNumber)
            .Select(a => new { a.Id, a.ReferenceNumber, a.AccountName })
            .ToListAsync();

        var expenseBreakdown = expenseAccountsMeta
            .Select(a => new ExpenseAccountItemDto
            {
                AccountId = a.Id.ToString(CultureInfo.InvariantCulture),
                ReferenceNumber = a.ReferenceNumber.ToString(CultureInfo.InvariantCulture),
                AccountName = a.AccountName,
                Balance = periodLines.Where(l => l.AccountId == a.Id).Sum(l => l.Debit - l.Credit),
            })
            .Where(a => a.Balance != 0)
            .ToList();

        var totalExpense = expenseBreakdown.Where(x => x.Balance > 0).Sum(x => x.Balance);
        var netIncome = totalIncome - totalExpense;

        // 3. Liabilities & Equity
        var liabilityTypes = new[] { "Liabilities", "Liability" };
        var liabilityIds = await this.db.ChartOfAccounts.Where(a => a.UserId == userId && a.IsActive && liabilityTypes.Contains(a.Type)).Select(a => a.Id).ToListAsync();

        var totalLiabilitiesMonthly = monthlyLines.Where(l => liabilityIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);
        var totalLiabilitiesAnnual = annualLines.Where(l => liabilityIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);
        var totalLiabilities = isAnnual ? totalLiabilitiesAnnual : totalLiabilitiesMonthly;
        var totalLiabilitiesCumulative = cumulativeLines.Where(l => liabilityIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);

        var equityIds = await this.db.ChartOfAccounts.Where(a => a.UserId == userId && a.IsActive && a.Type == "Equity").Select(a => a.Id).ToListAsync();
        var totalEquity = cumulativeLines.Where(l => equityIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);

        // 4. Chart Trend Data
        var trendData = isAnnual
            ? Enumerable.Range(1, 12).Select(m =>
            {
                var monthLines = periodLines.Where(l => l.EntryDate.Month == m);
                var rev = monthLines.Where(l => incomeIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);
                var exp = monthLines.Where(l => expenseAccountsMeta.Select(e => e.Id).Contains(l.AccountId)).Sum(l => l.Debit - l.Credit);
                return new ChartTrendItemDto
                {
                    Label = new DateTime(2000, m, 1).ToString("MMM", CultureInfo.InvariantCulture),
                    Revenue = rev,
                    Expense = exp,
                    Net = rev - exp,
                };
            }).ToList()
            : periodLines.GroupBy(l => l.EntryDate.Date).OrderBy(g => g.Key).Select(g =>
            {
                var rev = g.Where(l => incomeIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);
                var exp = g.Where(l => expenseAccountsMeta.Select(e => e.Id).Contains(l.AccountId)).Sum(l => l.Debit - l.Credit);
                return new ChartTrendItemDto
                {
                    Label = g.Key.ToString("dd MMM", CultureInfo.InvariantCulture),
                    Revenue = rev,
                    Expense = exp,
                    Net = rev - exp,
                };
            }).ToList();

        var activeCashBreakdown = isAnnual ? annualCashBreakdown : monthlyCashBreakdown;

        return new DashboardDataDto
        {
            Success = true,
            HasPeriodSelected = activePeriod != null,
            SelectedPeriodName = displayPeriodName,
            IsPeriodClosed = activePeriod?.IsClosed ?? false,

            TotalAssets = totalAssets,
            TotalCashOnHand = totalCashOnHand,
            TotalBankBalance = totalBankBalance,
            TotalLiabilities = totalLiabilities,

            TotalAssetsMonthly = totalAssetsMonthly,
            TotalAssetsAnnual = totalAssetsAnnual,
            TotalCashOnHandMonthly = totalCashOnHandMonthly,
            TotalCashOnHandAnnual = totalCashOnHandAnnual,
            TotalBankBalanceMonthly = totalBankBalanceMonthly,
            TotalBankBalanceAnnual = totalBankBalanceAnnual,
            TotalLiabilitiesMonthly = totalLiabilitiesMonthly,
            TotalLiabilitiesAnnual = totalLiabilitiesAnnual,

            TotalAssetsCumulative = cumulativeCashBreakdown.Sum(x => x.Balance),
            TotalCashOnHandCumulative = cumulativeCashBreakdown.Where(x => !x.IsBank).Sum(x => x.Balance),
            TotalBankBalanceCumulative = cumulativeCashBreakdown.Where(x => x.IsBank).Sum(x => x.Balance),
            TotalLiabilitiesCumulative = totalLiabilitiesCumulative,

            TotalEquity = totalEquity,
            TotalRevenue = totalIncome,
            TotalExpenses = totalExpense,
            NetIncome = netIncome,

            CashAccounts = activeCashBreakdown.Where(x => !x.IsBank).ToList(),
            BankAccounts = activeCashBreakdown.Where(x => x.IsBank).ToList(),
            CashAccountsMonthly = monthlyCashBreakdown.Where(x => !x.IsBank).ToList(),
            BankAccountsMonthly = monthlyCashBreakdown.Where(x => x.IsBank).ToList(),
            CashAccountsAnnual = annualCashBreakdown.Where(x => !x.IsBank).ToList(),
            BankAccountsAnnual = annualCashBreakdown.Where(x => x.IsBank).ToList(),

            ExpenseAccountsList = expenseBreakdown,
            ChartTrend = trendData,
            RecentEntries = new List<AumoBackend.DTOs.RecentActivityDto>(),
        };
    }
}

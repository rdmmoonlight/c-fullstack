using System.Globalization;
// <copyright file="DashboardController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Controllers;

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AumoBackend.Data;
using AumoBackend.DTOs;
using AumoBackend.Helpers;
using AumoBackend.Validators.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("/dashboard")]
[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]

public class DashboardController : ControllerBase
{
    private readonly AppDbContext db;

    private readonly IValidator<GetDashboardQuery> queryValidator;

    public DashboardController(AppDbContext db, IValidator<GetDashboardQuery> queryValidator)
    {
        this.db = db;
        this.queryValidator = queryValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardData([FromQuery] GetDashboardQuery query)
    {
        var validation = await this.queryValidator.ValidateAsync(query);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        var period = query.Period ?? "monthly";

        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

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

        // Scope per tab - REAL, bukan kumulatif
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

        // Kumulatif tetap diambil untuk referensi (kalau masih butuh)
        var cumulativeLines = await this.db.GeneralJournals
          .AsNoTracking()
          .Where(l => l.UserId == userId && l.EntryDate <= reqEnd)
          .Select(l => new { l.AccountId, l.Debit, l.Credit })
          .ToListAsync();

        // --- 1. KAS & BANK (FIX: Harus REAL per tab) ---
        var cashBankAccounts = await this.db.ChartOfAccounts
          .Where(a => a.UserId == userId && a.IsActive && a.Role == "CashAndEquivalents")
          .OrderBy(a => a.ReferenceNumber)
          .Select(a => new { a.Id, a.ReferenceNumber, a.AccountName })
          .ToListAsync();

        Func<List<dynamic>, List<dynamic>> buildCashBreakdown = (lines) =>
        {
            // lines is List of {AccountId, Debit, Credit}
            return cashBankAccounts.Select(a =>
            {
                var bal = lines.Where(l => l.AccountId == a.Id).Sum(l => (decimal)(l.Debit - l.Credit));
                bool isBank = a.AccountName.Contains("Bank", StringComparison.OrdinalIgnoreCase)
                           || a.AccountName.Contains("Rekening", StringComparison.OrdinalIgnoreCase);
                return new { accountId = a.Id, referenceNumber = a.ReferenceNumber, accountName = a.AccountName, balance = bal, isBank } as dynamic;
            }).ToList();
        };

        var monthlyCashBreakdown = cashBankAccounts.Select(a =>
        {
            var bal = monthlyLines.Where(l => l.AccountId == a.Id).Sum(l => l.Debit - l.Credit);
            bool isBank = a.AccountName.Contains("Bank", StringComparison.OrdinalIgnoreCase) || a.AccountName.Contains("Rekening", StringComparison.OrdinalIgnoreCase);
            return new { accountId = a.Id, referenceNumber = a.ReferenceNumber, accountName = a.AccountName, balance = bal, isBank };
        }).ToList();

        var annualCashBreakdown = cashBankAccounts.Select(a =>
        {
            var bal = annualLines.Where(l => l.AccountId == a.Id).Sum(l => l.Debit - l.Credit);
            bool isBank = a.AccountName.Contains("Bank", StringComparison.OrdinalIgnoreCase) || a.AccountName.Contains("Rekening", StringComparison.OrdinalIgnoreCase);
            return new { accountId = a.Id, referenceNumber = a.ReferenceNumber, accountName = a.AccountName, balance = bal, isBank };
        }).ToList();

        var cumulativeCashBreakdown = cashBankAccounts.Select(a =>
        {
            var bal = cumulativeLines.Where(l => l.AccountId == a.Id).Sum(l => l.Debit - l.Credit);
            bool isBank = a.AccountName.Contains("Bank", StringComparison.OrdinalIgnoreCase) || a.AccountName.Contains("Rekening", StringComparison.OrdinalIgnoreCase);
            return new { accountId = a.Id, referenceNumber = a.ReferenceNumber, accountName = a.AccountName, balance = bal, isBank };
        }).ToList();

        var totalCashOnHandMonthly = monthlyCashBreakdown.Where(x => !x.isBank).Sum(x => x.balance);
        var totalBankBalanceMonthly = monthlyCashBreakdown.Where(x => x.isBank).Sum(x => x.balance);
        var totalAssetsMonthly = monthlyCashBreakdown.Sum(x => x.balance);

        var totalCashOnHandAnnual = annualCashBreakdown.Where(x => !x.isBank).Sum(x => x.balance);
        var totalBankBalanceAnnual = annualCashBreakdown.Where(x => x.isBank).Sum(x => x.balance);
        var totalAssetsAnnual = annualCashBreakdown.Sum(x => x.balance);

        // Field utama yang dipakai frontend lama - sekarang REAL per tab
        var totalCashOnHand = isAnnual ? totalCashOnHandAnnual : totalCashOnHandMonthly;
        var totalBankBalance = isAnnual ? totalBankBalanceAnnual : totalBankBalanceMonthly;
        var totalAssets = isAnnual ? totalAssetsAnnual : totalAssetsMonthly;

        // --- 2. PENDAPATAN & BEBAN (Period - sudah benar) ---
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
          .Select(a => new { accountId = a.Id, referenceNumber = a.ReferenceNumber, accountName = a.AccountName, balance = periodLines.Where(l => l.AccountId == a.Id).Sum(l => l.Debit - l.Credit) })
          .Where(a => a.balance != 0)
          .ToList();

        var totalExpense = expenseBreakdown.Where(x => x.balance > 0).Sum(x => x.balance);
        var netIncome = totalIncome - totalExpense;

        // --- 3. LIABILITIES - kasih 2 versi: Real per tab + Kumulatif ---
        var liabilityTypes = new[] { "Liabilities", "Liability" };
        var liabilityIds = await this.db.ChartOfAccounts.Where(a => a.UserId == userId && a.IsActive && liabilityTypes.Contains(a.Type)).Select(a => a.Id).ToListAsync();

        var totalLiabilitiesMonthly = monthlyLines.Where(l => liabilityIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);
        var totalLiabilitiesAnnual = annualLines.Where(l => liabilityIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);
        var totalLiabilities = isAnnual ? totalLiabilitiesAnnual : totalLiabilitiesMonthly;
        var totalLiabilitiesCumulative = cumulativeLines.Where(l => liabilityIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);

        var equityIds = await this.db.ChartOfAccounts.Where(a => a.UserId == userId && a.IsActive && a.Type == "Equity").Select(a => a.Id).ToListAsync();
        var totalEquity = cumulativeLines.Where(l => equityIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);

        // --- 4. CHARTJS ---
        var trendData = isAnnual
          ? Enumerable.Range(1, 12).Select(m =>
           {
               var monthLines = periodLines.Where(l => l.EntryDate.Month == m);
               var rev = monthLines.Where(l => incomeIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);
               var exp = monthLines.Where(l => expenseAccountsMeta.Select(e => e.Id).Contains(l.AccountId)).Sum(l => l.Debit - l.Credit);
               return new { label = new DateTime(2000, m, 1).ToString("MMM", CultureInfo.InvariantCulture), revenue = rev, expense = exp, net = rev - exp };
           }).ToList()
            : periodLines.GroupBy(l => l.EntryDate.Date).OrderBy(g => g.Key).Select(g =>
            {
                var rev = g.Where(l => incomeIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);
                var exp = g.Where(l => expenseAccountsMeta.Select(e => e.Id).Contains(l.AccountId)).Sum(l => l.Debit - l.Credit);
                return new { label = g.Key.ToString("dd MMM", CultureInfo.InvariantCulture), revenue = rev, expense = exp, net = rev - exp };
            }).ToList();

        return this.Ok(new
        {
            success = true,
            hasPeriodSelected = activePeriod != null,
            selectedPeriodName = displayPeriodName,
            isPeriodClosed = activePeriod?.IsClosed ?? false,

            // REAL per tab - ini yang dipakai frontend baru
            totalAssets,
            totalCashOnHand,
            totalBankBalance,
            totalLiabilities,

            // Explicit monthly / annual biar frontend gak perlu fetch 2x
            totalAssetsMonthly,
            totalAssetsAnnual,
            totalCashOnHandMonthly,
            totalCashOnHandAnnual,
            totalBankBalanceMonthly,
            totalBankBalanceAnnual,
            totalLiabilitiesMonthly,
            totalLiabilitiesAnnual,

            // Kumulatif tetap dikirim buat audit / kalau butuh
            totalAssetsCumulative = cumulativeCashBreakdown.Sum(x => x.balance),
            totalCashOnHandCumulative = cumulativeCashBreakdown.Where(x => !x.isBank).Sum(x => x.balance),
            totalBankBalanceCumulative = cumulativeCashBreakdown.Where(x => x.isBank).Sum(x => x.balance),
            totalLiabilitiesCumulative,

            totalEquity,
            totalRevenue = totalIncome,
            totalExpenses = totalExpense,
            netIncome,

            // breakdown sesuai tab
            cashAccounts = (isAnnual ? annualCashBreakdown : monthlyCashBreakdown).Where(x => !x.isBank),
            bankAccounts = (isAnnual ? annualCashBreakdown : monthlyCashBreakdown).Where(x => x.isBank),
            cashAccountsMonthly = monthlyCashBreakdown.Where(x => !x.isBank),
            bankAccountsMonthly = monthlyCashBreakdown.Where(x => x.isBank),
            cashAccountsAnnual = annualCashBreakdown.Where(x => !x.isBank),
            bankAccountsAnnual = annualCashBreakdown.Where(x => x.isBank),

            expenseAccountsList = expenseBreakdown,
            chartTrend = trendData,
            recentEntries = Array.Empty<object>(),
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub");
        return Guid.TryParse(userIdStr, out Guid userId) ? userId : Guid.Empty;
    }
}

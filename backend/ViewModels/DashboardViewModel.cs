// <copyright file="DashboardViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.ViewModels;

using System;
using System.Collections.Generic;
using AumoBackend.DTOs;

public class DashboardViewModel
{
    public Guid UserId { get; set; }

    public bool HasSelectedPeriod { get; set; }

    public bool IsSelectedPeriodClosed { get; set; }

    public string ActivePeriodName { get; set; } = string.Empty;

    public DateTime ActivePeriodStart { get; set; }

    public DateTime ActivePeriodEnd { get; set; }

    public decimal TotalCashAndEquivalents { get; set; }

    public decimal TotalAssets { get; set; }

    public decimal TotalLiabilities { get; set; }

    public decimal RevenueThisPeriod { get; set; }

    public decimal OperatingExpenses { get; set; }

    public decimal NetIncome { get; set; }

    public decimal? RevenueTrendPercent { get; set; }

    public decimal? ExpenseTrendPercent { get; set; }

    public decimal? NetIncomeTrendPercent { get; set; }

    public List<string> ChartLabels { get; set; } = new();

    public List<decimal> ChartRevenue { get; set; } = new();

    public List<decimal> ChartExpenses { get; set; } = new();

    public List<string> ExpenseCategoryLabels { get; set; } = new();

    public List<decimal> ExpenseCategoryValues { get; set; } = new();

    public List<CoaBalanceDto> MainCoaBalances { get; set; } = new();

    public List<JournalEntryDto> RecentJournals { get; set; } = new();

    public decimal MonthlyBurnRate { get; set; }

    public double CashRunwayMonths { get; set; }

    public int FinancialHealthScore { get; set; }
}

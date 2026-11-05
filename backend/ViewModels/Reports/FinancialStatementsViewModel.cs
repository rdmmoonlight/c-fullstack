// <copyright file="FinancialStatementsViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.ViewModels.Reports;

using System;
using System.Collections.Generic;
using System.Linq;
using AumoBackend.DTOs.Reports;

public class CashFlowStatementViewModel
{
    public decimal BeginningCash { get; set; }

    public List<CashFlowLine> OperatingActivities { get; set; } = new();

    public List<CashFlowLine> InvestingActivities { get; set; } = new();

    public List<CashFlowLine> FinancingActivities { get; set; } = new();

    public decimal NetOperating => this.OperatingActivities.Sum(l => l.Amount);

    public decimal NetInvesting => this.InvestingActivities.Sum(l => l.Amount);

    public decimal NetFinancing => this.FinancingActivities.Sum(l => l.Amount);

    public decimal NetChangeInCash => this.NetOperating + this.NetInvesting + this.NetFinancing;

    public decimal EndingCash => this.BeginningCash + this.NetChangeInCash;
}

public class IncomeStatementViewModel
{
    public DateTime AsOfDate { get; set; }

    public List<IncomeStatementLine> Revenues { get; set; } = new();

    public List<IncomeStatementLine> OperatingExpenses { get; set; } = new();

    public List<IncomeStatementLine> OtherIncome { get; set; } = new();

    public List<IncomeStatementLine> OtherExpenses { get; set; } = new();

    public decimal TotalRevenue => this.Revenues.Sum(l => l.Amount);

    public decimal TotalOperatingExpenses => this.OperatingExpenses.Sum(l => l.Amount);

    public decimal OperatingIncome => this.TotalRevenue - this.TotalOperatingExpenses;

    public decimal TotalOtherIncome => this.OtherIncome.Sum(l => l.Amount);

    public decimal TotalOtherExpenses => this.OtherExpenses.Sum(l => l.Amount);

    public decimal NetIncome => this.OperatingIncome + this.TotalOtherIncome - this.TotalOtherExpenses;
}

public class StatementOfFinancialPositionViewModel
{
    public DateTime AsOfDate { get; set; }

    public bool IsPostClosing { get; set; }

    public List<FinancialPositionLine> Assets { get; set; } = new();

    public List<FinancialPositionLine> Liabilities { get; set; } = new();

    public List<FinancialPositionLine> EquityExcludingRetainedEarnings { get; set; } = new();

    public decimal RetainedEarningsEnding { get; set; }

    public decimal TotalAssets => this.Assets.Sum(l => l.Amount);

    public decimal TotalLiabilities => this.Liabilities.Sum(l => l.Amount);

    public decimal TotalEquity => this.EquityExcludingRetainedEarnings.Sum(l => l.Amount) + this.RetainedEarningsEnding;

    public decimal TotalLiabilitiesAndEquity => this.TotalLiabilities + this.TotalEquity;

    public bool IsBalanced => Math.Round(this.TotalAssets - this.TotalLiabilitiesAndEquity, 2) == 0;
}

public class RetainedEarningsViewModel
{
    public string AccountName { get; set; } = "Retained Earnings";

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal BeginningBalance { get; set; }

    public decimal NetIncome { get; set; }

    public decimal Dividends { get; set; }

    public decimal EndingBalance => this.BeginningBalance + this.NetIncome - this.Dividends;
}

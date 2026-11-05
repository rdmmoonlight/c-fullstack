// <copyright file="PeriodsViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.ViewModels;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AumoBackend.Models;

public class OpenPeriodViewModel
{
    public const string ModeLoadExisting = "LoadExisting";
    public const string ModeCreateNew = "CreateNew";

    [Required]
    [Display(Name = "Month")]
    public int Month { get; set; }

    [Required]
    [Display(Name = "Year")]
    public int Year { get; set; }

    [Required]
    public string SetupMode { get; set; } = ModeLoadExisting;

    [Display(Name = "Cash Account")]
    public int? CashAccountId { get; set; }

    [Display(Name = "Bank Account")]
    public int? BankAccountId { get; set; }

    [Display(Name = "Retained Earnings Account")]
    public int? RetainedEarningsAccountId { get; set; }

    [Display(Name = "Cash Account Ref (Code)")]
    public string? CashAccountCode { get; set; }

    [Display(Name = "Cash Account Name")]
    public string? CashAccountName { get; set; }

    [Display(Name = "Cash Opening Balance")]
    [Range(0, double.MaxValue, ErrorMessage = "Balance cannot be negative.")]
    public decimal? CashBalance { get; set; }

    [Display(Name = "Bank Account Ref (Code)")]
    public string? BankAccountCode { get; set; }

    [Display(Name = "Bank Account Name")]
    public string? BankAccountName { get; set; }

    [Display(Name = "Bank Opening Balance")]
    [Range(0, double.MaxValue, ErrorMessage = "Balance cannot be negative.")]
    public decimal? BankBalance { get; set; }

    [Display(Name = "Retained Earnings Ref (Code)")]
    public string? RetainedEarningsAccountCode { get; set; }

    [Display(Name = "Retained Earnings Name")]
    public string? RetainedEarningsAccountName { get; set; }

    public List<ChartOfAccount> PermanentAccounts { get; set; } = new();

    public List<ChartOfAccount> AvailableCashAndBankAccounts { get; set; } = new();

    public List<ChartOfAccount> AvailableRetainedEarningsAccounts { get; set; } = new();

    public bool HasExistingPermanentAccounts { get; set; }
}

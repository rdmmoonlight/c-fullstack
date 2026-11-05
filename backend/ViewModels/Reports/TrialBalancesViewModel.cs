namespace AumoBackend.ViewModels.Reports;

using System;
using System.Collections.Generic;
using System.Linq;
using AumoBackend.DTOs.Reports;

public class TrialBalancesViewModel
{
    public string Title { get; set; } = string.Empty;

    public List<TrialBalancesRow> Rows { get; set; } = new();

    public decimal TotalDebit => this.Rows.Sum(r => r.Debit);

    public decimal TotalCredit => this.Rows.Sum(r => r.Credit);

    public bool IsBalanced => Math.Round(this.TotalDebit - this.TotalCredit, 2) == 0;
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace AumoFinance.Models.Reports;

public class GeneralLedgerAccountViewModel
{
    public int AccountId { get; set; }
    public int ReferenceNumber { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal EndingBalance { get; set; }
    public List<GeneralLedgerLineViewModel> Lines { get; set; } = new();
    public CultureInfo IdrCulture { get; set; } = new("id-ID");

    public string FormattedEndingBalance => EndingBalance.ToString("C0", IdrCulture);
    public string EndingBalanceColor => EndingBalance >= 0 ? "#4FA36A" : "#D7192F";
}

public class GeneralLedgerLineViewModel
{
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
    public CultureInfo IdrCulture { get; set; } = new("id-ID");

    public string FormattedDebit => Debit.ToString("C0", IdrCulture);
    public string FormattedCredit => Credit.ToString("C0", IdrCulture);
    public string FormattedRunningBalance => RunningBalance.ToString("C0", IdrCulture);

    public bool HasDebit => Debit > 0;
    public bool HasCredit => Credit > 0;
}

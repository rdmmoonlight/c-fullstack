using System;
using System.Collections.Generic;
using System.Globalization;

namespace AumoFinance.Models;

public class LedgerAccountDisplayModel
{
    public int AccountId { get; set; }
    public int ReferenceNumber { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool NormalBalanceIsDebit { get; set; }
    public decimal EndingBalance { get; set; }
    public List<LedgerLineDisplayModel> Lines { get; set; } = new();

    public string EndingBalanceColor => EndingBalance >= 0 ? "#4FA36A" : "#D7192F";

    public string FormattedEndingBalance
    {
        get
        {
            var culture = new CultureInfo("id-ID");
            var position = NormalBalanceIsDebit ? "Dr" : "Cr";
            return $"Saldo: Rp {EndingBalance.ToString("N0", culture)} ({position})";
        }
    }
}

public class LedgerLineDisplayModel
{
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }

    public bool HasDebit => Debit > 0;
    public bool HasCredit => Credit > 0;

    public string FormattedDebit => $"Dr: Rp {Debit.ToString("N0", new CultureInfo("id-ID"))}";
    public string FormattedCredit => $"Cr: Rp {Credit.ToString("N0", new CultureInfo("id-ID"))}";
    public string FormattedRunningBalance => $"Rp {RunningBalance.ToString("N0", new CultureInfo("id-ID"))}";
}

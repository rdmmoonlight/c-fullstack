using System.Globalization;

namespace AumoFinance.Models;

public static class AccountClassification
{
    public static bool IsTemporary(string type) =>
        type.Equals("OperatingIncome", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("OperatingExpense", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("OperatingExpenses", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("OtherIncome", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("OtherExpense", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("OtherExpenses", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("Revenue", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("Expense", StringComparison.OrdinalIgnoreCase);

    public static bool IsPermanent(string type) => !IsTemporary(type);

    public static bool NormalBalanceIsDebit(string type) =>
        type.Equals("Asset", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("Assets", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("Expense", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("OperatingExpense", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("OperatingExpenses", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("OtherExpense", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("OtherExpenses", StringComparison.OrdinalIgnoreCase);
}

public class TrialBalanceRowViewModel
{
    public int AccountId { get; set; }
    public int ReferenceNumber { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool NormalBalanceIsDebit { get; set; }
    public decimal NetBalance { get; set; }
    public decimal Debit => NormalBalanceIsDebit ? Math.Max(NetBalance, 0) : Math.Max(-NetBalance, 0);
    public decimal Credit => NormalBalanceIsDebit ? Math.Max(-NetBalance, 0) : Math.Max(NetBalance, 0);

    private static readonly CultureInfo Idr = new("id-ID");
    public string FormattedDebit => Debit > 0 ? Debit.ToString("N0", Idr) : "-";
    public string FormattedCredit => Credit > 0 ? Credit.ToString("N0", Idr) : "-";
}

public class LedgerAccountViewModel
{
    public int AccountId { get; set; }
    public int ReferenceNumber { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool NormalBalanceIsDebit { get; set; }
    public decimal EndingBalance { get; set; }
    public List<LedgerLineViewModel> Lines { get; set; } = new();

    public string EndingBalanceColor => EndingBalance >= 0 ? "#4FA36A" : "#D7192F";
    public string FormattedEndingBalance => $"Saldo: Rp {EndingBalance.ToString("N0", Idr)} ({(NormalBalanceIsDebit ? "Dr" : "Cr")})";

    private static readonly CultureInfo Idr = new("id-ID");
}

public class LedgerLineViewModel
{
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }

    public bool HasDebit => Debit > 0;
    public bool HasCredit => Credit > 0;
    public string FormattedDebit => $"Dr: Rp {Debit.ToString("N0", Idr)}";
    public string FormattedCredit => $"Cr: Rp {Credit.ToString("N0", Idr)}";
    public string FormattedRunningBalance => $"Rp {RunningBalance.ToString("N0", Idr)}";

    private static readonly CultureInfo Idr = new("id-ID");
}

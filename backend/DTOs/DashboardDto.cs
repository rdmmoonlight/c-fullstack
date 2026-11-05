
namespace AumoBackend.DTOs;

public class DashboardDataDto
{
    public bool Success { get; set; } = true;

    public bool HasPeriodSelected { get; set; }

    public string SelectedPeriodName { get; set; } = string.Empty;

    public bool IsPeriodClosed { get; set; }

    public decimal TotalAssets { get; set; }

    public decimal TotalLiabilities { get; set; }

    public decimal TotalEquity { get; set; }

    public decimal NetIncome { get; set; }

    public decimal TotalCashOnHand { get; set; }

    public decimal TotalBankBalance { get; set; }

    public decimal TotalAssetsMonthly { get; set; }

    public decimal TotalAssetsAnnual { get; set; }

    public decimal TotalCashOnHandMonthly { get; set; }

    public decimal TotalCashOnHandAnnual { get; set; }

    public decimal TotalBankBalanceMonthly { get; set; }

    public decimal TotalBankBalanceAnnual { get; set; }

    public decimal TotalLiabilitiesMonthly { get; set; }

    public decimal TotalLiabilitiesAnnual { get; set; }

    public decimal TotalAssetsCumulative { get; set; }

    public decimal TotalCashOnHandCumulative { get; set; }

    public decimal TotalBankBalanceCumulative { get; set; }

    public decimal TotalLiabilitiesCumulative { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal TotalExpenses { get; set; }

    public List<CoaBalanceDto> Accounts { get; set; } = new();

    public List<CashAccountItemDto> CashAccounts { get; set; } = new();

    public List<CashAccountItemDto> BankAccounts { get; set; } = new();

    public List<CashAccountItemDto> CashAccountsMonthly { get; set; } = new();

    public List<CashAccountItemDto> BankAccountsMonthly { get; set; } = new();

    public List<CashAccountItemDto> CashAccountsAnnual { get; set; } = new();

    public List<CashAccountItemDto> BankAccountsAnnual { get; set; } = new();

    public List<ExpenseAccountItemDto> ExpenseAccountsList { get; set; } = new();

    public List<ChartTrendItemDto> ChartTrend { get; set; } = new();

    public List<RecentActivityDto> RecentEntries { get; set; } = new();
}

public class CoaBalanceDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string AccountCode { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Balance { get; set; }
}

public class CashAccountItemDto
{
    public object AccountId { get; set; } = string.Empty;

    public string ReferenceNumber { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsBank { get; set; }

    public decimal Balance { get; set; }
}

public class ExpenseAccountItemDto
{
    public object AccountId { get; set; } = string.Empty;

    public string ReferenceNumber { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal Balance { get; set; }
}

public class ChartTrendItemDto
{
    public string Label { get; set; } = string.Empty;

    public string Period { get; set; } = string.Empty;

    public decimal Revenue { get; set; }

    public decimal Expense { get; set; }

    public decimal Expenses { get; set; }

    public decimal Net { get; set; }
}

public class RecentActivityDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}

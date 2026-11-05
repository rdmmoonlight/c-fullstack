namespace AumoBackend.DTOs;

public class GetNotificationsQuery
{
    public int Limit { get; set; } = 20;
}

public class GetDashboardQuery
{
    public string? Period { get; set; } = "monthly";
}

public class GetKursQuery
{
    public string? BaseCurrency { get; set; } = "USD";

    public string? TargetCurrency { get; set; } = "IDR";
}

public class GetAccountsQuery
{
    public string? Search { get; set; }

    public string? Category { get; set; }
}

public class SearchDescriptionsQuery
{
    public string? Q { get; set; }
}

public class NextTransactionNumberQuery
{
    public string? JournalType { get; set; } = "General";

    public DateTime? EntryDate { get; set; }
}

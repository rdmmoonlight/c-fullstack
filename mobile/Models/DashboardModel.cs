namespace AumoFinance.Models;

public class DashboardModel
{
    public decimal TotalCash { get; set; }
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetIncome { get; set; }
    public string ActivePeriod { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
}

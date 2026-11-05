using System.Text.Json.Serialization;

namespace AumoFinance.Models;

public class DashboardDto
{
    [JsonPropertyName("activePeriod")]
    public string? ActivePeriod { get; set; }

    [JsonPropertyName("isClosed")]
    public bool IsClosed { get; set; }

    [JsonPropertyName("totalCash")]
    public decimal TotalCash { get; set; }

    [JsonPropertyName("netIncome")]
    public decimal NetIncome { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("expenses")]
    public decimal Expenses { get; set; }
}

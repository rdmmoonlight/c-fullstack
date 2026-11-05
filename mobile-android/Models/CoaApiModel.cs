using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AumoFinance.Models;

public class CoaApiModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }
}

// Wrapper respons GET /api/mobile/chart-of-accounts: { success, selectedPeriodName, accounts: [...] }
public class CoaEnvelopeModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("selectedPeriodName")]
    public string? SelectedPeriodName { get; set; }

    [JsonPropertyName("accounts")]
    public List<CoaApiModel> Accounts { get; set; } = new();
}

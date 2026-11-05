using System.Text.Json.Serialization;

namespace AumoFinance.Models;

public class AccountLookupModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("referenceNumber")]
    public int ReferenceNumber { get; set; }

    public string DisplayText => $"[{ReferenceNumber}] {AccountName}";
}

using System.Text.Json.Serialization;

namespace AumoFinance.Models;

public class CreateJournalDto
{
    [JsonPropertyName("entryDate")]
    public DateTime EntryDate { get; set; } = DateTime.Today;

    [JsonPropertyName("lines")]
    public List<CreateJournalLineDto> Lines { get; set; } = new();
}

public class CreateJournalLineDto
{
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    [JsonPropertyName("lineDescription")]
    public string? LineDescription { get; set; }

    [JsonPropertyName("debit")]
    public decimal Debit { get; set; }

    [JsonPropertyName("credit")]
    public decimal Credit { get; set; }
}

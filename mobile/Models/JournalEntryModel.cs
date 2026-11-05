namespace AumoFinance.Models;

public class JournalEntryModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

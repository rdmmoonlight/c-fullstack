namespace AumoBackend.Services.JournalEntries;

public interface ITransactionNumberService
{
    Task<string> GenerateAsync(Guid userId, string journalType, DateTime entryDate);

    Task<string> PeekNextAsync(Guid userId, string journalType, DateTime entryDate);
}

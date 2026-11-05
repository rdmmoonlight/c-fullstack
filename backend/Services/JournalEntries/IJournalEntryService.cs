namespace AumoBackend.Services.JournalEntries;

using AumoBackend.DTOs.Reports;

public interface IJournalEntryService
{
    Task<GeneralJournalResponseDto?> GetByIdAsync(Guid userId, int id);
    Task<List<GeneralJournalResponseDto>> GetByTransactionNumberAsync(Guid userId, string transactionNumber);
    Task<GeneralJournalResponseDto> CreateAsync(Guid userId, CreateGeneralJournalRequest request);
    Task UpdateAsync(Guid userId, int id, UpdateGeneralJournalRequest request);
    Task DeleteByTransactionNumberAsync(Guid userId, string transactionNumber);
    Task DeleteAsync(Guid userId, int id);
    Task<List<string>> SearchLineDescriptionsAsync(Guid userId, string query);
    Task<string> GetNextTransactionNumberAsync(Guid userId, string journalType, DateTime? entryDate);
}
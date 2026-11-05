using AumoBackend.DTOs.Reports;
using AumoBackend.Models;

namespace AumoBackend.Services.Reports.Journals;

public interface IJournalsService
{
    /// <summary>Jurnal tersimpan untuk periode terpilih. journalType: "General", "Adjusting", atau "Closing".</summary>
    Task<JournalReportResult> GetJournalAsync(Guid userId, string journalType);

    /// <summary>
    /// Menghapus jurnal penyesuaian. Penghapusan didelegasikan ke IJournalEntryService agar aturan
    /// periode tertutup (PeriodLock) sama dengan jurnal umum. Melempar KeyNotFoundException jika tidak
    /// ada dan InvalidOperationException jika periode tertutup.
    /// </summary>
    Task DeleteAdjustingAsync(Guid userId, int journalEntryId);

    /// <summary>
    /// Jurnal penutup periode terpilih. Dihitung on-the-fly dari GeneralJournals (tidak pernah disimpan).
    /// </summary>
    Task<ClosingJournalReportResult> GetClosingJournalAsync(Guid userId);

    Task<List<ClosingJournalEntryGroupApiResponse>> BuildClosingJournalGroupsAsync(Guid userId, Period period);
}

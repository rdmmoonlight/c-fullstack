namespace AumoBackend.Services.Reports.Worksheet;

using AumoBackend.DTOs.Reports;

public interface IWorksheetService
{
    Task<WorksheetResult> GetWorksheetAsync(Guid userId);

    /// <summary>Mengganti snapshot kertas kerja periode tersebut di tabel Worksheet (baris akun + baris total).</summary>
    Task SaveSnapshotAsync(Guid userId, WorksheetResult result);
}

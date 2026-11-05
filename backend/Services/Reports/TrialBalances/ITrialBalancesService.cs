using AumoBackend.DTOs.Reports;
using AumoBackend.Models;

namespace AumoBackend.Services.Reports.TrialBalances;

public interface ITrialBalancesService
{
    Task<List<TrialBalancesRow>> BuildTrialBalancesRowsAsync(
        Guid userId,
        Period period,
        bool includeAdjusting = false,
        string reportType = "unadjusted");

    Task<decimal> ComputeRetainedEarningsEndingAsync(Guid userId, Period period);

    /// <summary>Mengganti snapshot periode tersebut di tabel sesuai reportType.</summary>
    Task SaveSnapshotAsync(string reportType, Guid userId, Period period, IReadOnlyList<TrialBalancesRow> rows);

    Task<List<UnadjustedTrialBalanceRow>> BuildUnadjustedTrialBalanceAsync(Guid userId, Period period);

    Task<List<AdjustedTrialBalanceRow>> BuildAdjustedTrialBalanceAsync(Guid userId, Period period);

    Task<List<PostClosingTrialBalanceRow>> BuildPostClosingTrialBalanceAsync(Guid userId, Period period);
}
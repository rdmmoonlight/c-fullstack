namespace AumoBackend.Services.Reports.FinancialStatements;

using AumoBackend.DTOs.Reports;
using AumoBackend.Models;

/// <summary>
/// Laporan keuangan (laba rugi, laba ditahan, arus kas, posisi keuangan) yang dibaca dari tabel Worksheet,
/// bukan dihitung ulang dari jurnal. Jika snapshot belum ada atau sudah basi, snapshot dibangun dulu.
/// </summary>
public interface IFinancialStatementsService
{
    Task<IncomeStatementApiResponse> BuildIncomeStatementAsync(Guid userId, Period period);

    Task<RetainedEarningsApiResponse> BuildRetainedEarningsAsync(Guid userId, Period period);

    Task<StatementOfCashFlowApiResponse> BuildCashFlowAsync(Guid userId, Period period);

    Task<StatementOfFinancialPositionApiResponse> BuildSofpAsync(Guid userId, Period period, bool isPostClosing);
}

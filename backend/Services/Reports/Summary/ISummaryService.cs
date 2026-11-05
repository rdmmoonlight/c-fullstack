namespace AumoBackend.Services.Reports.Summary;

using System.Security.Claims;
using AumoBackend.DTOs.Reports;

public interface ISummaryService
{
    Task<SummaryDto?> GetSummaryAsync(ClaimsPrincipal userPrincipal);
}

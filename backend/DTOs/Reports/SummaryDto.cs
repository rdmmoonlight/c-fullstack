
namespace AumoBackend.DTOs.Reports;

public class SummaryDto
{
    public Guid? SelectedPeriodId { get; set; }

    public int TotalJournal { get; set; }

    public int ActiveCoa { get; set; }

    public string ActivePeriodName { get; set; } = string.Empty;

    public bool IsPeriodOpen { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public T? Data { get; set; }
}

namespace AumoBackend.Models;

public static class PeriodLock
{
    public static bool IsLocked(Period? period)
    {
        if (period is null)
        {
            return false;
        }

        return period.GetType()
            .GetProperties()
            .FirstOrDefault(property => property.Name == "IsClosed")?
            .GetValue(period) is true;
    }

    public static bool IsLocked(bool isClosed) => isClosed;

    public static bool IsDateLocked(DateTime date, IEnumerable<Period>? periods)
    {
        if (periods is null)
        {
            return false;
        }

        foreach (var period in periods)
        {
            var properties = period?.GetType().GetProperties();
            var startDate = properties?
                .FirstOrDefault(property => property.Name == "StartDate")?
                .GetValue(period);
            var endDate = properties?
                .FirstOrDefault(property => property.Name == "EndDate")?
                .GetValue(period);

            if (IsLocked(period)
                && startDate is DateTime start
                && endDate is DateTime end
                && date >= start
                && date <= end)
            {
                return true;
            }
        }

        return false;
    }
}

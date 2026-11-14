namespace Application.Helpers;

/// <summary>
/// When the daily overdue job runs: at 03:00 UTC. Dates are UTC dates; there are no time zones.
/// </summary>
public static class OverdueSchedule
{
    public static readonly TimeOnly RunTime = new(3, 0);

    /// <summary>
    /// The UTC date at the given instant
    /// </summary>
    public static DateOnly Today(DateTimeOffset utcNow)
    {
        return DateOnly.FromDateTime(utcNow.UtcDateTime);
    }

    /// <summary>
    /// When the run after a successful run for the given UTC date is due: 03:00 UTC the next day.
    /// A date that has had its run isn't run again, even when it ran before its own 03:00.
    /// </summary>
    public static DateTimeOffset NextRun(DateOnly lastRunDate)
    {
        return new DateTimeOffset(lastRunDate.AddDays(1).ToDateTime(RunTime), TimeSpan.Zero);
    }
}

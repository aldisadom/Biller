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
    /// The next 03:00 UTC after the given instant. At 03:00 exactly it is the next day's.
    /// </summary>
    public static DateTimeOffset NextRun(DateTimeOffset utcNow)
    {
        DateTime now = utcNow.UtcDateTime;
        DateTime run = now.Date.Add(RunTime.ToTimeSpan());
        if (now >= run)
            run = run.AddDays(1);

        return new DateTimeOffset(run, TimeSpan.Zero);
    }
}

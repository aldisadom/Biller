namespace Application.Helpers;

/// <summary>
/// When the daily overdue job runs: at 00:01 Vilnius time, the time zone of the invoice dates.
/// The containers run on UTC, so every date and run time goes through the zone.
/// </summary>
public static class OverdueSchedule
{
    public static readonly TimeOnly RunTime = new(0, 1);

    /// <summary>
    /// The Vilnius time zone, by its IANA id (Linux, and Windows with ICU), else on Windows by its
    /// Windows id: WebAPI runs with InvariantGlobalization, so Windows can't convert the IANA id.
    /// Throws TimeZoneNotFoundException when it isn't found.
    /// </summary>
    public static TimeZoneInfo FindZone(string ianaId = "Europe/Vilnius", string windowsId = "FLE Standard Time")
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out TimeZoneInfo? zone))
            return zone;

        // Only Windows has Windows ids in its registry. Linux turns one back into an IANA id
        // (Europe/Kiev for this one, which Ubuntu 24.04 no longer ships) and finds nothing better.
        if (OperatingSystem.IsWindows())
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);

        throw new TimeZoneNotFoundException($"The time zone {ianaId} was not found");
    }

    /// <summary>
    /// The date in the zone at the given instant
    /// </summary>
    public static DateOnly Today(DateTimeOffset utcNow, TimeZoneInfo zone)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, zone).DateTime);
    }

    /// <summary>
    /// The next 00:01 in the zone after the given instant, as a UTC instant. At 00:01 exactly it is
    /// the next day's. 00:01 is never skipped or repeated in Vilnius: the clocks change at 03:00/04:00.
    /// </summary>
    public static DateTimeOffset NextRun(DateTimeOffset utcNow, TimeZoneInfo zone)
    {
        DateTime local = TimeZoneInfo.ConvertTime(utcNow, zone).DateTime;
        DateTime run = local.Date.Add(RunTime.ToTimeSpan());
        if (local >= run)
            run = run.AddDays(1);

        return new DateTimeOffset(run, zone.GetUtcOffset(run)).ToUniversalTime();
    }
}

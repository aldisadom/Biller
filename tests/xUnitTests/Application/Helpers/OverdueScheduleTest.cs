using Application.Helpers;
using AwesomeAssertions;

namespace xUnitTests.Application.Helpers;

public class OverdueScheduleTest
{
    // Per test, not static: a failed lookup then fails each test with its own error
    private readonly TimeZoneInfo _vilnius = OverdueSchedule.FindZone();

    private static DateTimeOffset Utc(string time) => DateTimeOffset.Parse(time + "Z");

    public static IEnumerable<TheoryDataRow<string, string>> GetFindZoneTestData()
    {
        yield return new("Europe/Vilnius", "No Such Zone") { Label = "the IANA id is found" };
        yield return new("No/Such_Zone", "FLE Standard Time") { Label = "without the IANA id, the Windows id is used" };
    }

    [Theory]
    [MemberData(nameof(GetFindZoneTestData))]
    public void FindZone_GivenIds_ReturnsVilniusOffsets(string ianaId, string windowsId)
    {
        //Act
        TimeZoneInfo zone = OverdueSchedule.FindZone(ianaId, windowsId);

        //Assert
        zone.GetUtcOffset(Utc("2026-01-15T12:00:00")).Should().Be(TimeSpan.FromHours(2));
        zone.GetUtcOffset(Utc("2026-07-15T12:00:00")).Should().Be(TimeSpan.FromHours(3));
    }

    [Fact]
    public void FindZone_GivenUnknownIds_ThrowsTimeZoneNotFoundException()
    {
        //Act
        Action act = () => OverdueSchedule.FindZone("No/Such_Zone", "No Such Zone");

        //Assert
        act.Should().Throw<TimeZoneNotFoundException>();
    }

    public static IEnumerable<TheoryDataRow<DateTimeOffset, DateOnly>> GetTodayTestData()
    {
        yield return new(Utc("2026-01-15T21:59:00"), new DateOnly(2026, 1, 15)) { Label = "23:59 in Vilnius in winter (UTC+2) is still that date" };
        yield return new(Utc("2026-01-15T22:00:00"), new DateOnly(2026, 1, 16)) { Label = "midnight in Vilnius in winter is already the next date in Vilnius, though not in UTC" };
        yield return new(Utc("2026-07-15T20:59:00"), new DateOnly(2026, 7, 15)) { Label = "23:59 in Vilnius in summer (UTC+3) is still that date" };
        yield return new(Utc("2026-07-15T21:00:00"), new DateOnly(2026, 7, 16)) { Label = "midnight in Vilnius in summer is already the next date" };
    }

    [Theory]
    [MemberData(nameof(GetTodayTestData))]
    public void Today_GivenUtcInstant_ReturnsVilniusDate(DateTimeOffset utcNow, DateOnly expected)
    {
        //Act
        DateOnly result = OverdueSchedule.Today(utcNow, _vilnius);

        //Assert
        result.Should().Be(expected);
    }

    public static IEnumerable<TheoryDataRow<DateTimeOffset, DateTimeOffset>> GetNextRunTestData()
    {
        yield return new(Utc("2026-01-15T21:30:00"), Utc("2026-01-15T22:01:00")) { Label = "23:30 in Vilnius runs at 00:01 the same night" };
        yield return new(Utc("2026-01-15T22:00:30"), Utc("2026-01-15T22:01:00")) { Label = "00:00:30 in Vilnius, before that day's 00:01, runs at 00:01 the same day" };
        yield return new(Utc("2026-01-15T22:01:00"), Utc("2026-01-16T22:01:00")) { Label = "exactly 00:01 in Vilnius runs again the next night" };
        yield return new(Utc("2026-01-16T10:00:00"), Utc("2026-01-16T22:01:00")) { Label = "midday in Vilnius in winter runs at 00:01 that night (22:01 UTC)" };
        yield return new(Utc("2026-07-15T12:00:00"), Utc("2026-07-15T21:01:00")) { Label = "midday in Vilnius in summer runs at 00:01 that night (21:01 UTC)" };
        yield return new(Utc("2026-10-24T12:00:00"), Utc("2026-10-24T21:01:00")) { Label = "the night the clocks go back, 00:01 is still summer time" };
        yield return new(Utc("2026-10-25T12:00:00"), Utc("2026-10-25T22:01:00")) { Label = "the day after the clocks go back, 00:01 is winter time" };
        yield return new(Utc("2027-03-27T12:00:00"), Utc("2027-03-27T22:01:00")) { Label = "the night the clocks go forward, 00:01 is still winter time" };
        yield return new(Utc("2027-03-28T12:00:00"), Utc("2027-03-28T21:01:00")) { Label = "the day after the clocks go forward, 00:01 is summer time" };
    }

    [Theory]
    [MemberData(nameof(GetNextRunTestData))]
    public void NextRun_GivenUtcInstant_ReturnsNext0001InVilnius(DateTimeOffset utcNow, DateTimeOffset expected)
    {
        //Act
        DateTimeOffset result = OverdueSchedule.NextRun(utcNow, _vilnius);

        //Assert
        result.Should().Be(expected);
        result.Offset.Should().Be(TimeSpan.Zero);
    }
}

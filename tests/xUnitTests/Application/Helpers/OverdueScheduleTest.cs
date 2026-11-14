using Application.Helpers;
using AwesomeAssertions;

namespace xUnitTests.Application.Helpers;

public class OverdueScheduleTest
{
    private static DateTimeOffset Utc(string time) => DateTimeOffset.Parse(time + "Z");

    public static IEnumerable<TheoryDataRow<DateTimeOffset, DateOnly>> GetTodayTestData()
    {
        yield return new(Utc("2026-01-15T23:59:59"), new DateOnly(2026, 1, 15)) { Label = "a second before midnight UTC is still that date" };
        yield return new(Utc("2026-01-16T00:00:00"), new DateOnly(2026, 1, 16)) { Label = "midnight UTC is the next date" };
    }

    [Theory]
    [MemberData(nameof(GetTodayTestData))]
    public void Today_GivenUtcInstant_ReturnsUtcDate(DateTimeOffset utcNow, DateOnly expected)
    {
        //Act
        DateOnly result = OverdueSchedule.Today(utcNow);

        //Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Today_GivenInstantWithOffset_ReturnsItsUtcDate()
    {
        //Arrange
        // 01:00 on 16 January at UTC+2 is 23:00 on 15 January UTC
        var utcNow = new DateTimeOffset(2026, 1, 16, 1, 0, 0, TimeSpan.FromHours(2));

        //Act
        DateOnly result = OverdueSchedule.Today(utcNow);

        //Assert
        result.Should().Be(new DateOnly(2026, 1, 15));
    }

    public static IEnumerable<TheoryDataRow<DateTimeOffset, DateTimeOffset>> GetNextRunTestData()
    {
        yield return new(Utc("2026-01-16T02:59:00"), Utc("2026-01-16T03:00:00")) { Label = "before 03:00 UTC runs at 03:00 the same day" };
        yield return new(Utc("2026-01-16T03:00:00"), Utc("2026-01-17T03:00:00")) { Label = "exactly 03:00 UTC runs again the next day" };
        yield return new(Utc("2026-01-16T12:00:00"), Utc("2026-01-17T03:00:00")) { Label = "midday UTC runs at 03:00 the next day" };
        yield return new(Utc("2026-01-15T22:30:00"), Utc("2026-01-16T03:00:00")) { Label = "late evening UTC runs at 03:00 after midnight" };
        yield return new(Utc("2026-07-15T12:00:00"), Utc("2026-07-16T03:00:00")) { Label = "in summer it is still 03:00 UTC, with no daylight saving" };
    }

    [Theory]
    [MemberData(nameof(GetNextRunTestData))]
    public void NextRun_GivenUtcInstant_ReturnsNext0300Utc(DateTimeOffset utcNow, DateTimeOffset expected)
    {
        //Act
        DateTimeOffset result = OverdueSchedule.NextRun(utcNow);

        //Assert
        result.Should().Be(expected);
        result.Offset.Should().Be(TimeSpan.Zero);
    }
}

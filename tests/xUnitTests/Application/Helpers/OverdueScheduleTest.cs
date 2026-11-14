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

    public static IEnumerable<TheoryDataRow<DateOnly, DateTimeOffset>> GetNextRunTestData()
    {
        yield return new(new DateOnly(2026, 1, 15), Utc("2026-01-16T03:00:00")) { Label = "after the run for a date, the next is 03:00 UTC the day after" };
        yield return new(new DateOnly(2026, 12, 31), Utc("2027-01-01T03:00:00")) { Label = "after the last day of the year, 03:00 on 1 January" };
        yield return new(new DateOnly(2028, 2, 28), Utc("2028-02-29T03:00:00")) { Label = "in a leap year, 28 February is followed by 29 February" };
        yield return new(new DateOnly(2026, 7, 15), Utc("2026-07-16T03:00:00")) { Label = "in summer it is still 03:00 UTC, with no daylight saving" };
    }

    [Theory]
    [MemberData(nameof(GetNextRunTestData))]
    public void NextRun_GivenLastRunDate_Returns0300UtcTheNextDay(DateOnly lastRunDate, DateTimeOffset expected)
    {
        //Act
        DateTimeOffset result = OverdueSchedule.NextRun(lastRunDate);

        //Assert
        result.Should().Be(expected);
        result.Offset.Should().Be(TimeSpan.Zero);
    }
}

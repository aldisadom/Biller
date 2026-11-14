using Application.Interfaces;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.BackgroundServices;

namespace xUnitTests.WebAPI.BackgroundServices;

public class InvoiceOverdueJobTest
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new(MockBehavior.Strict);
    private readonly Mock<IServiceScope> _scopeMock = new(MockBehavior.Strict);
    private readonly Mock<IServiceProvider> _serviceProviderMock = new(MockBehavior.Strict);
    private readonly Mock<IInvoiceService> _invoiceServiceMock = new(MockBehavior.Strict);
    private readonly Mock<TimeProvider> _timeProviderMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<InvoiceOverdueJob>> _loggerMock = new(MockBehavior.Strict);
    private readonly Mock<ITimer> _timerMock = new(MockBehavior.Strict);
    private readonly InvoiceOverdueJob _job;

    // 00:30 on 16 January in Vilnius (UTC+2), still 15 January in UTC
    private readonly DateTimeOffset _utcNow = new(2026, 1, 15, 22, 30, 0, TimeSpan.Zero);
    private readonly DateOnly _vilniusToday = new(2026, 1, 16);

    public InvoiceOverdueJobTest()
    {
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(_utcNow);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeMock.Setup(s => s.Dispose());
        _serviceProviderMock.Setup(p => p.GetService(typeof(IInvoiceService))).Returns(_invoiceServiceMock.Object);
        _timerMock.Setup(t => t.Dispose());

        _job = new InvoiceOverdueJob(_scopeFactoryMock.Object, _timeProviderMock.Object, _loggerMock.Object);
    }

    private void SetupLog(LogLevel level, Exception? exception = null)
    {
        _loggerMock.Setup(l => l.Log(level, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), exception, It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    private void VerifyLog(LogLevel level, Exception? exception, int times)
    {
        _loggerMock.Verify(l => l.Log(level, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), exception, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(times));
    }

    private void VerifyRuns(int times)
    {
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Exactly(times));
        _scopeMock.VerifyGet(s => s.ServiceProvider, Times.Exactly(times));
        _serviceProviderMock.Verify(p => p.GetService(typeof(IInvoiceService)), Times.Exactly(times));
        _scopeMock.Verify(s => s.Dispose(), Times.Exactly(times));
    }

    // Starts the job and returns the waits it asked for. The first `waitsToPass` timers fire at once, as if
    // their time had passed; at the next wait the job is stopped.
    private async Task<List<TimeSpan>> StartAndStopAtWait(int waitsToPass = 0)
    {
        var waits = new List<TimeSpan>();
        var lastWait = new TaskCompletionSource();
        _timeProviderMock.Setup(t => t.CreateTimer(It.IsAny<TimerCallback>(), It.IsAny<object?>(), It.IsAny<TimeSpan>(), Timeout.InfiniteTimeSpan))
            .Callback((TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan _) =>
            {
                waits.Add(dueTime);
                if (waits.Count <= waitsToPass)
                    callback(state);
                else
                    lastWait.SetResult();
            })
            .Returns(_timerMock.Object);

        await _job.StartAsync(CancellationToken.None);
        await lastWait.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await _job.StopAsync(CancellationToken.None);

        _timeProviderMock.Verify(t => t.CreateTimer(It.IsAny<TimerCallback>(), It.IsAny<object?>(), It.IsAny<TimeSpan>(), Timeout.InfiniteTimeSpan), Times.Exactly(waits.Count));
        _timerMock.Verify(t => t.Dispose(), Times.Exactly(waits.Count));
        return waits;
    }

    [Fact]
    public async Task RunOnce_GivenToday_MarksOverdueAndLogsCount()
    {
        //Arrange
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_vilniusToday)).ReturnsAsync(3);
        SetupLog(LogLevel.Information);

        //Act
        bool result = await _job.RunOnce(_vilniusToday);

        //Assert
        result.Should().BeTrue();
        VerifyRuns(1);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_vilniusToday), Times.Once());
        _loggerMock.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString() == "Marked 3 invoices overdue, due before 2026-01-16"),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once());
        _loggerMock.VerifyNoOtherCalls();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Never());
    }

    [Fact]
    public async Task RunOnce_GivenServiceThrows_LogsErrorAndReturnsFalse()
    {
        //Arrange
        var error = new InvalidOperationException("database is down");
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_vilniusToday)).ThrowsAsync(error);
        SetupLog(LogLevel.Error, error);

        //Act
        bool result = await _job.RunOnce(_vilniusToday);

        //Assert
        result.Should().BeFalse();
        VerifyRuns(1);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_vilniusToday), Times.Once());
        VerifyLog(LogLevel.Error, error, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenStartAndStop_RunsForVilniusDateThenWaitsAnHourAtMostAndStopsQuietly()
    {
        //Arrange
        // From 00:30 on 16 January in Vilnius the next run is 23 h 31 min away; the job reads the clock again after an hour
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_vilniusToday)).ReturnsAsync(0);
        SetupLog(LogLevel.Information);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait();

        //Assert
        waits.Should().Equal(TimeSpan.FromHours(1));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Once());
        VerifyRuns(1);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_vilniusToday), Times.Once());
        VerifyLog(LogLevel.Information, null, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenFailingRun_WaitsToTryAgain()
    {
        //Arrange
        // After a reboot the database may not take connections yet
        var error = new InvalidOperationException("database is starting up");
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_vilniusToday)).ThrowsAsync(error);
        SetupLog(LogLevel.Error, error);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait();

        //Assert
        waits.Should().Equal(TimeSpan.FromSeconds(30));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Once());
        VerifyRuns(1);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_vilniusToday), Times.Once());
        VerifyLog(LogLevel.Error, error, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenEveryRunFails_TriesFiveTimesQuicklyThenEveryHour()
    {
        //Arrange
        var error = new InvalidOperationException("database is down");
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_vilniusToday)).ThrowsAsync(error);
        SetupLog(LogLevel.Error, error);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait(waitsToPass: 5);

        //Assert
        var retry = TimeSpan.FromSeconds(30);
        var hour = TimeSpan.FromHours(1);
        waits.Should().Equal(retry, retry, retry, retry, hour, hour);
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Exactly(6));
        VerifyRuns(6);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_vilniusToday), Times.Exactly(6));
        VerifyLog(LogLevel.Error, error, 6);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenOutagePastMidnight_StartsTheNewDayWithQuickRetries()
    {
        //Arrange
        // The database is down from 23:00 on 15 January in Vilnius: five failures, then the hourly wake at 00:40 on 16 January
        var lastDay = new DateOnly(2026, 1, 15);
        var lateEvening = new DateTimeOffset(2026, 1, 15, 21, 0, 0, TimeSpan.Zero);
        var error = new InvalidOperationException("database is down");
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(lateEvening).Returns(lateEvening).Returns(lateEvening).Returns(lateEvening).Returns(lateEvening)
            .Returns(new DateTimeOffset(2026, 1, 15, 22, 40, 0, TimeSpan.Zero));
        _invoiceServiceMock.Setup(s => s.MarkOverdue(lastDay)).ThrowsAsync(error);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_vilniusToday)).ThrowsAsync(error);
        SetupLog(LogLevel.Error, error);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait(waitsToPass: 5);

        //Assert
        var retry = TimeSpan.FromSeconds(30);
        waits.Should().Equal(retry, retry, retry, retry, TimeSpan.FromHours(1), retry);
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Exactly(6));
        VerifyRuns(6);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(lastDay), Times.Exactly(5));
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_vilniusToday), Times.Once());
        VerifyLog(LogLevel.Error, error, 6);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenFailureTheDayAfterASuccess_StartsWithQuickRetries()
    {
        //Arrange
        // Five failures and then a success on 16 January in Vilnius; the run at 00:01 on 17 January fails
        var nextDay = new DateOnly(2026, 1, 17);
        var error = new InvalidOperationException("database is down");
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(_utcNow).Returns(_utcNow).Returns(_utcNow).Returns(_utcNow).Returns(_utcNow).Returns(_utcNow)
            .Returns(new DateTimeOffset(2026, 1, 16, 22, 1, 0, TimeSpan.Zero));
        _invoiceServiceMock.SetupSequence(s => s.MarkOverdue(_vilniusToday))
            .ThrowsAsync(error).ThrowsAsync(error).ThrowsAsync(error).ThrowsAsync(error).ThrowsAsync(error)
            .ReturnsAsync(1);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(nextDay)).ThrowsAsync(error);
        SetupLog(LogLevel.Error, error);
        SetupLog(LogLevel.Information);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait(waitsToPass: 6);

        //Assert
        var retry = TimeSpan.FromSeconds(30);
        var hour = TimeSpan.FromHours(1);
        waits.Should().Equal(retry, retry, retry, retry, hour, hour, retry);
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Exactly(7));
        VerifyRuns(7);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_vilniusToday), Times.Exactly(6));
        _invoiceServiceMock.Verify(s => s.MarkOverdue(nextDay), Times.Once());
        VerifyLog(LogLevel.Error, error, 6);
        VerifyLog(LogLevel.Information, null, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenWaitPassesNext0001_RunsForTheNewDay()
    {
        //Arrange
        // Startup at 00:30 on 16 January in Vilnius; the next run is 00:01 on 17 January (22:01 UTC on 16 January).
        // The job wakes at 23:30 that evening with 31 minutes left, and after them it is 00:01 on 17 January.
        var nextRun = new DateTimeOffset(2026, 1, 16, 22, 1, 0, TimeSpan.Zero);
        var nextDay = new DateOnly(2026, 1, 17);
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(_utcNow)
            .Returns(nextRun.AddMinutes(-31))
            .Returns(nextRun);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_vilniusToday)).ReturnsAsync(0);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(nextDay)).ReturnsAsync(2);
        SetupLog(LogLevel.Information);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait(waitsToPass: 2);

        //Assert
        waits.Should().Equal(TimeSpan.FromHours(1), TimeSpan.FromMinutes(31), TimeSpan.FromHours(1));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Exactly(3));
        VerifyRuns(2);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_vilniusToday), Times.Once());
        _invoiceServiceMock.Verify(s => s.MarkOverdue(nextDay), Times.Once());
        VerifyLog(LogLevel.Information, null, 2);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenRetryPastMidnight_RunsForTheNewDate()
    {
        //Arrange
        // Startup at 23:59:50 on 15 January in Vilnius with the database down; the retry 30 s later is on 16 January
        var lastDay = new DateOnly(2026, 1, 15);
        var error = new InvalidOperationException("database is starting up");
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(new DateTimeOffset(2026, 1, 15, 21, 59, 50, TimeSpan.Zero))
            .Returns(new DateTimeOffset(2026, 1, 15, 22, 0, 20, TimeSpan.Zero));
        _invoiceServiceMock.Setup(s => s.MarkOverdue(lastDay)).ThrowsAsync(error);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_vilniusToday)).ReturnsAsync(4);
        SetupLog(LogLevel.Error, error);
        SetupLog(LogLevel.Information);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait(waitsToPass: 1);

        //Assert
        // After the run for 16 January the job waits until 00:01 (40 s), where it finds that day done
        waits.Should().Equal(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(40));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Exactly(2));
        VerifyRuns(2);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(lastDay), Times.Once());
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_vilniusToday), Times.Once());
        VerifyLog(LogLevel.Error, error, 1);
        VerifyLog(LogLevel.Information, null, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenNoTimeZone_LogsErrorAndKeepsTheApiRunning()
    {
        //Arrange
        var error = new TimeZoneNotFoundException("no Europe/Vilnius");
        var job = new InvoiceOverdueJob(_scopeFactoryMock.Object, _timeProviderMock.Object, _loggerMock.Object, () => throw error);
        SetupLog(LogLevel.Error, error);

        //Act
        await job.StartAsync(CancellationToken.None);
        await job.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        //Assert
        job.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue();
        VerifyLog(LogLevel.Error, error, 1);
        _loggerMock.VerifyNoOtherCalls();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Never());
        VerifyRuns(0);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(It.IsAny<DateOnly>()), Times.Never());
    }
}

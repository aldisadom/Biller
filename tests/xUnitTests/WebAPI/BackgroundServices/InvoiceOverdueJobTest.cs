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

    // 22:30 UTC on 15 January; the next run is 03:00 UTC on 16 January, 4 h 30 min later
    private readonly DateTimeOffset _utcNow = new(2026, 1, 15, 22, 30, 0, TimeSpan.Zero);
    private readonly DateOnly _today = new(2026, 1, 15);
    private readonly DateTimeOffset _nextRun = new(2026, 1, 16, 3, 0, 0, TimeSpan.Zero);
    private readonly DateOnly _nextDay = new(2026, 1, 16);

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
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_today)).ReturnsAsync(3);
        SetupLog(LogLevel.Information);

        //Act
        bool result = await _job.RunOnce(_today);

        //Assert
        result.Should().BeTrue();
        VerifyRuns(1);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_today), Times.Once());
        _loggerMock.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString() == "Marked 3 invoices overdue, due before 2026-01-15"),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once());
        _loggerMock.VerifyNoOtherCalls();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Never());
    }

    [Fact]
    public async Task RunOnce_GivenServiceThrows_LogsErrorAndReturnsFalse()
    {
        //Arrange
        var error = new InvalidOperationException("database is down");
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_today)).ThrowsAsync(error);
        SetupLog(LogLevel.Error, error);

        //Act
        bool result = await _job.RunOnce(_today);

        //Assert
        result.Should().BeFalse();
        VerifyRuns(1);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_today), Times.Once());
        VerifyLog(LogLevel.Error, error, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenStartAndStop_RunsForUtcDateThenWaitsAnHourAtMostAndStopsQuietly()
    {
        //Arrange
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_today)).ReturnsAsync(0);
        SetupLog(LogLevel.Information);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait();

        //Assert
        // The next run is 4 h 30 min away; the job reads the clock again after an hour
        waits.Should().Equal(TimeSpan.FromHours(1));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Once());
        VerifyRuns(1);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_today), Times.Once());
        VerifyLog(LogLevel.Information, null, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenFailingRun_WaitsToTryAgain()
    {
        //Arrange
        // After a reboot the database may not take connections yet
        var error = new InvalidOperationException("database is starting up");
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_today)).ThrowsAsync(error);
        SetupLog(LogLevel.Error, error);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait();

        //Assert
        waits.Should().Equal(TimeSpan.FromSeconds(30));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Once());
        VerifyRuns(1);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_today), Times.Once());
        VerifyLog(LogLevel.Error, error, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenEveryRunFails_TriesFiveTimesQuicklyThenEveryHour()
    {
        //Arrange
        var error = new InvalidOperationException("database is down");
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_today)).ThrowsAsync(error);
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
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_today), Times.Exactly(6));
        VerifyLog(LogLevel.Error, error, 6);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenWaitPasses0300_RunsAt0300AndNotAtMidnight()
    {
        //Arrange
        // After the startup run the job wakes at 02:29 UTC on 16 January: a new UTC date, but no run
        // before 03:00. After 31 more minutes it is 03:00, and the day's run goes.
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(_utcNow)
            .Returns(_nextRun.AddMinutes(-31))
            .Returns(_nextRun);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_today)).ReturnsAsync(0);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_nextDay)).ReturnsAsync(2);
        SetupLog(LogLevel.Information);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait(waitsToPass: 2);

        //Assert
        waits.Should().Equal(TimeSpan.FromHours(1), TimeSpan.FromMinutes(31), TimeSpan.FromHours(1));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Exactly(3));
        VerifyRuns(2);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_today), Times.Once());
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_nextDay), Times.Once());
        VerifyLog(LogLevel.Information, null, 2);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenStartupBefore0300_DoesNotRunThatDateAgainAt0300()
    {
        //Arrange
        // Startup at 01:00 UTC on 16 January runs for that date; the next run is 03:00 on 17 January, so the
        // wake at 03:00 on 16 January doesn't repeat it
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(_nextRun.AddHours(-2))
            .Returns(_nextRun);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_nextDay)).ReturnsAsync(0);
        SetupLog(LogLevel.Information);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait(waitsToPass: 1);

        //Assert
        waits.Should().Equal(TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Exactly(2));
        VerifyRuns(1);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_nextDay), Times.Once());
        VerifyLog(LogLevel.Information, null, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenClockStepsBack_RunsAtTheRealNext0300()
    {
        //Arrange
        // The clock is three days ahead at startup (it reads 22:30 UTC on 18 January), so the run is for 18 January
        // and the next would be 03:00 on 19 January. Then the clock is put right to 02:30 on 16 January: the next
        // run comes back to 03:00 on 17 January, and runs then, instead of skipping to 19 January.
        var aheadDay = new DateOnly(2026, 1, 18);
        var realNextDay = new DateOnly(2026, 1, 17);
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(_utcNow.AddDays(3))
            .Returns(_nextRun.AddMinutes(-30))
            .Returns(_nextRun.AddDays(1));
        _invoiceServiceMock.Setup(s => s.MarkOverdue(aheadDay)).ReturnsAsync(0);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(realNextDay)).ReturnsAsync(0);
        SetupLog(LogLevel.Information);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait(waitsToPass: 2);

        //Assert
        waits.Should().Equal(TimeSpan.FromHours(1), TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Exactly(3));
        VerifyRuns(2);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(aheadDay), Times.Once());
        _invoiceServiceMock.Verify(s => s.MarkOverdue(realNextDay), Times.Once());
        VerifyLog(LogLevel.Information, null, 2);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenRetryAfter0300_CountsAsThatDaysRun()
    {
        //Arrange
        // The run at 02:59:50 UTC fails; the retry at 03:00:20 succeeds, so the next run is 03:00 the day after
        var error = new InvalidOperationException("database is starting up");
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(_nextRun.AddSeconds(-10))
            .Returns(_nextRun.AddSeconds(20));
        _invoiceServiceMock.SetupSequence(s => s.MarkOverdue(_nextDay))
            .ThrowsAsync(error)
            .ReturnsAsync(4);
        SetupLog(LogLevel.Error, error);
        SetupLog(LogLevel.Information);

        //Act
        List<TimeSpan> waits = await StartAndStopAtWait(waitsToPass: 1);

        //Assert
        waits.Should().Equal(TimeSpan.FromSeconds(30), TimeSpan.FromHours(1));
        _job.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        _timeProviderMock.Verify(t => t.GetUtcNow(), Times.Exactly(2));
        VerifyRuns(2);
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_nextDay), Times.Exactly(2));
        VerifyLog(LogLevel.Error, error, 1);
        VerifyLog(LogLevel.Information, null, 1);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenOutagePastMidnight_KeepsRetryingHourlyWithTheNewDate()
    {
        //Arrange
        // The database is down from 23:00 UTC on 15 January: five failures, then the hourly wake at 00:40 on
        // 16 January tries again with that date. Midnight doesn't reset the count; only a success does.
        var lateEvening = new DateTimeOffset(2026, 1, 15, 23, 0, 0, TimeSpan.Zero);
        var error = new InvalidOperationException("database is down");
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(lateEvening).Returns(lateEvening).Returns(lateEvening).Returns(lateEvening).Returns(lateEvening)
            .Returns(new DateTimeOffset(2026, 1, 16, 0, 40, 0, TimeSpan.Zero));
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_today)).ThrowsAsync(error);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_nextDay)).ThrowsAsync(error);
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
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_today), Times.Exactly(5));
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_nextDay), Times.Once());
        VerifyLog(LogLevel.Error, error, 6);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_GivenFailureTheDayAfterASuccess_StartsWithQuickRetries()
    {
        //Arrange
        // Five failures and then a success on 15 January; the run at 03:00 UTC on 16 January fails
        var error = new InvalidOperationException("database is down");
        _timeProviderMock.SetupSequence(t => t.GetUtcNow())
            .Returns(_utcNow).Returns(_utcNow).Returns(_utcNow).Returns(_utcNow).Returns(_utcNow).Returns(_utcNow)
            .Returns(_nextRun);
        _invoiceServiceMock.SetupSequence(s => s.MarkOverdue(_today))
            .ThrowsAsync(error).ThrowsAsync(error).ThrowsAsync(error).ThrowsAsync(error).ThrowsAsync(error)
            .ReturnsAsync(1);
        _invoiceServiceMock.Setup(s => s.MarkOverdue(_nextDay)).ThrowsAsync(error);
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
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_today), Times.Exactly(6));
        _invoiceServiceMock.Verify(s => s.MarkOverdue(_nextDay), Times.Once());
        VerifyLog(LogLevel.Error, error, 6);
        VerifyLog(LogLevel.Information, null, 1);
        _loggerMock.VerifyNoOtherCalls();
    }
}

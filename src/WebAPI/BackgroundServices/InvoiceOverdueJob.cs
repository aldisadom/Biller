using Application.Helpers;
using Application.Interfaces;

namespace WebAPI.BackgroundServices;

/// <summary>
/// Marks Created and Send invoices due before today (UTC) overdue, once at startup and then every
/// day at 03:00 UTC. The update is safe to repeat, so the startup run catches up on a day missed
/// while the server was off.
/// </summary>
public class InvoiceOverdueJob : BackgroundService
{
    // After a reboot the database can come up after the API, so a failed run is tried again soon,
    // then every hour until one succeeds
    private const int QuickRetries = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);
    // A timer doesn't count time while the machine sleeps, so the clock is read again every hour
    private static readonly TimeSpan MaxWait = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InvoiceOverdueJob> _logger;

    /// <summary>
    ///
    /// </summary>
    /// <param name="scopeFactory">a scope per run, because the invoice service and the DB connection are scoped</param>
    /// <param name="timeProvider"></param>
    /// <param name="logger"></param>
    public InvoiceOverdueJob(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<InvoiceOverdueJob> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Runs now, then at every next 03:00 UTC, until the API stops
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTimeOffset? nextRun = null;   // when the next run is due; none yet: run now
        DateOnly? failedDay = null;       // the UTC date of the last failed run
        int failures = 0;                 // failed runs that date, so each day starts with quick retries
        while (true)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateOnly today = OverdueSchedule.Today(now);
            if (nextRun is null || now >= nextRun)
            {
                if (await RunOnce(today))
                {
                    nextRun = OverdueSchedule.NextRun(now);
                }
                else
                {
                    failures = failedDay == today ? failures + 1 : 1;
                    failedDay = today;
                }
            }

            TimeSpan wait;
            if (nextRun > now)
            {
                TimeSpan untilNextRun = nextRun.Value - now;
                wait = untilNextRun < MaxWait ? untilNextRun : MaxWait;
            }
            else
            {
                wait = failures < QuickRetries ? RetryDelay : MaxWait;
            }

            if (!await Wait(wait, stoppingToken))
                return;
        }
    }

    /// <summary>
    /// Marks the invoices due before the given UTC date overdue. A failure is logged and not
    /// thrown: an unhandled exception would stop the API.
    /// </summary>
    /// <param name="today">today's UTC date</param>
    /// <returns>whether the run succeeded</returns>
    public async Task<bool> RunOnce(DateOnly today)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IInvoiceService invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            int count = await invoiceService.MarkOverdue(today);

            _logger.LogInformation("Marked {Count} invoices overdue, due before {Today:yyyy-MM-dd}", count, today);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Marking invoices due before {Today:yyyy-MM-dd} overdue failed", today);
            return false;
        }
    }

    // False when the API stops during the wait
    private async Task<bool> Wait(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, _timeProvider, stoppingToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}

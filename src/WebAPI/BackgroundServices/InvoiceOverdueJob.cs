using Application.Helpers;
using Application.Interfaces;

namespace WebAPI.BackgroundServices;

/// <summary>
/// Marks Created and Send invoices due before today overdue, once at startup and then every day
/// at 00:01 Vilnius time. The update is safe to repeat, so the startup run catches up on a day
/// missed while the server was off.
/// </summary>
public class InvoiceOverdueJob : BackgroundService
{
    // After a reboot the database can come up after the API, so a failed run is tried again soon,
    // then every hour until a run for that day succeeds
    private const int QuickRetries = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);
    // A timer doesn't count time while the machine sleeps, so the clock is read again every hour
    private static readonly TimeSpan MaxWait = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InvoiceOverdueJob> _logger;
    private readonly Func<TimeZoneInfo> _findZone;

    /// <summary>
    ///
    /// </summary>
    /// <param name="scopeFactory">a scope per run, because the invoice service and the DB connection are scoped</param>
    /// <param name="timeProvider"></param>
    /// <param name="logger"></param>
    /// <param name="findZone">the Vilnius time zone lookup, OverdueSchedule.FindZone unless a test gives another</param>
    public InvoiceOverdueJob(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<InvoiceOverdueJob> logger,
        Func<TimeZoneInfo>? findZone = null)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
        _findZone = findZone ?? (() => OverdueSchedule.FindZone());
    }

    /// <summary>
    /// Runs now, then whenever the Vilnius date has no successful run yet, until the API stops
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeZoneInfo zone;
        try
        {
            zone = _findZone();
        }
        catch (Exception e)
        {
            // Without the zone there is no "today" to compare due dates with. The API keeps running.
            _logger.LogError(e, "Invoices are not marked overdue: the Europe/Vilnius time zone was not found");
            return;
        }

        DateOnly? lastRun = null;     // the Vilnius date of the last run that succeeded
        DateOnly? failedDay = null;   // the date of the last failed run
        int failures = 0;             // failed runs for that date, so each day starts with quick retries
        while (true)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateOnly today = OverdueSchedule.Today(now, zone);
            if (lastRun != today)
            {
                if (await RunOnce(today))
                {
                    lastRun = today;
                }
                else
                {
                    failures = failedDay == today ? failures + 1 : 1;
                    failedDay = today;
                }
            }

            TimeSpan wait;
            if (lastRun == today)
            {
                TimeSpan untilNextRun = OverdueSchedule.NextRun(now, zone) - now;
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
    /// Marks the invoices due before the given Vilnius date overdue. A failure is logged and not
    /// thrown: an unhandled exception would stop the API.
    /// </summary>
    /// <param name="today">today in Vilnius</param>
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

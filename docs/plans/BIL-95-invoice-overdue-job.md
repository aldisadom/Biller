# Plan: mark invoices overdue every day at 00:01 (BIL-95)

Jira: [BIL-95](https://aldisadom.atlassian.net/browse/BIL-95). Epic: BIL-89 (Billeris).

## Goal

- Every day at 00:01 (Europe/Vilnius), every `Created` or `Send` invoice whose `due_date` is before today becomes `OverDue`.
- The list and detail pages show the stored `OverDue` status. The front end already does this (BIL-73).

## Current state (master @ c51d3cf)

| Where | What |
|---|---|
| `InvoiceStatus` (BillerContracts 5.0.0) | `Payed = 0`, `OverDue = -1`, `Send = 1`, `Created = 10` |
| `invoices` | `due_date DATE NOT NULL`, `status INT NOT NULL DEFAULT 10` |
| `InvoiceService.Add` | New invoices start as `Created` (BIL-73) |
| `InvoiceService.UpdateStatus` | Rejects `OverDue` with a `ValidationException`. Any other status can be set from any status |
| `InvoiceRepository.UpdateStatus` | Changes one invoice at a time |
| `InvoiceEntity` | `CreatedDate`, `DueDate` are `DateTime` (midnight) |
| Hosting | No `BackgroundService` or `IHostedService`, no scheduler library. `WebAPI/Capabilities/StartupInjection.ConfigureInjection` registers everything. `ValidateScopes` is on |
| Time zone | The containers run on UTC. The `aspnet:10.0` image has `/usr/share/zoneinfo/Europe/Vilnius` (checked) |
| Nothing | compares `due_date` with today: no job, query or computed field |
| BIL-73 plan, decision 5 | Already says `OverDue` is stored and set by the BIL-95 job (changed 2026-10-23) |

## Design decisions

1. **One `UPDATE`, safe to repeat:** `UPDATE invoices SET status = @OverDue WHERE status IN (@Created, @Send) AND due_date < @Today`, returning the number of rows changed. One statement, as `CLAUDE.md` asks. The statuses are parameters, not literals, so the planned enum renumbering (BIL-73 "Later") changes only C#.
2. **The job only ever sets `OverDue`.** If a due date is moved later, the invoice stays `OverDue` until its status is changed by hand (answered 2026-11-13). The job can't tell whether it was `Created` or `Send` before.
3. **Manual changes stay as BIL-73 left them:** the API rejects setting `OverDue`, and an `OverDue` invoice can be set to `Payed`, `Send` or `Created` by hand. `Payed` always sticks. A `Send` or `Created` invoice whose due date has passed is overdue, so the next run (00:01 or a restart) marks it `OverDue` again; moving the due date to today or later first makes it stick (review F5).
4. **"Today" is the date in Europe/Vilnius.** The job runs once at startup and then every day at 00:01 Vilnius time. Because the update is safe to repeat, the startup run catches up on a day missed while the PC was off. The zone is found by its IANA id `Europe/Vilnius`, else by its Windows id `FLE Standard Time`: WebAPI runs with `InvariantGlobalization`, so on Windows (Visual Studio) the IANA id can't be converted. When neither is there, the job logs an error and ends; the API keeps running (review F1, F3).
5. **A hosted `BackgroundService` in WebAPI** (`src/WebAPI/BackgroundServices/InvoiceOverdueJob.cs`). The Web SDK has the hosting types, so there's no new package. Each run opens a DI scope (`IServiceScopeFactory`) because `IDbConnection` and `IInvoiceService` are scoped.
6. **The schedule maths is a pure static class in Application** (`OverdueSchedule`: `Today(utcNow, zone)` and `NextRun(utcNow, zone)`). The job reads the time from the in-box `TimeProvider`, registered as `TimeProvider.System`. The tests call the pure functions with fixed instants, so no `FakeTimeProvider` package.
7. **A failed run doesn't stop the API, and is tried again until one succeeds.** The job remembers the last Vilnius date whose run succeeded and runs whenever today has none. A failure is logged and tried again 30 s later, 5 times, then every hour; the count is per date, so a new day starts with quick retries again (review F15): after a reboot the DB can start after the API (compose's `depends_on` has no health condition), and a retry that runs past midnight runs for the new date. An unhandled `BackgroundService` exception would stop the host (review F2, F9, F10).
8. **Each run logs one line** at Information with Serilog: how many invoices it marked overdue and the date, also when the count is 0, so the log shows that the job ran.
9. **Unit tests only** (answered 2026-11-13). No integration test and no endpoint to trigger the job, so `OverDue` stays something only the job sets.
10. **The wait to 00:01 is at most an hour**, then the job reads the clock again and runs once the Vilnius date has changed: a timer doesn't count time while the machine sleeps (review F4).

## Dependencies

- BIL-73 (Done): new invoices start as `Created`.
- None on BillerContracts, migrations or the front end.

## Step 1: BillerContracts

- None.

## Step 2: Migration

- None. The update scans `invoices` by `status` and `due_date`. The table is small, so it has no index.

## Step 3: Domain and Infrastructure (done)

- `IInvoiceRepository.MarkOverdue(DateTime today)` returns `Task<int>`. Done without an XML doc comment: the interface's other methods have none. The comment above `InvoiceRepository.MarkOverdue` says why it is safe to repeat.
- `InvoiceRepository.MarkOverdue`:

```sql
UPDATE invoices
SET status = @OverDue
WHERE status IN (@Created, @Send)
  AND due_date < @Today
```

`ExecuteAsync` with `new { Today = today, OverDue = InvoiceStatus.OverDue, Created = InvoiceStatus.Created, Send = InvoiceStatus.Send }`. Pass the enum values the way `UpdateStatus` does, and check that Dapper sends them as numbers.

## Step 4: Application (done)

- `IInvoiceService.MarkOverdue(DateOnly today)` returns `Task<int>`. It calls the repository with `today.ToDateTime(TimeOnly.MinValue)`, the same midnight `DateTime` that `InvoiceEntity` uses.
- `OverdueSchedule` (static, `src/Application/Helpers/OverdueSchedule.cs`, also holding `RunTime` = 00:01):
  - `TimeZoneInfo FindZone(ianaId = "Europe/Vilnius", windowsId = "FLE Standard Time")`: the IANA id, else the Windows id (decision 4). A method, not a static field, so a failed lookup can be caught.
  - `DateOnly Today(DateTimeOffset utcNow, TimeZoneInfo zone)`: the local date.
  - `DateTimeOffset NextRun(DateTimeOffset utcNow, TimeZoneInfo zone)`: the next 00:01 local time after `utcNow`, as a UTC instant. 00:01 exists every day in Vilnius, because the clocks change at 03:00/04:00.

## Step 5: Validators

- None.

## Step 6: WebAPI (done)

- `InvoiceOverdueJob : BackgroundService`. Its constructor takes `IServiceScopeFactory`, `TimeProvider`, `ILogger<InvoiceOverdueJob>` and an optional zone lookup (`OverdueSchedule.FindZone` unless a test gives another; DI uses the default).
  - `RunOnce(today)`: opens a scope, gets `IInvoiceService`, calls `MarkOverdue(today)` and logs the count, with the date as `yyyy-MM-dd`. An exception is logged as an error, not thrown; it returns whether the run succeeded. It takes no token, because `MarkOverdue` takes none (review F6: no repository method does).
  - `ExecuteAsync`: finds the zone (decision 4), then loops: read the clock, run for today's Vilnius date unless a run for it already succeeded, and wait (`Task.Delay(delay, timeProvider, stoppingToken)`): after a success until `NextRun`, at most an hour (decision 10); after a failure 30 s, or an hour after 5 failures in a row (decision 7). It ends quietly when the token is cancelled.
- `StartupInjection.ConfigureInjection`: `services.AddSingleton(TimeProvider.System)` and `services.AddHostedService<InvoiceOverdueJob>()`.

## Step 7: Tests (`_write-tests` skill) (done)

- `InvoiceServiceTest.MarkOverdue_...`: with a strict mock, the repository gets midnight of the given date, and the service returns the repository's count.
- `OverdueScheduleTest` (a theory, with a `Label` per case):
  - `Today`: 21:59 UTC in winter (23:59 local) is the same date. 22:00 UTC is the next date. In summer the switch is at 21:00 UTC.
  - `NextRun`: before 00:01 local it's the same night. At exactly 00:01 and after, it's the next night.
  - `NextRun` across the DST changes: the night before 2026-10-25 (UTC+3 → UTC+2) and before 2027-03-28 (UTC+2 → UTC+3) give the right UTC instant.
- `InvoiceOverdueJobTest`:
  - `RunOnce` calls `MarkOverdue` once with the date it is given.
  - When the service throws, `RunOnce` logs and doesn't throw.
  - The mocks are strict: `IServiceScopeFactory`, `IServiceScope`, `IServiceProvider`, `TimeProvider`.
  - Added, through a strict `TimeProvider.CreateTimer` and `ITimer` mock whose timers fire at once when a test needs the wait to pass (still no `FakeTimeProvider` package), each asserting the list of waits:
    - Start and stop: runs for the Vilnius date, waits an hour (the next 00:01 is 23 h 31 min away from 00:30), stops quietly.
    - A failed run waits 30 s; every run failing gives 30 s four times, then an hour.
    - A wake 31 minutes before 00:01, then 00:01: runs for the new day.
    - A failed run at 23:59:50 retried at 00:00:20: runs for the new date (review F9).
    - A zone lookup that throws is logged and ends the job, with its task completed, not faulted.
  - `FindZone`: the IANA id; without it, the Windows id; with neither, `TimeZoneNotFoundException`.
- The expected dates and instants were checked against Python's `zoneinfo`. Each fix's test fails with the fix undone (`revert-check.sh`): `>` instead of `>=` at 00:01, no Windows-id fallback, a rethrown zone error, no retry, no hourly wake-up. With `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` on Windows, 18 of 20 schedule and job tests fail without the fallback.
- No integration test (decision 9).

## Step 8: Integration tests (done)

- None. The existing suite still runs against it (`_test-stack`). The job runs once when the stack starts.
- On a `TEST_STACK_NAME=bil-95` stack the API logged `Marked 0 invoices overdue` at startup (the SQL ran on Postgres), and `run-integration.sh --repeat 3` passed 219 of 219 each time.

## Step 9: Front end

- None. BIL-73 already highlights the stored `OverDue`.

## Out of scope

- Setting an `OverDue` invoice back to `Created` or `Send` when its due date moves later.
- An endpoint that runs the job.
- Reminders or emails about overdue invoices.
- A configurable run time or time zone.

## Risk

- An integration test run that crosses 00:01 Vilnius time could see a test invoice with a past due date change to `OverDue`. That's unlikely and harmless outside tests.

## Order of work

1. One Biller PR: repository, service, schedule, job, registrations and tests. No contracts, migration or front end.
2. It touches the same invoice files as BIL-90 (`IInvoiceRepository`, `InvoiceRepository`, `IInvoiceService`, `InvoiceService`, `InvoiceServiceTest`). Whichever merges second resolves small, additive conflicts.
3. After merge, tick "BIL-95: the daily overdue job" in the BIL-73 plan's order of work.

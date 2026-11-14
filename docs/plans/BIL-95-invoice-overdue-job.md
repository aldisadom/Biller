# Plan: mark invoices overdue every day at 03:00 UTC (BIL-95)

Jira: [BIL-95](https://aldisadom.atlassian.net/browse/BIL-95). Epic: BIL-89 (Billeris).

## Goal

- Every day at 03:00 UTC, and once when the API starts, every `Created` or `Send` invoice whose `due_date` is before today (the UTC date) becomes `OverDue`.
- The list and detail pages show the stored `OverDue` status. The front end already does this (BIL-73).
- Changed 2026-11-14 (owner): UTC and 03:00, no time zones. The issue first asked for 00:01 Europe/Vilnius; that design needed a time zone lookup that differs between Windows and Linux.

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
| Time | The containers run on UTC |
| Nothing | compares `due_date` with today: no job, query or computed field |
| BIL-73 plan, decision 5 | Already says `OverDue` is stored and set by the BIL-95 job (changed 2026-10-23) |

## Design decisions

1. **One `UPDATE`, safe to repeat:** `UPDATE invoices SET status = @OverDue WHERE status IN (@Created, @Send) AND due_date < @Today`, returning the number of rows changed. One statement, as `CLAUDE.md` asks. The statuses are parameters, not literals, so the planned enum renumbering (BIL-73 "Later") changes only C#.
2. **The job only ever sets `OverDue`.** If a due date is moved later, the invoice stays `OverDue` until its status is changed by hand (answered 2026-11-13). The job can't tell whether it was `Created` or `Send` before.
3. **Manual changes stay as BIL-73 left them:** the API rejects setting `OverDue`, and an `OverDue` invoice can be set to `Payed`, `Send` or `Created` by hand. `Payed` always sticks. A `Send` or `Created` invoice whose due date has passed is overdue, so the next run (03:00 UTC or a restart) marks it `OverDue` again; moving the due date to today or later first makes it stick (review F5).
4. **UTC, no time zones** (owner, 2026-11-14). "Today" is the UTC date. The job runs once at startup and then every day at 03:00 UTC (05:00 or 06:00 in Lithuania), when the UTC date is the local date too. Because the update is safe to repeat, the startup run catches up on a day missed while the PC was off. A startup run late in the evening UTC uses the UTC date, a day behind Lithuania; the 03:00 run catches up. This replaces the Europe/Vilnius lookup the review asked to harden (F1, F3), and its Windows/Linux fallback that CI on PR #86 failed.
5. **A hosted `BackgroundService` in WebAPI** (`src/WebAPI/BackgroundServices/InvoiceOverdueJob.cs`). The Web SDK has the hosting types, so there's no new package. Each run opens a DI scope (`IServiceScopeFactory`) because `IDbConnection` and `IInvoiceService` are scoped.
6. **The schedule maths is a pure static class in Application** (`OverdueSchedule`: `Today(utcNow)` and `NextRun(lastRunDate)`). The job reads the time from the in-box `TimeProvider`, registered as `TimeProvider.System`. The tests call the pure functions with fixed instants, so no `FakeTimeProvider` package.
7. **A failed run doesn't stop the API, and is tried again until one succeeds.** The job keeps the time the next run is due: none at startup, then, after a successful run for a UTC date, 03:00 UTC the day after. So a date is run once, even when its run came before its 03:00 (a startup at 01:00, review F19). A failure is logged and tried again 30 s later, 5 times, then every hour; a success resets the count, so the first failure after one gets quick retries again (review F15, F16, F17). After a reboot the DB can start after the API (compose's `depends_on` has no health condition). A retry that succeeds after 03:00 counts as that day's run. An unhandled `BackgroundService` exception would stop the host (review F2, F9, F10).
8. **Each run logs one line** at Information with Serilog: how many invoices it marked overdue and the date, also when the count is 0, so the log shows that the job ran.
9. **Unit tests only** (answered 2026-11-13). No integration test and no endpoint to trigger the job, so `OverDue` stays something only the job sets.
10. **A wait is at most an hour**, then the job reads the clock again and runs once the next run's time has passed: a timer doesn't count time while the machine sleeps (review F4). A wake between midnight and 03:00 UTC doesn't run: the date has changed, but the run is due at 03:00. On each wake the next run is pulled back to no later than 03:00 after today's date, so a run made while the clock was ahead doesn't push the next ones days away once the clock steps back (review F21).

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
- `OverdueSchedule` (static, `src/Application/Helpers/OverdueSchedule.cs`, also holding `RunTime` = 03:00):
  - `DateOnly Today(DateTimeOffset utcNow)`: the UTC date.
  - `DateTimeOffset NextRun(DateOnly lastRunDate)`: 03:00 UTC on the day after the date that just had its run.

## Step 5: Validators

- None.

## Step 6: WebAPI (done)

- `InvoiceOverdueJob : BackgroundService`. Its constructor takes `IServiceScopeFactory`, `TimeProvider` and `ILogger<InvoiceOverdueJob>`.
  - `RunOnce(today)`: opens a scope, gets `IInvoiceService`, calls `MarkOverdue(today)` and logs the count, with the date as `yyyy-MM-dd`. An exception is logged as an error, not thrown; it returns whether the run succeeded. It takes no token, because `MarkOverdue` takes none (review F6: no repository method does).
  - `ExecuteAsync`: loops: read the clock, run for today's UTC date when no run has succeeded yet or the next run's time has passed, and wait (`Task.Delay(delay, timeProvider, stoppingToken)`): after a success until `NextRun(today)`, at most an hour (decision 10); after a failure 30 s, or an hour after 5 failures since the last success (decision 7). It ends quietly when the token is cancelled.
- `StartupInjection.ConfigureInjection`: `services.AddSingleton(TimeProvider.System)` and `services.AddHostedService<InvoiceOverdueJob>()`.

## Step 7: Tests (`_write-tests` skill) (done)

- `InvoiceServiceTest.MarkOverdue_...`: with a strict mock, the repository gets midnight of the given date, and the service returns the repository's count.
- `OverdueScheduleTest` (theories, with a `Label` per case):
  - `Today`: a second before midnight UTC is that date, midnight is the next; an instant with another offset gives its UTC date.
  - `NextRun`: 03:00 UTC the day after the given date, across a year end and a leap day; in summer it's still 03:00 UTC.
- `InvoiceOverdueJobTest`:
  - `RunOnce` calls `MarkOverdue` once with the date it is given; when the service throws, it logs and doesn't throw.
  - The mocks are strict: `IServiceScopeFactory`, `IServiceScope`, `IServiceProvider`, `TimeProvider`, `ITimer`. `CreateTimer`'s timers fire at once when a test needs the wait to pass (still no `FakeTimeProvider` package), and each test asserts the list of waits:
    - Start and stop: runs for the UTC date, waits an hour (the next 03:00 is 4 h 30 min away from 22:30), stops quietly.
    - A failed run waits 30 s; every run failing gives 30 s four times, then an hour.
    - A wake at 02:29 UTC (a new date) doesn't run; at 03:00 the day's run goes.
    - A run failing at 02:59:50 and succeeding at 03:00:20 counts as that day's run (the next wait is to 03:00 the day after).
    - A startup run at 01:00 isn't repeated at 03:00 for the same date (F19).
    - An outage past midnight keeps retrying hourly, now with the new date (F17); a failure the day after a success starts with 30 s retries (F16).
    - A clock three days ahead at startup, then put right: the next run comes back to the real next 03:00 (F21).
- Each behaviour's test fails with it undone (`revert-check.sh`): running when the UTC date changes instead of at 03:00, no reset of the count after a success, the next run taken as the next 03:00 after the run (F19), no pull-back after the clock steps back (F21), and no hourly wake-up.
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
- A configurable run time, and local-time dates.

## Risk

- An integration test run that crosses 03:00 UTC could see a test invoice with a past due date change to `OverDue`. That's unlikely and harmless outside tests.

## Order of work

1. One Biller PR: repository, service, schedule, job, registrations and tests. No contracts, migration or front end.
2. It touches the same invoice files as BIL-90 (`IInvoiceRepository`, `InvoiceRepository`, `IInvoiceService`, `InvoiceService`, `InvoiceServiceTest`). Whichever merges second resolves small, additive conflicts.
3. After merge, tick "BIL-95: the daily overdue job" in the BIL-73 plan's order of work.

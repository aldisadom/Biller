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
3. **Manual changes stay as BIL-73 left them:** the API rejects setting `OverDue`, and an `OverDue` invoice can be set to `Payed`, `Send` or `Created` by hand.
4. **"Today" is the date in Europe/Vilnius.** The job runs once at startup and then every day at 00:01 Vilnius time. Because the update is safe to repeat, the startup run catches up on a day missed while the PC was off. The zone id is a constant: one owner, one country.
5. **A hosted `BackgroundService` in WebAPI** (`src/WebAPI/BackgroundServices/InvoiceOverdueJob.cs`). The Web SDK has the hosting types, so there's no new package. Each run opens a DI scope (`IServiceScopeFactory`) because `IDbConnection` and `IInvoiceService` are scoped.
6. **The schedule maths is a pure static class in Application** (`OverdueSchedule`: `Today(utcNow, zone)` and `NextRun(utcNow, zone)`). The job reads the time from the in-box `TimeProvider`, registered as `TimeProvider.System`. The tests call the pure functions with fixed instants, so no `FakeTimeProvider` package.
7. **A failed run doesn't stop the API.** It logs the error and waits for the next 00:01. An unhandled `BackgroundService` exception stops the host by default. If the DB isn't ready at startup, the catch-up waits at most a day.
8. **Each run logs one line** at Information with Serilog: how many invoices it marked overdue and the date, also when the count is 0, so the log shows that the job ran.
9. **Unit tests only** (answered 2026-11-13). No integration test and no endpoint to trigger the job, so `OverDue` stays something only the job sets.

## Dependencies

- BIL-73 (Done): new invoices start as `Created`.
- None on BillerContracts, migrations or the front end.

## Step 1: BillerContracts

- None.

## Step 2: Migration

- None. The update scans `invoices` by `status` and `due_date`. The table is small, so it has no index.

## Step 3: Domain and Infrastructure

- `IInvoiceRepository.MarkOverdue(DateTime today)` returns `Task<int>`, with an XML doc comment like the other methods.
- `InvoiceRepository.MarkOverdue`:

```sql
UPDATE invoices
SET status = @OverDue
WHERE status IN (@Created, @Send)
  AND due_date < @Today
```

`ExecuteAsync` with `new { Today = today, OverDue = InvoiceStatus.OverDue, Created = InvoiceStatus.Created, Send = InvoiceStatus.Send }`. Pass the enum values the way `UpdateStatus` does, and check that Dapper sends them as numbers.

## Step 4: Application

- `IInvoiceService.MarkOverdue(DateOnly today)` returns `Task<int>`. It calls the repository with `today.ToDateTime(TimeOnly.MinValue)`, the same midnight `DateTime` that `InvoiceEntity` uses.
- `OverdueSchedule` (static):
  - `TimeZoneInfo Zone`: `TimeZoneInfo.FindSystemTimeZoneById("Europe/Vilnius")`.
  - `DateOnly Today(DateTimeOffset utcNow, TimeZoneInfo zone)`: the local date.
  - `DateTimeOffset NextRun(DateTimeOffset utcNow, TimeZoneInfo zone)`: the next 00:01 local time after `utcNow`, as a UTC instant. 00:01 exists every day in Vilnius, because the clocks change at 03:00/04:00.

## Step 5: Validators

- None.

## Step 6: WebAPI

- `InvoiceOverdueJob : BackgroundService`. Its constructor takes `IServiceScopeFactory`, `TimeProvider` and `ILogger<InvoiceOverdueJob>`.
  - `RunOnce(CancellationToken)`: opens a scope, gets `IInvoiceService`, calls `MarkOverdue(OverdueSchedule.Today(now, zone))` and logs the count. An exception is logged as an error, not thrown.
  - `ExecuteAsync`: `RunOnce`, then a loop: wait until `NextRun` (`Task.Delay(delay, timeProvider, stoppingToken)`), then `RunOnce`. It ends quietly when the token is cancelled.
- `StartupInjection.ConfigureInjection`: `services.AddSingleton(TimeProvider.System)` and `services.AddHostedService<InvoiceOverdueJob>()`.

## Step 7: Tests (`_write-tests` skill)

- `InvoiceServiceTest.MarkOverdue_...`: with a strict mock, the repository gets midnight of the given date, and the service returns the repository's count.
- `OverdueScheduleTest` (a theory, with a `Label` per case):
  - `Today`: 21:59 UTC in winter (23:59 local) is the same date. 22:00 UTC is the next date. In summer the switch is at 21:00 UTC.
  - `NextRun`: before 00:01 local it's the same night. At exactly 00:01 and after, it's the next night.
  - `NextRun` across the DST changes: the night before 2026-10-25 (UTC+3 → UTC+2) and before 2027-03-28 (UTC+2 → UTC+3) give the right UTC instant.
- `InvoiceOverdueJobTest`:
  - `RunOnce` calls `MarkOverdue` once with the Vilnius date of the mocked `TimeProvider.GetUtcNow()`.
  - When the service throws, `RunOnce` logs and doesn't throw.
  - The mocks are strict: `IServiceScopeFactory`, `IServiceScope`, `IServiceProvider`, `TimeProvider`.
- No integration test (decision 9).

## Step 8: Integration tests

- None. The existing suite still runs against it (`_test-stack`). The job runs once when the stack starts.

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

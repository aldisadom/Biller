# Plan: integration tests use the BillerContracts clients

Jira: [BIL-92](https://aldisadom.atlassian.net/browse/BIL-92). Epic: BIL-89 (Billeris). Repos: Biller (step 1) and
BillioIntegrationTest (steps 2–6). It changes two repos, so it lives in Biller.

**Changed while building (2026-10-24):** no new BillerContracts release. Biller and the tests use the
published 1.1.1, the version BillerFrontEnd already uses. `ClientAPIException` in 1.1.1 has no
structured error, so the tests read the error body themselves (design decision 1). The BillerContracts
step was dropped.

## Goal

- BillioIntegrationTest uses `BillerContracts.Clients` 1.1.1, the same version as BillerFrontEnd,
  instead of its own copies of the HTTP clients.
- The 65 tests keep checking the same things, including the status code, message and extended
  message of every error case.
- Deleting all data before and after a run really deletes everything.

## Current state (BillioIntegrationTest master @ f5b2c2d, BillerContracts master @ 196fdcc)

| Where | What |
|---|---|
| `IntegrationTests.csproj` | `BillerContracts` 1.0.5. Newest on nuget.org: `BillerContracts` and `BillerContracts.Clients` 1.1.1 (2026-10-15) |
| `Clients/` (6 files), `Interfaces/` (5) | Local copies of the clients with `http://localhost:8090` hard-coded. They return `Result<T>` (from BillerContracts) and turn an error response into an `ErrorModel` (status code, message, extended message). `IInvoiceItemClient` is the item client's interface under the wrong name |
| Tests | 65 TUnit tests (62 when planned, BIL-73 added status tests), 118 `.Match(...)` calls on results, 29 error checks through `CheckErrors(ErrorModel)` |
| Biller `src/Domain/Domain.csproj` | `BillerContracts` 1.0.5 |
| BillerContracts master | Has the `UserGetRequest` namespace move (PR #8, after 1.1.1), so its next release is 2.0.0. 1.1.1 was published from 196fdcc, before the move |
| `*_Delete_All` (5 helpers) | Call `Get()` once and delete what comes back. `GET /invoices` returns only the first page (25 by default), so with more invoices data is left behind. The other four lists aren't paged (checked while building) |
| Packages | `LanguageExt.Core` (only two unused `using` lines), `FluentValidation` (unused), `Serilog*` (only the local `BaseHttpClient` logs) |

The contracts changed very little from 1.0.5 to 1.1.1: only `FileDownloadResponse` was added, so
nothing breaks.

| | Local clients | `BillerContracts.Clients` |
|---|---|---|
| Errors | `Result<T>` holding an `ErrorModel` | Throws `ClientAPIException` with a text message only: no status code property, no extended message |
| `Get(id)` | `Result<T?>` | `T`, throws on 404 |
| `Update`, `Delete` | `Result<bool>` | `Task`, throws on error |
| Invoices | CRUD | CRUD plus `UpdateStatus` and `GeneratePDF` |
| URLs | Hard-coded | `ClientsOptions` from configuration |
| List filters | Only the filter in the query | Every non-null property, so also `Page=1&PageSize=p25` (Biller's defaults) |

## Design decisions

1. **The tests read the error body themselves.** `ClientAPIException` in 1.1.1 has only a text
   message (status code and message, no extended message). The owner chose 1.1.1 over a new release.
   So `ApiErrorHandler`, a `DelegatingHandler` added to every client's `HttpClient`
   (`ConfigureHttpClientDefaults`), throws `ApiErrorException` for a non-success response: a
   `ClientAPIException` whose `Error` is the decoded `ErrorModel`. Error tests use
   `(await Assert.ThrowsAsync<ApiErrorException>(() => call))!.Error`. The error tests still compare
   the status code, message and extended message. (First built as an `ApiError.Capture` helper with an
   `AsyncLocal`; replaced after review because it was more code for the same check.)
   *Planned first:* `ClientAPIException.Error` in BillerContracts. Dropped, because it needs a 2.0.0 release.
2. **Reference only `BillerContracts.Clients` 1.1.1.** It depends on `BillerContracts` at the same
   version, so the two can't drift. BillerFrontEnd uses the same version, and Biller moves from 1.0.5 to 1.1.1.
3. **URLs from configuration.** `appsettings.json` in the test project with a `ClientsOptions` section
   (`http://localhost:8090/users`, …), copied to the output. Environment variables such as
   `ClientsOptions__UserUrl` can override it.
4. **One place builds the clients.** A static `TestClients` class builds a `ServiceProvider` once with
   `AddBillerClients(configuration.GetSection("ClientsOptions"))` and exposes `IUserClient`,
   `ISellerClient`, `ICustomerClient`, `IItemClient` and `IInvoiceClient`. Tests keep their
   `private static readonly` client fields, typed as the interfaces.
5. **Same checks, new shape.** Success: await the call and use the value. Error:
   `Assert.ThrowsAsync<ApiErrorException>` (design decision 1), then `CheckErrors` on its `Error`. Update and delete success: the
   call doesn't throw (instead of `IsTrue()` on a `bool`).
6. **Delete-all deletes everything.** Each `*_Delete_All` calls `Get()` and deletes that page until
   the list comes back empty.

## Steps

Run the `_sync-repos` skill before starting each repo.

1. [x] **Biller:** `BillerContracts` 1.0.5 → 1.1.1 in `src/Domain/Domain.csproj`. It adds only
   `FileDownloadResponse`; the 668 unit tests pass.
2. [x] **BillioIntegrationTest packages.** Replace `BillerContracts` 1.0.5 with `BillerContracts.Clients`
   1.1.1. Add `Microsoft.Extensions.Configuration.Json` and `.EnvironmentVariables` (DI comes in through
   the package). Remove `LanguageExt.Core`, `FluentValidation` and the `Serilog` packages.
3. [x] `appsettings.json`, `TestClients`, `ApiErrorHandler` and `DeleteAll` in `ApiClients/`
   (design decisions 1, 3 and 4).
4. [x] Convert the tests (design decision 5): all 118 `.Match(...)` calls, including the BIL-73 status
   tests. Then the delete-all helpers (design decision 6).
5. [x] Delete `Clients/` and `Interfaces/`, and the unused `using LanguageExt` lines.
   Also removed a second `_invoiceClient.Add` in `InvoiceAdd_InValid_Fail` whose result master
   ignored. With throwing clients it failed all 11 cases.
6. [x] Check (2026-10-24, local stack from this Biller branch):
   - Master suite: `Passed! - Failed: 0, Passed: 172` (65 tests, 172 cases). Branch: the same 172
     names (`--list-tests`), `Passed! - Failed: 0, Passed: 172`.
   - Changing one expected message ("Unauthorized") and one extended message ("Please specify last
     name") on purpose failed those cases with "Expected inputError.Message/ExtendedMessage to be
     equal to …". Undone.
   - `grep -rn "localhost:8090" src` finds it only in `appsettings.json`.
   - Two runs in a row both pass 172. Afterwards every list is empty, counted by rows.
   - With 30 invoices created beforehand (first page 25), the run passes and leaves no invoices.

## Order and other plans

- No publish is needed: 1.1.1 is on nuget.org. Biller's branch (the version bump) and the test branch
  have the same name, so each CI runs against the other.
- A later 2.0.0 release (the `UserGetRequest` namespace move) can add `ClientAPIException.Error`; then
  `ApiErrorHandler` can go.
- BIL-65 removes `InvoiceNumber` from `InvoiceUpdateRequest`, which `InvoiceTest` sets. Whichever lands
  second adapts the invoice tests.
- The package makes a new test possible: `GeneratePDF`, which `REVIEW.md` in BillioIntegrationTest
  lists as missing coverage. It is a follow-up, not part of this plan.

## Risk

Test code, plus a package bump in Biller that only adds a type. The risk is weaker tests: a
converted error test that no longer compares the messages, or a success test that swallows an
exception. The checks in step 6 cover both.

## How to revert

Revert the BillioIntegrationTest PR, which brings back its own clients and 1.0.5, and the Biller PR
(back to 1.0.5).

## Out of scope

- New tests (PDF, pagination, cross-user access).
- Changing Biller's error responses.

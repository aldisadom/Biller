# Plan: integration tests use the BillerContracts clients

Jira: [BIL-92](https://aldisadom.atlassian.net/browse/BIL-92). Epic: BIL-89 (Billeris). Repos: BillerContracts (step 1) and
BillioIntegrationTest (steps 2–6). It changes two repos, so it lives in Biller.

## Goal

- BillioIntegrationTest uses the newest BillerContracts and the `BillerContracts.Clients` package
  instead of its own copies of the HTTP clients.
- The 62 tests keep checking the same things, including the status code, message and extended
  message of every error case.
- Deleting all data before and after a run really deletes everything.

## Current state (BillioIntegrationTest master @ f5b2c2d, BillerContracts master @ 196fdcc)

| Where | What |
|---|---|
| `IntegrationTests.csproj` | `BillerContracts` 1.0.5. Newest on nuget.org: `BillerContracts` and `BillerContracts.Clients` 1.1.1 (2026-10-15) |
| `Clients/` (6 files), `Interfaces/` (5) | Local copies of the clients with `http://localhost:8090` hard-coded. They return `Result<T>` (from BillerContracts) and turn an error response into an `ErrorModel` (status code, message, extended message). `IInvoiceItemClient` is the item client's interface under the wrong name |
| Tests | 62 TUnit tests, 111 `.Match(...)` calls on results, 27 error checks through `CheckErrors(ErrorModel)` |
| `*_Delete_All` (5 helpers) | Call `Get()` once and delete what comes back. Biller returns only the first page (25 by default), so with more rows data is left behind |
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

1. **`ClientAPIException` carries the error (BillerContracts).** Add `public ErrorModel? Error { get; }`,
   filled for every non-success response: the status code always, the message and extended message
   from the `ErrorResponse` body when it parses. The existing constructors and message text stay, so
   this isn't a breaking change. Without it the error tests could only check the status code, which
   `REVIEW.md` counts as a failure. The front end can use the same property to show API errors.
2. **Reference only `BillerContracts.Clients`.** It depends on `BillerContracts` at the same version,
   so the two can't drift. BillerFrontEnd does the same.
3. **URLs from configuration.** `appsettings.json` in the test project with a `ClientsOptions` section
   (`http://localhost:8090/users`, …), copied to the output. Environment variables such as
   `ClientsOptions__UserUrl` can override it.
4. **One place builds the clients.** A static `TestClients` class builds a `ServiceProvider` once with
   `AddBillerClients(configuration.GetSection("ClientsOptions"))` and exposes `IUserClient`,
   `ISellerClient`, `ICustomerClient`, `IItemClient` and `IInvoiceClient`. Tests keep their
   `private static readonly` client fields, typed as the interfaces.
5. **Same checks, new shape.** Success: await the call and use the value. Error: assert that the call
   throws `ClientAPIException` and pass its `Error` to `CheckErrors`. Update and delete success: the
   call doesn't throw (instead of `IsTrue()` on a `bool`).
6. **Delete-all deletes everything.** Each `*_Delete_All` calls `Get()` and deletes that page until
   the list comes back empty.

## Steps

Run the `_sync-repos` skill before starting each repo.

1. **BillerContracts, own PR.** Design decision 1 in `Clients/ClientAPIException.cs` and
   `BaseHttpClient.DecodeResponseError`, with tests in `Clients.Tests/BaseHttpClientTest.cs`:
   - 400 with an `ErrorResponse` body: `Error` has the status code, message and extended message;
   - 404 with a body that isn't JSON: `Error` has the status code;
   - success: no exception.

   Then the owner publishes the next version: 1.2.0, or 2.0.0 if the `UserGetRequest` namespace
   move (BillerContracts PR #8) is in the same release. Biller's rule treats that move as breaking.
2. **BillioIntegrationTest packages.** Replace `BillerContracts` 1.0.5 with `BillerContracts.Clients`
   at step 1's version. Add `Microsoft.Extensions.Configuration.Json` (and
   `Microsoft.Extensions.DependencyInjection` if it doesn't come in through the package). Remove
   `LanguageExt.Core`, `FluentValidation` and the `Serilog` packages once nothing uses them.
3. `appsettings.json` and `TestClients` (design decisions 3 and 4).
4. Convert the tests file by file in chain order: users, sellers, customers, items, invoices
   (design decision 5). Then the delete-all helpers (design decision 6).
5. Delete `Clients/` and `Interfaces/`, and the unused `using LanguageExt` lines.
6. Check:
   - Before step 2, run the suite against Biller master and keep the summary line. After step 5, the
     same 62 tests pass, with the same names.
   - Change one expected error message on purpose and see that test fail, then undo it. This shows the
     error checks still compare the messages.
   - `grep -rn "localhost:8090" src` finds it only in `appsettings.json`.
   - Run the suite twice in a row. The second run starts from an empty database.

## Order and other plans

- Step 1 has to be published before step 2 can merge. Until then the test branch can use a preview
  package from the contracts branch (`<version>-preview.<run>`), replaced before merging.
- Biller doesn't change. Its CI keeps running these tests from the test branch with the same name,
  else master.
- BIL-65 removes `InvoiceNumber` from `InvoiceUpdateRequest`, which `InvoiceTest` sets. Whichever lands
  second adapts the invoice tests.
- The package makes new tests possible: `UpdateStatus` (BIL-73) and `GeneratePDF`, which
  `REVIEW.md` in BillioIntegrationTest lists as missing coverage. They are follow-ups, not part of
  this plan.

## Risk

Test code only, plus an additive change to `BillerContracts.Clients`. The risk is weaker tests: a
converted error test that no longer compares the messages, or a success test that swallows an
exception. The checks in step 6 cover both.

## How to revert

Revert the BillioIntegrationTest PR, which brings back its own clients and 1.0.5. The contracts
change can stay, since it only adds a property.

## Out of scope

- New tests (status, PDF, pagination, cross-user access).
- Changing Biller's error responses.

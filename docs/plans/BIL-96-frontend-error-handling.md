# Plan: global error handling in the front end (BIL-96)

Jira: [BIL-96](https://aldisadom.atlassian.net/browse/BIL-96). Epic: BIL-89 (Billeris). Repos:
BillerContracts (step 1), Biller (step 2), BillerFrontEnd (steps 3–4), BillioIntegrationTest (step 5).
It changes several repos, so it lives in Biller.

## Decisions from the owner (2026-10-25)

1. **Message in place of the page, with "Try again".** No toast and no automatic `Recover()`: a page
   whose `OnInitializedAsync` fails would throw again on every `Recover()` until
   `ErrorBoundary.MaximumErrorCount` (100) ends the circuit. (Design decision 4.)
2. **Keep prerendering.** Global `InteractiveServer` with the default prerender; pages keep
   `[StreamRendering]`. `OnInitializedAsync` still runs twice (prerender, then interactive). Step 6
   checks that an error during the prerender pass also ends in a readable message.
3. **BillerContracts 2.0.0** is released for this (the `UserGetRequest` namespace move is on master).

## Goal

- An API error (400, 404, 500) or an unexpected exception on any page shows a readable message, and
  the app keeps working: no "An unhandled error has occurred. Reload".
- The message is the API's message and extended message ("Validation failure: Please specify name").
- No page needs its own try/catch around client calls.
- The integration tests drop the BIL-92 workaround (`ApiErrorHandler`) and use the package's error.

## Current state (Biller master @ f827333, BillerFrontEnd @ 7b402f3, BillerContracts @ 6f7898e, BillioIntegrationTest @ c40bca2)

| Where | What |
|---|---|
| Contracts `Clients/ClientAPIException.cs` | Message text only. `BaseHttpClient.DecodeResponseError` puts the status code and `ErrorResponse.Message` in the text, drops `ExtendedMessage`, keeps nothing structured |
| Contracts `ErrorModel` | `StatusCode`, `Message`, `ExtendedMessage` (namespace `BillerContracts`) |
| Contracts master | `UserGetRequest` moved from `Requests.Seller` to `Requests.User` (unreleased, breaking). Nothing in Biller, BillerFrontEnd or BillioIntegrationTest uses `UserGetRequest` |
| Versions | Biller `BillerContracts` 1.1.1 (BIL-92). BillerFrontEnd `BillerContracts.Clients` 1.1.1. BillioIntegrationTest 1.1.1 (BIL-92, PR #16 merged after this plan was started) |
| FE `Components/App.razor` | `<Routes />` and `<HeadOutlet />` without a render mode, so router and `MainLayout` are static |
| FE pages | 25 pages set `@rendermode InteractiveServer`; 24 also `[StreamRendering]`. 24 load data in `OnInitializedAsync`, 55 `@onclick`/submit handlers |
| FE error handling | No try/catch, no `async void`, no `ErrorBoundary`. Pages throw `Domain.Exceptions.NotFoundException("Invalid id format")` for bad ids (4 pages). `MainLayout` has only `#blazor-error-ui` |
| FE `Domain/Models/ErrorModel.cs` | Unused, and clashes by name with `BillerContracts.ErrorModel` |
| FE Fluent UI | Only `Microsoft.Fast.Components.FluentUI` 3.7.8 (the old FAST package), imported in `Invoices_add.razor` but no component used. Not registered in `Program.cs`. No toast component available |
| FE `Program.cs` | `UseExceptionHandler("/Error")` runs when `DetailedErrors` is **true** (inverted). Pre-existing, affects only non-Blazor HTTP errors |
| IT `ApiClients/` | `ApiClients/ApiErrorHandler.cs`: `ApiErrorHandler` + `ApiErrorException`, the BIL-92 workaround; 29 error tests use `Assert.ThrowsAsync<ApiErrorException>(...)!.Error` |

## Design decisions

1. **`ClientAPIException.Error` (`BillerContracts.ErrorModel?`).** Filled for every non-success
   response: the status code always, message and extended message when the body is an
   `ErrorResponse`. Null for other failures (deserialization). The existing constructors stay.
   The text also gets `, extended message: …`, so logs and test failures show the useful part.
2. **Release 2.0.0** (because of the namespace move already on master), and all three consumers move
   to it. None uses `UserGetRequest`, so the move needs no code change.
3. **Global interactivity, with prerender:** `<Routes @rendermode="InteractiveServer" />` and
   `<HeadOutlet @rendermode="InteractiveServer" />`; the 25 per-page `@rendermode` lines go,
   `[StreamRendering]` stays. Then `MainLayout` is interactive and an `ErrorBoundary` in it sees errors
   from pages.
4. **`AppErrorBoundary : ErrorBoundary`** in `Components/Layout/`, around `@Body` in `MainLayout`:
   - `OnErrorAsync` logs the exception (`ILogger<AppErrorBoundary>`, full exception).
   - `ErrorContent` shows the message in a Bootstrap alert, with "Try again" (`Recover()`) and "Back"
     buttons. The page is replaced, so no half-rendered state stays on screen.
   - Navigation (`NavigationManager.LocationChanged`) calls `Recover()`.
   - No automatic `Recover()` and no toast (owner decision 1), so a page that fails while loading
     can't loop.
5. **One place turns an exception into text:** `ErrorMessages.For(Exception)` (static, in
   `src/FrontEnd/Capabilities/`):
   - `ClientAPIException` with `Error`: `Message`, plus `": " + ExtendedMessage` when there is one.
     A 404 reads "Entity not found: Invoice:… not found".
   - `ClientAPIException` without `Error`, or `HttpRequestException`: "The server could not be reached".
   - `NotFoundException`: its message.
   - Anything else: "Something went wrong". Details only in the log.
   Static and free of Blazor types, so BIL-87 can unit test it.
6. **The unused FE `Domain.Models.ErrorModel` is deleted**, so `ErrorModel` means the contracts one.
7. **Integration tests:** `Assert.ThrowsAsync<ClientAPIException>(...)` and its `Error`; delete
   `ApiClients/ApiErrorHandler.cs` and its registration. The checks stay status code, message and
   extended message.

## Dependencies

- BIL-73 (Done): removed the `async void` handlers.
- BIL-92: done (PR #16 merged, `ApiErrorHandler` is on BillioIntegrationTest master).
- BIL-91 (AutoMapper removal) and BIL-97 (PDF download) also edit the FE pages. Step 3 removes one
  line per page, so conflicts are trivial; whichever lands second rebases.
- BIL-94 (package updates) also releases contracts. If it goes first, this release is that one.

## Step 1: BillerContracts (own PR, then the owner publishes 2.0.0)

- `Clients/ClientAPIException.cs`: `public ErrorModel? Error { get; }` and a constructor
  `(string message, ErrorModel error)`.
- `Clients/BaseHttpClient.cs` `DecodeResponseError`: build the `ErrorModel` (status code always;
  message and extended message from `ErrorResponse`), add the extended message to the text, throw with it.
- `Clients.Tests/BaseHttpClientTest.cs`: 400 with an `ErrorResponse` body → `Error` has all three;
  404 with a non-JSON body → status code only; deserialization failure → `Error` is null; update the
  exact-text tests for the extended message.
- Pull request template: **breaking** (namespace move), so 2.0.0.

## Step 2: Biller

- `src/Domain/Domain.csproj`: `BillerContracts` 1.1.1 → 2.0.0. Unit tests pass.

## Step 3: BillerFrontEnd, render mode

- `Components/App.razor`: design decision 3.
- Remove `@rendermode InteractiveServer` from the 25 pages.
- `FrontEnd.csproj`: `BillerContracts.Clients` 2.0.0.

## Step 4: BillerFrontEnd, error boundary

- `Capabilities/ErrorMessages.cs` (design decision 5).
- `Components/Layout/AppErrorBoundary.razor` (design decision 4), used in `MainLayout.razor`
  around `@Body`. Keep `#blazor-error-ui` for errors outside the boundary (layout, nav menu).
- Delete `src/Domain/Models/ErrorModel.cs`.

## Step 5: BillioIntegrationTest

- `BillerContracts.Clients` 2.0.0; error tests as design decision 7; delete `ApiErrorHandler.cs`,
  its line in `TestClients.cs`, and update `CLAUDE.md`.

## Step 6: Check

- Contracts: `dotnet test -c Release`. Biller: `run-coverage.sh`. FE: `dotnet build BillerFrontEnd.sln -c Release`.
- In the browser, against a running Biller: a status change on a deleted invoice, an invalid form
  (seller without company number), `/invoices/not-a-guid`, `/invoices/<random guid>`, and Biller
  stopped. Each shows the message, "Try again" and navigation work, and the circuit stays alive.
- Each case above also on a fresh page load (F5), so the error happens during prerender.
- Integration tests: same 172 cases pass; one wrong expected extended message fails.

## Out of scope

- PDF download (BIL-97).
- Removing the unused FAST Fluent package and fixing the Fluent line in the FE `CLAUDE.md` (BIL-94).
- The inverted `DetailedErrors` check in `Program.cs` (pre-existing; separate fix).
- Showing an error next to a specific form field.

## Order of work

1. Step 1, PR, then the owner publishes 2.0.0.
2. Steps 2, 3 and 4 on branches named `BIL-96-frontend-error-handling` (Biller, BillerFrontEnd).
3. Step 5 (PR #16 is merged), with step 1's release.
4. Step 6.

## Risk

- Global interactivity: the layout and nav menu now run over the SignalR connection too.
- With prerender, every page still loads its data twice.
- "Try again" re-creates the page: unsaved form input is lost.
- Code outside the component tree (timers, `Task.Run`) isn't covered by the boundary.

## How to revert

Revert the FE PR (back to per-page render modes and no boundary). The contracts change is additive
apart from the namespace move, which ships in 2.0.0 regardless.

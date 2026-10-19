# Plan: Billio front-end testing

Jira: [BIL-87](https://aldisadom.atlassian.net/browse/BIL-87). Epic: BIL-89 (Billeris). Repo: `BillerFrontEnd`.

## Goal

- The front end has automated tests that run in CI on every PR.
- Page logic (loading, mapping, button actions, navigation, errors) is tested without a running API.
- The HTTP clients and AutoMapper profiles are tested.
- Later, a few end-to-end tests cover the main flows against the real stack.

## Current state (BillerFrontEnd master @ e9a6e89)

| Where | What |
|---|---|
| `src/FrontEnd` | Blazor Server (.NET 8, `InteractiveServer`), FluentUI 3.7.8, QuickGrid, AutoMapper 13 |
| Pages | 25 `.razor` pages under `Components/Pages/{Customer,Invoice,Items,Seller,Users}`. Largest: `Invoices_update` (288 lines), `Invoices_add` (272) |
| `src/Client` | `BaseHttpClient` (229 lines) plus one client per entity behind `I*Client` interfaces. Pages inject the interfaces, so they're easy to mock |
| `MappingProfiles` | One AutoMapper profile per entity |
| Tests | None |
| CI (`.github/workflows/build.yml`) | Restore and build only |
| Claude skills | None in this repo |

## Design decisions

1. **bUnit for component tests.** It renders Razor components in memory, lets tests click buttons and read the markup, and has a fake `NavigationManager`. No browser needed.
2. **Same test conventions as Biller** (see Biller's `write-tests` skill): xUnit, strict Moq mocks with `Verify`, hand-built test data, theories with a `Label`. Check that the bUnit version supports xUnit v3; fall back to xUnit v2 in this repo if not.
3. **Clients are tested with a fake `HttpMessageHandler`**, checking the URL, method, body, and how error responses become exceptions.
4. **End-to-end tests come later and use Playwright for .NET**, against `docker compose` (Biller + front end). They go in a separate project so the fast tests stay fast.
5. **Test the behaviour, not the markup.** Assert on what is shown (names, prices, totals) and what was called, not on exact HTML.

## Step 1: Test project

- `tests/FrontEnd.Tests/FrontEnd.Tests.csproj`: xUnit, `bunit`, `Moq`, `coverlet.collector`. References `FrontEnd` and `Clients`.
- Add it to `BillerFrontEnd.sln`.
- A small base class that registers mocks for every `I*Client`, the real AutoMapper configuration, and the FluentUI services, so each test only sets up what it uses.

## Step 2: Mapping profiles (quick win)

- `MapperConfiguration.AssertConfigurationIsValid()` over all profiles.
- One test per profile for the maps pages use (`ItemResponse` → `ItemUpdateRequest`, etc.).

## Step 3: Clients

- `BaseHttpClient`: success, 400 with a validation body, 404, 500, and how each becomes an exception or result.
- One test file per client: each method hits the right URL and method with the right body.

## Step 4: Pages (bUnit), in priority order

| Page | Cases |
|---|---|
| `Invoices_add` | Loads sellers/customers/items, adding and removing lines, total, submit calls `IInvoiceClient.Add` with the right request, navigation after save, API error is shown |
| `Invoices_update` | Loads the invoice, edits a line, submit sends `InvoiceUpdateRequest`, delete |
| `Users_login` | Wrong password shows an error, success navigates |
| `*_update` pages (Items, Customers, Sellers, Users) | Invalid id string throws `NotFoundException`, loads and binds values, Update calls the client and navigates, Delete calls the client and navigates |
| `*_add` pages | Submit sends the filled request |
| List pages (`Items`, `Invoices`, …) | Paging calls the client with the right page, empty list |

Writing these may show logic worth moving out of the `.razor` files into small classes (e.g. invoice line totals). Do that only where a test needs it.

## Step 5: CI

Add to `build.yml` after Build:
```yaml
      - name: Test
        run: dotnet test BillerFrontEnd.sln --no-build --configuration Release --collect:"XPlat Code Coverage"
```

## Step 6: Claude skills for this repo

- Copy and adapt Biller's `write-tests` and `test-coverage` skills into `BillerFrontEnd/.claude/skills/`, with a bUnit section (rendering, `Find`/`Click`, `FakeNavigationManager`, waiting for async rendering).

## Step 7: End-to-end tests (later phase)

- `tests/FrontEnd.E2E` with `Microsoft.Playwright`.
- Flows: log in → create seller → customer → item → invoice → download PDF; edit item and see the change.
- CI job that starts Biller (`docker-compose.ci.yml`), the front end, runs migrations, then the tests. Could share setup with the BillioIntegrationTest repo.

## Out of scope

- Visual/screenshot comparison.
- Rewriting pages for testability beyond what tests need.

## Order of work

1. Test project, mapping tests, CI step (one PR, so CI runs tests from the start).
2. Client tests.
3. `Invoices_add` and `Invoices_update`.
4. The other pages.
5. Skills.
6. E2E.

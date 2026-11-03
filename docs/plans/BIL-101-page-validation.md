# Plan: validate Page and PageSize on every paged GET (BIL-101)

Jira: [BIL-101](https://aldisadom.atlassian.net/browse/BIL-101). Epic: BIL-89 (Billeris).

## Progress

- [x] Step 0: `IUserClient.Get(UserGetRequest)` and its test in BillerContracts, PR #14, published as 4.1.0.
- [x] Steps 1–3: `PagingRules`, five Get validators, five controllers, tests. 797 unit tests pass; the new validators and the five controllers are fully covered.
- [x] Step 4 for invoices, customers, items, sellers: page 1, page 0 and page 42949673 per endpoint. 194 passed, three runs, on a throwaway stack.
- [x] Step 4 for users: BillioIntegrationTest on BillerContracts.Clients 4.1.0, the same three tests. 197 passed, three runs, on a throwaway stack.
- [x] Review: the page-1 tests became last-allowed-page tests (42949672 at p50), and `Tests/Paging.cs` holds `MaxPage` and the message. BillerFrontEnd goes to BillerContracts.Clients 4.1.0 too, on its own `BIL-101-page-validation` branch. 197 passed, three runs.
- [x] Owner's follow-ups:
  - non-nullable list queries without the null guard;
  - binding errors answered with `ErrorResponse` (`ModelStateErrors`);
  - empty-Guid invoice filters rejected;
  - invoice TotalCount and page-order integration tests;
  - the undefined page size tested on every paged GET;
  - the invoice list's double fetch fixed in BillerFrontEnd.

  798 unit tests and 207 integration tests pass, three runs.
- [x] Step 5, shared base types (branch `BIL-101-paged-base-types`, merged into this branch by
  Biller #75, BillioIntegrationTest #24, BillerFrontEnd #27):
  - BillerContracts 4.2.0 (#15, merged and published): `PagedRequest` and `PagedResponse` are the base records of every list query and response;
  - `PagedRequestValidator<T>` replaces `PagingRules`, `WithPaging` fills the paging fields;
  - empty-Guid filters rejected on customers, items, sellers and series too;
  - the paging rules are tested once (`PagedRequestValidatorTest`); each Get validator test checks its filters, and its default-request test checks that it inherits `PagedRequestValidator<T>` (owner's decision).

  775 unit tests and 211 integration tests pass, three runs. The JSON field order is unchanged.

## Goal

- `GET /invoices`, `/customers`, `/items`, `/sellers` and `/users` return 400 for a page below 1 or
  above the maximum, with the message `GET /series` gives ("Please specify page from 1 to 42949672"),
  instead of a 500 (invoices) or a silently ignored value (the others).
- `GET /users` takes the `UserGetRequest` query for this (owner's decision: add it now rather than
  with BIL-102). It still returns every user until BIL-102 pages users.

## Current state (master @ 9a822ea)

| Where | What |
|---|---|
| `SeriesGetValidator` | `Page` from 1 to `int.MaxValue / 50` (the largest `PageSize`), "Please specify page from 1 to {max}"; `PageSize` `IsInEnum`, "Please specify valid page size". The max is a private static field |
| `SeriesController.Get` | `if (query is not null) _validatorGet.CheckValidation(query);`, declares 400 |
| `InvoiceController.Get` | No validator. `InvoiceRepository.Get` pages in SQL (`OFFSET (page - 1) * pageSize`): page 0 or below gives a negative OFFSET and a 500 "Database error"; a page above about 85 million overflows the int multiplication |
| `CustomerController.Get`, `ItemController.Get`, `SellerController.Get` | Take `<X>GetRequest? query`, no validator, no 400 declared. The services ignore `Page`/`PageSize` and return every row; the responses don't set `Page`, `PageSize` or `TotalCount` |
| `UserController.Get()` | No query at all. `IUserClient` (BillerContracts.Clients) has only `Get()`, no `Get(UserGetRequest)` |
| BillerContracts 4.0.1 | `InvoiceGetRequest`, `CustomerGetRequest`, `ItemGetRequest`, `SellerGetRequest`, `UserGetRequest`, `SeriesGetRequest`: each has `int Page = 1` and `PageSize PageSize = p25`, no shared base type |
| `PageSize` from the query | An unknown value (`PageSize=7`) is rejected by model binding with its own 400 before any validator runs |
| Tests | `SeriesGetValidatorTest` (default, page 1 / max / 0 / -1 / max+1, undefined page size); `SeriesControllerTest` mocks `IValidator<SeriesGetRequest>` (strict) and covers a null query, a valid one and an invalid one |

## Design decisions

1. **Shared rules, one validator per request type.** Steps 1–3 put them in `Validators/PagingRules.cs`
   (`MaxPage` and two rule extensions), because the request types shared no base type. Step 5
   (owner's decision) gives the requests a base record, `PagedRequest`, so the rules now live in
   `PagedRequestValidator<T> where T : PagedRequest` and the six Get validators inherit it. Each
   validator class stays, because DI registers one `IValidator<<X>GetRequest>` per request type.
2. **Validated only when a query is given**, as `SeriesController` does: a null query falls back to
   page 1 and p25, which are valid.
3. **Contracts versions.** Steps 0–4: Biller keeps 4.0.1; the integration tests need
   `IUserClient.Get(UserGetRequest)`, so BillerContracts.Clients 4.1.0 adds that one method. Step 5:
   the base records ship as **4.2.0**, not 5.0.0 (owner's decision). No name, query string or JSON
   changes; only code compiled against 4.x without rebuilding would notice. Biller, BillioIntegrationTest
   and BillerFrontEnd all move to 4.2.0.
4. **`PageSize` is still checked with `IsInEnum`** although model binding catches unknown values from
   the URL: the validator also guards callers that build the request in code.

## Dependencies

None. For customers, items and sellers the validator only guards the input until BIL-102 makes
those endpoints page in SQL. The integration tests for `/users` need BillerContracts.Clients 4.1.0.

## Step 0: BillerContracts (branch `BIL-101-page-validation`)

- `Clients/Interfaces/IUserClient.cs` and `Clients/Clients/UserClient.cs`: `Get(UserGetRequest request)`,
  like `CustomerClient.Get(CustomerGetRequest)`, with a test in `Clients.Tests/UserClientTest.cs`.
- After merge the owner publishes 4.1.0; BillioIntegrationTest bumps to it.

## Step 1: Validators

- `src/Validators/PagingRules.cs`: `MaxPage = int.MaxValue / Enum.GetValues<PageSize>().Max(p => (int)p)`,
  `ValidPage<T>(this IRuleBuilder<T, int>)`, `ValidPageSize<T>(this IRuleBuilder<T, PageSize>)`.
- `SeriesGetValidator`: use them (same behaviour and messages).
- New `InvoiceGetValidator`, `CustomerGetValidator`, `ItemGetValidator`, `SellerGetValidator`,
  `UserGetValidator`, each two lines like `SeriesGetValidator`.
- `Validators/DependencyInjection.cs`: register the five, once each.

## Step 2: WebAPI

- `InvoiceController`, `CustomerController`, `ItemController`, `SellerController`, `UserController`:
  inject `IValidator<<X>GetRequest> validatorGet`, call it in `Get` when the query isn't null, and
  declare `[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]` on `Get`.
- `UserController.Get` takes `[FromQuery] UserGetRequest? query`; `IUserService.Get()` doesn't change.

## Step 3: Tests (`_write-tests`)

- `<X>GetValidatorTest` for the five, copying `SeriesGetValidatorTest`'s cases (default, page 1, max,
  0, -1, max + 1, undefined page size). `SeriesGetValidatorTest` stays as the check that it didn't change.
- `PagingRules` gets no test of its own: the five validator tests cover it.
- The five controller tests: the strict `IValidator<<X>GetRequest>` mock, and the three `Get` cases
  `SeriesControllerTest` has (null query not validated, valid query validated once, invalid query throws).
- `Validators/DependencyInjectionTests.cs`: the five registrations.

## Step 4: Integration tests (BillioIntegrationTest, branch `BIL-101-page-validation`)

- BillerContracts.Clients 4.1.0 once published (a preview from the contracts branch until then).
- Per endpoint (`/invoices`, `/customers`, `/items`, `/sellers`, `/users`): page 0 and page 42949673 return
  400 with "Please specify page from 1 to 42949672"; page 1 still returns 200.

## Step 5: Shared base types (branch `BIL-101-paged-base-types` in every repo, PRs into `BIL-101-page-validation`)

- BillerContracts: abstract records `Requests/PagedRequest` (`Page = 1`, `PageSize = p25`) and
  `Responses/PagedResponse` (`TotalCount`, `Page`, `PageSize`). The six `<X>GetRequest` keep only
  their filters, the six `<X>ListResponse` only their list; `UserListResponse` becomes a record.
  Published as 4.2.0.
- Biller: `PagedRequestValidator<T>` (rules) and a non-generic `PagedRequestValidator.MaxPage`;
  `PagingRules` is removed. Customers and series reject `SellerId = Guid.Empty`, items
  `CustomerId`, sellers `UserId`, like invoices. `WithPaging(query, totalCount)` fills the paging
  fields of the invoice and series list responses.
- Tests: `PagedRequestValidatorTest` covers the paging rules on a test-only `TestPagedRequest`;
  each `<X>GetValidatorTest` checks its filters and, in `Validate_GivenDefaultRequest_IsValid`,
  that the validator is a `PagedRequestValidator<<X>GetRequest>`.
- BillioIntegrationTest: an empty-Guid filter test per endpoint; clients 4.2.0.
- BillerFrontEnd: clients 4.2.0, no code change.

## Out of scope

- Paging customers, items, sellers and users in SQL, and setting `Page`/`PageSize`/`TotalCount` in
  their responses (BIL-102).
- Front end: it always sends page 1 or more.

## Order of work

1. Step 0 in BillerContracts (PR, then the owner publishes).
2. Steps 1–3 in Biller, `_test-coverage`.
3. Step 4, then `_test-stack` with `run-integration.sh --repeat 3`.
4. `_pre-pr` for each repo.
5. Step 5: BillerContracts #15 (merged, 4.2.0 published), then Biller #75, BillioIntegrationTest #24
   and BillerFrontEnd #27 into their `BIL-101-page-validation` branches, then #73, #23 and #25 into master.

# Plan: page the lists on the user, seller and customer pages (BIL-102)

Jira: [BIL-102](https://aldisadom.atlassian.net/browse/BIL-102). Epic: BIL-89 (Billeris).

## Goal

- The user page pages its sellers, the seller page its customers, and the customer page its items
  and its invoices. Each grid loads one page at a time from the API, has a page size select
  (10/25/50) and keeps the page in the URL (`?page=`), like `/invoices`.
- `GET /sellers`, `/customers` and `/items` page in SQL and return `TotalCount`, `Page` and `PageSize`.
- No caller silently loses rows after the first page.

## Current state (master @ 26f7b19)

| Where | What |
|---|---|
| BillerContracts 4.2.0 | `SellerGetRequest`, `CustomerGetRequest`, `ItemGetRequest` already extend `PagedRequest` (`Page = 1`, `PageSize = p25`); list responses extend `PagedResponse`. Clients send `Page`/`PageSize` in the query. No change needed |
| Validators | `SellerGetValidator`, `CustomerGetValidator`, `ItemGetValidator` validate `Page`/`PageSize` (BIL-101) |
| `SellerRepository` / `CustomerRepository` / `ItemRepository` | `Get()` and `GetByUserId` / `GetBySellerId` / `GetByCustomerId` return every row, no `ORDER BY` |
| `Validators/Invoice/InvoiceModelUpdateValidator.cs` | Class `InvoiceValidator`: loads every row with `GetByUserId`, `GetBySellerId`, `GetByCustomerId` to check one id. Not registered in DI and not called anywhere; only `InvoiceValidatorTest` uses it. The real checks are `SellerService`/`CustomerService`/`ItemService.GetWithValidation`, which use `Get(id)` |
| `SellerService` / `CustomerService` / `ItemService` `.Get(query)` | Pick `Get()` or `GetBy*` by filter, return `IEnumerable<Model>` |
| `SellerController` / `CustomerController` / `ItemController` `GET` | Build the list response without `.WithPaging(...)`, so `TotalCount`, `Page`, `PageSize` are unset |
| Reference | `SeriesRepository.Get(sellerId, page, pageSize)` → `(IEnumerable, int)`, `SeriesService.Get`, `SeriesController.Get` with `.WithPaging(query, totalCount)` |
| Indexes | `idx_sellers_user_id`, `idx_customers_seller_id`, `idx_items_user_id` (on `customer_id`) already exist. No migration |
| Front end `Users_id.razor`, `Sellers_id.razor`, `Customers_id.razor` | One `Get(request)` each, `QuickGrid` with `Paginator`, 50 per page in the browser |
| Front end `Customers_id.razor` | No invoice list |
| Front end `Invoices.razor` | The pattern: `ItemsProvider`, page size select, `?page=` |
| Front end `SeriesClientExtensions.GetAllForSeller` | Loads every page of series; used by `Sellers_id`, `Customers_add`, `Customers_update` |
| Front end selectors | `Invoices_add`: sellers by user, customers by seller, items by customer. `Invoices_update`: items by customer. All one `Get` call |
| BillioIntegrationTest | Sellers/customers/items tests call `Get()` for lists; `DeleteAll.Run` already loops until the list is empty |

## Design decisions

1. **Repositories get one paged `Get(Guid? <parent>Id, int page, int pageSize)` returning
   `(IEnumerable<Entity>, int)`**, like `SeriesRepository.Get`, replacing the unpaged `Get()`.
   Count and data are two statements, as in Series and Invoice (reads, so the one-statement rule
   for writes doesn't apply).
2. **Delete the unused `InvoiceValidator` and `GetByUserId`, `GetBySellerId`, `GetByCustomerId`**
   (Gap 1). The validator loads a whole list to check one id and nothing calls it; the ownership
   checks it duplicates are the services' `GetWithValidation`, which read one row by id. With it
   gone, the `GetBy*` methods have no callers left once the services use the paged `Get`, so they
   go too and no unpaged list query remains.
3. **Stable order:** sellers and customers `ORDER BY company_name, id`, items `ORDER BY name, id`.
   The `id` tie-break keeps pages from overlapping when names repeat.
4. **Services return `(IEnumerable<Model>, int)`** and default to page 1 / `p25` when the query is
   null, like `SeriesService.Get`. Controllers add `.WithPaging(query, totalCount)`.
5. **Front-end selectors load every page** (Gap 2) through `SellerClientExtensions.GetAllForUser`,
   `CustomerClientExtensions.GetAllForSeller` and `ItemClientExtensions.GetAllForCustomer`, copies
   of `SeriesClientExtensions.GetAllForSeller` (page size 50, stop on an empty page or when
   `TotalCount` is reached). Callers:
   - `Invoices_add.razor`: sellers by user (start), customers by seller (start and `SellerChange`),
     items by customer (`CustomerChange`).
   - `Invoices_update.razor`: items by customer (`GetItems`).
   - `Customers_add.razor` and `Customers_update.razor` load only series, which already use
     `GetAllForSeller`. No change.
6. **The seller page's series list stays as it is** (`GetAllForSeller`, paged in the browser). It
   already gets every row, and a seller has few series.
7. **Customer page invoice grid** uses `GET /invoices?customerId=`: series and number, created
   date, due date, status, total, and a link to `/invoices/{id}`. Same columns and formats as
   `Invoices.razor`, without seller and customer.
8. **No contract change, no migration.** The requests, responses and clients already carry paging.
9. **The other list pages move or go**, because once the API pages, a page that sends no
   `Page`/`PageSize` shows only the first 25 rows. This widens the issue's scope (it left the
   top-level lists out), agreed with the user:
   - `/sellers`, `/customers`, `/items` (`Sellers.razor`, `Customers.razor`, `Items.razor`) move to
     the same `ItemsProvider` grid as the detail pages, without a filter.
   - `/sellers/user/{id}`, `/customers/sellers/{id}`, `/items/customer/{id}` (`Sellers_userId.razor`,
     `Customers_sellerId.razor`, `Items_customerId.razor`) are deleted. No page links to them, and
     the user, seller and customer pages show the same lists.

## Dependencies

- BIL-101 (page validation): Done.
- BIL-99 (invoice edit shows names instead of seller/customer selectors): Done. `Invoices_update`
  still loads the item selector with one `Get`, covered by decision 5.
- Release order: Biller first or together with the front end. The old front end on a new API
  would show only 25 rows per list, so deploy both together.

## Step 1: BillerContracts

No change. Biller, the front end and the integration tests stay on 4.2.0.

## Step 2: Migration

None. The filter columns are indexed.

## Step 3: Domain and Infrastructure

- `ISellerRepository`: `Task<(IEnumerable<SellerEntity>, int)> Get(Guid? userId, int page, int pageSize)` replaces `Get()` and `GetByUserId`.
- `ICustomerRepository`: `Get(Guid? sellerId, int page, int pageSize)` replaces `Get()` and `GetBySellerId`.
- `IItemRepository`: `Get(Guid? customerId, int page, int pageSize)` replaces `Get()` and `GetByCustomerId`.
  `Get(List<Guid> ids)` stays (used by `ItemService.GetWithValidation`).

```sql
-- sellers
SELECT COUNT(*) FROM sellers WHERE (@UserId IS NULL OR user_id = @UserId);
SELECT * FROM sellers WHERE (@UserId IS NULL OR user_id = @UserId)
ORDER BY company_name, id
LIMIT @PageSize OFFSET @Offset;

-- customers
SELECT COUNT(*) FROM customers c WHERE (@SellerId IS NULL OR c.seller_id = @SellerId);
SELECT c.*, s.name AS series_name FROM customers c
JOIN series s ON s.id = c.series_id
WHERE (@SellerId IS NULL OR c.seller_id = @SellerId)
ORDER BY c.company_name, c.id
LIMIT @PageSize OFFSET @Offset;

-- items
SELECT COUNT(*) FROM items WHERE (@CustomerId IS NULL OR customer_id = @CustomerId);
SELECT * FROM items WHERE (@CustomerId IS NULL OR customer_id = @CustomerId)
ORDER BY name, id
LIMIT @PageSize OFFSET @Offset;
```

## Step 4: Application

- `ISellerService.Get(SellerGetRequest? query)`, `ICustomerService.Get(...)`, `IItemService.Get(ItemGetRequest?)`
  return `(IEnumerable<Model>, int)` and pass the filter, `Page` and `PageSize` straight through
  (no more `if`/`else` on the filter).

## Step 5: Validators

- Delete `src/Validators/Invoice/InvoiceModelUpdateValidator.cs` (decision 2).
- The Get validators don't change (BIL-101 already validates paging and the filter ids).

## Step 6: WebAPI

- `SellerController.Get`, `CustomerController.Get`, `ItemController.Get`: take the tuple and
  return `new XListResponse { ... }.WithPaging(query, totalCount)`.
- Swagger list examples: check they fill `TotalCount`, `Page`, `PageSize` (the example test
  requires every property).

## Step 7: Unit tests (`_write-tests`)

- `SellerServiceTest`, `CustomerServiceTest`, `ItemServiceTest`: `Get` with a filter, without one,
  and with a null query (defaults page 1 / `p25`), verifying the repository arguments and the total.
- `SellerControllerTest`, `CustomerControllerTest`, `ItemControllerTest`: the response carries
  `TotalCount`, `Page`, `PageSize`.
- Delete `Validators/Invoice/InvoiceValidatorTest.cs` with the validator.

## Step 8: Integration tests (BillioIntegrationTest, branch `BIL-102-page-detail-lists`)

- For sellers (by user), customers (by seller) and items (by customer): create 3 rows, get
  page 2 with `PageSize = p1`, and check it holds the second row by the order in decision 3 and
  that `TotalCount = 3`, `Page = 2`, `PageSize = p1`.
- A page past the end returns an empty list with the right `TotalCount`.
- Existing tests that expect every row from `Get()` keep passing (they create fewer than 25 rows).

## Step 9: Front end (BillerFrontEnd, branch `BIL-102-page-detail-lists`)

- `Users_id.razor` (sellers), `Sellers_id.razor` (customers), `Customers_id.razor` (items): copy the
  `Invoices.razor` grid: `ItemsProvider` with the parent id filter, page size select, `?page=`.
  Two grids on the customer page share one URL, so the items grid uses `?page=` and the invoice
  grid `?invoicePage=`.
- `Customers_id.razor`: new invoice grid (decision 7).
- `Capabilities/SellerClientExtensions.cs`, `CustomerClientExtensions.cs`, `ItemClientExtensions.cs`
  and the `Invoices_add` / `Invoices_update` call sites from decision 5.
- `Sellers.razor`, `Customers.razor`, `Items.razor` move to the `ItemsProvider` grid; delete
  `Sellers_userId.razor`, `Customers_sellerId.razor`, `Items_customerId.razor` (decision 9).
- Check in the browser with more rows than one page: moving between pages, changing the page size,
  the totals, and that the invoice add selectors list every seller, customer and item.

## Out of scope

- Paging `GET /users` and the users list (`/users`). It still returns every user (BIL-101's
  commit expected BIL-102 to page it, but the issue leaves the users list out).
- Server-side sorting or filtering by name in the grids.

## Order of work

1. Biller steps 3 to 7 (one PR).
2. BillioIntegrationTest step 8 on the same branch name, run with `_test-stack`.
3. BillerFrontEnd step 9, then the browser check against the test stack.
4. Merge and deploy Biller and the front end together.

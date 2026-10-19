# Plan: invoice summary by year and month on the seller page (BIL-90)

Jira: [BIL-90](https://aldisadom.atlassian.net/browse/BIL-90). Epic: BIL-89 (Billeris).

## Goal

- The seller page (`/sellers/{id}`) shows a summary of that seller's invoices.
- **By year:** one row per year that has invoices: number of invoices, total, paid, unpaid.
- **By month:** the user picks a year and sees all 12 months with the same columns, including months with no invoices.
- Both tables have a totals row.

## Current state (master @ 125ae5e)

| Where | What |
|---|---|
| `invoices` | `seller_id`, `created_date DATE`, `due_date`, `total_price decimal` (sum of the lines, set on insert and update), `status INT` |
| `InvoiceRepository.Get(userId, sellerId, customerId, page, pageSize)` | Paged list only. No aggregation anywhere |
| `InvoiceController` | `invoices`: `GET {id}`, `GET` (filters + paging), `POST`, `POST generate`, `PUT`, `PUT update_status`, `DELETE {id}` |
| Indexes | None on `invoices(seller_id, created_date)` |
| Front end `Sellers_id.razor` | Seller details, then the customer list. Injects `ISellerClient` and `ICustomerClient`. Uses QuickGrid |
| Status | New invoices are saved as `Payed` by mistake (BIL-73), so paid/unpaid is wrong until that is fixed |

## Design decisions

1. **Two endpoints on `InvoiceController`**, because the data comes from `invoices`:
   - `GET /invoices/summary/years?sellerId=` returns every year with invoices.
   - `GET /invoices/summary/months?sellerId=&year=` returns 12 months.
   The page loads both at the start and reloads only the months when the year changes.
2. **Grouped by `created_date`** (the invoice date printed on the document), not by due date or payment date.
3. **Totals come from the stored `total_price`**, so no line data is read. This works the same before and after BIL-86.
4. **Paid = `status = Payed`. Unpaid = everything else.** The columns are `Count`, `Total`, `Paid`, `Unpaid`. Overdue is not a column, because BIL-73 calculates it from the due date.
5. **One SQL query each, with `GROUP BY`.** The database adds up the numbers. Empty months are filled in C# (Application layer), so the repository stays a plain query and the filling can be unit tested.
6. **Years with no invoices are not listed.** The year selector offers the years from the yearly summary plus the current year.
7. **One currency (EUR).** Invoices have no currency column. If one is added later, the summary groups by currency too.
8. **Unknown seller → 404**, the same as other endpoints. **Year outside 2000 to current year + 1 → 400.**

## Dependencies

- **BIL-73 blocks the paid/unpaid columns.** Counts and totals are right now. Ship after BIL-73, or ship the API now and show the paid/unpaid columns only once BIL-73 is deployed.
- No schema changes besides an index, so no conflict with the BIL-65/BIL-86 migrations other than the number.

## Step 1: BillerContracts

- `Requests/Invoice/InvoiceYearSummaryRequest`: `Guid SellerId`.
- `Requests/Invoice/InvoiceMonthSummaryRequest`: `Guid SellerId`, `int Year`.
- `Responses/Invoice/InvoiceSummaryRowResponse`: `int Period` (year or month 1–12), `int Count`, `decimal Total`, `decimal Paid`, `decimal Unpaid`.
- `Responses/Invoice/InvoiceSummaryResponse`: `List<InvoiceSummaryRowResponse> Rows`, `InvoiceSummaryRowResponse Totals` (`Period = 0`).

## Step 2: Migration (next free number)

```sql
--liquibase formatted sql
-- changeset Aldis:<n>
-- comment: Index for invoice summaries per seller and date
CREATE INDEX ix_invoices_seller_id_created_date ON invoices (seller_id, created_date);
-- rollback DROP INDEX ix_invoices_seller_id_created_date;
```

## Step 3: Domain and Infrastructure

- `InvoiceSummaryEntity`: `int Period`, `int Count`, `decimal Total`, `decimal Paid`.
- `IInvoiceRepository`: `GetYearSummary(Guid sellerId)` and `GetMonthSummary(Guid sellerId, int year)`.

```sql
-- years
SELECT EXTRACT(YEAR FROM created_date)::int AS period,
       COUNT(*)::int AS count,
       COALESCE(SUM(total_price), 0) AS total,
       COALESCE(SUM(total_price) FILTER (WHERE status = 0), 0) AS paid
FROM invoices
WHERE seller_id = @SellerId
GROUP BY 1 ORDER BY 1;

-- months of one year (range on created_date so the index is used)
SELECT EXTRACT(MONTH FROM created_date)::int AS period, ... same columns ...
FROM invoices
WHERE seller_id = @SellerId
  AND created_date >= make_date(@Year, 1, 1) AND created_date < make_date(@Year + 1, 1, 1)
GROUP BY 1 ORDER BY 1;
```

`status = 0` is `InvoiceStatus.Payed`. Pass it as a parameter (`@Paid`) rather than a literal, so a later enum renumbering (BIL-73 "Later") changes only C#.

## Step 4: Application

- `InvoiceSummaryModel` (`Period`, `Count`, `Total`, `Paid`, `Unpaid => Total - Paid`) and `InvoiceSummaryListModel` (`Rows`, `Totals`).
- `IInvoiceService.GetYearSummary(Guid sellerId)` / `GetMonthSummary(Guid sellerId, int year)`:
  1. `ISellerService.Get(sellerId)` to get the 404 (already throws `NotFoundException`).
  2. Repository call.
  3. Months: fill 1–12, using zeros for missing months.
  4. Totals row = sum of the rows.
- Mapping extensions `ToModel` / `ToResponse`.

## Step 5: Validators

- `InvoiceYearSummaryValidator`: `SellerId` not empty.
- `InvoiceMonthSummaryValidator`: `SellerId` not empty, `Year` between 2000 and `DateTime.Today.Year + 1`.

## Step 6: WebAPI

```csharp
[HttpGet("summary/years")]
[ProducesResponseType(typeof(InvoiceSummaryResponse), StatusCodes.Status200OK)]
[SwaggerResponseExample(StatusCodes.Status200OK, typeof(InvoiceYearSummaryResponseExample))]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetYearSummary([FromQuery] InvoiceYearSummaryRequest query)

[HttpGet("summary/months")]  // same shape
```

- These routes are literal segments, so they don't clash with `GET {id}` (a Guid). Add a route test or check it in Swagger anyway.
- Add Swagger response examples for both, and register the validators in DI.

## Step 7: Tests (`write-tests` skill)

- `InvoiceServiceTest`:
  - Months: repository returns March and November only. The result has 12 rows, zeros elsewhere, and correct totals. Theory with labels: no invoices at all, every month, one month.
  - Years: rows pass through in order, totals add up, `Unpaid = Total - Paid`.
  - Unknown seller throws `NotFoundException`, and the repository is not called (strict mock).
- Validator tests: year 1999, 2000, current + 1, current + 2, empty seller id.
- Controller tests for both actions. Swagger example tests. DI registration test for the new validators.
- Integration tests (BillioIntegrationTest): create a seller, customer and items. Create invoices in two months of one year and one in another year, set one to Payed. Check both summaries, then delete an invoice and check the numbers drop.

## Step 8: Front end (BillerFrontEnd, separate PR)

- `IInvoiceClient.GetYearSummary(sellerId)`, `GetMonthSummary(sellerId, year)`.
- `Sellers_id.razor`: new "Invoices summary" section between the seller details and the clients list:
  - Year table (Year, Invoices, Total, Paid, Unpaid) with a totals row. Clicking a year selects it.
  - A year `<select>` (years from the yearly summary plus the current year, default current year).
  - Month table for the selected year with month names (LT), all 12 rows and a totals row.
  - Amounts formatted as `0.00 €`.
- Inject `IInvoiceClient`.
- bUnit tests once BIL-87 is in place: changing the year calls `GetMonthSummary` with that year.

## Out of scope

- Charts. Easy to add later on top of the same endpoints.
- Summary per customer or across all sellers of a user. The same queries work with a different `WHERE`.
- Grouping by payment date (there is no payment date column).
- Export to Excel/PDF.

## Order of work

1. BIL-73 (so paid/unpaid is right).
2. Contracts release.
3. Biller: migration, repository, service, validators, controller, tests.
4. Integration tests.
5. Front end section.

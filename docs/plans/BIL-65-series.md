# Plan: invoice series table and invoice numbering (BIL-65)

Jira: [BIL-65](https://aldisadom.atlassian.net/browse/BIL-65) (series table and invoice numbering; BIL-76 was merged into it). Epic: BIL-89 (Billeris).

## Goal

- Move the invoice series name and the running number out of `customers` into a new `series` table.
- Every series belongs to a seller. Every customer points to one series. Several customers of the same seller can share a series.
- Invoices store their own `series_id` and number. The series name comes from `series`; a series with invoices can't be renamed.
- Any invoice can be deleted. Only deleting the **latest** invoice of a series lowers the series' next number.
- Reserving a number on create and releasing it on delete must be atomic, so two requests at the same time can't get the same number.

## Progress

- [x] Step 1: BillerContracts on `BIL-65-series` (82f867a). Not published yet.
- [x] Step 2: migrations 033, 034 (cc845e2). Tested on a throwaway Postgres 16 with old-shape data: update, `rollback-count 2`, update again.
- [x] Steps 3–8: Biller code and unit tests (e5ff9b8). 729 tests pass when built against the local contracts.
- [x] Step 9: BillioIntegrationTest (c21be3f). 190 passed, 0 failed, twice in a row, against this branch's API on a throwaway DB, built against the local contracts.
- [x] Step 10.3: BillerFrontEnd pages (0763dc0). Release build succeeds against the local clients. Not checked in a browser yet.
- [x] Step 1.4: BillerContracts PR #10 merged, published as **3.0.0** (major, because properties were removed). The plan first said 2.1.0.
- [x] Step 10.1–10.2: `BillerContracts` / `BillerContracts.Clients` bumped to 3.0.0 in Biller, BillioIntegrationTest and BillerFrontEnd.
- [x] 2026-10-26: `invoices.series_name` dropped; a series with invoices can't be renamed. Invoice writes and series updates lock the series row in a transaction (design decisions 4, 5). Migration 034 links old invoices to the series named in their customer copy. Tested on a throwaway Postgres 16 (update, rollback, update; lock waits in both directions) and with BillioIntegrationTest: 190 passed.
- [x] 2026-10-26, after a first look at the front end: a customer with invoices can't change its series, and an invoice can't be moved to another customer (design decision 7). The series list page is gone; the seller page shows the seller's series. A new customer gets the seller's first series. BillioIntegrationTest: 192 passed, twice, on a throwaway Postgres 16.
- [x] 2026-10-27, review round 4: customer update and invoice create no longer race (tested by holding the series lock in psql: before the fix the customer moved series and the invoice landed in the old one; after it, the API request in each scenario returns 400). Reserving stops at `next_number` 2147483647 instead of overflowing (500 before, 400 after). `PUT /series` rejects `NextNumber` = `int.MaxValue`, which can't be reserved. New pre-check for series names longer than 10 characters in old invoice copies. BillioIntegrationTest: 193 passed, twice.
- [x] 2026-10-28: merged master with BIL-86 (invoices store user, seller and customer ids; master's changeset 33 drops `user_data`, `seller_data` and `customer_data`). Done as BIL-86's plan, step 9:
  - The series changesets are now 034 and 035. 035 can't read `customer_data`, so every old invoice goes into its customer's current series, and its rollback no longer writes `customer_data` (033's rollback rebuilds it from the customer's restored `invoice_name`). Tested on a throwaway Postgres 16 with 032-shape data: update, `rollback-count 3`, update. An invoice issued under an old name lands in its customer's series, and two same-name customers with the same invoice number make 035 fail on its unique index, as the pre-check says.
  - `InvoiceModel` holds ids plus `SeriesId`/`SeriesName`; `InvoiceModelFull` adds the current user, seller and customer for responses and PDFs. `GenerateFileLocation()` stays on `InvoiceModel` and uses `SeriesName`. `Update` rejects another seller or customer before validating, keeps the stored series and number, and copies line names and prices from the current items.
  - `CustomerUpdateValidator.ForInvoiceUpdate()` is gone: `PUT /invoices` sends no customer copy any more.
  - BillerContracts 4.0.0 (PR #12). Its branch had lost the series contracts in an earlier master merge (d2a3fc6); they are back.
  - BillioIntegrationTest: 182 passed, three runs, against this branch's API on a throwaway Postgres 16, all built against BillerContracts packed locally from #12. `run-integration.sh` now also sets the series client's URL, which otherwise fell back to the user's own stack on port 8090.
- [ ] Step 10: check the front end in a browser, merge the three PRs, deploy.

Done differently from the plan:
- `ISeriesRepository` also has `GetByName(sellerId, name)` for the duplicate-name check.
- `InvoiceService.Update` keeps the stored invoice's `SeriesId`, `SeriesName` and number. Before the BIL-86 merge it also copied them into the customer copy it saved, and `InvoiceUpdateValidator` used `CustomerUpdateValidator.ForInvoiceUpdate()`; both went with the copies.
- `PUT /invoices` can't move an invoice to another seller: the invoice keeps its series, and a series belongs to one seller.
- Adding a series for a missing seller returns 404 (via `ISellerService.Get`).
- `SeriesAddRequest` has no `NextNumber`: a new series always starts at 1 (decided 2026-10-25, after PR BillerContracts#10 was opened). The number can be changed afterwards with `PUT /series`.
- 20 parallel inserts into one series on the test DB got 20 unique, consecutive numbers.

## Current state (master @ 6bb2079)

| Where | What |
|---|---|
| `customers.invoice_name`, `customers.invoice_number` | Series name and the **next** number to use (migration 020/021, `CustomerService.Add` sets it to 1) |
| `InvoiceService.Add` | Uses `Customer.InvoiceNumber` as the invoice number, saves the invoice, then calls `CustomerService.IncreaseInvoiceNumber`. These are two separate statements, so a crash or a concurrent request can produce a wrong or duplicate number |
| `InvoiceService.Delete` | Deletes the row. The number is never released |
| `InvoiceService.Update` / `InvoiceUpdateRequest.InvoiceNumber` | The number can be changed by hand to any value |
| `InvoiceModel.GenerateInvoiceName()` / `GenerateFileLocation()` | **Bug:** they use `Customer.InvoiceNumber` / `Customer.InvoiceName` from the customer snapshot instead of the invoice's own number |
| `invoices.file_path` | Written on insert/update but never read. PDFs are streamed since #37 |
| DB constraints | Nothing prevents duplicate `(customer, invoice_number)` |
| `InvoiceDocumentSF.cs`, `InvoiceDocumentADA.cs` | The PDF header prints `Model.Customer!.InvoiceName` as the series |
| Foreign keys | None has `ON DELETE`. Deleting a seller that still has customers fails; the same will hold for series |
| `migrations/sql/` | Last changeset is `032-Alter-table-invoices_status_default.sql` (BIL-73) |
| BillerContracts | Biller pins 2.0.0. BillioIntegrationTest uses `BillerContracts.Clients` 2.0.0 (BIL-96) |

## Design decisions

1. **`series` has `seller_id`; `customers` has `series_id`.** A customer's series must belong to the customer's seller.
2. **Series names are unique per seller** (`UNIQUE (seller_id, name)`), not globally.
3. **`next_number`** keeps today's meaning: the number the next invoice will get. The latest existing invoice has `next_number - 1`.
4. **A series update and a write to one of its invoices never run at the same time.** Invoice create, update, status change and delete, and series update each run in a transaction that first locks the series row (`LockSeries`, `SELECT … FOR NO KEY UPDATE`). A customer update that changes the series locks the customer row, then the customer's current series. Invoice create reserves a number only while the customer is still in the series it was read with. The transaction starts and ends inside one repository method (`InTransaction`), because all repositories share one scoped `IDbConnection`. Statements after the lock see everything committed before it, so a series update checks the real highest invoice number, and a delete sees the current next number. Numbers are still reserved and released in one statement (data-modifying CTE). Until 2026-10-26 everything was single statements, and a series update was rejected when the series changed meanwhile.
5. **Invoices don't copy the series name.** It's read from `series` with a join. A series with invoices can't be renamed (`PUT /series` returns 400), so an invoice and its PDF never change. Until 2026-10-26 invoices had their own `series_name` column.
6. **The invoice number can't be edited.** `InvoiceNumber` is removed from `InvoiceUpdateRequest`, and `Update` no longer writes `invoice_number` or `series_id`.
7. **A customer with invoices keeps its series, and an invoice keeps its customer.** `PUT /customers` returns 400 when it changes the series of a customer with invoices; the check runs after locking the customer's series, so an invoice being created at the same time is seen. `PUT /invoices` returns 400 for another customer, as for another seller. Until 2026-10-26 both were allowed, and a new series affected only new invoices.
8. **`invoices.file_path` is dropped.** It's unused, and keeping it would mean computing the file name inside SQL. File names come from `InvoiceModel.GenerateFileLocation()`.

### Numbering rules (examples)

Series `ABC`, invoices 1–5 exist, `next_number = 6`.

| Action | Result |
|---|---|
| Delete #5 | `next_number = 5`. The next invoice gets #5 again |
| Then delete #4 | #4 is now the latest, so `next_number = 4` |
| Delete #3 while #4 and #5 exist | Allowed. `next_number` stays at 6. #3 stays missing (gap) |
| Delete #3, then later delete #5 | `next_number = 5`. The gap at #3 remains |

## Prerequisites

- [x] **Sync the sibling repos.** All four repos are on `BIL-65-series`, with master merged in (2026-10-25).
- [ ] **Back up the production database** (the small PC, BIL-82) before deploying migration 035.
- [ ] **Run the pre-check queries** below against a copy of the real data.

### Pre-check queries (run before migration 035)

Changeset 33 (BIL-86) drops the invoices' customer copies first, so these use the customers' current names only.

```sql
-- Empty series names. The migration would create a series named ''; fix these first.
SELECT id, company_name FROM customers WHERE COALESCE(TRIM(invoice_name), '') = '';

-- Customers of the same seller that share a series name. They will share one series.
SELECT seller_id, TRIM(invoice_name), COUNT(*), ARRAY_AGG(invoice_number)
FROM customers GROUP BY seller_id, TRIM(invoice_name) HAVING COUNT(*) > 1;

-- Invoice numbers that will collide in one series. Each invoice goes into its customer's current series,
-- and customers of one seller with the same name each had their own counter. A row here is two invoices
-- of one seller that will have the same series and number. It makes migration 035 fail on its unique
-- index (it runs in one transaction, so the database stays as it was). Fix these by hand first.
SELECT c.seller_id, TRIM(c.invoice_name) AS series, i.invoice_number, COUNT(*), ARRAY_AGG(c.company_name)
FROM invoices i JOIN customers c ON c.id = i.customer_id
GROUP BY 1, 2, 3 HAVING COUNT(*) > 1;
```

BIL-86's pre-check 1 (`prefix_changed`) lists the old invoices whose customer was renamed since. They
print the customer's current name from BIL-86 on, and go into that series here.

## Step 1: BillerContracts (new version 3.0.0)

Repo: `BillerContracts`. Namespaces below are `BillerContracts.*` as on `origin/master`.

### 1.1 New files

`Contracts/Requests/Series/SeriesAddRequest.cs`
```csharp
public record SeriesAddRequest
{
    public Guid SellerId { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

`Contracts/Requests/Series/SeriesUpdateRequest.cs`
```csharp
public record SeriesUpdateRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NextNumber { get; set; }
}
```
`SellerId` can't be changed on update.

`Contracts/Requests/Series/SeriesGetRequest.cs`: `Guid? SellerId`, `int Page = 1`, `PageSize PageSize = PageSize.p25` (same shape as `CustomerGetRequest`).

`Contracts/Responses/Series/SeriesResponse.cs`: `Id`, `SellerId`, `Name`, `NextNumber`.

`Contracts/Responses/Series/SeriesListResponse.cs`: `List<SeriesResponse> Series`, `TotalCount`, `Page`, `PageSize` (same shape as `CustomerListResponse`).

### 1.2 Changed files

| File | Change |
|---|---|
| `Requests/Customer/CustomerAddRequest.cs` | Remove `InvoiceName`. Add `Guid SeriesId` |
| `Requests/Customer/CustomerUpdateRequest.cs` | Remove `InvoiceName`, `InvoiceNumber`. Add `Guid SeriesId` |
| `Responses/Customer/CustomerResponse.cs` | Remove `InvoiceName`, `InvoiceNumber`. Add `Guid SeriesId`, `string SeriesName` |
| `Requests/Invoice/InvoiceUpdateRequest.cs` | Remove `InvoiceNumber` |
| `Responses/Invoice/InvoiceResponse.cs` | Add `Guid SeriesId`, `string SeriesName`. Keep `InvoiceNumber` |

`InvoiceUpdateRequest.Customer` is a `CustomerUpdateRequest`, so it also loses `InvoiceName`/`InvoiceNumber` and gains `SeriesId`. On invoice update, the customer's `SeriesId` is ignored; the invoice keeps its own series.

### 1.3 Clients project

- `Clients/Interfaces/ISeriesClient.cs` and `Clients/Clients/SeriesClient.cs`: `Get(id)`, `Get(SeriesGetRequest)`, `Add`, `Update`, `Delete`, following `CustomerClient` (route `series`).
- Register it in `Clients/DependencyInjection.cs`.
- `Clients.Tests/SeriesClientTest.cs` following `CustomerClientTest.cs`. Update `Clients.Tests/DependencyInjectionTests.cs`.
- Update `CustomerClientTest` / `InvoiceClientTest` test data for the changed contracts.

### 1.4 Release

- PR in BillerContracts, merge, run the `Publish` workflow with version `3.0.0`.
- If Plan B (units, BIL-75) is done at the same time, put both contract changes into 3.0.0.

## Step 2: Database migrations (Biller)

Use the `add-db-migration` skill. Files go in `migrations/sql/` (picked up by `includeAll` in `migrations/dbChangeLog.yaml`).

### 2.1 `034-Create-table-series.sql`

```sql
--liquibase formatted sql

-- changeset Aldis:34
-- comment: Create series table (invoice series per seller)
CREATE TABLE series (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    seller_id uuid NOT NULL,
    name character varying(10) NOT NULL,
    next_number integer NOT NULL DEFAULT 1,

    PRIMARY KEY(id),
    CONSTRAINT fk_series_seller_id
      FOREIGN KEY(seller_id)
        REFERENCES sellers(id),
    CONSTRAINT uq_series_seller_id_name UNIQUE (seller_id, name),
    CONSTRAINT ck_series_next_number CHECK (next_number >= 1)
);

CREATE INDEX idx_series_seller_id ON series (seller_id);

-- rollback DROP TABLE series;
```

### 2.2 `035-Migrate-customers_invoices_to_series.sql`

One changeset. PostgreSQL DDL is transactional, so it either runs completely or not at all. The SQL is in the file; in short:

1. One series per seller and trimmed customer name. `next_number` = the highest "next number" of the customers with that name.
2. Each customer points to the series with its current name.
3. Each invoice points to its customer's series. Unique index on `(series_id, invoice_number)`.
4. `next_number` is raised above every invoice number in its series.
5. `customers.invoice_name`, `customers.invoice_number` and `invoices.file_path` are dropped.

Notes:
- Changeset 33 (BIL-86) has dropped `customer_data`, so the name an old invoice was issued with is gone. An invoice made while its customer had a different `invoice_name` goes into the customer's current series. The collision pre-check groups invoices the same way.
- `customers.invoice_name` is `varchar(10)` (migration 003), so series names stay at max 10 characters and the rollback can restore them.
- The rollback gives each customer its series' name and next number. Customers that shared a series all get that series' next number. Rolling back 33 as well rebuilds every invoice's customer copy from that name and the invoice's own number, so the old code prints the right series and number.

### 2.3 Verify locally

```bash
docker compose up -d --build db liquibase
docker logs billio-liquibase      # changesets 34 and 35 ran
```
Then compare row counts and spot-check: every customer has a series, every invoice has `series_id`, old invoices are in their customer's series, and `next_number` = max invoice number + 1 per series. Test the rollback once on a copy (`liquibase rollback-count 2`).

## Step 3: Domain (`src/Domain`)

| File | Change |
|---|---|
| `Entities/SeriesEntity.cs` (new) | `Id`, `SellerId`, `Name`, `NextNumber` |
| `Entities/CustomerEntity.cs` | Remove `InvoiceName`, `InvoiceNumber`. Add `Guid SeriesId` |
| `Entities/InvoiceEntity.cs` | Remove `FilePath`. Add `Guid SeriesId`, `string SeriesName` |
| `Repositories/ISeriesRepository.cs` (new) | See below |
| `Repositories/ICustomerRepository.cs` | Remove `IncreaseInvoiceNumber` |
| `Repositories/IInvoiceRepository.cs` | `Add` reserves the number (see 4.3). `Delete` releases it |

```csharp
public interface ISeriesRepository
{
    Task<SeriesEntity?> Get(Guid id);
    Task<(IEnumerable<SeriesEntity>, int)> Get(Guid? sellerId, int page, int pageSize);
    Task<SeriesEntity?> GetByName(Guid sellerId, string name);
    Task<bool> IsInUse(Guid id);   // referenced by any customer or invoice
    Task<Guid> Add(SeriesEntity series);
    // Locks the series row, then updates only when NextNumber > the highest invoice number and the name
    // is unchanged or the series has no invoices. Null when the series doesn't exist.
    Task<(bool Updated, int MaxInvoiceNumber)?> Update(SeriesEntity series);
    Task Delete(Guid id);
}
```

## Step 4: Infrastructure (`src/Infrastructure`)

### 4.1 `Repository/SeriesRepository.cs` (new)

Dapper, same style as `CustomerRepository`. Paging like `InvoiceRepository.Get(...)` (count query + `LIMIT/OFFSET`, `ORDER BY name`).

```sql
-- IsInUse
SELECT EXISTS (SELECT 1 FROM customers WHERE series_id = @Id)
    OR EXISTS (SELECT 1 FROM invoices WHERE series_id = @Id);
```

### 4.2 `Repository/CustomerRepository.cs`

- `Add`: insert `series_id` instead of `invoice_name`, `invoice_number`.
- `Update`: set `series_id` instead of `invoice_name`, `invoice_number`. It locks the customer row; when the series changes it also locks the current series, and the `UPDATE` changes the series only while the customer has no invoices. Returns null when the customer doesn't exist, false when the series change was refused.
- Remove `IncreaseInvoiceNumber`.
- `Get` queries join the series name for the response:
  ```sql
  SELECT c.*, s.name AS series_name FROM customers c JOIN series s ON s.id = c.series_id WHERE c.id = @Id
  ```
  This needs `SeriesName` on `CustomerEntity`. Alternatively `CustomerService` loads the series. **Recommendation:** use the join; it's one query and keeps the service simple.

### 4.3 `Repository/InvoiceRepository.cs`

`Add`, `Update`, `UpdateStatus` and `Delete` each run in a transaction (`InTransaction`) that first locks the invoice's series row (`LockSeries`):
```sql
SELECT id FROM series WHERE id = @SeriesId FOR NO KEY UPDATE;
```
The others read the invoice's `series_id` first; it never changes. `NO KEY UPDATE` is the lock an `UPDATE` of the series takes, so foreign-key checks from customers and invoices don't wait for it. A rename takes `FOR UPDATE` (the name is in a unique key), so those checks wait for it. `SeriesRepository.Update` locks the same row, so a series update and a write to one of its invoices wait for each other. Statements after the lock see everything committed before it.

`Add`: then reserve the number and insert in one statement.
```sql
WITH reserved AS (
    UPDATE series
    SET next_number = next_number + 1
    WHERE id = @SeriesId
    RETURNING next_number - 1 AS invoice_number
)
INSERT INTO invoices
    (customer_id, seller_id, user_id, series_id, invoice_number,
     created_date, due_date, items_data, comments, total_price, status)
SELECT @CustomerId, @SellerId, @UserId, @SeriesId, reserved.invoice_number,
       @CreatedDate, @DueDate, @ItemsData, @Comments, @TotalPrice, @Status
FROM reserved
RETURNING id;
```
`reserved` also requires `EXISTS (SELECT 1 FROM customers WHERE id = @CustomerId AND series_id = @SeriesId)`, so an invoice is created only while the customer is still in the series the service read. Otherwise nothing is inserted and the result is `Guid.Empty`, which the service turns into a 400.

`Delete`: then delete and release the number in one statement.
```sql
WITH deleted AS (
    DELETE FROM invoices
    WHERE id = @Id
    RETURNING series_id, invoice_number
)
UPDATE series s
SET next_number = s.next_number - 1
FROM deleted d
WHERE s.id = d.series_id
  AND s.next_number = d.invoice_number + 1
  AND d.invoice_number >= 1;
```
Invoices from before migration 020 can have number 0 (customers started at 0 until 2024-08). Number 0 is never given back, so `next_number` stays >= 1.

`Update`: remove `file_path` and `invoice_number` from the `SET` list. `series_id` isn't updated either.

`Get(...)`: join `series` for the name: `SELECT i.*, s.name AS series_name FROM invoices i JOIN series s ON s.id = i.series_id`.

### 4.4 `DependencyInjection.cs`

Add `services.AddScoped<ISeriesRepository, SeriesRepository>();`

## Step 5: Application (`src/Application`)

### 5.1 Models and mapping

| File | Change |
|---|---|
| `Models/SeriesModel.cs` (new) | `Id`, `SellerId`, `Name`, `NextNumber` |
| `Models/CustomerModel.cs` | Remove `InvoiceName`, `InvoiceNumber`. Add `SeriesId`, `SeriesName` |
| `Models/InvoiceModel.cs` | Add `SeriesId`, `SeriesName`. Fix `GenerateInvoiceName()` to use `InvoiceNumber` (the invoice's own number) and `GenerateFileLocation()` to use `SeriesName`. Simplify the padding to `InvoiceNumber.ToString("D6")` (same output as today) |
| `MappingProfiles/SeriesMappingProfile.cs` (new) | `SeriesEntity` ⇄ `SeriesModel`, `SeriesAddRequest`/`SeriesUpdateRequest` → `SeriesModel`, `SeriesModel` → `SeriesResponse` |
| `MappingProfiles/CustomerMappingProfile.cs` | Map `SeriesId`/`SeriesName` instead of the old fields |
| `MappingProfiles/InvoiceMappingProfile.cs` | Map `SeriesId`/`SeriesName` in `ToModel`/`ToEntity`/`ToResponse`. Remove `FilePath`. `InvoiceUpdateRequest.ToModel` no longer sets `InvoiceNumber` |

Old invoices: since BIL-86 they have no customer copy. The PDF reads the series name from `InvoiceModel.SeriesName` (joined from `series`), not from the customer snapshot: change the header line in `Models/Invoice/Documents/InvoiceDocumentSF.cs` and `InvoiceDocumentADA.cs` to `Model.SeriesName`.

### 5.2 `Interfaces/ISeriesService.cs` + `Services/SeriesService.cs` (new)

```csharp
Task<SeriesModel> Get(Guid id);                                         // NotFoundException if missing
Task<(IEnumerable<SeriesModel>, int)> Get(SeriesGetRequest? query);
Task<Result<SeriesModel>> GetWithValidation(Guid id, Guid sellerId);    // error if missing or other seller
Task<Guid> Add(SeriesModel series);                                     // seller must exist; NextNumber is always 1
Task Update(SeriesModel series);                                        // keeps SellerId from DB
Task Delete(Guid id);                                                   // ValidationException if IsInUse
```

- Duplicate name for the same seller: the unique constraint throws `PostgresException` 23505. Check first in `Add`/`Update` and return a clear validation error; keep the constraint as a safety net.
- `Update` lowering `NextNumber` below an existing invoice number would cause a unique-index error on the next invoice. Reject `NextNumber <= MAX(invoice_number)` for that series. Reject a new name when the series has any invoice (checked with `EXISTS`, since old invoices can be number 0). `ISeriesRepository.Update` checks both after locking the series row and returns the highest invoice number it read, for the error message.

### 5.3 `Services/CustomerService.cs`

- `Add`: remove `customer.InvoiceNumber = 1`. Validate the series with `_seriesService.GetWithValidation(customer.SeriesId, customer.SellerId)` and throw `ValidationException` on error (same pattern as `InvoiceService.Validate`).
- `Update`: load the existing customer, validate the new `SeriesId` against the existing customer's `SellerId`. 404 when the repository returns null, 400 when it refuses the series change (the customer has invoices).
- Remove `IncreaseInvoiceNumber` (also from `ICustomerService`).
- Constructor gets `ISeriesService`.

### 5.4 `Services/InvoiceService.cs`

- `GetInvoiceData`: remove `invoiceData.InvoiceNumber = invoiceData.Customer.InvoiceNumber`. Set `invoiceData.SeriesId = customer.SeriesId`.
- `Add`: remove the `IncreaseInvoiceNumber` call. `_invoiceRepository.Add` now reserves the number. Throw if it returns `Guid.Empty`.
- `Update`: copy `SeriesId`, `SeriesName` and `InvoiceNumber` from the stored invoice (like `User` today) so they can't be changed. Another seller or another customer → 400.
- `Delete`: unchanged in the service (`Get` then `Delete`). The repository releases the number.

### 5.5 `DependencyInjection.cs`

Add `services.AddScoped<ISeriesService, SeriesService>();`

## Step 6: Validators (`src/Validators`)

| File | Rules |
|---|---|
| `Series/SeriesAddValidator.cs` (new) | `SellerId` not empty. `Name` not empty, max 10 |
| `Series/SeriesUpdateValidator.cs` (new) | `Id` not empty. `Name` not empty, max 10. `NextNumber` ≥ 1 and < `int.MaxValue` |
| `Customer/CustomerAddValidator.cs` | Replace the `InvoiceName` rule with `SeriesId` not empty |
| `Customer/CustomerUpdateValidator.cs` | Same; remove any `InvoiceNumber` rule |
| `Invoice/InvoiceUpdateValidator.cs` | Remove the `InvoiceNumber` rule |
| `DependencyInjection.cs` | Register the two new validators |

## Step 7: WebAPI (`src/WebAPI`)

- `Controllers/SeriesController.cs` (new), route `series`, following `ItemController`:
  - `GET series/{id}` → `SeriesResponse`
  - `GET series?SellerId=&Page=&PageSize=` → `SeriesListResponse`
  - `POST series` → `AddResponse`
  - `PUT series`
  - `DELETE series/{id}` (400 if in use)
- `SwaggerExamples/Series/`: `SeriesAddRequestExample`, `SeriesUpdateRequestExample`, `SeriesResponseExample`, `SeriesListResponseExample`. They're picked up by `AddSwaggerExamplesFromAssemblies`.
- Update `SwaggerExamples/Customer/*` and `SwaggerExamples/Invoice/*` for the changed contracts.

## Step 8: Tests (`tests/xUnitTests`)

Use the `write-tests` skill: hand-built data, strict Moq with `Verify`, a theory `Label` per case.

| Test file | Cases |
|---|---|
| `Application/Services/SeriesServiceTest.cs` (new) | Get found / not found. List with and without `SellerId`. `GetWithValidation`: OK / missing / other seller. Add: `NextNumber` 1 / duplicate name. Update: OK / not found / duplicate name / `NextNumber` ≤ max invoice number / new name with invoices / deleted meanwhile. Delete: OK / not found / in use |
| `Application/Services/CustomerServiceTest.cs` | Add/Update with valid series / missing series / series of another seller. Remove the `IncreaseInvoiceNumber` tests |
| `Application/Services/InvoiceServiceTest.cs` | Add sets `SeriesId` from customer and doesn't call `IncreaseInvoiceNumber`. Add throws when the repository returns `Guid.Empty`. Update keeps the stored number, `SeriesId` and `SeriesName` even if the request differs. Delete calls the repository once |
| `Application/MappingProfiles/SeriesMappingProfileTest.cs` (new) | All mappings, including `NullChecker` for every property |
| `Application/MappingProfiles/CustomerMappingProfileTest.cs`, `InvoiceMappingProfileTest.cs` | New and removed fields. `InvoiceModel.GenerateInvoiceName()` uses the invoice number (1 → `000001`, 123456 → `123456`). `GenerateFileLocation()` uses `SeriesName`. Old JSON with `InvoiceName` in customer data still deserializes |
| `Validators/Series/SeriesAddValidatorTest.cs`, `SeriesUpdateValidatorTest.cs` (new) | Each rule valid / invalid, name length 10 vs 11 |
| `Validators/Customer/*`, `Validators/Invoice/InvoiceUpdateValidatorTest.cs` | Updated rules |
| `Validators/DependencyInjectionTests.cs`, `Application/DependencyInjectionTests.cs` | New registrations |
| `WebAPI/Controllers/SeriesControllerTest.cs` (new) | Every action: OK, validation failure, service exception passes through |
| `WebAPI/Swagger/SeriesSwaggerExampleTest.cs` (new), `CustomerSwaggerExampleTest.cs`, `InvoiceSwaggerExampleTest.cs` | Examples return populated objects |

There are no repository tests in the repo today, so the SQL in 4.3 is checked by hand (Step 9) and in the BillioIntegrationTest repo.

Run the `test-coverage` skill when done.

## Step 9: Manual and integration checks

On the local Docker stack with a copy of the real data:

1. Create invoices A1, A2, A3 for one customer → numbers 1, 2, 3, and `next_number = 4`.
2. Delete A3 → `next_number = 3`. Create another → it gets #3.
3. Delete A2 (not the latest) → `next_number` unchanged, the gap at #2 stays.
4. Two customers sharing one series → their invoices share one sequence.
5. Change the series of a customer with invoices → 400. Move an invoice to another customer → 400. Renaming a series with invoices → 400.
6. Fire ~20 parallel `POST /invoices` for the same series → all numbers unique and consecutive.
7. `DELETE /series/{id}` for a series in use → 400.
8. Generate the PDF for an old invoice (from before the migration) → same number as before, and its customer's current series name.

BillioIntegrationTest (branch `BIL-65-series`) covers customers and invoices, so it must change in the same PR round, or CI fails:
- Bump `BillerContracts.Clients` to 3.0.0.
- Customer tests create a series first and send `SeriesId`. Invoice tests check `SeriesId`, `SeriesName` and the numbering rules above.
- Add `SeriesTests` (CRUD, duplicate name, delete in use → 400).
- The clean-up that deletes all data first must delete series after customers and before sellers, since there's no `ON DELETE CASCADE`.

## Step 10: Rollout

1. Merge and publish BillerContracts 4.0.0 (PR #12). 3.0.0 is deprecated; 3.0.1 is BIL-86 without series.
2. Biller PR: `BillerContracts` 4.0.0 in `src/Domain/Domain.csproj`, plus all the changes above.
3. Update BillerFrontEnd in this task (branch `BIL-65-series`), and deploy it together with the API, because the 2.0.0 front end can't create customers against the new API: bump `BillerContracts.Clients` to 4.0.0, customer forms pick a series instead of typing a name, show the seller's series on the seller page, show `SeriesName` on customers and invoices, drop the invoice number field from invoice editing, and disable the seller and customer selectors there (an invoice can't move to another seller or customer).
4. On the small PC: back up the database, run the pre-check queries, then `make update`.

## Out of scope

- Ownership checks (whether the calling user owns the seller). The API has no per-user authorization on customers or sellers today.
- Series formats beyond a name plus a 6-digit number.
- Year-based number resets.

## Order of work

1. BillerContracts 4.0.0 (+ clients)
2. Migrations 034, 035 (verify on a data copy)
3. Domain + Infrastructure
4. Application (series, then customer, then invoice)
5. Validators + WebAPI
6. Tests + coverage
7. BillioIntegrationTest on `BIL-65-series`
8. BillerFrontEnd on `BIL-65-series`
9. Manual checks, then deploy the API and front end together

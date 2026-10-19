# Plan: invoice series table and invoice numbering (BIL-65)

Jira: [BIL-65](https://aldisadom.atlassian.net/browse/BIL-65) (series table and invoice numbering; BIL-76 was merged into it). Epic: BIL-89 (Billeris).

## Goal

- Move the invoice series name and the running number out of `customers` into a new `series` table.
- Every series belongs to a seller. Every customer points to one series. Several customers of the same seller can share a series.
- Invoices store their own `series_id`, a snapshot of the series name, and their number.
- Any invoice can be deleted. Only deleting the **latest** invoice of a series lowers the series' next number.
- Reserving a number on create and releasing it on delete must be atomic, so two requests at the same time can't get the same number.

## Current state (master @ 125ae5e)

| Where | What |
|---|---|
| `customers.invoice_name`, `customers.invoice_number` | Series name and the **next** number to use (migration 020/021, `CustomerService.Add` sets it to 1) |
| `InvoiceService.Add` | Uses `Customer.InvoiceNumber` as the invoice number, saves the invoice, then calls `CustomerService.IncreaseInvoiceNumber`. These are two separate statements, so a crash or a concurrent request can produce a wrong or duplicate number |
| `InvoiceService.Delete` | Deletes the row. The number is never released |
| `InvoiceService.Update` / `InvoiceUpdateRequest.InvoiceNumber` | The number can be changed by hand to any value |
| `InvoiceModel.GenerateInvoiceName()` / `GenerateFileLocation()` | **Bug:** they use `Customer.InvoiceNumber` / `Customer.InvoiceName` from the customer snapshot instead of the invoice's own number |
| `invoices.file_path` | Written on insert/update but never read. PDFs are streamed since #37 |
| DB constraints | Nothing prevents duplicate `(customer, invoice_number)` |

## Design decisions

1. **`series` has `seller_id`; `customers` has `series_id`.** A customer's series must belong to the customer's seller.
2. **Series names are unique per seller** (`UNIQUE (seller_id, name)`), not globally.
3. **`next_number`** keeps today's meaning: the number the next invoice will get. The latest existing invoice has `next_number - 1`.
4. **Atomic numbering with single SQL statements** (data-modifying CTEs). PostgreSQL runs one statement atomically, so no transaction or unit-of-work plumbing is needed in the repositories. All repositories share one scoped `IDbConnection` today, and adding transactions there would touch every repository.
5. **Invoices snapshot the series name** in a new `invoices.series_name` column, filled from the database in the same insert statement. Renaming a series later doesn't change old invoices or their PDFs.
6. **The invoice number can't be edited.** `InvoiceNumber` is removed from `InvoiceUpdateRequest`, and `Update` no longer writes `invoice_number`, `series_id` or `series_name`.
7. **Changing a customer's series** only affects new invoices. Existing invoices keep their `series_id` and `series_name`.
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

- [ ] **Sync the local BillerContracts checkout.** It is 2 commits ahead of `origin/master`, has uncommitted changes (`.gitignore`, `Contracts/Enums/DocumentType.cs`), and still uses the old `Contracts.*` namespace. `origin/master` uses `BillerContracts.*` (what Biller references in 1.0.5) and contains the `Clients` project. Decide what to keep from the local changes, then reset to `origin/master` before starting.
- [ ] **Back up the production database** (the small PC, BIL-82) before deploying migration 033.
- [ ] **Run the pre-check queries** below against a copy of the real data.

### Pre-check queries (run before migration 033)

```sql
-- Customers with an empty series name. The migration needs a name; fix these first.
SELECT id, company_name FROM customers WHERE COALESCE(TRIM(invoice_name), '') = '';

-- Customers of the same seller that share a series name. They will share one series.
SELECT seller_id, invoice_name, COUNT(*), ARRAY_AGG(invoice_number)
FROM customers GROUP BY seller_id, invoice_name HAVING COUNT(*) > 1;

-- Duplicate invoice numbers per customer. The unique index will fail if any exist.
SELECT customer_id, invoice_number, COUNT(*)
FROM invoices GROUP BY customer_id, invoice_number HAVING COUNT(*) > 1;
```

## Step 1: BillerContracts (new version 1.0.6)

Repo: `BillerContracts`. Namespaces below are `BillerContracts.*` as on `origin/master`.

### 1.1 New files

`Contracts/Requests/Series/SeriesAddRequest.cs`
```csharp
public record SeriesAddRequest
{
    public Guid SellerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? NextNumber { get; set; }   // defaults to 1
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

- PR in BillerContracts, merge, run the `Publish` workflow with version `1.0.6`.
- If Plan B (units, BIL-75) is done at the same time, put both contract changes into 1.0.6.

## Step 2: Database migrations (Biller)

Use the `add-db-migration` skill. Files go in `migrations/sql/` (picked up by `includeAll` in `migrations/dbChangeLog.yaml`).

### 2.1 `032-Create-table-series.sql`

```sql
--liquibase formatted sql

-- changeset Aldis:32
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

### 2.2 `033-Migrate-customers_invoices_to_series.sql`

One changeset. PostgreSQL DDL is transactional, so it either runs completely or not at all.

```sql
--liquibase formatted sql

-- changeset Aldis:33
-- comment: Move invoice series from customers to series table, link customers and invoices to series

-- 1. One series per (seller, invoice_name). next_number = highest "next number" of those customers.
INSERT INTO series (seller_id, name, next_number)
SELECT seller_id, invoice_name, MAX(GREATEST(COALESCE(invoice_number, 1), 1))
FROM customers
GROUP BY seller_id, invoice_name;

-- 2. Link customers to their series.
ALTER TABLE customers ADD COLUMN series_id uuid;

UPDATE customers c
SET series_id = s.id
FROM series s
WHERE s.seller_id = c.seller_id AND s.name = c.invoice_name;

ALTER TABLE customers ALTER COLUMN series_id SET NOT NULL;
ALTER TABLE customers ADD CONSTRAINT fk_customers_series_id
    FOREIGN KEY (series_id) REFERENCES series(id);
CREATE INDEX idx_customers_series_id ON customers (series_id);

-- 3. Link invoices to the series of their customer; snapshot the series name the invoice was created with.
ALTER TABLE invoices ADD COLUMN series_id uuid;
ALTER TABLE invoices ADD COLUMN series_name character varying(10);

UPDATE invoices i
SET series_id = c.series_id,
    series_name = COALESCE(i.customer_data::json->>'InvoiceName', c.invoice_name)
FROM customers c
WHERE c.id = i.customer_id;

ALTER TABLE invoices ALTER COLUMN series_id SET NOT NULL;
ALTER TABLE invoices ALTER COLUMN series_name SET NOT NULL;
ALTER TABLE invoices ADD CONSTRAINT fk_invoices_series_id
    FOREIGN KEY (series_id) REFERENCES series(id);
CREATE UNIQUE INDEX uq_invoices_series_id_invoice_number ON invoices (series_id, invoice_number);

-- 4. next_number must be above every existing invoice number in the series.
UPDATE series s
SET next_number = GREATEST(s.next_number, m.max_number + 1)
FROM (SELECT series_id, MAX(invoice_number) AS max_number FROM invoices GROUP BY series_id) m
WHERE m.series_id = s.id;

-- 5. Remove the old columns.
ALTER TABLE customers DROP COLUMN invoice_name;
ALTER TABLE customers DROP COLUMN invoice_number;
ALTER TABLE invoices DROP COLUMN file_path;

-- rollback ALTER TABLE invoices ADD COLUMN file_path character varying(255) NOT NULL DEFAULT '';
-- rollback ALTER TABLE customers ADD COLUMN invoice_name character varying(10);
-- rollback ALTER TABLE customers ADD COLUMN invoice_number INT DEFAULT 0;
-- rollback UPDATE customers c SET invoice_name = s.name, invoice_number = s.next_number FROM series s WHERE s.id = c.series_id;
-- rollback DROP INDEX uq_invoices_series_id_invoice_number;
-- rollback ALTER TABLE invoices DROP COLUMN series_name;
-- rollback ALTER TABLE invoices DROP COLUMN series_id;
-- rollback ALTER TABLE customers DROP COLUMN series_id;
-- rollback DELETE FROM series;
```

Notes:
- `customer_data` is Newtonsoft JSON with PascalCase names, so the old series name is at `->>'InvoiceName'`.
- If old invoices were made while the customer had a different `invoice_name`, they get the customer's current series as `series_id` but keep their original name in `series_name`. Their numbers then count towards the current series in the unique index. The duplicate pre-check covers this.
- `customers.invoice_name` is `varchar(10)` (migration 003), so series names stay at max 10 characters. Existing data always fits, and the rollback can restore it.

### 2.3 Verify locally

```bash
docker compose up -d --build db liquibase
docker logs billio-liquibase      # changesets 32 and 33 ran
```
Then compare row counts and spot-check: every customer has a series, every invoice has `series_id` and `series_name`, and `next_number` = max invoice number + 1 per series. Test the rollback once on a copy (`liquibase rollback-count 2`).

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
    Task<Guid> Add(SeriesEntity series);
    Task Update(SeriesEntity series);
    Task Delete(Guid id);
    Task<bool> IsInUse(Guid id);   // referenced by any customer or invoice
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
- `Update`: set `series_id` instead of `invoice_name`, `invoice_number`.
- Remove `IncreaseInvoiceNumber`.
- `Get` queries join the series name for the response:
  ```sql
  SELECT c.*, s.name AS series_name FROM customers c JOIN series s ON s.id = c.series_id WHERE c.id = @Id
  ```
  This needs `SeriesName` on `CustomerEntity`. Alternatively `CustomerService` loads the series. **Recommendation:** use the join; it's one query and keeps the service simple.

### 4.3 `Repository/InvoiceRepository.cs`

`Add`: reserve the number and insert in one statement. The series name is read from the database at the same moment, so the snapshot is never stale.
```sql
WITH reserved AS (
    UPDATE series
    SET next_number = next_number + 1
    WHERE id = @SeriesId
    RETURNING next_number - 1 AS invoice_number, name
)
INSERT INTO invoices
    (customer_id, seller_id, user_id, series_id, series_name, invoice_number, user_data,
     created_date, due_date, seller_data, customer_data, items_data, comments, total_price, status)
SELECT @CustomerId, @SellerId, @UserId, @SeriesId, reserved.name, reserved.invoice_number, @UserData,
       @CreatedDate, @DueDate, @SellerData, @CustomerData, @ItemsData, @Comments, @TotalPrice, @Status
FROM reserved
RETURNING id;
```
If the series doesn't exist, nothing is inserted and the result is `Guid.Empty`. The service checks the series first, but treat `Guid.Empty` as an error anyway.

`Delete`: delete and release the number in one statement.
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
  AND s.next_number = d.invoice_number + 1;
```

`Update`: remove `file_path` and `invoice_number` from the `SET` list. `series_id` and `series_name` aren't updated either.

`Get(...)`: no change (`SELECT *` picks up the new columns).

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

Old invoices: the `customer_data` JSON still contains `InvoiceName`/`InvoiceNumber`. Newtonsoft ignores unknown properties, so deserialization keeps working. The PDF reads the series name from `InvoiceModel.SeriesName` (the column), not from the customer snapshot.

### 5.2 `Interfaces/ISeriesService.cs` + `Services/SeriesService.cs` (new)

```csharp
Task<SeriesModel> Get(Guid id);                                         // NotFoundException if missing
Task<(IEnumerable<SeriesModel>, int)> Get(SeriesGetRequest? query);
Task<Result<SeriesModel>> GetWithValidation(Guid id, Guid sellerId);    // error if missing or other seller
Task<Guid> Add(SeriesModel series);                                     // seller must exist; NextNumber ?? 1
Task Update(SeriesModel series);                                        // keeps SellerId from DB
Task Delete(Guid id);                                                   // ValidationException if IsInUse
```

- Duplicate name for the same seller: the unique constraint throws `PostgresException` 23505. Check first in `Add`/`Update` and return a clear validation error; keep the constraint as a safety net.
- `Update` lowering `NextNumber` below an existing invoice number would cause a unique-index error on the next invoice. Reject `NextNumber <= MAX(invoice_number)` for that series. This needs `ISeriesRepository.GetMaxInvoiceNumber(Guid id)` (returns 0 if none).

### 5.3 `Services/CustomerService.cs`

- `Add`: remove `customer.InvoiceNumber = 1`. Validate the series with `_seriesService.GetWithValidation(customer.SeriesId, customer.SellerId)` and throw `ValidationException` on error (same pattern as `InvoiceService.Validate`).
- `Update`: load the existing customer, validate the new `SeriesId` against the existing customer's `SellerId`.
- Remove `IncreaseInvoiceNumber` (also from `ICustomerService`).
- Constructor gets `ISeriesService`.

### 5.4 `Services/InvoiceService.cs`

- `GetInvoiceData`: remove `invoiceData.InvoiceNumber = invoiceData.Customer.InvoiceNumber`. Set `invoiceData.SeriesId = customer.SeriesId`.
- `Add`: remove the `IncreaseInvoiceNumber` call. `_invoiceRepository.Add` now reserves the number. Throw if it returns `Guid.Empty`.
- `Update`: copy `SeriesId`, `SeriesName` and `InvoiceNumber` from the stored invoice (like `User` today) so they can't be changed.
- `Delete`: unchanged in the service (`Get` then `Delete`). The repository releases the number.

### 5.5 `DependencyInjection.cs`

Add `services.AddScoped<ISeriesService, SeriesService>();`

## Step 6: Validators (`src/Validators`)

| File | Rules |
|---|---|
| `Series/SeriesAddValidator.cs` (new) | `SellerId` not empty. `Name` not empty, max 10. `NextNumber` ≥ 1 when set |
| `Series/SeriesUpdateValidator.cs` (new) | `Id` not empty. `Name` not empty, max 10. `NextNumber` ≥ 1 |
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
| `Application/Services/SeriesServiceTest.cs` (new) | Get found / not found. List with and without `SellerId`. `GetWithValidation`: OK / missing / other seller. Add: default `NextNumber` 1 / given value / duplicate name. Update: OK / not found / duplicate name / `NextNumber` ≤ max invoice number. Delete: OK / not found / in use |
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
5. Change a customer's series → old invoices keep the old `series_name` in the PDF; new invoices use the new series.
6. Fire ~20 parallel `POST /invoices` for the same series → all numbers unique and consecutive.
7. `DELETE /series/{id}` for a series in use → 400.
8. Generate the PDF for an old invoice (from before the migration) → same series and number as before.

Update the BillioIntegrationTest repo for the new contracts and add a series flow, if that repo covers customers and invoices.

## Step 10: Rollout

1. Merge and publish BillerContracts 1.0.6.
2. Biller PR: bump `BillerContracts` to 1.0.6 in `src/Domain/Domain.csproj`, plus all the changes above.
3. Update BillerFrontEnd: customer forms pick a series instead of typing a name; add series management; drop the invoice number field from invoice editing.
4. On the small PC: back up the database, run the pre-check queries, then `make update`.

## Out of scope

- Ownership checks (whether the calling user owns the seller). The API has no per-user authorization on customers or sellers today.
- Series formats beyond a name plus a 6-digit number.
- Year-based number resets.

## Order of work

1. BillerContracts 1.0.6 (+ clients)
2. Migrations 032, 033 (verify on a data copy)
3. Domain + Infrastructure
4. Application (series, then customer, then invoice)
5. Validators + WebAPI
6. Tests + coverage
7. Manual checks, then deploy

# Plan: remove serialized data from invoices

Jira: [BIL-86](https://aldisadom.atlassian.net/browse/BIL-86). Epic: BIL-89 (Billeris). Goes together with [BIL-85-item-history.md](BIL-85-item-history.md). Changes how [BIL-75-units.md](BIL-75-units.md) stores the unit.

## Goal

- Invoices keep their own copy of seller, customer, user and item data, but in typed columns and rows instead of JSON text.
- Invoice data can be queried in SQL (e.g. "invoices containing item X", "total by customer").
- The user's password hash is no longer copied into every invoice.
- Old invoices look the same after the migration, including their PDFs.

## Current state (master @ 125ae5e)

| Where | What |
|---|---|
| `invoices` table | `user_data`, `seller_data`, `customer_data`, `items_data`: `TEXT` columns holding Newtonsoft JSON (PascalCase) |
| `InvoiceModelExtensions.ToEntity` / `ToModel` | `JsonConvert.SerializeObject` / `DeserializeObject` for all four |
| `user_data` | A serialized `UserModel`, **including `Password` (the hash)**. `UserService.Get` maps it in. The PDFs only use `Name` and `LastName` |
| `customer_data` | Includes `InvoiceName` and `InvoiceNumber`. The series plan replaces these with `invoices.series_name` |
| `InvoiceItemEntity` | Exists but unused |
| `InvoiceUpdateRequest` | Lets the caller edit seller, customer, item names and prices per invoice. The copies must stay editable per invoice |

## Design decisions

1. **Keep a snapshot, change its shape.** Invoices must not change when a seller, customer or item changes later, and invoice updates edit the invoice's own copy. So invoices keep their own data. Linking to history tables was considered, but per-invoice edits would then have to create seller/customer/item versions, which is wrong.
2. **Seller, customer and user snapshots become prefixed columns on `invoices`** (`seller_company_name`, `customer_city`, `user_name`, …). They are 1:1 with the invoice, so a separate table only adds a join.
3. **Items go to a new `invoice_items` table**: `invoice_id` (FK, `ON DELETE CASCADE`), `position`, `item_id` (no FK, the item may be deleted later), `item_version` (nullable, from the item history plan), `name`, `price`, `quantity`, `comments`. Units add `unit_name_lt`, `unit_name_en`, `unit_id` here instead of in JSON.
4. **User snapshot keeps only `user_name` and `user_last_name`.** That is all the documents use. No password, no email.
5. **Writes are single SQL statements** (CTE inserting the invoice and its items via `unnest` of arrays). Updates replace the item rows in one statement. Same reasoning as the series plan: no transactions on the shared connection.
6. **Two migrations: copy, then drop.** 037 adds the columns and table and copies the JSON. 038 drops the JSON columns. Deploy 037, check the data, then deploy 038 in a later release. Until 038 runs, rolling back the code is safe.

## Prerequisites

- [ ] Backup script exists and a backup is taken ([BIL-84-db-backup-restore.md](BIL-84-db-backup-restore.md)).
- [ ] Series plan is merged (it adds `series_name` and removes `InvoiceName`/`InvoiceNumber` from the customer snapshot). If not, keep those two fields as columns here.
- [ ] Decide the order with units: **recommended** to do this plan first, then update BIL-75 to add unit columns to `invoice_items` instead of `items_data`.
- [ ] Run the pre-check queries on a copy of production.

### Pre-check queries

```sql
-- JSON that doesn't parse
SELECT id FROM invoices WHERE NOT (items_data IS JSON AND seller_data IS JSON
    AND customer_data IS JSON AND user_data IS JSON);  -- PostgreSQL 16

-- Invoices with no items, or items without a Name/Price
SELECT id FROM invoices WHERE jsonb_array_length(items_data::jsonb) = 0;
SELECT i.id, e FROM invoices i, jsonb_array_elements(i.items_data::jsonb) e
WHERE e->>'Name' IS NULL OR e->>'Price' IS NULL;

-- Stored totals that don't match the items (should be 0 rows)
SELECT id, total_price, (SELECT SUM((e->>'Price')::decimal * (e->>'Quantity')::decimal)
    FROM jsonb_array_elements(items_data::jsonb) e) AS computed
FROM invoices WHERE total_price <> (SELECT SUM((e->>'Price')::decimal * (e->>'Quantity')::decimal)
    FROM jsonb_array_elements(items_data::jsonb) e);
```

## Step 1: Migration 037 (add and copy)

```sql
ALTER TABLE invoices
    ADD COLUMN seller_company_name text, ADD COLUMN seller_company_number text,
    ADD COLUMN seller_street text, ADD COLUMN seller_city text, ADD COLUMN seller_state text,
    ADD COLUMN seller_email text, ADD COLUMN seller_phone text,
    ADD COLUMN seller_bank_name text, ADD COLUMN seller_bank_number text,
    ADD COLUMN customer_company_name text, ADD COLUMN customer_company_number text,
    ADD COLUMN customer_street text, ADD COLUMN customer_city text, ADD COLUMN customer_state text,
    ADD COLUMN customer_email text, ADD COLUMN customer_phone text,
    ADD COLUMN user_name text, ADD COLUMN user_last_name text;

UPDATE invoices SET
    seller_company_name = seller_data::jsonb->>'CompanyName', ...,
    customer_company_name = customer_data::jsonb->>'CompanyName', ...,
    user_name = user_data::jsonb->>'Name', user_last_name = user_data::jsonb->>'LastName';

ALTER TABLE invoices ALTER COLUMN seller_company_name SET NOT NULL, ...;  -- after the copy

CREATE TABLE invoice_items (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    invoice_id uuid NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    position int NOT NULL,
    item_id uuid NULL,
    item_version int NULL,
    name text NOT NULL,
    price decimal NOT NULL,
    quantity decimal NOT NULL,
    comments text NULL,
    PRIMARY KEY (id),
    UNIQUE (invoice_id, position)
);
CREATE INDEX ix_invoice_items_item_id ON invoice_items(item_id);

INSERT INTO invoice_items (invoice_id, position, item_id, name, price, quantity, comments)
SELECT i.id, e.ord, (e.v->>'Id')::uuid, e.v->>'Name', (e.v->>'Price')::decimal,
       (e.v->>'Quantity')::decimal, e.v->>'Comments'
FROM invoices i, jsonb_array_elements(i.items_data::jsonb) WITH ORDINALITY AS e(v, ord);

ALTER TABLE invoices ALTER COLUMN user_data DROP NOT NULL, ALTER COLUMN seller_data DROP NOT NULL,
    ALTER COLUMN customer_data DROP NOT NULL, ALTER COLUMN items_data DROP NOT NULL;
-- rollback DROP TABLE invoice_items; ALTER TABLE invoices DROP COLUMN seller_company_name, ...; SET NOT NULL back
```

Existing invoices get `item_version = NULL` (unknown).

## Step 2: Migration 038 (drop, next release)

`ALTER TABLE invoices DROP COLUMN user_data, DROP COLUMN seller_data, DROP COLUMN customer_data, DROP COLUMN items_data;`
Rollback is not possible without the backup. Say so in the changeset comment.

## Step 3: Domain and Infrastructure

- `InvoiceEntity`: remove the four `*Data` strings, add the prefixed columns and `List<InvoiceItemEntity> Items`.
- `InvoiceItemEntity`: match `invoice_items`.
- `InvoiceRepository`:
  - `Get(id)`: `QueryMultiple` (invoice row + its items ordered by `position`).
  - `Get(filters, page)`: page of invoices, then one items query with `invoice_id = ANY(@Ids)`, grouped in C#.
  - `Add`: one statement, `WITH inv AS (INSERT ... RETURNING id) INSERT INTO invoice_items SELECT inv.id, x.* FROM inv, unnest(@Positions, @ItemIds, @Names, ...) AS x(...)`.
  - `Update`: one statement that updates the invoice, deletes its items and inserts the new ones.

## Step 4: Application

- `InvoiceModelExtensions`: map columns to `SellerModel`, `CustomerModel`, `UserModel` (only `Id`, `Name`, `LastName`) and `InvoiceItemModel`. Remove `Newtonsoft.Json` from this file. Remove the package from `Application.csproj` if nothing else uses it.
- `InvoiceItemModel`: add `int? ItemVersion`.
- No API contract changes. Responses look the same.

## Step 5: Tests (`write-tests` skill)

- `InvoiceMappingProfileTest`: entity ↔ model for every snapshot field, items keep their order, the user snapshot has no password.
- `InvoiceServiceTest`: unchanged behaviour, update mocks to the new entity shape.
- Integration tests: create, get, update items (count changes), delete (items gone by cascade), list with paging.

## Step 6: Manual checks

- [ ] On a copy of production: before 037, generate the PDF (SF and ADA, LT and EN) for 3 old invoices. After 037 and the new code, generate them again and compare the text (`pdftotext`, then diff).
- [ ] Counts: `SELECT COUNT(*) FROM invoice_items` equals the total length of all `items_data` arrays.
- [ ] `total_price` equals `SUM(price * quantity)` per invoice.

## Out of scope

- Changing what fields an invoice snapshots.
- History for sellers or customers.

## Order of work

1. Backup script.
2. Series plan (already planned).
3. This plan (037 + code) and item history (036) in the same release.
4. Units (BIL-75), changed to use `invoice_items` columns.
5. 038 in the next release, after checking production data.

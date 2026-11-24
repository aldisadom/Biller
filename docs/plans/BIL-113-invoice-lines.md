# Plan: invoice lines in an `invoice_lines` table (BIL-113)

Jira: [BIL-113](https://aldisadom.atlassian.net/browse/BIL-113). Epic: BIL-89 (Billeris). Blocks
BIL-85 (item versions, In progress, branch `BIL-85-item-versions`, not merged) and BIL-75 (units,
To Do). Planned against master's code; BIL-85 is not a blocker.

## Open questions

None. The user decided:

- Lines hold no name and no price: every read joins `items` and shows the item's current name and
  price (decision 3). Until BIL-85, an item edit changes every invoice that uses the item, its PDF
  and its total, paid invoices too. The user accepted this.
- Lines whose item was already deleted: decided by pre-check 1's count on a production copy
  (decision 4). Both branches are written below; the deploy step picks one.

## Goal

- Invoice lines are rows in `invoice_lines` instead of JSON text in `invoices.items_data`.
- The database guarantees that a line's item exists: an item an invoice uses can't be deleted (400),
  even by a request at the same moment, and deleting an invoice deletes its lines.
- Line positions and quantities are checked by the database.
- Lines can be queried with SQL (sales per item; BIL-75 adds a unit, BIL-85 a version).
- The API, BillerContracts, BillerFrontEnd and the PDFs keep their shape. Values on old invoices
  change: they show each item's current name and price.

## Current state (master @ ea10918)

Checked again at ea10918 (origin/master has nothing newer).

| Where | What |
|---|---|
| `invoices.items_data` (026) | `TEXT NOT NULL`: Newtonsoft JSON of `List<InvoiceItemModel>`, PascalCase keys `Id`, `Name`, `Price`, `Quantity`, `Comments`. No foreign key from a line to `items` |
| `invoices.total_price` (026) | `decimal NOT NULL`, written from `InvoiceModel.CalculateTotal()` in `InvoiceModelExtensions.ToEntity`. Never read by Biller: `ToModel` doesn't map it, and responses (`InvoiceModelFullExtensions.ToResponse`) and both PDF documents compute the total from the lines with `CalculateTotal()`. Only BIL-90's plan reads it (not built) |
| `items` (006, 025, 027) | `id`, `customer_id`, `name varchar(255)`, `price numeric`, `quantity decimal`, all `NOT NULL`. Foreign key to `customers` is `fk_invoice_customers_id` |
| Last migration | `035-Migrate-customers_invoices_to_series.sql`. Liquibase 4.26.0, `includeAll` of `migrations/sql/` |
| `InvoiceRepository` | `Get(id)` and the paged `Get` select `i.*` with the series name. `Add` is one statement: `reserved` CTE takes the series number, then `INSERT INTO invoices (..., items_data, ..., total_price, ...)`. `Update` sets `items_data` and `total_price`. `Delete` gives the number back in one statement |
| `ItemRepository` | `Get(id)`, `Get(ids)` (`ANY(@Ids)`), paged `Get`, `Update`, and `Delete`: a plain `DELETE FROM items`, no "used by invoices" check. `ItemService.Delete` checks only that the item exists (404) |
| `InvoiceEntity` | `string ItemsData`, `decimal TotalPrice` |
| `InvoiceItemEntity` | Unused (`Id`, `Name`, `Price`, `Quantity`, `TotalPrice`, `Comments`) |
| `InvoiceModelExtensions` (`MappingProfiles/InvoiceMappingProfile.cs`) | `ToModel(InvoiceEntity)` deserializes `ItemsData`; `ToEntity(InvoiceModel)` serializes `Items` and sets `TotalPrice` |
| `InvoiceItemModel` | `Id`, `Name`, `Price`, `Quantity`, `Comments`, `CalculateTotal()`. Used by the service, mappings, responses and both PDF documents |
| `InvoiceService.Add` / `Update` | `Validate` (seller of user, customer of seller, `ItemService.GetForCustomer` for the line items: 400 when an id isn't the customer's), then the public static `MapItemToInvoiceItem` copies name and price from the current items |
| Validators | `InvoiceItemValidator`: item id not empty, quantity > 0, comments drawable. `InvoiceModelUpdateValidator` repeats them. `ItemAddValidator`/`ItemUpdateValidator`: price > 0 |
| `ErrorChecking` middleware | Postgres "update or delete ... violates foreign key constraint" → 400 "Can not delete (please clear all dependants) or update (item not found): <constraint after `fk_`>". "insert or update" → 400 "Key does not exist: ...". A check-constraint violation → 500 |
| BillerContracts | Biller uses 5.0.0: `InvoiceItemRequest` (`Id`, `Quantity`, `Comments`), `InvoiceItemResponse` (`Id`, `Name`, `Price`, `Quantity`, `Comments`), `InvoiceResponse.TotalPrice`. BillerContracts master already has BIL-85's `Version` (not used by Biller yet); this issue needs no contract change |
| BillerFrontEnd | Works only through the contracts; no change |
| BillioIntegrationTest (master) | `InvoiceTest.InvoiceKeepsItemPriceUntilUpdated_Valid_Success` (line 2080: item edit keeps the line until `PUT`). `InvoiceWithDeletedItem_Valid_Success` (2123: deletes an item an invoice uses, reads the invoice). `CustomerDelete_WhenHasInvoice_Fail` (2145: deletes the invoice's item first, then expects 400 on `invoices_customer_id`). Clean-up deletes invoices before items (`TestPreparation.DeleteAllData`, `InvoiceNumbering_SeriesRules_Success`) |
| `CLAUDE.md` (master, Conventions) | "They keep their own number, dates and lines. Editing an item must not change an existing invoice or its PDF. Saving the invoice itself (`PUT /invoices`) copies line names and prices from the current items." |
| `REVIEW.md` | Failure: "An old invoice or its PDF changes after an item or unit is edited." |
| Other plans | BIL-85's plan on its branch (`docs/plans/BIL-85-item-versions.md`, migrations 036–038): lines stay in `items_data` with a `Version` key (decision 2), versions rebuilt from the JSON lines (decision 4), item delete stays a hard delete (decision 5). Master's `BIL-85-item-history.md` is the older design. `BIL-75-units.md` snapshots the unit into `items_data` (decision 3). `BIL-90-seller-invoice-summary.md` sums the stored `total_price` (decision 3). `BIL-86-...md` "Out of scope" names this move |

## Design decisions

1. **Table `invoice_lines`, key `(invoice_id, line_no)`.** `line_no` is the 1-based position, so
   lines keep their order. No separate `id`: nothing points to a line yet (BIL-85 and BIL-75 add
   columns, not references). `invoice_id` is `ON DELETE CASCADE`, so `InvoiceRepository.Delete`
   stays as it is.
2. **`item_id` is `NOT NULL` and references `items(id)` without cascade.** Deleting an item a line
   uses fails on `fk_invoice_lines_item_id`, and the middleware already turns that into 400 "Can not
   delete (please clear all dependants) or update (item not found): invoice_lines_item_id". So the
   check is the foreign key's job; `ItemService` and `ItemRepository.Delete` don't change. This is a
   new rule: on master, used items can be deleted. Index on `item_id` for the delete check and
   per-item queries.
3. **A line holds no name and no price; reads join the item (user's decision).** Columns:
   `invoice_id`, `line_no`, `item_id`, `quantity numeric`, `comments text`. Every invoice read joins
   `items` and shows its current name and price. Reason: the user wants no copy of item data on the
   line, and BIL-85 brings back what was billed through item versions. Accepted by the user: until
   BIL-85, editing an item changes every invoice that uses it, its lines, PDF and total, paid
   invoices too; `CLAUDE.md` and `REVIEW.md` say so (Step 10). `PUT /invoices` no longer copies name
   and price (`MapItemToInvoiceItem` goes). The migration drops the line name and price where they
   differ from the item's current values; pre-check 2 lists those lines for information.
4. **Lines whose item was already deleted: decided by pre-check 1's count (user's decision).**
   - **0 on the production copy (4a):** no extra mechanism. If such a line appears before the
     deploy, 036 fails on the foreign key and changes nothing; fix or delete that invoice and deploy
     again.
   - **Above 0 (4b): restore them as hidden archived items.** 036 adds
     `items.archived boolean NOT NULL DEFAULT false` and inserts one `items` row per missing id: the
     line's id, the invoice's customer, and the name, price and quantity of its line on the newest
     invoice, with `archived = true`. Every item read in `ItemRepository` filters `NOT archived`, so
     `GET /items/{id}` gives 404, they're missing from the item list, `PUT /items` and
     `DELETE /items/{id}` give 404 (both call `Get` first), and `PUT /invoices` with such a line gives
     400 from `GetForCustomer`, as on master. Invoice reads join `items` without the filter, so old
     invoices show them.
   - Rejected: a nullable `item_id` (it only worked with name and price on the line: a line with
     no item would have no name or price to show); dropping orphan lines (loses billed data); no
     foreign key (the issue's main goal).
5. **`invoices.total_price` is dropped (changeset 038).** A stored total goes stale on the first
   item edit (decision 3), and keeping it right would mean updating every invoice of an item on each
   item edit. Computing it on read is what Biller already does: responses and PDFs use
   `CalculateTotal()` over the lines, now with the joined current prices, so the response, the PDF
   and the total can't disagree. Nothing in Biller reads the column. BIL-90's plan sums it; that
   plan changes to sum `quantity * price` over `invoice_lines` joined to `items` (follow-up). The
   rollback rebuilds it from the lines and the current prices.
6. **Line checks:** `line_no >= 1`, `quantity > 0` (as `InvoiceItemValidator`). Validators stay in
   front, so a check failing (500) means a bug. Pre-check 3 shows whether old lines break a check;
   if they do, fix them before 036.
7. **Each write is one statement.** `Add`: the existing `reserved` CTE, then the invoice insert,
   then the lines insert from the invoice's new id. `Update`: update the invoice, upsert lines
   `1..n` (`ON CONFLICT (invoice_id, line_no) DO UPDATE`), delete lines above `n`. The upsert and the
   delete touch different rows, so the key never sees an old and a new line 1 at once (a
   delete-all-then-insert in one statement could, since the CTEs' order isn't defined). Lines go in
   as arrays (`unnest(...) WITH ORDINALITY`), as `ItemRepository.Get(ids)` already passes
   `ANY(@Ids)`.
8. **Reads are two queries:** the invoice (or page), then its lines joined to `items` by
   `invoice_id = ANY(@Ids) ORDER BY invoice_id, line_no`. The one-statement rule is for writes. A
   `json_agg` column would bring the JSON parsing back.
9. **Three changesets.** 036 creates the table and moves the lines (with 4b, first restores the
   missing items); 037 drops `items_data`; 038 drops `total_price`. The move reads only `items_data`
   values that are JSON arrays (`IS JSON ARRAY`, PG16), so non-JSON text doesn't fail a `::jsonb`
   cast. 037 has a Liquibase precondition (`onFail:HALT`) that counts invoices whose `items_data`
   isn't a JSON array or whose array length differs from their moved lines; anything above 0 stops
   before the drop, so nothing is lost silently. A line element that isn't an object, or misses `Id`
   or `Quantity` (wrong key case), fails 036 on `NOT NULL`. GUID case doesn't matter: `::uuid` reads
   both.
10. **Rollbacks rebuild the JSON from the current items.** 038's rollback adds `total_price` back and
    fills it from the lines and the items' current prices. 037's rollback adds `items_data` back and
    fills it from the lines with `Name` and `Price` from the items as they are now
    (`jsonb_agg(... ORDER BY line_no)`, `'[]'` for an invoice without lines), so lines changed after
    the deploy survive a rollback; the old line names and prices dropped by 036 don't come back.
    Newtonsoft reads jsonb's key order, spacing and `50` for `50.0` the same. 036's rollback drops
    the table (with 4b, then deletes the archived items and drops the column).
11. **No contract, front-end or PDF layout change.** `InvoiceItemModel` keeps its shape; its `Name`
    and `Price` now come from the join. Only the entities, the repository, the mapping and the
    service change.

## Dependencies

- No blockers. BIL-113 goes to master before BIL-85 and BIL-75 (Jira: BIL-113 blocks both).
- **Follow-up on BIL-85 (plan and branch; not edited here):**
  - Its migrations renumber after 038.
  - Its decision 4 (versions rebuilt from the JSON lines) can't work: after BIL-113 the old line
    names and prices are gone. Versions start from the items as they are at BIL-85's deploy (one
    version per item); every existing line points to it.
  - Its decision 2 (lines keep a copy plus `Version`) becomes `invoice_lines.item_version_id`
    (foreign key to `item_versions`), read through the version; no name or price copy on the line.
    Its 037 reads `invoice_lines`, not `items_data`.
  - Its decision 5 (hard delete, invoices keep their copy) becomes 400 by this foreign key (its
    `CLAUDE.md` text already says so).
  - It restores the `CLAUDE.md` rule "Editing an item must not change an existing invoice or its
    PDF" and the `REVIEW.md` failure line for items (Step 10 marks both as suspended until BIL-85).
  - With 4b, archived items get a version like any item, and its item reads keep `NOT archived`.
- **Follow-up on BIL-75:** `docs/plans/BIL-75-units.md` snapshots the unit into `items_data`
  (decision 3). It moves to the line table: until BIL-85 a line shows its item's current unit
  (join), as it does name and price; with BIL-85 the unit belongs to the item version.
- **Follow-up on BIL-90:** `docs/plans/BIL-90-seller-invoice-summary.md` decision 3 sums
  `invoices.total_price`, which 038 drops. It sums `l.quantity * it.price` over `invoice_lines`
  joined to `items` instead (with BIL-85, the version's price).

## Pre-check queries

Run on a copy of production before deploying (`make update` takes a backup first).

```sql
-- 1. Lines whose item is gone: the count picks decision 4a (0) or 4b (above 0)
SELECT i.id, i.invoice_number, l.value->>'Id' AS item_id, l.value->>'Name' AS name
FROM invoices i
CROSS JOIN LATERAL jsonb_array_elements(
    CASE WHEN i.items_data IS JSON ARRAY THEN i.items_data::jsonb ELSE '[]'::jsonb END) l
WHERE NOT EXISTS (SELECT 1 FROM items it WHERE it.id::text = lower(l.value->>'Id'));

-- 2. For information: lines whose name or price differ from the item now. After the deploy these
--    invoices, their PDFs and totals show the item's current name and price (decision 3).
SELECT i.id, i.invoice_number, i.status, l.value->>'Id' AS item_id,
       l.value->>'Name' AS billed_name, it.name AS name_now,
       (l.value->>'Price')::numeric AS billed_price, it.price AS price_now
FROM invoices i
CROSS JOIN LATERAL jsonb_array_elements(
    CASE WHEN i.items_data IS JSON ARRAY THEN i.items_data::jsonb ELSE '[]'::jsonb END) l
JOIN items it ON it.id::text = lower(l.value->>'Id')
WHERE l.value->>'Name' IS DISTINCT FROM it.name
   OR (l.value->>'Price')::numeric IS DISTINCT FROM it.price
ORDER BY i.invoice_number;

-- 3. Lines that break a column rule (missing key, wrong case, quantity <= 0)
SELECT i.id, l.value
FROM invoices i
CROSS JOIN LATERAL jsonb_array_elements(
    CASE WHEN i.items_data IS JSON ARRAY THEN i.items_data::jsonb ELSE '[]'::jsonb END) l
WHERE jsonb_typeof(l.value) <> 'object'
   OR l.value->>'Id' IS NULL OR l.value->>'Quantity' IS NULL
   OR (l.value->>'Quantity')::numeric <= 0;

-- 4. items_data that isn't a JSON array (037's precondition would stop)
SELECT id, left(items_data, 80) FROM invoices WHERE NOT (items_data IS JSON ARRAY);
```

## Step 1: BillerContracts

None. Check: Biller stays on 5.0.0, and no request or response changes.

## Step 2: Migrations (`_add-db-migration`; numbers are taken when written)

`036-Create-table-invoice_lines.sql` (decision 4a):

```sql
--liquibase formatted sql

-- changeset Aldis:36
-- comment: Invoice lines move from invoices.items_data (JSON text) to invoice_lines. A line names its item and keeps quantity and comments; name and price are read from the item. items_data that isn't a JSON array moves no lines; changeset 37 stops before dropping it.
CREATE TABLE invoice_lines (
    invoice_id uuid NOT NULL,
    line_no integer NOT NULL,
    item_id uuid NOT NULL,
    quantity numeric NOT NULL,
    comments text,

    PRIMARY KEY (invoice_id, line_no),
    CONSTRAINT fk_invoice_lines_invoice_id
      FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE,
    CONSTRAINT fk_invoice_lines_item_id
      FOREIGN KEY (item_id) REFERENCES items(id),
    CONSTRAINT ck_invoice_lines_line_no CHECK (line_no >= 1),
    CONSTRAINT ck_invoice_lines_quantity CHECK (quantity > 0)
);

CREATE INDEX idx_invoice_lines_item_id ON invoice_lines (item_id);

INSERT INTO invoice_lines (invoice_id, line_no, item_id, quantity, comments)
SELECT i.id, l.ord, (l.value->>'Id')::uuid, (l.value->>'Quantity')::numeric, l.value->>'Comments'
FROM invoices i
CROSS JOIN LATERAL jsonb_array_elements(
    CASE WHEN i.items_data IS JSON ARRAY THEN i.items_data::jsonb ELSE '[]'::jsonb END)
    WITH ORDINALITY AS l(value, ord);

-- rollback DROP TABLE invoice_lines;
```

With decision 4b, 036 starts with the archived items (before `CREATE TABLE`), and its rollback
undoes them after the drop:

```sql
ALTER TABLE items ADD COLUMN archived boolean NOT NULL DEFAULT false;

INSERT INTO items (id, customer_id, name, price, quantity, archived)
SELECT DISTINCT ON (l.item_id) l.item_id, i.customer_id, l.name, l.price, l.quantity, true
FROM invoices i
CROSS JOIN LATERAL (
    SELECT (e->>'Id')::uuid AS item_id, e->>'Name' AS name, (e->>'Price')::numeric AS price,
           (e->>'Quantity')::numeric AS quantity
    FROM jsonb_array_elements(
        CASE WHEN i.items_data IS JSON ARRAY THEN i.items_data::jsonb ELSE '[]'::jsonb END) e) l
WHERE NOT EXISTS (SELECT 1 FROM items it WHERE it.id = l.item_id)
ORDER BY l.item_id, i.created_date DESC, i.invoice_number DESC;

-- rollback DROP TABLE invoice_lines;
-- rollback DELETE FROM items WHERE archived;
-- rollback ALTER TABLE items DROP COLUMN archived;
```

`037-Alter-table-invoices_drop_items_data.sql`:

```sql
--liquibase formatted sql

-- changeset Aldis:37
-- preconditions onFail:HALT onError:HALT
-- precondition-sql-check expectedResult:0 SELECT COUNT(*) FROM invoices i WHERE CASE WHEN i.items_data IS JSON ARRAY THEN jsonb_array_length(i.items_data::jsonb) <> (SELECT COUNT(*) FROM invoice_lines l WHERE l.invoice_id = i.id) ELSE true END
-- comment: Drop invoices.items_data: the lines are in invoice_lines. Stops when an invoice's JSON lines weren't all moved. The rollback rebuilds the JSON from invoice_lines with the items' current name and price.
ALTER TABLE invoices DROP COLUMN items_data;

-- rollback ALTER TABLE invoices ADD COLUMN items_data TEXT;
-- rollback UPDATE invoices i SET items_data = COALESCE((SELECT jsonb_agg(jsonb_build_object('Id', l.item_id, 'Name', it.name, 'Price', it.price, 'Quantity', l.quantity, 'Comments', l.comments) ORDER BY l.line_no) FROM invoice_lines l JOIN items it ON it.id = l.item_id WHERE l.invoice_id = i.id), '[]'::jsonb)::text;
-- rollback ALTER TABLE invoices ALTER COLUMN items_data SET NOT NULL;
```

`038-Alter-table-invoices_drop_total_price.sql`:

```sql
--liquibase formatted sql

-- changeset Aldis:38
-- comment: Drop invoices.total_price: the total is computed from the lines and the items' current prices (BIL-113). The rollback rebuilds it the same way.
ALTER TABLE invoices DROP COLUMN total_price;

-- rollback ALTER TABLE invoices ADD COLUMN total_price decimal;
-- rollback UPDATE invoices i SET total_price = COALESCE((SELECT SUM(l.quantity * it.price) FROM invoice_lines l JOIN items it ON it.id = l.item_id WHERE l.invoice_id = i.id), 0);
-- rollback ALTER TABLE invoices ALTER COLUMN total_price SET NOT NULL;
```

Check on the test stack that Liquibase 4.26 runs the precondition in formatted SQL, that it halts
on a broken row, and all three rollbacks on real rows (`_test-stack`), with 4b's variant too when
pre-check 1 is above 0.

## Step 3: Domain and Infrastructure

- `Domain/Entities/InvoiceLineEntity.cs` replaces the unused `InvoiceItemEntity`: `InvoiceId`,
  `LineNo`, `ItemId`, `Quantity`, `Comments`, and `Name`, `Price` filled only by reads (from the
  join).
- `InvoiceEntity`: `List<InvoiceLineEntity> Lines` replaces `string ItemsData`; `TotalPrice` goes.
- `IInvoiceRepository`: signatures unchanged.
- `InvoiceRepository`:
  - `Get(id)`: invoice query as today, then

    ```sql
    SELECT l.*, it.name, it.price FROM invoice_lines l
    JOIN items it ON it.id = l.item_id
    WHERE l.invoice_id = @Id ORDER BY l.line_no
    ```

    No `archived` filter here (decision 4b).
  - Paged `Get`: page query as today, then the same lines query for the page's ids
    (`l.invoice_id = ANY(@Ids) ORDER BY l.invoice_id, l.line_no`), grouped onto each entity.
  - `Add`:

    ```sql
    WITH reserved AS ( ...unchanged... ),
    inv AS (
        INSERT INTO invoices
        (customer_id, seller_id, user_id, series_id, invoice_number, created_date, due_date,
         comments, status)
        SELECT @CustomerId, @SellerId, @UserId, @SeriesId, reserved.invoice_number, @CreatedDate, @DueDate,
               @Comments, @Status
        FROM reserved
        RETURNING id
    ),
    lines AS (
        INSERT INTO invoice_lines (invoice_id, line_no, item_id, quantity, comments)
        SELECT inv.id, l.ord, l.item_id, l.quantity, l.comments
        FROM inv, unnest(@ItemIds::uuid[], @Quantities::numeric[], @LineComments::text[])
                  WITH ORDINALITY AS l(item_id, quantity, comments, ord)
    )
    SELECT id FROM inv
    ```

    Still returns `Guid.Empty` when `reserved` takes no number, so `InvoiceService.Add` keeps its
    check.
  - `Update`:

    ```sql
    WITH inv AS (
        UPDATE invoices
        SET created_date=@CreatedDate, due_date=@DueDate, comments=@Comments
        WHERE id=@Id
        RETURNING id
    ),
    upserted AS (
        INSERT INTO invoice_lines (invoice_id, line_no, item_id, quantity, comments)
        SELECT inv.id, l.ord, l.item_id, l.quantity, l.comments
        FROM inv, unnest(@ItemIds::uuid[], @Quantities::numeric[], @LineComments::text[])
                  WITH ORDINALITY AS l(item_id, quantity, comments, ord)
        ON CONFLICT (invoice_id, line_no) DO UPDATE
        SET item_id=EXCLUDED.item_id, quantity=EXCLUDED.quantity, comments=EXCLUDED.comments
    )
    DELETE FROM invoice_lines
    WHERE invoice_id IN (SELECT id FROM inv)
      AND line_no > cardinality(@ItemIds::uuid[])
    ```

  - The array parameters are built from `invoice.Lines` in a private helper, so `Add` and `Update`
    share it.
  - `Delete`, `UpdateStatus`, `MarkOverdue`: unchanged (lines cascade).
- `ItemRepository`: unchanged with 4a. With 4b: `ItemEntity` gets no new property; `Get(id)`,
  `Get(ids)` and the paged `Get` (count and data) add `NOT archived`.

## Step 4: Application

- `InvoiceModelExtensions`: `ToModel(InvoiceEntity)` maps `Lines` to `Items` (by hand, in
  `line_no` order, `Name` and `Price` from the line's joined values); `ToEntity(InvoiceModel)` maps
  `Items` to `Lines` with `LineNo = index + 1`, `InvoiceId = Id`, `ItemId`, `Quantity`, `Comments`
  (no `Name`, `Price`), and drops `TotalPrice`. No more `JsonConvert` there.
- `InvoiceService`: `MapItemToInvoiceItem` and its two calls go; `Validate` still calls
  `GetForCustomer` for the check (its result is no longer needed by `Add`/`Update`).
- `InvoiceItemModel`, `InvoiceModel`, `InvoiceModelFull`, `ToResponse`, PDF documents: unchanged.
- Remove the `Newtonsoft.Json` reference from `Application` if nothing else uses it.

## Step 5: Validators

None. Quantity > 0 and item ids are already checked, and items' prices are > 0.

## Step 6: WebAPI

None. Check that `DELETE /items/{id}`'s Swagger docs list the 400 for an item an invoice uses.

## Step 7: Tests (`_write-tests`)

- `InvoiceMappingProfileTest`: entity lines ↔ model items, order kept, `LineNo` from 1, every
  property mapped (names and prices from the entity lines on read, not set on write); empty lines;
  the `TotalPrice` expectation goes from `ToEntity`.
- `InvoiceServiceTest`: entities built with `Lines` instead of `ItemsData` and without
  `TotalPrice` (two places); `Add`/`Update` no longer expect names and prices copied onto the
  saved lines.
- `Infrastructure` is outside coverage; the SQL is covered by the integration tests.
- Coverage: compare with the minimums + 1 before the PR (`CLAUDE.md`).

## Step 8: Integration tests (BillioIntegrationTest, branch `BIL-113-invoice-lines`)

- `InvoiceKeepsItemPriceUntilUpdated_Valid_Success` becomes
  `InvoiceLineFollowsItemEdit_Valid_Success` (the new rule): after the item edit, `GET` shows the
  item's new name and price and the total from them, before any `PUT /invoices`; a `PUT` with new
  quantity and comments keeps them; the series' `NextNumber` check stays.
- `InvoiceWithDeletedItem_Valid_Success` becomes `ItemDelete_WhenUsedByInvoice_Fail`: 400
  "Validation failure", "Can not delete (please clear all dependants) or update (item not found):
  invoice_lines_item_id"; the invoice still reads the same; after the invoice is deleted, the item
  delete works (cascade).
- `CustomerDelete_WhenHasInvoice_Fail`: can no longer delete the item first. Delete the customer
  with its item and invoice in place and expect 400 on whichever constraint Postgres reports
  (`invoice_customers_id` from items or `invoices_customer_id`; pin it from a run).
- New: `InvoiceUpdateLines_OrderKept_Success`: add with 2 lines, `PUT` with 3 (new line last), then
  with 1; each read returns exactly those lines in order, and the total matches.
- New tests go into the `[DependsOn]` lists as `docs/review-evals.md` #15 requires.
- Run the whole suite on the test stack (`_test-stack`), plus the 036/037/038 rollbacks on real rows.

## Step 9: Front end

None.

## Step 10: Docs and rules

- `CLAUDE.md` "Conventions", the invoices paragraph becomes (wording when writing):
  "Invoices store their user, seller and customer ids and show the current data. They keep their
  own number and dates. Lines are rows in `invoice_lines`: a line names its item and keeps its
  quantity and comments, and shows the item's current name and price. Until BIL-85, editing an item
  changes every invoice that uses it, its PDF and total, paid ones too (accepted; BIL-85 brings back
  'editing an item must not change an existing invoice or its PDF'). An item that an invoice line
  uses can't be deleted (400). The series name isn't copied, so a series with invoices can't be
  renamed."
- `REVIEW.md`: "An old invoice or its PDF changes after an item or unit is edited." becomes "An old
  invoice or its PDF changes after a unit is edited (items: allowed until BIL-85, see `CLAUDE.md`)."
- `README.md` / `docs/README_lt.md`: nothing names `items_data`, `total_price` or the tables;
  check again when writing.

## Out of scope

- Units (BIL-75) and item versions (BIL-85), apart from the follow-ups above.
- New endpoints for per-item sales.

## Order of work

1. Run pre-checks 1–4 on a production copy. Pre-check 1's count picks 4a or 4b; pre-check 2 is for
   the user's information; pre-checks 3 and 4 must be empty (or fixed) before deploying.
2. Biller branch `BIL-113-invoice-lines`: migrations 036–038 → entities and repositories → mapping
   and service → unit tests → `CLAUDE.md`, `REVIEW.md`.
3. BillioIntegrationTest branch with the same name: Step 8.
4. Test stack: migrations on rows like production's, all rollbacks, integration tests.
5. `_pre-pr`, then PRs (Biller and BillioIntegrationTest).
6. After merge: update BIL-85's plan and branch, BIL-75's plan and BIL-90's plan (Dependencies).

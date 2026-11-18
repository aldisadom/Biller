# Plan: item versions that invoice lines are pinned to (BIL-85)

Jira: [BIL-85](https://aldisadom.atlassian.net/browse/BIL-85). Epic: BIL-89 (Billeris). Replaces
the earlier `item_history` design of this issue (a copy of every item state next to `items`).

## Goal

- Items can be edited freely. Every name or price change is kept as a numbered version.
- An invoice line changes only when the owner refreshes it to the newest item version, or picks
  another version.

## Current state (master @ f3ad202)

| Where | What |
|---|---|
| `items` (006, 025, 027) | `id`, `customer_id`, `name varchar(255)`, `price numeric`, `quantity decimal`. No version |
| `ItemRepository.Update` | `UPDATE items SET name, price, quantity`. Old values are lost; they exist only as copies on invoices |
| `ItemRepository.Delete` | Hard delete. Invoices that used the item keep their copy |
| `invoices.items_data` | JSON `List<InvoiceItemModel>`: `Id`, `Name`, `Price`, `Quantity`, `Comments` |
| `InvoiceService.Update` | `Validate` → `ItemService.GetWithValidation`, then `MapItemToInvoiceItem` copies name and price from the **current** item for every line. Saving an old invoice to fix a comment re-prices it |
| `InvoiceItemRequest` (BillerContracts 5.0.0) | `Id`, `Quantity`, `Comments` |
| `ItemController` | `GET {id}`, `GET`, `POST`, `PUT`, `DELETE {id}` |
| Last migration | `035-Migrate-customers_invoices_to_series.sql` |

## How it works

A line is **(item id, version, quantity, comments)**. Name and price are copied from that version
into the line, as today, so reads and PDFs don't change.

| Action | What the client sends | Result |
|---|---|---|
| Edit an item's name or price | `PUT /items` | New row in `item_versions`. No invoice changes |
| Save an invoice, lines untouched | each line with the version it has | Same name, price and total |
| Refresh a line (button) | the item's current version (`ItemResponse.Version`) | Line gets the newest name and price |
| Pick another version | any version from `GET /items/{id}/versions` | Line gets that version's name and price |
| New invoice | `Version` left empty | Current version |
| Delete an item | `DELETE /items/{id}` | Item and its versions are gone. Invoices keep their copy |

No refresh endpoint: refresh and pick are the same `PUT /invoices`, with a different version number.
The front end shows "newer version available" by comparing the line's version with the item's.

## Database, before and after

Example: item A "Programming" was 50, is edited to 60. One invoice used it at 50.

Now, `items` holds only the latest values; 50 is gone from it:

| id | customer_id | name | price | quantity |
|---|---|---|---|---|
| A | C1 | Programming | 60 | 1 |

After, `items` holds what has no versions. `name` and `price` are gone:

| id | customer_id | quantity |
|---|---|---|
| A | C1 | 1 |

New table `item_versions`. Rows are only inserted, never updated. The highest `version` is the
item's current name and price:

| id | item_id | version | name | price | changed_date |
|---|---|---|---|---|---|
| V1 | A | 1 | Programming | 50 | 2026-03-01 09:00 |
| V2 | A | 2 | Programming | 60 | 2026-11-10 14:30 |

`invoices.items_data` (JSON, no new column): each line gets one new key.

```
now:   {"Id":"A","Name":"Programming","Price":50.0,"Quantity":10.0,"Comments":null}
after: {"Id":"A","Name":"Programming","Price":50.0,"Quantity":10.0,"Comments":null,"Version":1}
```

`item_versions.version` is unique with `item_id`. It is the "v2" the page shows and the number the
API takes, and "latest" is the highest number. `changed_date` alone can't do that: the rebuilt
versions (decision 4) have only approximate dates.

## Design decisions

1. **Split, not copy.** Name and price are stored once, in `item_versions`. `items` has no
   `version` counter to keep in step. `quantity` and `customer_id` are not versioned: a quantity
   edit makes no version.
2. **Lines keep their copy and add `Version`.** `InvoiceItemModel.Version` is stored in
   `items_data`. Reading name and price through a join was rejected: it changes every invoice read
   and the PDF path, and a deleted item's invoice couldn't be read.
3. **`PUT /invoices` requires `Version` on every line** (400 "Please specify item version"), so a
   save can never re-price by accident. `POST /invoices` allows an empty version = current.
4. **Versions are rebuilt from existing invoices in the migration.** Every distinct
   (item, name, price) found on an invoice line becomes a version, oldest invoice first, dated by
   that invoice. The item's current name and price are always the last version. Every existing line
   of an item that still exists gets the `Version` it matches, so old prices are pickable.
   - Rebuilt `changed_date`s are "first invoiced on", not the real edit time. The changeset comment
     says so.
5. **Deleting an item is a hard delete, as today.** Its versions go with it
   (`item_versions.item_id` is `ON DELETE CASCADE`). Invoices that used it keep their copy, so
   they still read and print. Such a line can't be saved again with `PUT /invoices` (400), which is
   what happens today: remove the line or keep the invoice as it is. Its `Version` then points to
   nothing, and lines of items deleted before the migration have none, so the response field is
   `int?`.
6. **Writes stay one statement each** (data-modifying CTE): add inserts the item and version 1;
   update sets `quantity` and inserts a version only when `(name, price)` differs from the latest.
7. **Line validation uses `item_versions`.** A line is valid when the item belongs to the invoice's
   customer and the version exists. An empty version means the latest.
8. **BillerContracts 6.0.0** (5.0.0 now): `PUT /invoices` gains a required field. Biller and
   BillerFrontEnd deploy together.

## Dependencies

- None open. BIL-65 (series), which blocked this issue, is Done.
- BIL-75 (units) touches the same lines: when it lands, `unit_id` goes into `item_versions`.

## Step 0: Setup

- Run `_sync-repos`. Branch `BIL-85-item-versions` in all four repos.
- Update the BIL-85 description in Jira to this design.

## Step 1: BillerContracts 6.0.0

- `ItemResponse`: add `int Version`.
- New `ItemVersionResponse` (`ItemId`, `Version`, `Name`, `Price`, `ChangedDate`) and
  `ItemVersionListResponse` (`Versions`).
- `InvoiceItemRequest`: add `int? Version`. `InvoiceItemResponse`: add `int? Version`.
- `BillerContracts.Clients`: `IItemClient.GetVersions(Guid id)`.
- Publish `6.0.0-preview.<run>` from the branch for CI, 6.0.0 after merge.

## Step 2: Migrations (`_add-db-migration` skill; 036 is the next free number)

Three changesets, as 034/035 did (schema, data, cleanup). Each has its own rollback.

`036-Create-table-item_versions.sql`

```sql
CREATE TABLE item_versions (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    item_id uuid NOT NULL,
    version int NOT NULL,
    name character varying(255) NOT NULL,
    price numeric NOT NULL,
    changed_date timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (id),
    CONSTRAINT fk_item_versions_item_id FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE,
    CONSTRAINT uq_item_versions_item_id_version UNIQUE (item_id, version)
);
-- rollback DROP TABLE item_versions;
```

`037-Migrate-items_to_item_versions.sql`, in this order:

1. Insert versions, for items that exist: distinct `(item_id, name, price)` from
   `jsonb_array_elements(items_data::jsonb)`, plus each item's current `(name, price)` when no line
   has it. Number them with
   `ROW_NUMBER() OVER (PARTITION BY item_id ORDER BY is_current, first_used, name, price)`, so the
   current state is last. `changed_date` = the first invoice's `created_date`, or `now()`.
2. Stamp every line that has a matching version:
   `jsonb_agg(CASE WHEN v.version IS NULL THEN l.value ELSE l.value || jsonb_build_object('Version', v.version) END ORDER BY l.ord)`
   over `jsonb_array_elements(...) WITH ORDINALITY`, with a `LEFT JOIN` so a line can never be
   dropped, and only where the invoice has lines.
- Rollback: strip `Version` from every line (`l.value - 'Version'`), `DELETE FROM item_versions`.

`038-Alter-table-items_drop_name_price.sql`

```sql
ALTER TABLE items DROP COLUMN name, DROP COLUMN price;
-- rollback ALTER TABLE items ADD COLUMN name character varying(255), ADD COLUMN price numeric;
-- rollback UPDATE items i SET name = v.name, price = v.price FROM item_versions v
-- rollback WHERE v.item_id = i.id AND v.version = (SELECT MAX(version) FROM item_versions WHERE item_id = i.id);
-- rollback ALTER TABLE items ALTER COLUMN name SET NOT NULL, ALTER COLUMN price SET NOT NULL;
```

Checks on a copy of production:
- Before: lines with a null `Id`, `Name` or `Price`; lines whose item is not in `items` (they stay
  without a version); invoices with no lines.
- After: every item has at least one version; each item's latest version equals the name and price
  it had before; the only lines without `Version` are those whose item is not in `items`;
  `SUM(total_price)` and the invoice count unchanged.

## Step 3: Domain and Infrastructure (`_add-api-feature` skill, mirror Item)

- `src/Domain/Entities/ItemEntity.cs`: add `int Version`. Still has `Name` and `Price`, filled by
  the join. New `ItemVersionEntity` (`Id`, `ItemId`, `Version`, `Name`, `Price`, `ChangedDate`, and
  from the join `CustomerId`).
- `src/Domain/Repositories/IItemRepository.cs`: `GetVersions(Guid itemId)`,
  `GetVersions(List<Guid> itemIds)`.
- `src/Infrastructure/Repository/ItemRepository.cs`:

```sql
-- shared by Get(id), Get(ids) and the paged Get
SELECT i.id, i.customer_id, i.quantity, v.version, v.name, v.price
FROM items i JOIN item_versions v ON v.item_id = i.id
WHERE v.version = (SELECT MAX(version) FROM item_versions WHERE item_id = i.id)

-- Add
WITH new_item AS (INSERT INTO items (customer_id, quantity) VALUES (@CustomerId, @Quantity) RETURNING id)
INSERT INTO item_versions (item_id, version, name, price)
SELECT id, 1, @Name, @Price FROM new_item RETURNING item_id;

-- Update
WITH updated AS (UPDATE items SET quantity = @Quantity WHERE id = @Id RETURNING id),
latest AS (SELECT version, name, price FROM item_versions WHERE item_id = @Id ORDER BY version DESC LIMIT 1)
INSERT INTO item_versions (item_id, version, name, price)
SELECT updated.id, latest.version + 1, @Name, @Price FROM updated, latest
WHERE (latest.name, latest.price) IS DISTINCT FROM (@Name, @Price);

-- Delete: unchanged (DELETE FROM items WHERE id=@Id); the foreign key removes the versions
```

## Step 4: Application

- `ItemModel.Version`, new `ItemVersionModel`, mapping extensions in
  `src/Application/MappingProfiles/ItemMappingProfile.cs` (every property by hand).
- `InvoiceItemModel`: `int? Version`. `ToInvoiceItemModel(InvoiceItemRequest)` and
  `ToInvoiceItemResponse` map it.
- `src/Application/Services/ItemService.cs`:
  - `GetVersions(Guid id)`: `NotFoundException` when there are no rows.
  - `GetVersionsWithValidation(lines, customerId)` replaces `GetWithValidation` for invoices. One
    query (`GetVersions(itemIds)`), then per line in C#: unknown item or another customer's →
    today's message `Items id … is invalid for customer id …`; empty version → the latest; a number
    that doesn't exist → `Item id {id} has no version {n}`.
- `src/Application/Services/InvoiceService.cs`:
  - `Validate` calls `GetVersionsWithValidation`.
  - `MapItemToInvoiceItem` matches on item id **and** version, and sets `Name`, `Price`, `Version`.
  - `Get`, the paged `Get` and `GeneratePDF` don't change.

## Step 5: Validators and WebAPI

- `src/Validators/Invoice/InvoiceUpdateValidator.cs`: every line needs `Version >= 1`.
  `InvoiceItemValidator`: a given version must be `>= 1`.
- `src/WebAPI/Controllers/ItemController.cs`: `[HttpGet("{id}/versions")]` →
  `ItemVersionListResponse`, with a Swagger example. Update the item and invoice examples for `Version`.

## Step 6: Rules

- `CLAUDE.md`: replace "Saving the invoice itself (`PUT /invoices`) copies line names and prices from
  the current items." with "A line names the item version it was saved with. `PUT /invoices` copies
  name and price from the version each line names, and never moves a line to another version by
  itself. Item names and prices live in `item_versions`, whose rows are never updated."
- `REVIEW.md`: add "A `PUT /invoices` changes a line's name or price though the request kept its
  version" and "An `item_versions` row is updated, or deleted while its item still exists."

## Step 7: Tests

Unit (`_write-tests` skill):
- `ItemServiceTest`: `GetVersions` (ordered, empty → 404); `GetVersionsWithValidation` (empty
  version → latest; explicit old version; unknown version; other customer's item; unknown item).
- `InvoiceServiceTest`: `Update` with the stored version keeps name and price after the item
  changed; with the current version takes the new ones; with an older version takes those; the same
  item on two lines at two versions maps each correctly. `Add` with an empty version stores the
  current version.
- Validators, mapping (`Version` round-trip through `items_data`, and a line without `Version`
  reads as `null`), controller, Swagger examples, DI.

## Step 8: Integration tests (BillioIntegrationTest, same branch; responses only, no PDF content)

- Item: create, change price twice, save unchanged, change only quantity → 3 versions, `GET` shows
  the latest and the new quantity. Delete → `GET /items/{id}` and its versions 404.
- Invoice: edit the item, then `PUT` unchanged → line and total unchanged; `PUT` with the current
  version → new price; `PUT` with version 1 → old price; `PUT` without a version or with version 99
  → 400; delete the item → `GET` still shows the line with its name and price.
- `UpdateRequestFromInvoice` copies `Version` from the response.

## Step 9: BillerFrontEnd (separate PR, its own `CLAUDE.md`)

- `Invoices_update.razor`: each line shows its stored name, price and version. A version drop-down
  per line (from `GetVersions`), a "Refresh" button per line and "Refresh all", shown when the
  item's version is newer. Row prices and the total come from the selected versions, not the
  current items. Nothing changes until Save. A line whose item was deleted shows its stored name
  and price without a version choice.
- `Items_id.razor`: version table.
- Update its `CLAUDE.md`/`REVIEW.md` line about the total coming from current item prices.

## Verification

1. `bash .claude/skills/_test-coverage/scripts/build.sh` and
   `bash .claude/skills/_test-coverage/scripts/run-coverage.sh`: build and unit tests pass.
2. `_test-stack`: `test-stack.sh up` from master, seed an invoice, edit its item and delete another
   used item; switch to the branch and migrate; `run-integration.sh`. Then `rollback-count 3` and
   `update`: items, lines and totals identical each way.
3. On a copy of production (as in BIL-86 step 10): save `GET /items`, `GET /invoices` (all pages)
   and every invoice PDF as text before; migrate; save again and diff. Expected difference: only the
   new `version` fields. Run the checks from step 2.
4. In the browser against the throwaway stack: edit an item's price, open an old invoice (old price,
   "newer version" shown), save unchanged (total same), refresh (new price), pick version 1 (old price).
5. `_pre-pr` before each PR. Deploy Biller and BillerFrontEnd together with `make update`.

## Out of scope

- Saving an invoice that still has a line of a deleted item (400 today and after).
- Versions for sellers, customers and users (invoices show their current data, by BIL-86).
- Units (BIL-75): when it lands, `unit_id` goes into `item_versions` next to name and price.
- Moving lines from `items_data` JSON into an `invoice_items` table.
- Restoring an item to an old version.

## Order of work

1. Step 0, then BillerContracts (preview package).
2. Biller PR: steps 2–7.
3. BillioIntegrationTest PR, then BillerFrontEnd PR, same branch name.
4. Verification 3 on a production copy, then deploy Biller and BillerFrontEnd together.

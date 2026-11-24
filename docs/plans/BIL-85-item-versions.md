# Plan: item versions that invoice lines are pinned to (BIL-85)

Jira: [BIL-85](https://aldisadom.atlassian.net/browse/BIL-85). Epic: BIL-89 (Billeris). Replaces
the earlier `item_history` design of this issue (a copy of every item state next to `items`).

## Open questions

None.

## Goal

- Items can be edited freely. Every name or price change is kept as a numbered version.
- An invoice line changes only when the owner refreshes it to the newest item version, or picks
  another version.

## Current state (master @ 5d96f82)

Written at f3ad202. The only master commit since, BIL-111 (`make` runs the API as `Prod`, the test
stack and CI as `Dev`), touches none of the files below.

| Where | What |
|---|---|
| `items` (006, 025, 027) | `id`, `customer_id`, `name varchar(255)`, `price numeric`, `quantity decimal`. No version |
| `ItemRepository.Get` | `SELECT * FROM items`. The paged `Get` orders by `name, id` and ends with `COUNT(*) OVER() AS total_count`, which `PagedQuery.QueryPage` splits on |
| `ItemRepository.Update` | `UPDATE items SET name, price, quantity`. Old values are lost; they exist only as copies on invoices |
| `ItemRepository.Delete` | Hard delete. Invoices that used the item keep their copy |
| `invoices.items_data` | `TEXT` (026): Newtonsoft JSON of `List<InvoiceItemModel>`: `Id`, `Name`, `Price`, `Quantity`, `Comments` |
| `InvoiceService.Update` | `Validate` → `ItemService.GetWithValidation` (its only caller), then `MapItemToInvoiceItem` copies name and price from the **current** item for every line. Saving an old invoice to fix a comment re-prices it |
| `PUT /invoices` validation | `InvoiceUpdateValidator` (each line through `InvoiceItemValidator`, shared with `POST`), then `InvoiceModelUpdateValidator` on the mapped `InvoiceModel` (`InvoiceController.cs:147`), which repeats the line rules |
| `ItemMappingProfile.cs` | Item mapping both ways, including `ItemResponse.ToModel` |
| `InvoiceItemRequest` (BillerContracts 5.0.0) | `Id`, `Quantity`, `Comments`. Biller, BillerFrontEnd and BillioIntegrationTest all use 5.0.0 |
| `ItemController` | `GET {id}`, `GET`, `POST`, `PUT`, `DELETE {id}` |
| Last migration | `035-Migrate-customers_invoices_to_series.sql` |
| Tests that expect today's re-pricing | Biller `InvoiceServiceTest.Update_GivenValidInvoice_KeepsStoredUserStatusSeriesAndNumberAndCopiesCurrentItemPrices`; BillioIntegrationTest `InvoiceKeepsItemPriceUntilUpdated_Valid_Success` (after an item edit, `PUT` gives the line the new name and price) |
| BillioIntegrationTest `PUT` lines | `UpdateRequestFromInvoice` copies lines from the response. `InvoiceIdsUpdate_Invalid_Fail`, `InvoiceUpdateChangeItems_Valid_Success`, `InvoiceUpdateChangeCustomer_Invalid_Fail` and `InvoiceUpdateChangeSeller_Invalid_Fail` build their own lines |
| BillerFrontEnd `Invoices_update.razor` | Line prices and the total from the current items. `MappingProfiles/InvoiceMappingProfile.ToUpdateRequest(InvoiceItemResponse)` copies `Id`, `Quantity`, `Comments`. `ItemChange` sets only `Id` when a line's item is picked or a line is added |
| `_test-stack` | A stack runs the migrations of the one checkout it was started from. No command migrates a running stack to another checkout |

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
| Delete an item | `DELETE /items/{id}` | 400 while an invoice uses it; otherwise the item and its versions are gone |

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
   that invoice. The item's current name and price are always the last version, dated no earlier
   than an older version (or `now()` when never invoiced). Every existing line
   of an item that still exists gets the `Version` it matches, so old prices are pickable.
   - Rebuilt `changed_date`s are "first invoiced on", not the real edit time. The changeset comment
     says so.
5. **An item that an invoice line uses can't be deleted** (changed after review, owner's decision:
   it was a hard delete). `DELETE /items/{id}` is 400 "Item id {id} is used by invoices" while any
   invoice's `items_data` has a line with that id; the check is in the `DELETE` statement itself.
   Once no invoice uses it, delete is a hard delete and its versions go with it
   (`item_versions.item_id` is `ON DELETE CASCADE`). Items deleted before this change can still
   have lines on invoices: those read and print from their copy, can't be saved again with
   `PUT /invoices` (400), and lines of items deleted before the migration have no `Version`, so the
   response field is `int?`.
6. **Writes stay one statement each** (data-modifying CTE): add inserts the item and version 1;
   update sets `quantity` and inserts a version only when `(name, price)` differs from the latest.
7. **Line validation uses `item_versions`.** A line is valid when the item belongs to the invoice's
   customer and the version exists. An empty version means the latest.
8. **BillerContracts 6.0.0** (5.0.0 now): `PUT /invoices` gains a required field. Biller and
   BillerFrontEnd deploy together.

## Dependencies

- None open. BIL-65 (series), which blocked this issue, is Done.
- BIL-75 (units) touches the same lines: when it lands, `unit_id` goes into `item_versions`. Its
  plan (`BIL-75-units.md`, master @ 125ae5e) still puts `unit_id` on `items` and uses an
  `InvoiceItemUpdateRequest` that doesn't exist; check it with `_write-plan` after this one merges.

## Step 0: Setup (done)

- [x] Run `_sync-repos`. Branch `BIL-85-item-versions` in all four repos.
- [x] Update the BIL-85 description in Jira to this design.

## Step 1: BillerContracts 6.0.0 (done: PR #23 merged, 6.0.0 published)

- `ItemResponse`: add `int Version` (the version it shows).
- New `ItemVersionListResponse` (`ItemId`, `Versions`; not paged, so no `PagedResponse`) of
  `ItemVersionResponse` (`Version`, `Name`, `Price`, `ChangedDate`). The item id is on the list
  once, not on each version (user's decision).
- `InvoiceItemRequest`: add `int? Version`. `InvoiceItemResponse`: add `int? Version`.
- `BillerContracts.Clients`, each with a test in `Clients.Tests/ItemClientTest.cs`:
  - `IItemClient.GetVersions(Guid id)` → `GET {id}/versions`.
  - `IItemClient.Get(Guid id, int version)` → `GET {id}?Version=n`, one version of the item
    (user's decision). An overload, not an optional parameter, so `Get(id)` calls and mocks stay.

## Step 2: Migrations (done; `_add-db-migration` skill; 036 is the next free number)

Three changesets: schema, data, cleanup. Each has its own rollback.

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
   current state is last. `changed_date` = the first invoice's `created_date`; for the current
   version, the latest such date of any of the item's pairs when it was invoiced, else `now()`; every
   rebuilt date is capped at `now()`, since `created_date` can be in the future (review rounds 1–3:
   dates never go back as versions go up). The
   current version takes its name and price from `items`, not from a line: `numeric` 50 and 50.0
   are equal, so a line's copy could change how `GET /items` prints the price.
2. Stamp every line that has a matching version:
   `jsonb_agg(CASE WHEN v.version IS NULL THEN l.value ELSE l.value || jsonb_build_object('Version', v.version) END ORDER BY l.ord)`
   over `jsonb_array_elements(...) WITH ORDINALITY`, with a `LEFT JOIN` so a line can never be
   dropped, and only where the invoice has lines. `items_data` is `TEXT`: read it as
   `items_data::jsonb` and write the result back with `::text`. jsonb reorders keys and adds
   spaces; Newtonsoft reads it the same.
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

## Step 3: Domain and Infrastructure (done; `_add-api-feature` skill, mirror Item)

- `src/Domain/Entities/ItemEntity.cs`: add `int Version`. Still has `Name` and `Price`, filled by
  the join. New `ItemVersionEntity` (`Id`, `ItemId`, `Version`, `Name`, `Price`, `ChangedDate`, and
  from the join `CustomerId`).
- `src/Domain/Repositories/IItemRepository.cs`: `Get(Guid id, int? version)` replaces `Get(Guid id)`
  (user's decision: one method, `null` = the latest; one query with
  `v.version = COALESCE(@Version, <latest>)`), plus `GetVersions(Guid itemId)` and
  `GetVersions(List<Guid> itemIds)`.
- `src/Infrastructure/Repository/ItemRepository.cs`:

Both tables have `id`, so every column is qualified: `WHERE i.id = @Id`, `i.id = ANY(@Ids)`,
and the paged `Get` orders by `v.name, i.id` and keeps `COUNT(*) OVER() AS total_count` as the
last column. The latest-version condition sits in the join, so each query keeps its own `WHERE`.

```sql
-- shared by Get(id), Get(ids) and the paged Get
SELECT i.id, i.customer_id, i.quantity, v.version, v.name, v.price
FROM items i JOIN item_versions v ON v.item_id = i.id
    AND v.version = (SELECT MAX(version) FROM item_versions WHERE item_id = i.id)

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

-- Delete (decision 5): returns whether a row went; the foreign key removes the versions
DELETE FROM items WHERE id=@Id
  AND NOT EXISTS (SELECT 1 FROM invoices
                  WHERE items_data::jsonb @> jsonb_build_array(jsonb_build_object('Id', @Id)));
```

## Step 4: Application (done)

- `ItemModel.Version`, new `ItemVersionModel`, mapping extensions in
  `src/Application/MappingProfiles/ItemMappingProfile.cs` (every property by hand, `ItemResponse.ToModel`
  too).
- `ItemService.Get(Guid id, int? version)` replaces `Get(Guid id)` (user's decision): `null` = the
  latest; `NotFoundException` `Invoice item:{id} not found`, or `Invoice item:{id} version {n} not found`
  when a version was asked for. The controller passes its query value straight through.
- `InvoiceItemModel`: `int? Version`. `ToInvoiceItemModel(InvoiceItemRequest)` and
  `ToInvoiceItemResponse` map it.
- `src/Application/Services/ItemService.cs`:
  - `GetVersions(Guid id)`: `NotFoundException` when there are no rows.
  - `GetVersionsForCustomer(items, customerId)` replaces `GetForCustomer` for invoices. One
    query (`GetVersions(itemIds)`), then per line in C#: unknown item or another customer's →
    today's message `Items id … is invalid for customer id …`; empty version → the latest; a number
    that doesn't exist → `Item id {id} has no version {n}`. Both throw `ValidationException`
    (BIL-112, merged from master: ownership checks throw instead of returning `Result`).
  - `GetForCustomer` goes, from `IItemService` too: `InvoiceService.Validate` is its only caller.
- `src/Application/Services/InvoiceService.cs`:
  - `Validate` calls `GetVersionsForCustomer`.
  - `MapItemToInvoiceItem` gives a line with an empty version its item's latest version number, so
    every saved line has one, then matches on item id **and** version and sets `Name` and `Price`.
  - `Get`, the paged `Get` and `GeneratePDF` don't change.

## Step 5: Validators and WebAPI (done)

- `src/Validators/Invoice/InvoiceUpdateValidator.cs`: every line needs a version ("Please specify
  item version for {id}"). `InvoiceItemValidator` (`POST` and `PUT` lines): a given version must be
  `>= 1`. `InvoiceModelUpdateValidator` (runs on `PUT` after the request validator and repeats its
  line rules): every line needs `Version >= 1`.
- `src/WebAPI/Controllers/ItemController.cs`:
  - `GET {id}` takes an optional `[FromQuery] int? version`: empty → the latest, a number → that
    version (user's decision); 404 when the item has no such version.
  - `[HttpGet("{id}/versions")]` → `ItemVersionListResponse` with `ItemId`, with a Swagger example.
  - Update the item examples and the invoice update example for `Version`.

## Step 6: Rules (done)

- `CLAUDE.md`: replace "Saving the invoice itself (`PUT /invoices`) copies line names and prices from
  the current items." with "A line names the item version it was saved with. `PUT /invoices` copies
  name and price from the version each line names, and never moves a line to another version by
  itself. Item names and prices live in `item_versions`, whose rows are never updated."
- `REVIEW.md`: add "A `PUT /invoices` changes a line's name or price though the request kept its
  version" and "An `item_versions` row is updated, or deleted while its item still exists."
- `README.md` / `docs/README_lt.md`: no change. The new endpoint is in the existing items group, and
  no command, setting, port, package or layout changes.

## Step 7: Tests (done)

Unit (`_write-tests` skill):
- `ItemServiceTest`: `GetVersions` (ordered, empty → 404); `GetVersionsForCustomer` (empty
  version → latest; explicit old version; unknown version; other customer's item; unknown item).
- `InvoiceServiceTest`: `Update` with the stored version keeps name and price after the item
  changed; with the current version takes the new ones; with an older version takes those; the same
  item on two lines at two versions maps each correctly. `Add` with an empty version stores the
  current version.
- Existing tests that change: `Update_GivenValidInvoice_KeepsStoredUserStatusSeriesAndNumberAndCopiesCurrentItemPrices`
  copies the prices of each line's version instead; every `InvoiceServiceTest` setup of
  `GetForCustomer` moves to `GetVersionsForCustomer`; `ItemServiceTest`'s
  `GetForCustomer` tests go with the method.
- Validators, mapping (`Version` round-trip through `items_data`, and a line without `Version`
  reads as `null`), controller, Swagger examples, DI.

## Step 8: Integration tests (done; BillioIntegrationTest, same branch; responses only, no PDF content)

- Item: create, change price twice, save unchanged, change only quantity → 3 versions, `GET` shows
  the latest and the new quantity; `GET ?version=1` shows the first name and price, `?version=9`
  404. Delete → its versions 404.
- Invoice: edit the item, then `PUT` unchanged → line and total unchanged; `PUT` with the current
  version → new price; `PUT` with version 1 → old price; `PUT` without a version or with version 99
  → 400. Deleting the item while the invoice uses it → 400 "Item id {id} is used by invoices", item,
  versions and invoice unchanged; after the invoice is deleted, the item can be
  (`ItemDelete_WhenUsedByInvoice_Fail`). `CustomerDelete_WhenHasInvoice_Fail` deleted the item first,
  which is now refused: it became `CustomerDelete_WhenHasItemAndInvoice_Fail`, and Postgres reports
  the items constraint first (`fk_invoice_customers_id`).
- `UpdateRequestFromInvoice` copies `Version` from the response.
- `InvoiceKeepsItemPriceUntilUpdated_Valid_Success` asserted today's re-pricing: it became
  `InvoiceKeepsItemVersionUntilChanged_Valid_Success` (unchanged `PUT` keeps the old name and price;
  version 2 takes the new ones; version 1 brings the old ones back). The 400 cases are
  `InvoiceUpdateLineVersion_Invalid_Fail` (own data "Rickon": each own-data test needs its own name,
  or parallel runs collide on the user's email).
- Lines built by hand in `PUT` tests get the item's `Version` (from the saved `ItemResponse`), or
  the validator's "Please specify item version" replaces the error they expect or fails the save:
  `InvoiceIdsUpdate_Invalid_Fail`, `InvoiceUpdateChangeItems_Valid_Success`,
  `InvoiceUpdateChangeCustomer_Invalid_Fail`, `InvoiceUpdateChangeSeller_Invalid_Fail`.

## Step 9: BillerFrontEnd (done; separate PR, its own `CLAUDE.md`)

- `Invoices_update.razor`: each line shows its stored name, price and version. A version drop-down
  per line (from `GetVersions`), a "Refresh" button per line and "Refresh all", shown when the
  item's version is newer. Row prices and the total come from the selected versions, not the
  current items. Nothing changes until Save. A line whose item was deleted shows its stored name
  and price without a version choice.
  - `ItemChange`: a line whose item is picked or changed, and a new line, get that item's current
    `Version`. Otherwise `PUT` gets no version (400), or the old item's version number for another
    item (400, or silently an old version of the new item).
- `MappingProfiles/InvoiceMappingProfile.ToUpdateRequest(InvoiceItemResponse)` copies `Version`;
  its test in `tests/FrontEnd.Tests/MappingProfiles/InvoiceMappingProfileTest.cs`.
- `Items_id.razor`: version table. `ItemVersionResponse.ChangedDate` arrives as UTC (`timestamptz`
  through Npgsql). Shown as UTC with a "UTC" label, not with `ToLocalTime()`: Blazor Server runs it
  in the container, which sets no time zone, so it would print UTC as if it were local.
- `Invoices_update.razor` loads every version of the customer's items (`GetVersions` per item) on
  open; a line of a deleted item keeps its stored name in its item list and its stored price.
- Update its `CLAUDE.md`/`REVIEW.md` line about the total coming from current item prices.

## Verification

1. `bash .claude/skills/_test-coverage/scripts/build.sh` and
   `bash .claude/skills/_test-coverage/scripts/run-coverage.sh`: build and unit tests pass.
2. `_test-stack` from the branch (a stack runs one checkout's migrations, so rows written by
   master's code are checked in 3): `test-stack.sh up --seed`; change the seeded item's price once;
   add a second item, put it on a new invoice and delete the item. Save `GET /items`,
   `GET /items/{id}/versions` and `GET /invoices`. `test-stack.sh rollback-check --count 3 --table items`
   rolls 038–036 back and applies them again, so 037 rebuilds the versions from those rows. Save
   again and diff. Expected differences: rebuilt `changedDate`s, and the deleted item's line loses
   its `version` (no item to rebuild it from). Then `run-integration.sh --repeat 3`.
   Done on 2026-11-18: exactly those differences, plus a rebuilt old version's price printing as
   `10.0` instead of `10` (it comes from the line's JSON copy; equal value). Integration: 3 runs of
   266, 0 failed.
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

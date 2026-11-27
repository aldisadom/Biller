# Plan: item versions that invoice items are pinned to (BIL-85)

Jira: [BIL-85](https://aldisadom.atlassian.net/browse/BIL-85). Epic: BIL-89 (Billeris). Replaces
the earlier `item_history` design of this issue (a copy of every item state next to `items`).

Re-planned on 2026-11-27 for master after BIL-113 (#92, eb845fe): invoice items are rows in
`invoice_items` and show their item's current name and price; `invoices.items_data` and
`total_price` are gone. The first version of this plan built versions from the `items_data` JSON
lines and stamped a `Version` key into them; that can't work any more (decisions 2 and 4), and its
migrations 036–038 clash with master's numbers. What the branch keeps and redoes is in
"Branch rework"; what the sibling branches keep, redo and merge is in steps 1, 8 and 9 (checked
against their code on 2026-11-27).

## Open questions

None. The one open question (drop `items.name` and `items.price` now or in a later release) was
answered on 2026-11-27: decision 11.

## Goal

- Items can be edited freely. Every name or price change is kept as a numbered version.
- An invoice item changes only when the owner refreshes it to the newest item version, or picks
  another version. Editing an item doesn't change an existing invoice, its total or its PDF.

## Current state (master @ bfbd1a7)

The master commits after BIL-113, 1112a12 (revert of BIL-110) and bfbd1a7 (BIL-115/116,
`run-integration.sh` reports), touch only skills (`_test-stack` and its scripts,
`_add-api-feature`, `_add-db-migration`). BIL-113 also made `billio-webapi` wait for the migrations
(`docker-compose.yml`, both READMEs).

| Where | What |
|---|---|
| `items` (006, 025, 027) | `id`, `customer_id`, `name varchar(255)`, `price numeric`, `quantity decimal`, all `NOT NULL`. Foreign key to `customers` is `fk_invoice_customers_id`. No version |
| `invoice_items` (036, BIL-113) | Key `(invoice_id, line_no)`; `item_id NOT NULL` with `fk_invoice_items_item_id` to `items` (no cascade); `quantity`, `comments`; checks `line_no >= 1`, `quantity > 0`; index `idx_invoice_items_item_id`. No name, price or version |
| `invoices` (038, BIL-113) | `items_data` and `total_price` dropped. 038's rollback rebuilds both from `invoice_items` with `items.name` and `items.price`. 037 took BIL-113's decision 4a: no archived items, no `items.archived` |
| Last migration | `038-Alter-table-invoices_drop_items_data_total_price.sql`. Liquibase `includeAll` of `migrations/sql/` |
| `InvoiceRepository` | `ItemsSql`: `SELECT ii.*, it.name, it.price FROM invoice_items ii JOIN items it ...` for one invoice or a page (`LoadItems`). `Add`: `reserved` → `inv` → `items_added` from `unnest(@ItemIds, @Quantities, @ItemComments) WITH ORDINALITY`. `Update`: upsert lines `1..n` `ON CONFLICT (invoice_id, line_no)`, delete above `n`. Arrays built by `WithItems` |
| `InvoiceItemEntity` | `InvoiceId`, `LineNo`, `ItemId`, `Quantity`, `Comments`; `Name`, `Price` filled only by the read join |
| `InvoiceModelExtensions` | `ToInvoiceItemModel(InvoiceItemEntity)` and `ToInvoiceItemEntity(model, invoiceId, lineNo)` (item, quantity, comments only); request and response mappings without `Version` |
| `InvoiceService` | `MapItemToInvoiceItem` is gone. `Validate` calls `ItemService.GetForCustomer` only as a check and returns the customer |
| `ItemRepository`, `IItemRepository` | Unchanged by BIL-113: `SELECT * FROM items` for `Get(id)`, `Get(ids)` and the paged `Get` (`ORDER BY name, id`, `total_count` last); `Add`/`Update` write `name`, `price`, `quantity`; `Delete` is a plain `DELETE` returning `Task` |
| `ItemService`, `IItemService`, `ItemController`, validators | Unchanged since the branch's merge base: `Get(Guid id)`, `Get(List<Guid>)`, `GetForCustomer`; `Delete` checks only that the item exists |
| Item delete while an invoice uses it | `fk_invoice_items_item_id` fails; `ErrorChecking` gives 400 "Validation failure", "Can not delete (please clear all dependants) or update (item not found): invoice_items_item_id" |
| BillerContracts | Biller master on 5.0.0. 6.0.0 is published (`ItemVersionListResponse`, `ItemVersionResponse`, `Version` on items and invoice items; seen in the local NuGet cache) |
| `CLAUDE.md` (Conventions) | BIL-113's paragraph: invoice items show the item's current name and price; "Until BIL-85, editing an item changes every invoice that uses it ... BIL-85 brings back 'editing an item must not change an existing invoice or its PDF'" |
| `REVIEW.md` | "An old invoice or its PDF changes after a unit is edited (items: allowed until BIL-85, see `CLAUDE.md`)." |
| `CI.yaml` | `COVERAGE_MIN_LINE: 93.0`, `COVERAGE_MIN_BRANCH: 88.5` |
| Tests | `InvoiceServiceTest` matches saved entities with `Saved(expected)` (record compare plus invoice items one by one). `Add_GivenValidData_SavesCustomersSeriesAndInvoiceItemsWithoutItemNamesOrPrices`, `Update_GivenValidInvoice_KeepsStoredUserStatusSeriesAndNumberAndSavesInvoiceItemsWithoutItemPrices`. `InvoiceMappingProfileTest`: `ToInvoiceItemModel_GivenInvoiceItemEntity_MapsAllProperties`, `ToInvoiceItemEntity_GivenInvoiceItemModel_MapsItemQuantityAndCommentsOnly` |
| Other plans | `BIL-85-item-history.md` (old design) is still on master; the branch deletes it. `BIL-75-units.md` still snapshots the unit into `items_data`, `BIL-90-seller-invoice-summary.md` still sums `invoices.total_price`: BIL-113's follow-ups for them aren't done |
| BillerContracts | master ab1dfc8 is PR #23 (squash), published as 6.0.0. The local `BIL-85-item-versions` (5 commits, deleted on origin after the merge) has the same tree as master. Step 1 |
| BillioIntegrationTest | Master af3c2e3 (BIL-113, #35) changed `InvoiceTest.cs` and `REVIEW.md`. Branch `BIL-85-item-versions`: 3 pushed commits on 713d90e (PR #34 open, CI green against Biller's pushed branch) plus 6e58a5c, local. Master isn't merged; a trial merge conflicts in `InvoiceTest.cs`. Step 8 |
| BillerFrontEnd | Master ec39ffb hasn't moved since the branch started (BIL-113 changed nothing there). Branch: 5 commits, pushed (PR #36 open, CI green). Step 9 |

Checked on a throwaway Postgres 16 (2026-11-27), with master's tables and rows and the SQL of
039–041 below run by hand: a used item's delete still fails on `fk_invoice_items_item_id` (so the
400 above stays), an unused item's delete removes its versions, an unknown version fails on
`fk_invoice_items_item_id_version`, and the rollbacks 041–039 give master's tables back. Not run
through Liquibase (file format, precondition): that is Verification 2.

## Branch rework

Branch `BIL-85-item-versions`: 14 commits on ea10918 (BIL-112), pushed, plus 156c6a4 (item delete
400), local only. PR #90 is open, so merge `origin/master` into the branch; no rebase, no force push. The
merge conflicts in `CI.yaml`, `CLAUDE.md`, `REVIEW.md`, `InvoiceService.cs` and
`InvoiceServiceTest.cs`. Git merges the other files on its own, some of them into code that is
wrong after BIL-113 (marked "change" or "drop" below).

| Branch change | After the merge |
|---|---|
| `.claude/skills/_write-plan/SKILL.md` (Opus, xhigh), its `CLAUDE.md` "More" line, `.claude/skills/_add-api-feature/SKILL.md` (merges on its own) | Keep |
| `docs/plans/BIL-85-item-history.md` deleted | Keep |
| `src/Domain/Domain.csproj` BillerContracts 6.0.0 | Keep |
| `ItemEntity.Version`, `ItemVersionEntity`, `ItemModel.Version`, `ItemVersionModel`, `InvoiceItemModel.Version` (`int?`), `ItemMappingProfile.cs` | Keep |
| `IItemRepository`: `Get(Guid id, int? version)`, `GetVersions(Guid)`, `GetVersions(List<Guid>)`, `Get(ids)` gone | Keep. **Change** `Task<bool> Delete` back to `Task Delete` (decision 5) |
| `ItemRepository`: item reads joined to the latest or asked-for version, `Add` and `Update` CTEs, `GetVersions` | Keep. **Change** `Delete` back to master's plain `DELETE` (its `items_data` check reads a dropped column) |
| `IItemService`, `ItemService`: `Get(id, version)`, `GetVersions`, `GetVersionsForCustomer`; `Get(ids)` and `GetForCustomer` gone | Keep. **Change** `Delete` back to master's (no "Item id {id} is used by invoices") |
| `InvoiceMappingProfile.cs`: `Version` in `ToInvoiceItemModel(InvoiceItemRequest)` and `ToInvoiceItemResponse` | Keep (merges on its own). **Add** `Version` to the entity mappings (step 4) |
| `InvoiceService.cs`: `MapItemToInvoiceItem` over versions, `Validate` returning versions | **Redo** on master's file (conflict): take master's, then step 4. `MapItemToInvoiceItem` stays gone |
| Validators (`InvoiceItemValidator`, `InvoiceUpdateValidator`, `InvoiceModelUpdateValidator`) | Keep |
| `ItemController` (`GET {id}?version`, `GET {id}/versions`), Swagger examples (`InvoiceUpdateRequestExample`, `ItemListResponseExample`, `ItemResponseExample`, `ItemVersionListResponseExample`) | Keep |
| `migrations/sql/036-Create-table-item_versions.sql`, `037-Migrate-items_to_item_versions.sql`, `038-Alter-table-items_drop_name_price.sql` | **Drop** all three in the merge commit. 039 takes 036's table, 041 takes 038's drop and rollback; 037 (rebuild from `items_data`) has no successor. The review rounds on 037's dating (0bfc1eb in part, c5ef713, 4b6e7d6, c5c40b4) go with it |
| `CLAUDE.md` invoices paragraph, `REVIEW.md` lines | **Redo** on master's text (conflict): step 6 |
| `CI.yaml` 93.0 / 88.8 | **Redo** (conflict): take master's 93.0 / 88.5; set again at `_pre-pr` from the branch's new numbers |
| Tests: `ItemServiceTest`, `ItemMappingProfileTest`, validator tests, `ItemControllerTest`, `ItemSwaggerExampleTest` | Keep, except **drop** `ItemServiceTest.Delete_GivenItemUsedByInvoice_ThrowsValidationException` and put `Delete_GivenValidId_DeletesFromRepository` back to a `Task` setup |
| `InvoiceMappingProfileTest.cs`: `Version` in the request and response tests | Keep (merges on its own). **Add** entity cases (step 7) |
| `InvoiceServiceTest.cs`: `GetVersionsForCustomer` setups, tests on `ItemsData` JSON and `MapItemToInvoiceItem` | **Redo** on master's file (conflict): step 7. The `MapItemToInvoiceItem_*` tests go |
| 156c6a4 (local) | Its code is undone in the merge (decision 5). Its `REVIEW.md` line stays, reworded (step 6) |

The merge commit builds and holds only the resolution:
- the five conflicts resolved to master's side, with the branch's `_write-plan` line kept in
  `CLAUDE.md`;
- in `InvoiceService.cs`, `Validate` switched to `GetVersionsForCustomer`; in
  `InvoiceServiceTest.cs`, master's `GetForCustomer` setups and verifies switched to
  `GetVersionsForCustomer(invoiceData.Items!, ...)`, as the branch did;
- the branch's 036–038 deleted;
- `Delete` back to master's in `IItemRepository`, `ItemRepository` and `ItemService`, with
  `ItemServiceTest`'s delete tests.

Steps 2–7 follow as their own commits. Push only when the build and the tests pass (`CLAUDE.md`).

The owner's local `billio-db` ran the branch's old 036–038 (BIL-113's plan, review F9). If it still
has them, master's and this branch's migrations don't run on it: roll those three back from the
branch as it is now, before the merge, or restore a backup from before BIL-85. Ask first: it is the
user's own stack.

## How it works

An invoice item is **(item id, version, quantity, comments)**. Its name and price are read from
that version, so the response, the total and the PDF come from one place and can't disagree.

| Action | What the client sends | Result |
|---|---|---|
| Edit an item's name or price | `PUT /items` | New row in `item_versions`. No invoice changes |
| Save an invoice, invoice items untouched | each invoice item with the version it has | Same name, price and total |
| Refresh an invoice item (button) | the item's current version (`ItemResponse.Version`) | It shows the newest name and price |
| Pick another version | any version from `GET /items/{id}/versions` | It shows that version's name and price |
| New invoice | `Version` left empty | Current version |
| Delete an item | `DELETE /items/{id}` | 400 while an invoice uses it (foreign key); otherwise the item and its versions are gone |

No refresh endpoint: refresh and pick are the same `PUT /invoices`, with a different version number.
The front end shows "newer version available" by comparing the invoice item's version with the
item's.

## Database, before and after

Example: item A "Programming" at 50, on one invoice. After the deploy it is edited to 60.

Now, `items` holds the only name and price; every invoice item shows them:

| id | customer_id | name | price | quantity |
|---|---|---|---|---|
| A | C1 | Programming | 50 | 1 |

After, `items` holds what has no versions:

| id | customer_id | quantity |
|---|---|---|
| A | C1 | 1 |

New table `item_versions`. Rows are only inserted, never updated. The highest `version` is the
item's current name and price. Version 1 is the item as it was at the deploy:

| id | item_id | version | name | price | changed_date |
|---|---|---|---|---|---|
| V1 | A | 1 | Programming | 50 | deploy time |
| V2 | A | 2 | Programming | 60 | 2026-12-10 14:30 |

`invoice_items` gets a `version` column; every existing invoice item gets 1:

| invoice_id | line_no | item_id | version | quantity | comments |
|---|---|---|---|---|---|
| I1 | 1 | A | 1 | 10 | |

The invoice still shows 50 after the edit, until a `PUT /invoices` sends version 2.

## Design decisions

1. **Split, not copy.** Name and price are stored once, in `item_versions`. `items` has no
   `version` counter to keep in step. `quantity` and `customer_id` are not versioned: a quantity
   edit makes no version.
2. **An invoice item names its version: `invoice_items.version`**, with
   `fk_invoice_items_item_id_version (item_id, version) REFERENCES item_versions (item_id, version)`
   (the existing unique key). Reads join `item_versions` on that pair for name and price; no copy
   on the invoice item (BIL-113 decision 3). BIL-113's follow-up named an `item_version_id`; the
   user asked for a version column. Reasons: it is the number the API already takes and returns
   (BillerContracts 6.0.0), and the foreign key makes the database check that the version belongs
   to the invoice item's item, which an `item_version_id` next to `item_id` wouldn't (the two could
   disagree). `fk_invoice_items_item_id` stays as it is.
3. **`PUT /invoices` requires `Version` on every invoice item** (400 "Please specify item version
   for {id}"), so a save can never re-price by accident. `POST /invoices` allows an empty version =
   the item's latest, which `InvoiceService.Add` sets from the versions `Validate` loaded.
   `InvoiceService.Update` never sets a version.
4. **Versions start from the items as they are at the deploy.** Every item gets version 1 with its
   current name and price, dated at the deploy (`changed_date` default `now()`), and every invoice
   item gets version 1. BIL-113's 038 dropped the old line names and prices, so there is no
   history to rebuild, and items have no created date. Version 1 is what master shows now, so no
   invoice, total or PDF changes at the deploy.
5. **An item that an invoice uses can't be deleted: master's foreign key does it** (BIL-113
   decision 2 and its follow-up for BIL-85, which the user asked this plan to take). The branch's
   check against `items_data`
   and its message "Item id {id} is used by invoices" go; `ItemService.Delete` and
   `ItemRepository.Delete` stay as on master, and the 400 is the middleware's "Can not delete
   (please clear all dependants) or update (item not found): invoice_items_item_id". Once no
   invoice uses the item, its versions go with it (`fk_item_versions_item_id ... ON DELETE
   CASCADE`).
6. **Writes stay one statement each.** Item add inserts the item and version 1; item update sets
   `quantity` and inserts a version only when `(name, price)` differs from the latest (both as on
   the branch). Invoice add and update are master's statements with a `version` column. Two item
   updates at the same moment can both take latest + 1; the unique key then fails one (400), and
   nothing wrong is saved (accepted, `CLAUDE.md`).
7. **Invoice item validation uses `item_versions`** (`GetVersionsForCustomer`): the item belongs
   to the invoice's customer and the version exists, else 400. An unknown version that got past
   it would still fail `fk_invoice_items_item_id_version` (400 "Key does not exist").
8. **BillerContracts 6.0.0** (published, no change). `InvoiceItemResponse.Version` stays `int?`,
   though Biller now always sets it: making it `int` would need 7.0.0 for nothing. Biller and
   BillerFrontEnd deploy together.
9. **Three changesets: schema, data, then constraint and drop (039–041).** As BIL-113's layout.
   041 has a precondition (`onFail:HALT`) that stops before the drop while an item has no version
   or an invoice item has no version, so a name and price can't be lost silently.
10. **Rollbacks give master's behaviour back.** 041's rollback puts `items.name` and `price` back
    from each item's latest version; 040's deletes every version and clears `invoice_items.version`.
    After a rollback every invoice shows its items' latest name and price again, as on master;
    older versions are lost.
11. **`items.name` and `items.price` are dropped in this PR (041), not in a later release** (user's
    decision, 2026-11-27, as for BIL-113's drop on 2026-11-24). This departs from
    `_add-db-migration`'s rule that a drop ships in a later release than the code change, once
    production has run on the new code. Reasons: 041's rollback puts both columns back from each
    item's latest version, so a rollback loses only the older versions (decision 10), never a name
    or price that master's code needs; and kept columns would go stale with the first edit (the
    new code doesn't write them), so master's code put back without 041's rollback would show old
    names and prices anyway. 041's precondition (decision 9) stops before the drop while an item
    or an invoice item has no version.

## Dependencies

- None open. BIL-65 (series) and BIL-113 (invoice items), which blocked this issue, are Done.
  BIL-112 is Done and merged into the branch.
- BIL-75 (units): with this, `unit_id` belongs in `item_versions` next to name and price. Its plan
  still snapshots the unit into `items_data`; BIL-113's follow-up updates it. Check it with
  `_write-plan` after this one merges.
- BIL-90 (seller summary): its plan sums `invoices.total_price`, which BIL-113 dropped. After this,
  it sums `ii.quantity * v.price` over `invoice_items` joined to `item_versions` on
  `(item_id, version)`.

## Step 0: Setup

- [x] Run `_sync-repos`. Branch `BIL-85-item-versions` in all four repos.
- [ ] Update the BIL-85 description in Jira to this design: invoice items get a `version` column
  (no copy, no `Version` in JSON), versions start from the items at the deploy (not rebuilt), an
  item an invoice uses can't be deleted (400, foreign key).
- [ ] Merge `origin/master` into the Biller branch ("Branch rework").

## Step 1: BillerContracts 6.0.0 (done: PR #23 merged, 6.0.0 published)

Checked against BillerContracts master ab1dfc8 (2026-11-27): every item below is there, and BIL-113
needed no contract change. Biller, BillerFrontEnd and BillioIntegrationTest reference 6.0.0 on
their BIL-85 branches (5.0.0 on their masters).

Branch: the local `BIL-85-item-versions` has the same tree as master (PR #23 was squash-merged and
its origin branch deleted). Nothing to keep, redo, merge or push. `_cleanup-branches` keeps the
branch while Biller is on `BIL-85-item-versions`; delete it after BIL-85 merges.

- `ItemResponse`: add `int Version` (the version it shows).
- New `ItemVersionListResponse` (`ItemId`, `Versions`; not paged, so no `PagedResponse`) of
  `ItemVersionResponse` (`Version`, `Name`, `Price`, `ChangedDate`). The item id is on the list
  once, not on each version (user's decision).
- `InvoiceItemRequest`: add `int? Version`. `InvoiceItemResponse`: add `int? Version`.
- `BillerContracts.Clients`, each with a test in `Clients.Tests/ItemClientTest.cs`:
  - `IItemClient.GetVersions(Guid id)` → `GET {id}/versions`.
  - `IItemClient.Get(Guid id, int version)` → `GET {id}?Version=n`, one version of the item
    (user's decision). An overload, not an optional parameter, so `Get(id)` calls and mocks stay.
  - Tests: `Get_GivenIdAndVersion_SendsVersionAsQuery`, `GetVersions_GivenId_GetsVersionsUrl`,
    `GetVersions_GivenResponse_ReturnsVersions`, `Get_GivenId_ReturnsItemWithVersion`.

## Step 2: Migrations (redo; `_add-db-migration`; numbers are taken when written, 039 is next)

`039-Create-table-item_versions.sql`: the branch's 036 table, plus the new column, nullable until
041.

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
ALTER TABLE invoice_items ADD COLUMN version integer;
-- rollback ALTER TABLE invoice_items DROP COLUMN version; DROP TABLE item_versions;
```

`040-Migrate-items_to_item_versions.sql`: version 1 of every item, and every invoice item on it,
in one statement.

```sql
WITH added AS (
    INSERT INTO item_versions (item_id, version, name, price)
    SELECT id, 1, name, price FROM items
    RETURNING item_id
)
UPDATE invoice_items ii SET version = 1
FROM added a
WHERE a.item_id = ii.item_id;
-- rollback WITH cleared AS (UPDATE invoice_items SET version = NULL) DELETE FROM item_versions;
```

`041-Alter-table-invoice_items_version_not_null_items_drop_name_price.sql` (decision 11):

```sql
-- preconditions onFail:HALT onError:HALT
-- precondition-sql-check expectedResult:0 SELECT (SELECT COUNT(*) FROM items i WHERE NOT EXISTS (SELECT 1 FROM item_versions v WHERE v.item_id = i.id)) + (SELECT COUNT(*) FROM invoice_items WHERE version IS NULL)
ALTER TABLE invoice_items
    ALTER COLUMN version SET NOT NULL,
    ADD CONSTRAINT fk_invoice_items_item_id_version
      FOREIGN KEY (item_id, version) REFERENCES item_versions (item_id, version);
ALTER TABLE items DROP COLUMN name, DROP COLUMN price;
-- rollback ALTER TABLE items ADD COLUMN name character varying(255), ADD COLUMN price numeric;
-- rollback UPDATE items i SET name = v.name, price = v.price FROM item_versions v WHERE v.item_id = i.id AND v.version = (SELECT MAX(version) FROM item_versions WHERE item_id = i.id);
-- rollback ALTER TABLE items ALTER COLUMN name SET NOT NULL, ALTER COLUMN price SET NOT NULL;
-- rollback ALTER TABLE invoice_items DROP CONSTRAINT fk_invoice_items_item_id_version, ALTER COLUMN version DROP NOT NULL;
```

- No new index: `idx_invoice_items_item_id` serves the new foreign key's lookups, and
  `uq_item_versions_item_id_version` serves `item_versions.item_id`.
- The changeset comments say: 040's `changed_date` is the deploy, not when the item was last
  edited; 041's rollback keeps only each item's latest name and price (decision 10).
- Master's 038 rollback reads `items.name` and `price`; Liquibase rolls 041 back first, which puts
  them back.

Checks on a copy of production: before, the item and invoice item counts. After, every item has
exactly one version, equal to the name and price it had; every invoice item has version 1; the
invoice count and every invoice's computed total unchanged.

## Step 3: Domain and Infrastructure (items: done, keep; invoice items: new)

- Items, as on the branch: `ItemEntity.Version`; `ItemVersionEntity` (`Id`, `ItemId`, `Version`,
  `Name`, `Price`, `ChangedDate`, and from the join `CustomerId`); `IItemRepository.Get(Guid id,
  int? version)` (`null` = the latest, one query with `v.version = COALESCE(@Version, <latest>)`),
  `GetVersions(Guid itemId)`, `GetVersions(List<Guid> itemIds)`. Every item query qualifies its
  columns (both tables have `id`); the paged `Get` orders by `v.name, i.id` and keeps
  `COUNT(*) OVER() AS total_count` last.
- `IItemRepository.Delete` and `ItemRepository.Delete`: master's (`Task`, plain `DELETE`), with a
  comment that `invoice_items`' foreign keys refuse an item an invoice uses and its versions go by
  cascade.
- `InvoiceItemEntity`: add `int Version`. `Name` and `Price` come from the version (join); fix
  their comment.
- `InvoiceRepository`:

```sql
-- ItemsSql: invoice items show the name and price of the version they name
SELECT ii.*, v.name, v.price FROM invoice_items ii
JOIN item_versions v ON v.item_id = ii.item_id AND v.version = ii.version
WHERE ii.invoice_id = ANY(@Ids)
ORDER BY ii.invoice_id, ii.line_no

-- Add: items_added
INSERT INTO invoice_items (invoice_id, line_no, item_id, version, quantity, comments)
SELECT inv.id, ii.ord, ii.item_id, ii.version, ii.quantity, ii.comments
FROM inv, unnest(@ItemIds::uuid[], @Versions::int[], @Quantities::numeric[], @ItemComments::text[])
    WITH ORDINALITY AS ii(item_id, version, quantity, comments, ord)

-- Update: upserted, the same insert plus
ON CONFLICT (invoice_id, line_no) DO UPDATE
SET item_id=EXCLUDED.item_id, version=EXCLUDED.version, quantity=EXCLUDED.quantity, comments=EXCLUDED.comments
```

  `WithItems` adds `Versions` (`invoice.Items.Select(ii => ii.Version)`). `Delete`, `UpdateStatus`,
  `MarkOverdue` and the paged read: unchanged.

## Step 4: Application (items: done, keep; invoices: redo on master)

- Items, as on the branch: `ItemModel.Version`, `ItemVersionModel`, `ItemMappingProfile.cs` (every
  property by hand, `ItemResponse.ToModel` too); `ItemService.Get(Guid id, int? version)`
  (`NotFoundException` `Invoice item:{id} not found`, or `Invoice item:{id} version {n} not found`);
  `GetVersions(Guid id)` (404 when there are no rows); `GetVersionsForCustomer(items, customerId)`
  (one `GetVersions(itemIds)` query; unknown or other customer's item → `Items id … is invalid for
  customer id …`; unknown version → `Item id {id} has no version {n}`; both `ValidationException`).
  `Get(List<Guid>)` and `GetForCustomer` are gone from `IItemService`.
- `ItemService.Delete`: master's (`Get`, then `Delete`).
- `InvoiceItemModel.Version` (`int?`, as on the branch).
- `InvoiceModelExtensions`: `ToInvoiceItemModel(InvoiceItemEntity)` maps `Version`;
  `ToInvoiceItemEntity` maps `Version = from.Version.GetValueOrDefault()` (a missing version
  becomes 0, which the foreign key rejects, never the latest); request and response mappings as on
  the branch.
- `InvoiceService` (from master's file):
  - `Validate` calls `GetVersionsForCustomer(invoiceData.Items!, invoiceData.CustomerId)` and
    returns the customer and the versions.
  - `Add` gives each invoice item without a version its item's latest version from those (a
    private method, e.g. `SetLatestVersion`); no name or price is copied.
  - `Update` saves the versions it was given (decision 3). `Get`, the paged `Get` and `GeneratePDF`
    don't change: name and price arrive from the repository.
  - Fix the comment above `Validate`.

## Step 5: Validators and WebAPI (done, keep)

- `InvoiceUpdateValidator`: every invoice item needs a version ("Please specify item version for
  {id}"). `InvoiceItemValidator` (`POST` and `PUT`): a given version must be `>= 1`.
  `InvoiceModelUpdateValidator` (runs on `PUT` after the request validator): every invoice item
  needs `Version >= 1`.
- `ItemController`: `GET {id}` takes an optional `[FromQuery] int? version` (404 when the item has
  no such version); `[HttpGet("{id}/versions")]` → `ItemVersionListResponse` with `ItemId`, with a
  Swagger example. Item examples and the invoice update example show `Version`.
- `DELETE /items/{id}`: no change (master's foreign-key 400; `DELETE /customers` and
  `DELETE /sellers` don't list theirs either).

## Step 6: Rules (redo on master's text)

- `CLAUDE.md`, Conventions, the invoices paragraph becomes:
  "Invoices store their user, seller and customer ids and show the current data. They keep their
  own number and dates. Invoice items are rows in `invoice_items`: an invoice item names its item
  and the item version it was saved with, keeps its quantity and comments, and shows that
  version's name and price. The total is computed from the invoice items. Editing an item must not
  change an existing invoice or its PDF: a name or price edit adds a row to `item_versions`, whose
  rows are never updated, and `PUT /invoices` never moves an invoice item to another version by
  itself. An item that an invoice uses can't be deleted (400). The series name isn't copied, so a
  series with invoices can't be renamed."
  Keep the branch's "More" line (`_write-plan` on Opus, xhigh).
- `REVIEW.md`: "An old invoice or its PDF changes after a unit is edited (items: allowed until
  BIL-85, see `CLAUDE.md`)." goes back to "An old invoice or its PDF changes after an item or unit
  is edited." Add "A `PUT /invoices` changes an invoice item's name or price though the request
  kept its version.", "An `item_versions` row is updated, or deleted while its item still exists."
  and "An item is deleted while an invoice item still uses it."
- `README.md` / `docs/README_lt.md`: no change. The new endpoint is in the existing items group,
  and no command, setting, port, package or layout changes.

## Step 7: Tests (`_write-tests`; items: done, keep; invoices: redo)

- Keep the branch's `ItemServiceTest` (`GetVersions`, `GetVersionsForCustomer`, `Get` with a
  version), `ItemMappingProfileTest`, validator tests, `ItemControllerTest` and
  `ItemSwaggerExampleTest`. Drop `Delete_GivenItemUsedByInvoice_ThrowsValidationException`;
  `Delete_GivenValidId_DeletesFromRepository` goes back to master's `Task` setup.
- `InvoiceMappingProfileTest` (master's file plus the branch's request and response `Version`):
  `ToInvoiceItemModel_GivenInvoiceItemEntity_MapsAllProperties` with `Version`;
  `ToInvoiceItemEntity_GivenInvoiceItemModel_MapsItemQuantityAndCommentsOnly` maps `Version` too
  (rename to say so); a model item without a version maps to 0; `ToModel`/`ToEntity` cases carry
  versions.
- `InvoiceServiceTest` (master's file): every `GetForCustomer` setup and verify becomes
  `GetVersionsForCustomer(invoiceData.Items!, customerId)`. With `Saved(expected)`:
  - `Add` with invoice items without a version saves each item's latest version (an item with
    versions 1 and 2 saves 2); one with an explicit old version keeps it.
  - `Update` saves the request's versions though a newer one exists; the same item on two invoice
    items at two versions saves both as given.
  - Replaces `Add_GivenValidData_SavesCustomersSeriesAndInvoiceItemsWithoutItemNamesOrPrices` and
    `Update_GivenValidInvoice_KeepsStoredUserStatusSeriesAndNumberAndSavesInvoiceItemsWithoutItemPrices`
    (or extends them). The branch's `MapItemToInvoiceItem_*` tests go.
- Name and price by version are SQL (Infrastructure, outside coverage): covered by the
  integration tests.
- `revert-check.sh`: `Update` setting the latest version, or `ToInvoiceItemEntity` dropping
  `Version`, must fail a test.
- Coverage: compare with the minimums + 1 before the PR (`CLAUDE.md`), never below master's
  93.0 / 88.5.

## Step 8: Integration tests (BillioIntegrationTest, same branch; done before BIL-113, merge and redo)

Checked against the branch and master on 2026-11-27.

**Branch.** `BIL-85-item-versions`: d12c263, c55eac5 and c611578 on 713d90e are pushed (PR #34
open). 6e58a5c (2026-11-24) is local only, the counterpart of Biller's 156c6a4: it turns
`InvoiceWithDeletedItem_Valid_Success` into `ItemDelete_WhenUsedByInvoice_Fail` expecting 156c6a4's
"Item id {id} is used by invoices", and `CustomerDelete_WhenHasInvoice_Fail` into
`CustomerDelete_WhenHasItemAndInvoice_Fail` (`invoice_customers_id`, plus a check that the item
still has its customer). Master's af3c2e3 (BIL-113) made the same two renames, the second with the
same expected key.

**Merge.** Merge `origin/master` into the branch on top of 6e58a5c (PR #34 is open: no rebase, no
force push). `REVIEW.md` merges on its own. `InvoiceTest.cs` conflicts in 5 hunks, because git pairs
the branch's new tests with master's neighbouring ones; resolve each hunk to the branch's side, then
put back what the table says. After the merge:

| Test or helper | Branch | Master | After the merge |
|---|---|---|---|
| "Arya" | `InvoiceKeepsItemVersionUntilChanged_Valid_Success` | `InvoiceItemFollowsItemEdit_Valid_Success` (an edit changes the invoice at once) | **Branch's**. Master's BIL-113 comment goes |
| "Sansa" | `ItemDelete_WhenUsedByInvoice_Fail`, "Item id {id} is used by invoices" (6e58a5c) | Same name, "Can not delete (please clear all dependants) or update (item not found): invoice_items_item_id" | **Master's message** (decision 5), with the branch's checks that the item still has version 1 and one version, and `AssertSingleLine(..., 1)` |
| "Bran" | `CustomerDelete_WhenHasItemAndInvoice_Fail` | Same name, same key | Either comment; **keep** the branch's item check |
| `InvoiceAddTakesItemVersion_Valid_Success` ("Robb"), `InvoiceLineVersion_Invalid_Fail` (5 cases; "Rickon", "Rickard", "Lyanna", "Benjen", "Brandon") | New | — | **Keep**, but give "Robb" and "Rickon" names no other test uses (e.g. "Meera", "Jojen"): master's two new tests use them, `AddInvoiceWithOwnData` adds the user `<name>@thewall.com`, and `users.email` is unique (029), so one test of each pair would fail |
| `InvoiceUpdateLines_OrderKept_Success` ("Rickon") | — | New; inside a conflict hunk, so put back by hand | **Keep**: `AssertSingleLine(..., 1)` (the branch's signature needs a version) |
| `InvoiceGetAllForCustomer_EachInvoiceItsOwnLines_Success` ("Robb") | — | New, merges on its own | **Keep**: its `POST` without versions takes version 1 |
| `AddOwnItem`, `UpdateLines`, `AssertLines` | — | New helpers, merge on their own | **Change**: `UpdateLines` sends `Version = l.item.Version` (`PUT` needs it, decision 3, else 400); `AssertLines` checks `Version` against `expected[i].item.Version`, as every invoice item assert on the branch does |
| `REVIEW.md` | — | "(...; since BIL-113 an item edit does change the names and prices on old invoices until BIL-85, see Biller's `CLAUDE.md`)" | **Change** to "an old invoice's number, dates or lines changing when another invoice, its series or an item is edited (checked through the invoice GET, not the PDF)" |

`ItemDelete_WhenUsedByInvoice_Fail` pins `invoice_items_item_id`. After 041 the delete would break
`fk_invoice_items_item_id_version` too (through the cascade to `item_versions`); the throwaway check
under "Current state" shows Postgres reports `fk_invoice_items_item_id`. The test stack run
(Verification 2) confirms it on a database built by Liquibase.

**Kept from the branch** (checked against its code):
- Items (`ItemTest.cs`): `Version` asserted on add (1), list (2) and update (2).
  `ItemVersions_Valid_Success`: a price-only edit, a name-only edit and a quantity-only edit give 3
  versions, oldest first, with rising `ChangedDate`; `GET` shows version 3 and the new quantity;
  `GET ?version=1` shows the first name and price with the current quantity.
  `ItemGetVersion_NotFound_Fail` (`?version=99`: 404 "Invoice item:{id} version 99 not found"),
  `ItemGetVersions_NotFound_Fail` (unknown id: 404). `ItemDelete_Valid_Success`: after the delete,
  `GET /items/{id}/versions` is 404 too.
- Invoices (`InvoiceTest.cs`): `InvoiceKeepsItemVersionUntilChanged_Valid_Success` (unchanged `PUT`
  keeps the old name and price; version 2 takes the new ones; version 1 brings the old ones back);
  `InvoiceAddTakesItemVersion_Valid_Success` (`POST` without a version takes the latest, with
  version 1 that version; invoice numbers 2 and 3); `InvoiceLineVersion_Invalid_Fail` (`PUT` without
  a version, with 0, with 99; `POST` with 0, with 99: 400 with Biller's messages, and the rejected
  save changes nothing, the next number included). `InvoiceAdd_Valid_Success`,
  `InvoiceDetailsUpdate_Valid_Success` and the other invoice item asserts check `Version`.
- `UpdateRequestFromInvoice` copies `Version`. Invoice items built by hand in `PUT` tests send the
  item's `Version`: `InvoiceIdsUpdate_Invalid_Fail`, `InvoiceUpdateChangeItems_Valid_Success`,
  `InvoiceUpdateChangeCustomer_Invalid_Fail`, `InvoiceUpdateChangeSeller_Invalid_Fail`.
- `BillerContracts.Clients` 6.0.0, `InvoiceLineVersionTestModel`.
- `README.md` / `docs/README_lt.md`: no change (they name no tests).

## Step 9: BillerFrontEnd (done before BIL-113; separate PR #36, its own `CLAUDE.md`)

Checked against the branch on 2026-11-27. Front-end master hasn't moved since the branch started
(ec39ffb), so there is nothing to merge. BIL-113 changed no contract and nothing in the front end.

**Keep** (as on the branch, checked):
- `Invoices_update.razor`: each invoice item shows its item, its version as "v{n} {name} {price}"
  in a drop-down per invoice item (versions of every customer item loaded with `GetVersions`, in
  parallel), a "Refresh" button per invoice item and "Refresh all", shown when the item's version
  is newer. Row prices and the total come from the selected versions. Nothing changes until
  Update. `ItemChange`: an invoice item whose item is picked or changed, and a new one, get that
  item's current `Version`; picking the original item again brings its own version back. Update is
  disabled, and does nothing, while an invoice item's version isn't among its item's versions
  (`CanSave`).
- `MappingProfiles/InvoiceMappingProfile.ToUpdateRequest(InvoiceItemResponse)` copies `Version`;
  its test in `tests/FrontEnd.Tests/MappingProfiles/InvoiceMappingProfileTest.cs`.
- `Items_id.razor`: version table, `ChangedDate` shown as UTC with a "UTC" label.
- `BillerContracts.Clients` 6.0.0. `REVIEW.md`'s line (total from the version each line has
  selected; a line sent without a version, or with another item's version, is a failure).
- 58cd398, `Program.cs` `UseStaticWebAssets()` for Rider's Docker fast mode: not BIL-85 work, but
  already in PR #36; it stays unless the user wants it split off.

**Change** (BIL-113 and this plan make the branch's code wrong):
- `Invoices_update.razor`: `deletedItemLines` goes, with its fallback in `LinePrice` and its
  `TryAdd` to the item drop-down (review round 1, "deleted-item lines per version"). Since BIL-113
  an item an invoice uses can't be deleted (decision 5), `fk_invoice_items_item_id_version` keeps
  every invoice item's version, and an invoice item's item is always one of the invoice customer's
  items, which `GetAllForCustomer` reads page by page. So every invoice item's item is in
  `versionsByItem`, and the code can't run. `CanSave` stays as the guard `REVIEW.md` asks for.
- `CLAUDE.md`, Conventions: "because `PUT /invoices` copies name and price from the version each
  line names (BIL-85)" becomes "because each invoice item shows the name and price of the version
  it names (BIL-85)": nothing is copied since BIL-113.

**Not in scope:** deleting an item that an invoice uses gets the API's 400 since BIL-113, on master
too; the item pages don't catch `ClientAPIException` (front-end `REVIEW.md`: no page does today).

## Verification

1. `bash .claude/skills/_test-coverage/scripts/build.sh` and
   `bash .claude/skills/_test-coverage/scripts/run-coverage.sh`: build and unit tests pass.
2. `_test-stack` from the branch: `test-stack.sh up --seed`; change the seeded item's price (version
   2); add a second invoice with an empty version (gets 2). Save `GET /items`,
   `GET /items/{id}/versions` and `GET /invoices`: the seeded invoice still shows the old price.
   `test-stack.sh rollback-check --count 3 --table invoice_items` rolls 041–039 back and applies
   them again (as a deploy onto master's tables). Save again and diff. Expected: every item has
   only version 1, with its latest name and price, dated now; every invoice item has version 1, so
   the seeded invoice shows the new price (decision 10). Check 041's precondition halts with an
   item that has no version. Then `run-integration.sh --repeat 3`.
3. On a copy of production: save `GET /items`, `GET /invoices` (all pages) and every invoice PDF as
   text before; migrate; save again and diff. Expected difference: only the new `version` fields.
   Run the checks from step 2.
4. In the browser against the throwaway stack: edit an item's price, open an old invoice (old price,
   "newer version" shown), save unchanged (total same), refresh (new price), pick version 1 (old
   price).
5. `_pre-pr` before each PR: every step runs again, since the code changed; it rewrites PR #90's
   description. Deploy Biller and BillerFrontEnd together with `make update`.

## Out of scope

- Versions for sellers, customers and users (invoices show their current data, by BIL-86).
- Units (BIL-75): when it lands, `unit_id` goes into `item_versions` next to name and price.
- Restoring an item to an old version.
- Version history from before the deploy (BIL-113 dropped it).

## Order of work

1. Done (2026-11-27): `_sync-repos` for the three siblings, then `_write-plan`; steps 1, 8 and 9
   are checked against their code. BillerContracts needs nothing.
2. Biller: merge `origin/master` ("Branch rework"), then steps 2–7, one commit per step.
3. BillioIntegrationTest: merge `origin/master` into the branch on top of 6e58a5c, resolved as in
   step 8, then step 8's changes. BillerFrontEnd: no merge; step 9's changes.
4. Verification 1–2 and 4, `_pre-pr` for each repo.
5. Push Biller and BillioIntegrationTest one right after the other (BillioIntegrationTest's push
   carries 6e58a5c too), then re-run the CI of the one pushed first: each CI clones the other's
   same-name branch from origin, and either new branch fails against the other's old one (the old
   tests delete an item an invoice uses; the old Biller lets them). Then push BillerFrontEnd and
   update the three PRs (#90, #34, #36).
6. Verification 3 on a production copy, then deploy Biller and BillerFrontEnd together.

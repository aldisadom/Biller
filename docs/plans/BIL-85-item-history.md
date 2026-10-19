# Plan: item history

Jira: [BIL-85](https://aldisadom.atlassian.net/browse/BIL-85). Epic: BIL-89 (Billeris). Goes together with [BIL-86-invoice-remove-serialized-data.md](BIL-86-invoice-remove-serialized-data.md).

## Goal

- Every change to an item (create, update, delete) is kept as a numbered version.
- The API can list an item's versions, also after the item is deleted.
- Invoice lines record which item version they were created from.

## Current state (master @ 125ae5e)

| Where | What |
|---|---|
| `items` table | `id`, `customer_id`, `name`, `price`, `quantity`. No version, no timestamps |
| `ItemRepository.Update` | `UPDATE items SET name, price, quantity`. The old values are lost |
| `ItemRepository.Delete` | Hard delete |
| `ItemController` | `items`: `GET {id}`, `GET`, `POST`, `PUT`, `DELETE {id}` |
| Invoices | Copy name and price into `items_data` JSON. No link to what the item looked like at the time beyond that copy |

## Design decisions

1. **`items.version int NOT NULL DEFAULT 1`**, bumped on each real change.
2. **New `item_history` table, one row per version**, including the current one. Reading history is one query, and the current state can be checked against the latest row.
3. **`item_history.item_id` has no foreign key.** History survives deleting the item. A delete writes a final row with `change_type = 'deleted'`.
4. **Written by the repository in one SQL statement** (data-modifying CTE), the same approach as the series plan (decision 4 there). No transactions are needed, and the logic stays in C# where the tests and code review can see it.
   - A trigger was considered. It would also catch edits made by hand in pgAdmin, but it hides logic from the code and from unit tests. Revisit if hand edits become common.
5. **No-op updates don't create a version.** The `UPDATE` has `WHERE ... AND (name, price, quantity) IS DISTINCT FROM (@Name, @Price, @Quantity)`.
6. **Existing items get one `'created'` row, version 1**, with `changed_at` set to the migration time. The real creation dates are unknown, and the plan says so in the migration comment.
7. **No `changed_by` for now.** Requests don't carry an authenticated user yet. Add the column when auth lands.

## Step 1: BillerContracts

- `Responses/Item/ItemHistoryResponse.cs`: `Guid ItemId`, `int Version`, `string ChangeType`, `DateTime ChangedAt`, `string Name`, `decimal Price`, `decimal Quantity`.
- `Responses/Item/ItemHistoryListResponse.cs`: `List<ItemHistoryResponse> Versions`.
- `ItemResponse`: add `int Version`.

## Step 2: Migration `036-Create-table-item_history.sql` (number after the series and units plans)

```sql
--liquibase formatted sql
-- changeset Aldis:36
-- comment: Item versions. Existing items get version 1 dated at migration time (real creation date unknown)
ALTER TABLE items ADD COLUMN version int NOT NULL DEFAULT 1;

CREATE TABLE item_history (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    item_id uuid NOT NULL,
    version int NOT NULL,
    change_type varchar(10) NOT NULL CHECK (change_type IN ('created', 'updated', 'deleted')),
    changed_at timestamptz NOT NULL DEFAULT now(),
    customer_id uuid NOT NULL,
    name text NOT NULL,
    price decimal NOT NULL,
    quantity decimal NOT NULL,
    PRIMARY KEY (id),
    UNIQUE (item_id, version)
);

INSERT INTO item_history (item_id, version, change_type, customer_id, name, price, quantity)
SELECT id, 1, 'created', customer_id, name, price, quantity FROM items;
-- rollback DROP TABLE item_history; ALTER TABLE items DROP COLUMN version;
```

Use the `add-db-migration` skill for numbering and the rollback check. If units ship first, add `unit_id uuid NULL` to `item_history` here.

## Step 3: Domain and Infrastructure

- `ItemEntity`: add `int Version`. New `ItemHistoryEntity`.
- `IItemRepository`: add `Task<IEnumerable<ItemHistoryEntity>> GetHistory(Guid itemId)`.
- `ItemRepository`:

```sql
-- Add
WITH ins AS (
    INSERT INTO items (name, price, customer_id, quantity)
    VALUES (@Name, @Price, @CustomerId, @Quantity)
    RETURNING *)
INSERT INTO item_history (item_id, version, change_type, customer_id, name, price, quantity)
SELECT id, version, 'created', customer_id, name, price, quantity FROM ins
RETURNING item_id;

-- Update
WITH upd AS (
    UPDATE items SET name=@Name, price=@Price, quantity=@Quantity, version=version+1
    WHERE id=@Id AND (name, price, quantity) IS DISTINCT FROM (@Name, @Price, @Quantity)
    RETURNING *)
INSERT INTO item_history (item_id, version, change_type, customer_id, name, price, quantity)
SELECT id, version, 'updated', customer_id, name, price, quantity FROM upd;

-- Delete
WITH del AS (DELETE FROM items WHERE id=@Id RETURNING *)
INSERT INTO item_history (item_id, version, change_type, customer_id, name, price, quantity)
SELECT id, version + 1, 'deleted', customer_id, name, price, quantity FROM del;

-- GetHistory
SELECT * FROM item_history WHERE item_id=@ItemId ORDER BY version;
```

## Step 4: Application, WebAPI

- `ItemHistoryModel` and mapping extensions (`ToModel`, `ToResponse`).
- `IItemService.GetHistory(Guid id)`: throws `NotFoundException` when there are no rows (item never existed).
- `ItemController`: `[HttpGet("{id}/history")]` returning `ItemHistoryListResponse`, with a Swagger example.
- Invoices: when an invoice is created, `InvoiceItemModel` gets `ItemVersion` from the loaded item (stored in `invoice_items.item_version`, see the serialized-data plan).

## Step 5: Tests (`write-tests` skill)

- `ItemServiceTest`: `GetHistory` returns mapped versions in order. Empty result throws `NotFoundException`. Strict mocks with `Verify`.
- `ItemControllerTest`: the history endpoint.
- Mapping and Swagger example tests.
- Integration tests (BillioIntegrationTest repo): create, update twice, a no-op update, delete, then history has versions 1–4 with the right types and values.

## Step 6: Frontend (BillerFrontEnd, separate PR)

- `IItemClient.GetHistory`, and a version table on `Items_id.razor`.

## Out of scope

- History for sellers, customers and users. The same pattern applies if it's wanted later.
- Restoring an item to an old version (can be done by hand with an update).

## Order of work

1. Contracts.
2. Migration, repository, service, controller, tests in one Biller PR.
3. Integration tests PR on the same branch name.
4. Frontend PR.

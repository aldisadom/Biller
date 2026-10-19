# Plan: units for items (BIL-75)

Jira: [BIL-75](https://aldisadom.atlassian.net/browse/BIL-75). Epic: BIL-89 (Billeris).

## Goal

- Items can have a unit of measure (pcs, h, kg, …), chosen from a `units` table.
- The `units` table has **one name column per document language** (today `name_lt` and `name_en`).
- The unit is optional. With no unit, the invoice shows an empty string.
- Units are managed through the API (list, get, add, update, delete).
- Invoice PDFs show the unit in the document's language, in both layouts (invoice SF and act ADA).
- Old invoices keep working and show an empty unit.

## Current state (master @ 125ae5e)

| Where | What |
|---|---|
| `items` table | `id`, `customer_id`, `name`, `price`, `quantity` (decimal since 027). No unit |
| `ItemEntity`, `ItemModel`, `InvoiceItemModel` | No unit (`InvoiceItemEntity` exists but is unused) |
| `invoices.items_data` | JSON snapshot of `List<InvoiceItemModel>` (Newtonsoft, PascalCase) |
| `InvoiceDocumentSF` / `InvoiceDocumentADA` | Item table columns: No. (`ConstantColumn(25)`), name, price, quantity, sum (`RelativeColumn` 19/5/4/6) |
| `IInvoiceTexts` (`InvoiceTextsLT`, `InvoiceTextsEN`) | Labels per language, e.g. `Amount()` = "Kiekis" / "Quantity" |
| `Language` enum (BillerContracts) | `LT`, `EN` |

## Design decisions

1. **`units` table with `name_lt`, `name_en`.** A new language means a migration that adds a `name_xx` column (see "Adding a language" below).
2. **`items.unit_id` is nullable** and references `units(id)`. `NULL` = no unit = empty string on documents.
3. **Invoices snapshot the whole unit** (id and all language names) inside `items_data`. Renaming or deleting a unit later doesn't change old invoices, and a PDF can be generated in any language.
4. **Deleting a unit used by items is refused** (400). Invoices don't reference units by foreign key (only the snapshot), so they don't block deletion.
5. **Unit names are unique per language column**, so the drop-down never shows two identical entries.
6. **Unit text on the PDF comes from `IInvoiceTexts.UnitName(UnitModel?)`.** Each language's texts class picks its own column. The documents stay language-agnostic, and adding a language follows the existing `add-language` pattern.
7. **`GET /units` returns all units** without paging. The list is short and is used for drop-downs.

## Prerequisites

- [ ] Sync the local BillerContracts checkout with `origin/master` (see the prerequisites in [BIL-65-series.md](BIL-65-series.md)).
- [ ] Decide whether this ships together with the series plan. Migration numbers below assume the series plan goes first (032, 033). If units go first, use 032/033 here and renumber the series plan.

## Step 1: BillerContracts (1.0.6, or 1.0.7 if released separately)

### 1.1 New files

`Contracts/Requests/Unit/UnitAddRequest.cs`
```csharp
public record UnitAddRequest
{
    public string NameLt { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
}
```

`Contracts/Requests/Unit/UnitUpdateRequest.cs`: `Guid Id`, `NameLt`, `NameEn`.

`Contracts/Responses/Unit/UnitResponse.cs`: `Guid Id`, `NameLt`, `NameEn`.

`Contracts/Responses/Unit/UnitListResponse.cs`: `List<UnitResponse> Units`, `int TotalCount`.

### 1.2 Changed files

| File | Change |
|---|---|
| `Requests/Item/ItemAddRequest.cs` | Add `Guid? UnitId` |
| `Requests/Item/ItemUpdateRequest.cs` | Add `Guid? UnitId` |
| `Responses/Item/ItemResponse.cs` | Add `UnitResponse? Unit` |
| `Requests/Invoice/InvoiceItemUpdateRequest.cs` | Add `Guid? UnitId` (change the unit of an invoice line when editing an invoice) |
| `Responses/Invoice/InvoiceItemResponse.cs` | Add `UnitResponse? Unit` |

`InvoiceItemRequest` (used by invoice add) doesn't change: the unit comes from the stored item, like name and price.

### 1.3 Clients project

- `Clients/Interfaces/IUnitClient.cs`, `Clients/Clients/UnitClient.cs`: `Get()`, `Get(id)`, `Add`, `Update`, `Delete` (route `units`). Register in `Clients/DependencyInjection.cs`.
- `Clients.Tests/UnitClientTest.cs`. Update `DependencyInjectionTests.cs`, `ItemClientTest.cs`, `InvoiceClientTest.cs` test data.

### 1.4 Release

PR, merge, run the `Publish` workflow. Bump `BillerContracts` in `src/Domain/Domain.csproj`.

## Step 2: Database migrations (Biller)

Use the `add-db-migration` skill.

### 2.1 `034-Create-table-units.sql`

```sql
--liquibase formatted sql

-- changeset Aldis:34
-- comment: Create units table with a name column per document language, seed common units
CREATE TABLE units (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    name_lt character varying(20) NOT NULL,
    name_en character varying(20) NOT NULL,

    PRIMARY KEY(id),
    CONSTRAINT uq_units_name_lt UNIQUE (name_lt),
    CONSTRAINT uq_units_name_en UNIQUE (name_en)
);

INSERT INTO units (name_lt, name_en) VALUES
    ('vnt.',   'pcs'),
    ('val.',   'h'),
    ('d.',     'day'),
    ('mėn.',   'month'),
    ('kg',     'kg'),
    ('m',      'm'),
    ('m²',     'm²'),
    ('km',     'km'),
    ('kompl.', 'set');

-- rollback DROP TABLE units;
```

Check that Liquibase reads the file as UTF-8 (`ė`, `²`). If it doesn't, add `encoding: UTF-8` to the `includeAll` entry in `migrations/dbChangeLog.yaml`.

### 2.2 `035-Alter-table-items_add_unit_id.sql`

```sql
--liquibase formatted sql

-- changeset Aldis:35
-- comment: Add optional unit to items
ALTER TABLE items ADD COLUMN unit_id uuid;

ALTER TABLE items ADD CONSTRAINT fk_items_unit_id
    FOREIGN KEY (unit_id) REFERENCES units(id);

CREATE INDEX idx_items_unit_id ON items (unit_id);

-- rollback ALTER TABLE items DROP COLUMN unit_id;
```

Existing items get `NULL` (no unit). No data migration for invoices: old `items_data` JSON has no `Unit`, which deserializes to `null` and prints as an empty string.

## Step 3: Domain (`src/Domain`)

| File | Change |
|---|---|
| `Entities/UnitEntity.cs` (new) | `Id`, `NameLt`, `NameEn` |
| `Entities/ItemEntity.cs` | Add `Guid? UnitId`, plus read-only join columns `string? UnitNameLt`, `string? UnitNameEn` (filled by the item queries, not written) |
| `Repositories/IUnitRepository.cs` (new) | `Get(Guid id)`, `Get(List<Guid> ids)`, `Get()`, `Add`, `Update`, `Delete`, `IsInUse(Guid id)`, `ExistsByName(string nameLt, string nameEn, Guid? excludeId)` |

## Step 4: Infrastructure (`src/Infrastructure`)

### 4.1 `Repository/UnitRepository.cs` (new)

Dapper, same style as `ItemRepository`. `Get()` orders by `name_lt`.

```sql
-- IsInUse
SELECT EXISTS (SELECT 1 FROM items WHERE unit_id = @Id);

-- ExistsByName (for clear validation messages; the unique constraints stay as a safety net)
SELECT EXISTS (
    SELECT 1 FROM units
    WHERE (name_lt = @NameLt OR name_en = @NameEn)
      AND (@ExcludeId IS NULL OR id <> @ExcludeId));
```

### 4.2 `Repository/ItemRepository.cs`

- Every `SELECT` joins the unit so the item comes back with its names in one query:
  ```sql
  SELECT i.*, u.name_lt AS unit_name_lt, u.name_en AS unit_name_en
  FROM items i
  LEFT JOIN units u ON u.id = i.unit_id
  WHERE i.id = @Id
  ```
  Same for `Get(ids)`, `GetByCustomerId`, `Get()`.
- `Add`: insert `unit_id`.
- `Update`: set `unit_id`.

### 4.3 `DependencyInjection.cs`

Add `services.AddScoped<IUnitRepository, UnitRepository>();`

## Step 5: Application (`src/Application`)

### 5.1 Models and mapping

| File | Change |
|---|---|
| `Models/UnitModel.cs` (new) | `Id`, `NameLt`, `NameEn` |
| `Models/ItemModel.cs` | Add `UnitModel? Unit` |
| `Models/InvoiceItemModel.cs` | Add `UnitModel? Unit` (stored in `items_data`) |
| `MappingProfiles/UnitMappingProfile.cs` (new) | `UnitEntity` ⇄ `UnitModel`, `UnitAddRequest`/`UnitUpdateRequest` → `UnitModel`, `UnitModel` → `UnitResponse` |
| `MappingProfiles/ItemMappingProfile.cs` | `ItemEntity` → `ItemModel`: build `Unit` from `UnitId`/`UnitNameLt`/`UnitNameEn` when `UnitId` is set. `ItemModel` → `ItemEntity`: `UnitId = Unit?.Id`. `ItemAddRequest`/`ItemUpdateRequest` → `ItemModel`: `Unit = UnitId is null ? null : new() { Id = UnitId.Value }` (names are filled by the service). `ItemModel` → `ItemResponse`: `Unit?.ToResponse()` |
| `MappingProfiles/InvoiceMappingProfile.cs` | `InvoiceItemUpdateRequest` → `InvoiceItemModel`: unit id only, as above. `InvoiceItemModel` → `InvoiceItemResponse`: `Unit?.ToResponse()` |

### 5.2 `Interfaces/IUnitService.cs` + `Services/UnitService.cs` (new)

```csharp
Task<UnitModel> Get(Guid id);                                 // NotFoundException
Task<IEnumerable<UnitModel>> Get();
Task<Result<IEnumerable<UnitModel>>> GetWithValidation(List<Guid> ids);  // error listing unknown ids
Task<Guid> Add(UnitModel unit);                               // ValidationException on duplicate name
Task Update(UnitModel unit);                                  // NotFound / duplicate name
Task Delete(Guid id);                                         // NotFound / ValidationException if in use
```
`GetWithValidation` follows `ItemService.GetWithValidation` (returns an `ErrorModel` with status 400).

### 5.3 `Services/ItemService.cs`

- `Add` / `Update`: if `item.Unit` is set, validate it with `_unitService.GetWithValidation([item.Unit.Id])` and throw `ValidationException` on error.
- Constructor gets `IUnitService`.

### 5.4 `Services/InvoiceService.cs`

- `MapItemToInvoiceItem`: also copy `Unit` from the item to the invoice item (with name and price), so the snapshot holds all language names.
- `MapInvoiceItemToItem`: no change.
- `Update`: collect the distinct `UnitId`s from the request items, load them with `_unitService.GetWithValidation(ids)`, and replace each item's `Unit` with the full model. Items without `UnitId` get `Unit = null`.
- Constructor gets `IUnitService`.

### 5.5 PDF texts and documents

`Helpers/Invoice/IInvoiceTexts.cs`, add:
```csharp
string ItemUnit();                 // column header
string UnitName(UnitModel? unit);  // "" when unit is null
```

| File | `ItemUnit()` | `UnitName(unit)` |
|---|---|---|
| `InvoiceTextsLT.cs` | `"Mato vnt."` | `unit?.NameLt ?? string.Empty` |
| `InvoiceTextsEN.cs` | `"Unit"` | `unit?.NameEn ?? string.Empty` |

`Models/Invoice/Documents/InvoiceDocumentSF.cs` and `InvoiceDocumentADA.cs`:
- Add one `columns.RelativeColumn(…)` after quantity. Take the width from the name column (for example 19 → 15, unit 4).
- Header: `header.Cell().Element(CellStyle).AlignRight().Text(_texts.ItemUnit());` after `Amount()`.
- Rows: `table.Cell().Element(CellStyle).AlignRight().Text(_texts.UnitName(item.Unit));` after the quantity cell.

### 5.6 `DependencyInjection.cs`

Add `services.AddScoped<IUnitService, UnitService>();`

## Step 6: Validators (`src/Validators`)

| File | Rules |
|---|---|
| `Unit/UnitAddValidator.cs` (new) | `NameLt` and `NameEn` not empty, max 20 |
| `Unit/UnitUpdateValidator.cs` (new) | `Id` not empty. Same name rules |
| `Item/ItemAddValidator.cs`, `Item/ItemUpdateValidator.cs` | `UnitId` must not be `Guid.Empty` when set (`When(x => x.UnitId is not null)`) |
| `Invoice/InvoiceItemUpdateValidator.cs` | Same `UnitId` rule |
| `DependencyInjection.cs` | Register the new validators |

## Step 7: WebAPI (`src/WebAPI`)

- `Controllers/UnitController.cs` (new), route `units`, following `ItemController`:
  - `GET units` → `UnitListResponse`
  - `GET units/{id}` → `UnitResponse`
  - `POST units` → `AddResponse`
  - `PUT units`
  - `DELETE units/{id}` (400 if used by items)
- `SwaggerExamples/Unit/`: `UnitAddRequestExample`, `UnitUpdateRequestExample`, `UnitResponseExample`, `UnitListResponseExample`.
- Update `SwaggerExamples/Item/*` and `SwaggerExamples/Invoice/*` (`InvoiceUpdateRequestExample`, `InvoiceResponseExample`, `InvoiceListResponseExample`) to include units.

## Step 8: Tests (`tests/xUnitTests`)

Use the `write-tests` skill.

| Test file | Cases |
|---|---|
| `Application/Services/UnitServiceTest.cs` (new) | Get found / not found. Get all. `GetWithValidation` all known / some unknown / empty list. Add OK / duplicate. Update OK / not found / duplicate. Delete OK / not found / in use |
| `Application/Services/ItemServiceTest.cs` | Add/Update with no unit (no unit-service call) / valid unit / unknown unit |
| `Application/Services/InvoiceServiceTest.cs` | Add copies the full unit into invoice items / item without unit gives `null`. Update resolves unit ids to full models / unknown unit id fails / no unit ids skips the unit service |
| `Application/MappingProfiles/UnitMappingProfileTest.cs` (new) | All mappings, `NullChecker` |
| `Application/MappingProfiles/ItemMappingProfileTest.cs` | Entity with and without unit columns. Request with and without `UnitId` |
| `Application/MappingProfiles/InvoiceMappingProfileTest.cs` | Unit in item responses. `items_data` JSON without `Unit` (old invoices) deserializes with `Unit == null`. JSON with `Unit` round-trips |
| `Application/Helpers/Invoice/InvoiceTextsTest.cs` (new, or extend existing) | `UnitName` per language with a unit and with `null`. `ItemUnit` per language |
| `Validators/Unit/*` (new), `Validators/Item/*`, `Validators/Invoice/InvoiceItemUpdateValidatorTest.cs` | Each rule, name length 20 vs 21, `UnitId` null / empty / valid |
| `Validators/DependencyInjectionTests.cs`, `Application/DependencyInjectionTests.cs` | New registrations |
| `WebAPI/Controllers/UnitControllerTest.cs` (new) | Every action: OK, validation failure, exceptions pass through |
| `WebAPI/Swagger/UnitSwaggerExampleTest.cs` (new), `ItemSwaggerExampleTest.cs`, `InvoiceSwaggerExampleTest.cs` | Examples populated |

Run the `test-coverage` skill when done.

## Step 9: Manual checks

On the local Docker stack:

1. `GET /units` returns the 9 seeded units.
2. Create an item with `UnitId` = "val./h". `GET /items/{id}` shows the unit.
3. Create an invoice with that item. Generate the PDF in LT ("val.") and EN ("h"), both document types (SF and ADA). Check the column widths look right with long item names.
4. An item without a unit: the unit cell is empty.
5. An invoice created before the migration: the PDF still renders, unit cell empty.
6. Rename the unit, regenerate the PDF of the existing invoice: it keeps the old name (snapshot).
7. `DELETE /units/{id}` for a unit used by an item: 400. For an unused unit: 200.
8. Edit an invoice and change one line's `UnitId`: the new unit shows in the response and PDF.

## Adding a language later

When the `add-language` skill adds a new language (for example `PL`), units also need:

1. A migration: `ALTER TABLE units ADD COLUMN name_pl character varying(20) NOT NULL DEFAULT '';`, filling the seeded rows, then a unique constraint.
2. `NamePl` in `UnitEntity`, `UnitModel`, the unit requests and `UnitResponse` (BillerContracts version bump), mappings, validators and Swagger examples.
3. `InvoiceTextsPL.ItemUnit()` / `UnitName(unit) => unit?.NamePl ?? string.Empty`.

Add this checklist to `.claude/skills/add-language/SKILL.md` as part of this work.

## Rollout

1. BillerContracts release (with the series changes if shipped together).
2. Biller PR with migrations and all layers.
3. BillerFrontEnd: unit drop-down on the item form, unit column in item and invoice views, a units management page.
4. Deploy with `make update` on the small PC.

## Out of scope

- Converting between units.
- Units per seller or per user (units are global).
- Plural or grammatical forms of unit names on documents.

## Order of work

1. BillerContracts (+ clients)
2. Migrations 034, 035
3. Domain + Infrastructure
4. Application (unit service, then item, then invoice, then PDF texts and documents)
5. Validators + WebAPI
6. Tests + coverage
7. Sample PDFs, manual checks, deploy

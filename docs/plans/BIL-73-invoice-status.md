# Plan: invoice status, default generated (BIL-73)

Jira: [BIL-73](https://aldisadom.atlassian.net/browse/BIL-73). Epic: BIL-89 (Billeris).

## Goal

- Every invoice has a status: generated (`Created`), sent or paid.
- A new invoice always starts as `Created`.
- The status can be changed from the API and the front end (this part already works).

## Current state (master @ 125ae5e)

| Where | What |
|---|---|
| `invoices.status` | `INT`, **nullable, no default** (migration 031). 031 set every existing invoice to `0` |
| `InvoiceStatus` (BillerContracts) | `Payed = 0`, `OverDue = -1`, `Send = 1`, `Created = 10` |
| `InvoiceRepository.Add` | Inserts `@Status` from the entity |
| `InvoiceAddRequest.ToModel()` / `InvoiceService.Add` | **Never set `Status`**, so it stays at the enum default `0` = `Payed`. **Every new invoice is saved as Payed** |
| `InvoiceRepository.Update` | Doesn't touch `status` (good: editing an invoice keeps its status) |
| `PUT` status endpoint | `InvoiceService.UpdateStatus` checks `Enum.IsDefined`, then `InvoiceRepository.UpdateStatus` |
| `InvoiceResponse` | Has `Status` |
| Front end | Status column on `Invoices.razor`. Buttons for each status on `Invoices_id.razor` |
| Tests | `InvoiceServiceTest.Add_GivenValidData_ReturnsGuid` doesn't check the status. `UpdateStatus_*` tests exist |

## Design decisions

1. **The service sets the status, not the request.** `InvoiceService.Add` sets `Status = InvoiceStatus.Created` before mapping to the entity. Callers can't create an invoice that is already paid.
2. **The database default backs it up:** `status INT NOT NULL DEFAULT 10`. A row inserted without a status (e.g. by hand) is `Created`, never `Payed`.
3. **Leave the enum numbers as they are for now.** Making `Created = 0` would be safer in the long run, but it needs a BillerContracts release and a migration that remaps stored values in step with it. Steps 1 and 2 already remove the actual bug. Revisit together with the next contracts release (see "Later").
4. **Existing invoices stay as they are** unless the data says otherwise. Migration 031 marked them all `Payed`. Most old invoices are likely paid, but invoices created after 031 were also saved as `Payed` by the bug. The pre-check query below lists them so they can be fixed by hand.
5. **`OverDue` is shown, not stored.** `Created`/`Send` with `due_date < today` is displayed as overdue in the front end. Storing it would need a scheduled job to keep it correct. The enum value stays (removing it is a contracts change). The API rejects setting it by hand, so nothing new gets stored as `OverDue`.

## Prerequisites

- [ ] Backup ([BIL-84-db-backup-restore.md](BIL-84-db-backup-restore.md)).
- [ ] Run the pre-check queries on production and decide which invoices to fix.

### Pre-check queries

```sql
-- Status counts
SELECT status, COUNT(*) FROM invoices GROUP BY status ORDER BY status;

-- NULL status (would fail the NOT NULL migration)
SELECT id, invoice_number, created_date FROM invoices WHERE status IS NULL;

-- Invoices marked Payed. Recent ones were probably saved as Payed by the bug, not paid
SELECT id, invoice_number, created_date, due_date, total_price
FROM invoices WHERE status = 0 ORDER BY created_date DESC;
```

Fix the wrong ones by hand (pgAdmin or `PUT` status) before or after the migration.

## Step 1: Application

- `InvoiceService.Add`: `invoiceData.Status = InvoiceStatus.Created;` before `ToEntity()`.
- `InvoiceService.UpdateStatus`: also reject `OverDue` with a `ValidationException` ("Overdue is calculated from the due date").

## Step 2: Migration (next free number)

```sql
--liquibase formatted sql
-- changeset Aldis:<n>
-- comment: invoices.status NOT NULL, default Created (10)
UPDATE invoices SET status = 10 WHERE status IS NULL;
ALTER TABLE invoices ALTER COLUMN status SET DEFAULT 10;
ALTER TABLE invoices ALTER COLUMN status SET NOT NULL;
-- rollback ALTER TABLE invoices ALTER COLUMN status DROP NOT NULL; ALTER TABLE invoices ALTER COLUMN status DROP DEFAULT;
```

This task is small and has no dependencies besides the backup, so it can go before BIL-65. In that case it takes number 032, and the numbers in the other plans move up by one.

## Step 3: Tests (`write-tests` skill)

- `InvoiceServiceTest.Add_GivenValidData_ReturnsGuid`: verify the repository gets an entity with `Status == InvoiceStatus.Created` (`It.Is<InvoiceEntity>(e => e.Status == InvoiceStatus.Created)`).
- `InvoiceServiceTest.UpdateStatus`: theory over `Created`, `Send`, `Payed` (accepted) and `OverDue`, `(InvoiceStatus)99` (rejected), each with a `Label`.
- `InvoiceServiceTest.Update`: the entity passed on update keeps the existing status. The repository `Update` doesn't write `status`, so this only documents it.
- Integration test (BillioIntegrationTest, same branch name): create → `Created`; `PUT` status `Send` → `Send`; `Payed` → `Payed`; `OverDue` → 400; unknown value → 400.

## Step 4: Front end (BillerFrontEnd, separate PR)

- `Invoices_id.razor`: remove the "Status over due" button.
- Show "Overdue" (e.g. red) when status is `Created` or `Send` and `DueDate` < today, on both the list and the detail page.
- Optional: a status filter on the invoice list.

## Later (with the next BillerContracts release)

- Renumber the enum so `Created = 0`, with a migration that remaps the stored values in the same release: `UPDATE invoices SET status = CASE status WHEN 10 THEN 0 WHEN 0 THEN 2 ... END`. Then the migration default becomes `0`.
- Consider renaming `Send` → `Sent` and `Payed` → `Paid` at the same time. The names go out over JSON (`JsonStringEnumConverter`), so the front end and integration tests must update together.

## Out of scope

- Status history (who changed it and when).
- Sending invoices by email.

## Order of work

1. Pre-check queries and backup.
2. Service fix, migration and tests in one PR. It's small, so it can go before BIL-65.
3. Integration tests.
4. Front end.

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
5. **`OverDue` is stored, but only a daily job sets it** ([BIL-95](https://aldisadom.atlassian.net/browse/BIL-95), changed 2026-10-23; first planned as computed in the front end). The API rejects setting it by hand. Until BIL-95 ships, nothing becomes `OverDue`.

## Prerequisites

- [ ] Backup ([BIL-84-db-backup-restore.md](BIL-84-db-backup-restore.md)). The scripts are merged (`make backup`); run it on production before deploying.
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

## Step 1: Application (done)

- [x] `InvoiceService.Add`: `invoiceData.Status = InvoiceStatus.Created;` before `ToEntity()`.
- [x] `InvoiceService.UpdateStatus`: also reject `OverDue` with a `ValidationException` ("Overdue status cannot be set manually").

## Step 2: Migration (done: `032-Alter-table-invoices_status_default.sql`)

```sql
--liquibase formatted sql
-- changeset Aldis:<n>
-- comment: invoices.status NOT NULL, default Created (10)
UPDATE invoices SET status = 10 WHERE status IS NULL;
ALTER TABLE invoices ALTER COLUMN status SET DEFAULT 10;
ALTER TABLE invoices ALTER COLUMN status SET NOT NULL;
-- rollback ALTER TABLE invoices ALTER COLUMN status DROP NOT NULL; ALTER TABLE invoices ALTER COLUMN status DROP DEFAULT;
```

This task went before BIL-65, so it took number 032. The numbers in the other plans move up by one.

## Step 3: Tests (`write-tests` skill)

- [x] `InvoiceServiceTest.Add_GivenValidData_ReturnsGuid`: the model comes in as `Payed`, and the repository must get the same entity with `Status = Created` (record equality on the strict mock, rather than `It.Is`).
- [x] `InvoiceServiceTest.UpdateStatus`: theory over `Created`, `Send`, `Payed` (accepted) and `OverDue`, `(InvoiceStatus)99` (rejected), each with a `Label`.
- [x] `InvoiceServiceTest.Update_GivenValidInvoice_UpdatesInvoiceAndKeepsStatus`: the stored invoice is `Send`, the request model is `Payed`, and the repository gets `Send`. `InvoiceService.Update` now copies the stored status onto the model (found in review), so keeping the status no longer rests only on `InvoiceRepository.Update` leaving `status` out of its SQL.
- [x] Integration test (BillioIntegrationTest, same branch name): create → `Created`; `PUT` status `Send` → `Send`; `Payed` → `Payed`; `OverDue` → 400; unknown value → 400; editing a `Send` invoice (`PUT /invoices`) keeps it `Send`. Done as: `InvoiceAdd_Valid_Success` checks `Created`; `InvoiceStatusUpdate_Valid_Success` (Send, Payed, Created, each on its own invoice, also edits it and checks the status stays, then deletes it; runs after the `GetAll` count tests); `InvoiceStatusUpdate_Invalid_Fail` (OverDue, 99); `InvoiceStatusUpdate_NotFound_Fail` (404).

## Step 4: Front end (BillerFrontEnd, branch `BIL-73-invoice-status`, deployed together with this PR)

- [x] `Invoices_id.razor`: remove the "Status over due" button. The API now rejects `OverDue`, so the button would only return 400.
- [x] Highlight the stored `OverDue` status (e.g. red) on the list and the detail page. No date logic in the front end: BIL-95 sets the status.
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
4. Front end, deployed together with step 2.
5. BIL-95: the daily overdue job.

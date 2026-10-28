# Plan: invoices point to their user, seller and customer instead of copying them (BIL-86)

Jira: [BIL-86](https://aldisadom.atlassian.net/browse/BIL-86). Epic: BIL-89 (Billeris). Goes before BIL-65 (series), which builds on it. Jira says it blocks BIL-75 (units); see Out of scope.

## Goal

- Invoices store `user_id`, `seller_id` and `customer_id` only. Responses and PDFs show the current user, seller and customer.
- No user data is stored in invoices, so neither is the password hash (review eval 7).
- An invoice keeps what belongs to it: number, dates, lines, comments, status and total.
- `PUT /invoices` takes ids, not copies (BillerContracts 3.0.1, PR #11).
- PDFs and file names use the invoice's own number (review eval 4).

## Current state (master @ 6bb2079)

| Where | What |
|---|---|
| BillerContracts PR #11 (`BIL-86-invoice-remove-serialized-data`) | `InvoiceUpdateRequest`: `Id`, `InvoiceNumber`, `SellerId`, `CustomerId`, `Items` (`List<InvoiceItemRequest>`: `Id`, `Quantity`, `Comments`), `Comments`, `DueDate`, `CreatedDate`. `InvoiceItemUpdateRequest` is removed. `InvoiceResponse` is unchanged. Also reverts #10 (series), so it is 2.0.0 plus BIL-86. Publish as 3.0.1, which replaces 3.0.0 (deprecated on NuGet) |
| BillerContracts PR #12 (`BIL-65-series`, stacked on #11) | Brings the series contracts back. Publish as 4.0.0, after 3.0.1 |
| Biller master | BillerContracts 2.0.0. Last migration `032-Alter-table-invoices_status_default.sql` |
| `invoices` (026, 028, 031, 032) | `user_data`, `seller_data`, `customer_data`, `items_data`: `TEXT NOT NULL`, Newtonsoft JSON with PascalCase names. Foreign keys `fk_invoices_user_id`, `fk_invoices_seller_id`, `fk_invoices_customer_id`, without cascade, so a user, seller or customer with invoices can't be deleted (400). `file_path varchar(255) NOT NULL`: written, never read; PDFs are streamed |
| Draft `InvoiceModel` | `Id`, `UserId`, `SellerId`, `CustomerId`, `Items`, `InvoiceNumber`, `Comments`, `Status`, dates. `CalculateTotal`, `GenerateFileLocation` and `GenerateInvoiceName` moved to `InvoiceModelFull` |
| Draft `InvoiceModelFull` (new) | A copy of every `InvoiceModel` field plus `User`, `Seller`, `Customer`, and the three methods. `GenerateInvoiceName` still pads `Customer.InvoiceNumber`, the customer's *next* number (eval 4). With live customers, every old invoice would show it |
| Draft `InvoiceFullMappingProfile.cs` | `ToResponse(this InvoiceModelFull)` |
| Draft `InvoiceModelExtensions` | `ToModel(InvoiceEntity)` and `ToEntity` map ids. `ToEntity` no longer sets `TotalPrice` (saved as 0) or `FilePath` (saved as `''`). **Bug:** `ToModel(InvoiceAddRequest)` sets `SellerId` and `CustomerId` from `UserId`. `ToModel(InvoiceUpdateRequest)` reads `from.Seller.Id`/`from.Customer.Id` (2.0.0) |
| Draft `InvoiceService` | `Add`, `Update` and both `Get`s use `InvoiceModelFull`. `Get(id)` calls `GetInvoiceData`. That runs `Validate` (seller of user, customer of seller, items of customer), sets `InvoiceNumber = Customer.InvoiceNumber`, and overwrites line names and prices with the current items' |
| Draft `InvoiceRepository` | `Add` without the JSON columns. `Update` still sets `user_data`, `seller_data`, `customer_data` |
| Draft migration `033-Alter-table-invoices_drop_seller_user_customer_data.sql` | Drops the three columns. Its rollback (`ADD COLUMN … TEXT NOT NULL`) fails on a table with rows |
| Draft build | 4 errors in `InvoiceService.cs` (lines 81, 95, 103, 120): `InvoiceModel` returned where `InvoiceModelFull` is expected, and no `ToEntity` for `InvoiceModelFull` |
| Validators | `InvoiceUpdateValidator` runs `SellerUpdateValidator`, `CustomerUpdateValidator` and `InvoiceItemUpdateValidator` on the copies. `InvoiceItemUpdateValidator` is registered in `Validators/DependencyInjection.cs:37`. `InvoiceValidator` (`InvoiceModelUpdateValidator.cs`) is used only by its own test; the draft changed it to `InvoiceModelFull` |
| Rules | `CLAUDE.md`: "Invoices keep their own copy of seller, customer, item and user data. Editing those must not change an existing invoice or its PDF." `REVIEW.md` line 6: "An old invoice or its PDF changes after a seller, customer, item, or unit is edited." |
| Review evals | 4 (PDF number from the customer's next number), 7 (password hash in `user_data`) and 8 (empty ids in copies saved by `PUT`) are open |
| BIL-65 (Biller `BIL-65-series`, not merged) | BillerContracts 3.0.0, which is deprecated; it moves to 4.0.0 (step 9). Migrations `033-Create-table-series.sql` and `034-Migrate-customers_invoices_to_series.sql`. 034 reads `customer_data::json->>'InvoiceName'`, and its rollback writes `customer_data` |
| BillerFrontEnd | `Invoices_update.razor` builds an `InvoiceUpdateRequest` with `Seller`, `Customer`, and lines with `Name`/`Price` (lines 167–253). `MappingProfiles/InvoiceMappingProfile.cs:18-20` maps `InvoiceResponse` to `InvoiceUpdateRequest` with AutoMapper. BillerContracts.Clients 2.0.0 |
| BillioIntegrationTest | `InvoiceTest.cs`: about 35 uses of `InvoiceUpdateRequest`/`InvoiceItemUpdateRequest`, and update cases that edit the seller and customer copies and line names and prices. BillerContracts.Clients 2.0.0 |

## Design decisions

1. **Invoices don't copy the user, seller or customer.** They store the ids, and the foreign keys keep those rows from being deleted. Every read loads the current rows, so editing a seller, customer or user changes every invoice's response and PDF. This replaces the `CLAUDE.md` rule and `REVIEW.md` line 6 for these three (step 6).
2. **The invoice keeps its own number, dates, lines, comments, status and total.** PDFs and file names use `InvoiceModel.InvoiceNumber` (fixes eval 4). The series prefix stays `Customer.InvoiceName`, read live. Until BIL-65 gives invoices their own series, renaming a customer's `invoice_name` changes the prefix on its old invoices.
3. **Two models.** `InvoiceModel` holds the ids and is what writes and the repository use. `InvoiceModelFull : InvoiceModel` adds only `User`, `Seller` and `Customer`, and is what responses and PDFs use. Inheritance removes the draft's duplicated fields. `CalculateTotal` and `GenerateInvoiceName` need only the invoice, so they move back to `InvoiceModel`. `GenerateFileLocation` needs `Customer.InvoiceName`, so it stays on `InvoiceModelFull`. Done as: `InvoiceModel.GenerateFileLocation(invoiceName)` builds the name, and `InvoiceModelFull.GenerateFileLocation()` passes `Customer.InvoiceName` (review: writes need the path without loading the user).
4. **Reads don't validate.** `Get(id)` loads the user, seller and customer by id (`IUserService.Get`, `ISellerService.Get`, `ICustomerService.Get`) and keeps the stored number and lines. The add-time checks (seller of user, customer of seller, items of customer), the item mapping and the number copy run only in `Add` and `Update`.
5. **The paged `Get` loads each distinct user, seller and customer once per page,** not once per invoice. There is one owner, so a page has a handful of distinct ids. A `JOIN` in `InvoiceRepository` would be one statement, but it would repeat the seller, customer and user mapping inside the invoice repository.
6. **Writes work as today, from ids.**
   - `Add`: the user must exist (404 as today). Then seller of user, customer of seller, items of customer. The number comes from the customer, line names and prices from the items, and the status is `Created`.
   - `Update`: keeps the stored `UserId` and `Status`. Validates the request's `SellerId`, `CustomerId` and items the same way, and copies line names and prices from the current items (decision 9). Moving an invoice to another seller or customer, and editing `InvoiceNumber`, stay allowed as today; BIL-65 stops both.
   - `file_path` keeps being written from `GenerateFileLocation()` until BIL-65 drops the column.
7. **One changeset drops the three columns.** The rollback rebuilds them from the *current* user, seller and customer rows, with an empty password. Old code then works after a rollback. The original copies can't come back that way: the backup that `make update` takes is the only full undo. In the rebuilt customer copy, `InvoiceNumber` is the invoice's own number, so old code prints the right number.
8. **BillerContracts 3.0.1.** `InvoiceResponse` keeps its shape. Only the values of old invoices change, where their seller, customer or user was edited since. Also on every invoice, `Customer.InvoiceNumber` is now the customer's next number, not the invoice's (that is `InvoiceNumber`). Nothing in BillerFrontEnd reads it. Biller and BillerFrontEnd deploy together, because the `PUT /invoices` body changes shape.
9. **Invoice lines keep the name and price they were billed with.** `items_data` stays as it is, and reads use the stored lines. `PUT /invoices` sends no name or price (BillerContracts 3.0.1), so an update copies both from the current item, for every line. Reading lines from the current items was rejected: a price change would rewrite the totals of issued invoices, `total_price` would go stale, and an invoice whose item was deleted couldn't be read or printed. The draft does that today by accident, through `GetInvoiceData` in `Get` (decision 4). Kept after review (two reviews flagged that editing a paid invoice re-prices it): BillerFrontEnd's `CLAUDE.md` and `REVIEW.md` say the update page shows the total after saving, from the current item prices.
10. **No PDF archive before the deploy.** From the deploy on, old invoices and their PDFs show the current seller, customer and user. Pre-check 1 lists the ones that change. The backup `make update` takes still holds the old copies.

## Dependencies

- BIL-84 (backup and restore scripts) is Done.
- BillerContracts #11 is merged and 3.0.1 is published before Biller, BillerFrontEnd and BillioIntegrationTest can build against it. For CI before the merge, the `Publish` workflow can publish `3.0.1-preview.<run>` from the branch.
- BIL-65 goes after this, on BillerContracts 4.0.0 (#12). Step 9 lists what it has to change.

## Pre-check queries

Run on a copy of production before deploying. They are for information; none of them block the migration.

```sql
-- 1. Invoices that will look different after the migration, and why
SELECT i.id, i.invoice_number, i.created_date,
       (i.seller_data::jsonb->>'CompanyName', i.seller_data::jsonb->>'CompanyNumber', i.seller_data::jsonb->>'Street',
        i.seller_data::jsonb->>'City', i.seller_data::jsonb->>'State', i.seller_data::jsonb->>'Email',
        i.seller_data::jsonb->>'Phone', i.seller_data::jsonb->>'BankName', i.seller_data::jsonb->>'BankNumber')
         IS DISTINCT FROM (s.company_name, s.company_number, s.street, s.city, s.state, s.email, s.phone,
                           s.bank_name, s.bank_number) AS seller_changed,
       (i.customer_data::jsonb->>'CompanyName', i.customer_data::jsonb->>'CompanyNumber', i.customer_data::jsonb->>'Street',
        i.customer_data::jsonb->>'City', i.customer_data::jsonb->>'State', i.customer_data::jsonb->>'Email',
        i.customer_data::jsonb->>'Phone')
         IS DISTINCT FROM (c.company_name, c.company_number, c.street, c.city, c.state, c.email, c.phone) AS customer_changed,
       i.customer_data::jsonb->>'InvoiceName' IS DISTINCT FROM c.invoice_name AS prefix_changed,
       (i.customer_data::jsonb->>'InvoiceNumber')::int IS DISTINCT FROM i.invoice_number AS printed_number_changes,
       (i.user_data::jsonb->>'Name', i.user_data::jsonb->>'LastName') IS DISTINCT FROM (u.name, u.last_name) AS user_changed
FROM invoices i
JOIN sellers s ON s.id = i.seller_id
JOIN customers c ON c.id = i.customer_id
JOIN users u ON u.id = i.user_id
ORDER BY i.created_date;

-- 2. Number of invoices, to compare after the migration
SELECT COUNT(*) FROM invoices;
```

`prefix_changed` rows matter for BIL-65 too. Without `customer_data`, its migration puts those invoices into the customer's current series, so their numbers count towards it (step 9).

## Step 1: BillerContracts (done)

- [x] PR #11 is merged, and 3.0.1 was published from master on 2026-10-28. Done differently: it was merged with the unused `using BillerContracts.Requests.Customer;` and `using BillerContracts.Requests.Seller;` still in `InvoiceUpdateRequest.cs`, so 3.0.1 has them. They do no harm. Drop them in #12 (4.0.0).
- PR #12 waits for BIL-65 (step 9).

## Step 2: Migration `033-Alter-table-invoices_drop_seller_user_customer_data.sql` (done)

033 is the next free number on master. BIL-65 renumbers its own changesets. Keep the draft's `ALTER TABLE`s and replace the rollback:

```sql
-- rollback ALTER TABLE invoices ADD COLUMN user_data TEXT, ADD COLUMN seller_data TEXT, ADD COLUMN customer_data TEXT;
-- rollback UPDATE invoices i SET
-- rollback     user_data = jsonb_build_object('Id', u.id, 'Name', u.name, 'LastName', u.last_name)::text,
-- rollback     seller_data = jsonb_build_object('Id', s.id, 'UserId', s.user_id, 'CompanyName', s.company_name,
-- rollback         'CompanyNumber', s.company_number, 'Street', s.street, 'City', s.city, 'State', s.state,
-- rollback         'Email', s.email, 'Phone', s.phone, 'BankName', s.bank_name, 'BankNumber', s.bank_number)::text,
-- rollback     customer_data = jsonb_build_object('Id', c.id, 'SellerId', c.seller_id, 'InvoiceName', c.invoice_name,
-- rollback         'InvoiceNumber', i.invoice_number, 'CompanyName', c.company_name, 'CompanyNumber', c.company_number,
-- rollback         'Street', c.street, 'City', c.city, 'State', c.state, 'Email', c.email, 'Phone', c.phone)::text
-- rollback FROM users u, sellers s, customers c
-- rollback WHERE u.id = i.user_id AND s.id = i.seller_id AND c.id = i.customer_id;
-- rollback ALTER TABLE invoices ALTER COLUMN user_data SET NOT NULL, ALTER COLUMN seller_data SET NOT NULL, ALTER COLUMN customer_data SET NOT NULL;
```

- Update the changeset comment to say the copies are dropped without being kept anywhere, and that the rollback rebuilds them from the current rows.
- Newtonsoft fills in missing keys (`Email`, `Password`) with the model's defaults, so the old code reads the rebuilt user copy.
- Liquibase joins the `-- rollback` lines and splits them on `;`. Test the rollback on the copy (step 10). Done once on a throwaway database with one invoice: `rollback-count 1` rebuilt the three columns as `NOT NULL`, with no password and the invoice's own number in `customer_data`, and `update` dropped them again.
- Your local billio-db has already run the draft 033, and BIL-65's 033 and 034 too, so it matches no branch. Restore it from a backup or recreate it before step 10.

## Step 3: Domain and Infrastructure (done)

- `InvoiceEntity`: done in the draft (no `UserData`, `SellerData`, `CustomerData`).
- `InvoiceRepository.Update`: remove `user_data`, `seller_data` and `customer_data` from the `SET` list. Keep `customer_id`, `seller_id`, `file_path`, `invoice_number`.
- `InvoiceRepository.Add` and both `Get`s: no further change.
- `Domain.csproj`: BillerContracts 3.0.1. Undo the draft's `Helpers` lines: they exclude a `Helpers` folder that doesn't exist, and remove its empty folder entry.

## Step 4: Application (done)

- `InvoiceModel`: the draft's ids plus `CalculateTotal()` and `GenerateInvoiceName()`. `GenerateInvoiceName()` pads `InvoiceNumber`, the invoice's own number, as `InvoiceNumber.ToString("D6")`.
- `InvoiceModelFull : InvoiceModel`: only `UserModel User`, `SellerModel Seller`, `CustomerModel Customer` and `GenerateFileLocation()` (`$"{Customer.InvoiceName}-{GenerateInvoiceName()}.pdf"`).
- `InvoiceModelExtensions`:
  - `ToEntity`: set `TotalPrice = from.CalculateTotal()` again.
  - `ToModel(InvoiceAddRequest)`: `SellerId = from.SellerId`, `CustomerId = from.CustomerId`.
  - `ToModel(InvoiceUpdateRequest)`: `SellerId = from.SellerId`, `CustomerId = from.CustomerId`, `InvoiceNumber`, and lines through `ToInvoiceItemModel(InvoiceItemRequest)`. Remove `ToInvoiceItemModel(InvoiceItemUpdateRequest)`.
  - New `ToFull(this InvoiceModel from, UserModel user, SellerModel seller, CustomerModel customer)`: every property by hand.
- `InvoiceFullMappingProfile.cs`: `ToResponse` takes `UserId` from `from.UserId`. Remove the unused usings.
- `IInvoiceService`: `Add(InvoiceModel)`, `Update(InvoiceModel)`, `Get(Guid)` returns `InvoiceModelFull`, the paged `Get` returns `IEnumerable<InvoiceModelFull>`.
- `InvoiceService`:
  - Private `GetStored(Guid id)`: the repository row as an `InvoiceModel`, or `NotFoundException`. Used by `Update`, `UpdateStatus` and `Delete`, which don't need the user, seller or customer.
  - `Get(id)`: `GetStored`, then the three `Get`s by id, then `ToFull`. No `Validate`, no item mapping, no number copy.
  - Paged `Get`: collect the distinct `UserId`, `SellerId` and `CustomerId` of the page, load each once, then `ToFull` every invoice.
  - `Validate(InvoiceModel)`: from the ids (`GetWithValidation(SellerId, UserId)`, `(CustomerId, SellerId)`, items with `CustomerId`). Returns seller, customer and items. Done as: returns customer and items; the seller is only checked (review).
  - `Add`: `_userService.Get(UserId)`, `Validate`, `InvoiceNumber = customer.InvoiceNumber`, `MapItemToInvoiceItem`, `Status = Created`, `ToEntity` with `FilePath` from the `ToFull(...)` model, save, `IncreaseInvoiceNumber`. Remove `GetInvoiceData`. Done as: `ToEntity(GenerateFileLocation(customer.InvoiceName))`; the user load stays for the 404 (decision 6).
  - `Update`: `GetStored`, copy `UserId` and `Status` from it, `Validate`, `MapItemToInvoiceItem`, `ToEntity` with `FilePath`, save. Done as: `ToEntity(filePath)` maps every property, and `Add` and `Update` pass `GenerateFileLocation(customer.InvoiceName)`, so `Update` doesn't load the user (review finding).
  - `GeneratePDF`: `Get(id)` and `GenerateFileLocation()` on the full model.
- `InvoiceDocumentSF`, `InvoiceDocumentADA` and `InvoiceDocumentFactory`: take `InvoiceModelFull` (done in the draft). The header prints `Model.GenerateInvoiceName()`, which now uses the invoice's own number.
- `MapInvoiceItemToItem` is unused once `GetInvoiceData` is gone; remove it if nothing else calls it. Done: removed. Only the old `Add` test called it.

## Step 5: Validators and WebAPI (done)

- `InvoiceUpdateValidator`:
  - Add `SellerId` `NotEmpty` ("Please specify seller Id") and `CustomerId` `NotEmpty` ("Please specify customer id"), the same messages as `InvoiceAddValidator`.
  - Validate lines with `InvoiceItemValidator`, as `InvoiceAddValidator` does.
  - Remove the seller and customer validators. Keep the `InvoiceNumber` rule.
- Delete `InvoiceItemUpdateValidator.cs`, its line in `Validators/DependencyInjection.cs`, and its tests.
- `InvoiceValidator` (`InvoiceModelUpdateValidator.cs`): back to `AbstractValidator<InvoiceModel>`.
- `InvoiceUpdateRequestExample`: the 3.0.1 shape. The ids come from the other examples, as in `InvoiceAddRequestExample`.
- `InvoiceController`: `Add` and `Update` map to `InvoiceModel`. Both `Get`s map `InvoiceModelFull` with `ToResponse`.

## Step 6: Rules and evals (done)

- `CLAUDE.md`: "Invoices store their user, seller and customer ids and show the current data. They keep their own number, dates and lines. Editing an item must not change an existing invoice or its PDF."
- `REVIEW.md` line 6: "An old invoice or its PDF changes after an item or unit is edited." Line 8 (password, hash or salt in a response, log or invoice) stays.
- `docs/review-evals.md`: set the Fix column of rows 4, 7 and 8 to the BIL-86 commit. Row 8 is fixed because there are no copies left to break. Done as `BIL-86` for now; replace it with the merge commit's hash after the merge.
- Also `.claude/skills/_add-api-feature/SKILL.md` step 2, which named the old rule as a conflict to look for.

## Step 7: Unit tests (`_write-tests` skill) (done)

- `InvoiceMappingProfileTest`:
  - `ToFull`: every property, including `User`, `Seller` and `Customer`.
  - `ToEntity`: `TotalPrice` is the sum of the lines.
  - `ToModel(InvoiceAddRequest)`: `SellerId` and `CustomerId` from the request. This is the regression test for the draft bug: three different GUIDs, so swapping any two fails.
  - `ToModel(InvoiceUpdateRequest)`: ids, number and lines from the request.
  - `ToResponse`: `UserId`, seller, customer, lines, total.
  - Remove the `UserData`/`SellerData`/`CustomerData` entries from the ignore list (lines 102–104).
- `InvoiceServiceTest`:
  - `Get(id)`: loads the three by id, keeps the stored `InvoiceNumber` and line prices. Never calls `GetWithValidation` or the item service (strict mocks catch it).
  - Paged `Get`: two invoices with the same seller and customer load each once (`Times.Once`).
  - `Add`: the number comes from the customer, and the line name and price from the item.
  - `Update`: keeps the stored `UserId`/`Status`, copies line prices from the items, sets `FilePath`.
  - `GenerateInvoiceName`: an invoice with number 5 whose customer is at 9 prints `000005`. This is the eval 4 regression test.
- Validators: `InvoiceUpdateValidator` cases for empty `SellerId`, empty `CustomerId`, and an invalid line. Remove `InvoiceItemUpdateValidatorTest`.
- `DependencyInjectionTests`, `InvoiceControllerTest`, `InvoiceSwaggerExampleTest`: new types and shape.
- Done as:
  - `ToResponse` is tested in the new `InvoiceFullMappingProfileTest.cs`, one test file per source file.
  - The eval 4 test is `GeneratePDF_GivenValidId_ReturnsPdfNamedWithInvoicesOwnNumber`: number 5, customer at 9, file `WF-000005.pdf`. The new `InvoiceModelTest` covers the `D6` padding.
  - Undoing the eval 4 fix or the `ToModel(InvoiceAddRequest)` fix makes tests fail (checked).

## Step 8: BillioIntegrationTest and BillerFrontEnd (branch `BIL-86-invoice-remove-serialized-data`) (done)

Run `_sync-repos` first. Both bump BillerContracts.Clients to 3.0.1.

BillioIntegrationTest, `InvoiceTest.cs`:
- Rewrite the update tests to send ids and lines (`Id`, `Quantity`, `Comments`). Remove the cases that edit the seller and customer copies and line names and prices.
- Add one success test and one error test per new rule:
  - Edit the seller, customer and user through their own endpoints: `GET /invoices/{id}` shows the new data.
  - Edit an item's price: the invoice line keeps the old price, and `PUT /invoices` copies the new one.
  - Delete an item used by an invoice: `GET` and the PDF still work.
  - Create two invoices for one customer: each `GET` returns its own number.
  - `PUT` with empty `SellerId` or `CustomerId`: 400, with the messages from step 5.
  - `PUT` with a seller of another user, a customer of another seller, or an item of another customer: 400.
  - Delete a seller or customer that has invoices: 400.
- Done as:
  - The update tests build the request from the invoice (`UpdateRequestFromInvoice`). The six tests that edited copies are replaced by `InvoiceIdsUpdate_Invalid_Fail` (no seller id, no customer id, empty list, no item id, quantity 0, seller of another user, customer of another seller, item of another customer) and `InvoiceUpdate_NotFound_Fail`.
  - `InvoiceAddMultipleToSameCustomer_Valid_Success` also reads the first invoice after the second is added: number 1 (eval 4).
  - The tests that edit data use their own user, seller, customer and item (`AddInvoiceWithOwnData`), and run after the count tests, as the status tests do. Tests run in parallel, so editing shared rows would break other tests.
  - Not tested: anything a PDF shows, including the user edit, because the integration tests don't check PDFs (owner's decision after review; PDF checks taken out in BillioIntegrationTest 27f4be9). `InvoiceDocumentFactoryTest` in Biller checks that a PDF is returned from its first byte. The seller delete, because a seller with invoices also has customers, whose foreign key fails first (`seller_id`), as it does today. The customer case deletes the item first, so only `invoices_customer_id` is left.
  - `AssertInvoiceDataUnchanged` no longer compares `Customer.InvoiceNumber`: it is the customer's next number, and other tests add invoices to the same customer.
  - Found by the PDF tests, before they were taken out: `InvoiceDocumentFactory.GeneratePdf` returned the stream without rewinding it, so `POST /invoices/generate` failed with a 500. Fixed in this branch (`stream.Position = 0`), eval 9.
  - Run against a throwaway stack from this branch: 155 passed, three runs.

BillerFrontEnd, `Invoices_update.razor`:
- Seller and customer become pickers by id. Lines have item, quantity and comments; the price on the page comes from the item list, for the totals.
- Replace the two AutoMapper maps in `MappingProfiles/InvoiceMappingProfile.cs:18-20` with a hand-written `ToUpdateRequest`, per the no-mapping-library rule.
- Ask before adding comments or hint text to the page.
- Done as: `InvoiceResponseExtensions.ToUpdateRequest()` for the invoice and its lines (BIL-91's naming and its missing seller/customer rule), removed from `StartupMapper`. The page already had seller, customer and item pickers. The row and total prices come from the current items, as `PUT` saves them. The page loads against the throwaway stack with the right values and total. Saving needs a browser check.

## Step 9: What BIL-65 has to change after this

Note these in `docs/plans/BIL-65-series.md` when BIL-86 merges:
- Renumber its changesets to 034 and 035.
- Its series migration can't read `customer_data`. Use `c.invoice_name` for every invoice of the customer, and stop writing `customer_data` in its rollback. Old invoices with `prefix_changed` (pre-check 1) go into the customer's current series. Its duplicate-number pre-check covers that.
- BillerContracts 4.0.0 (#12): it has BIL-86's `InvoiceUpdateRequest`, without `InvoiceNumber`.
- Rebase onto the `InvoiceModel`/`InvoiceModelFull` split. Its `SeriesName` replaces `Customer.InvoiceName` in `GenerateFileLocation`. Eval 4 is already fixed.
- Its rule that an invoice can't move to another seller or customer goes into `Update` from step 4.

## Step 10: Check on a copy of production, then deploy

1. On production: `make backup`. Copy the dump to the dev PC.
2. Dev PC, on master: restore it into the local `Data` (`scripts/db-restore.sh <file>`; it replaces the local data, so ask first). Run the pre-checks and save their output.
3. Save `GET /invoices` (all pages) and the PDF of every invoice (SF and ADA, LT and EN) as text with `pdftotext`, to compare in steps 5 and 6.
4. Check out this branch, `docker compose up -d --build`. `docker compose logs liquibase` shows changeset 33. The invoice count matches pre-check 2.
5. Save the same output again and diff it. Expected: the invoices in pre-check 1 show the current seller, customer, user or prefix. Those with `printed_number_changes` print their own number. All others are identical.
6. Rollback test: `rollback-count 1`, run master's code, generate the PDFs again. The invoices outside pre-check 1 match step 3. Then check out the branch and update again.
7. Deploy Biller and BillerFrontEnd together with `make update` (it backs up first). Keep that backup until production has been checked.

## Out of scope

- Moving lines out of `items_data` into an `invoice_items` table. They stay JSON (decision 9). Jira says BIL-86 blocks BIL-75, which needs typed lines. Either move that link to a new issue for the lines, or BIL-75 does the move itself.
- Stopping invoice moves to another seller or customer, and making the number read-only: BIL-65.
- Duplicate numbers when adding two invoices at once (eval 5): BIL-65.
- The rest of BillerFrontEnd's AutoMapper use.

## Order of work

1. BillerContracts #11: remove the usings, merge, publish 3.0.1.
2. Biller PR on this branch: migration, repository, models, mapping, service, validators, controller, Swagger example, unit tests, rules, evals.
3. BillioIntegrationTest and BillerFrontEnd PRs on the same branch name.
4. Step 10, then deploy Biller and BillerFrontEnd together.
5. BIL-65 picks up step 9 and BillerContracts #12 (4.0.0).

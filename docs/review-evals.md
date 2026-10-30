# Known bugs to test reviews against

Real Biller bugs, with the commit that introduced each one. They are a small eval set for the
review setup (`REVIEW.md`, the `_pr-review` skill, `/code-review`).

**When you change `REVIEW.md` or the `_pr-review` skill**, run `/code-review` on two or three of the
introducing commits below and check that the bug is still reported. Keep the change only if it
catches at least as many as before.

**When a bug is found** (in review, in tests, or in production), add a row: what went wrong, the
commit that introduced it, and the fix (commit, or the Jira key while it's open).

| # | Bug | Introduced | Fixed |
|---|---|---|---|
| 1 | `InvoiceValidator.IsValidItemsId` always returned `false`, so every invoice with items failed validation. | af7d892 | 27f1f9a |
| 2 | `ItemAddValidator` and `ItemUpdateValidator` were registered twice in `src/Validators/DependencyInjection.cs` (copy-paste). | af7d892 | 74d356d |
| 3 | `ItemSwaggerExampleTest` built an `InvoiceListResponseExample`, so the item list example was never checked. | 013c8bf | 38e3c49 |
| 4 | `InvoiceModel.GenerateInvoiceName()` / `GenerateFileLocation()` use the customer's next number instead of the invoice's own `InvoiceNumber`, so the PDF and file name of an old invoice show a later number. | e2b7135 | BIL-86 |
| 5 | `InvoiceService.Add` saves the invoice and increases the customer's number in two statements, so a crash or two requests at once can give a duplicate number. | 0a480d5 | e5ff9b8 (BIL-65) |
| 6 | New invoices are saved with status `Payed`: `Status` is never set, and `Payed` is the enum's 0 value. | 1fdf8a4 | open, BIL-73 |
| 7 | Every invoice stores the whole `UserModel`, including the password hash, as JSON in `user_data`. | 0a480d5 | BIL-86 |
| 8 | Editing an invoice (`PUT /invoices`) saves the seller and customer copies from the request, which has no `UserId`/`SellerId`, so the stored `Seller.UserId` and `Customer.SellerId` become empty GUIDs. | 013c8bf | BIL-86 |
| 9 | `InvoiceDocumentFactory.GeneratePdf` returns the PDF `MemoryStream` without rewinding it, so `POST /invoices/generate` sends no bytes and fails with a 500 (`Content-Length mismatch`). Unit tests mock the factory; the BIL-86 integration tests were the first to download a PDF. | cdedf6b | BIL-86 |
| 10 | `PUT /invoices` can move an invoice to another seller of the same user while it keeps the first seller's series and number, so the new seller can print two invoices with the same series and number. | e5ff9b8 | BIL-65 |
| 11 | Deleting the latest invoice of a series when its number is 0 (possible for invoices from before migration 020) sets `next_number` to 0, breaks `ck_series_next_number`, and returns 500. | e5ff9b8 | BIL-65 |
| 12 | The rollback of migration 034 leaves invoices made after deploy without `InvoiceName`/`InvoiceNumber` in their customer copy, so the old code prints them with an empty series and number 000000. | cc845e2 | BIL-65 |
| 13 | Migration 034 inserts the series name from old invoices' customer copy into `series.name` (`varchar(10)`), but `PUT /invoices` saved that copy from the request and only checked that the name wasn't empty, so a longer name makes the changeset fail on deploy. No pre-check looked for it. | cc845e2 | BIL-65 (pre-check query) |
| 14 | `PUT /series` accepts any `NextNumber`. With `int.MaxValue`, reserving the next number (`next_number + 1`) overflows and every later invoice in that series returns 500. | e5ff9b8 | BIL-65 |

For an open bug, review the introducing commit. Once it's fixed, also check that reverting the
fix's source change makes a test fail.

# Known bugs to test reviews against

Real Biller bugs, with the commit that introduced each one. They are a small eval set for the
review setup (`REVIEW.md`, the `_pre-pr` skill, `/code-review`).

**When you change `REVIEW.md` or the `_pre-pr` skill**, run `/code-review` on two or three of the
introducing commits below and check that the bug is still reported. Keep the change only if it
catches at least as many as before.

**When a bug is found** (in review, in tests, or in production), add a row: what went wrong, the
commit that introduced it, and the fix (commit, or the Jira key while it's open).

| # | Bug | Introduced | Fixed |
|---|---|---|---|
| 1 | `InvoiceValidator.IsValidItemsId` always returned `false`, so every invoice with items failed validation. | af7d892 | 27f1f9a |
| 2 | `ItemAddValidator` and `ItemUpdateValidator` were registered twice in `src/Validators/DependencyInjection.cs` (copy-paste). | af7d892 | 74d356d |
| 3 | `ItemSwaggerExampleTest` built an `InvoiceListResponseExample`, so the item list example was never checked. | 013c8bf | 38e3c49 |
| 4 | `InvoiceModel.GenerateInvoiceName()` / `GenerateFileLocation()` use the customer's next number instead of the invoice's own `InvoiceNumber`, so the PDF and file name of an old invoice show a later number. | e2b7135 | open, BIL-65 |
| 5 | `InvoiceService.Add` saves the invoice and increases the customer's number in two statements, so a crash or two requests at once can give a duplicate number. | 0a480d5 | open, BIL-65 |
| 6 | New invoices are saved with status `Payed`: `Status` is never set, and `Payed` is the enum's 0 value. | 1fdf8a4 | BIL-73, PR pending |
| 7 | Every invoice stores the whole `UserModel`, including the password hash, as JSON in `user_data`. | 0a480d5 | open, BIL-86 |

For an open bug, review the introducing commit. Once it's fixed, also check that reverting the
fix's source change makes a test fail.

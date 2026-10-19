# Plan: update invoice generate (BIL-69)

Jira: [BIL-69](https://aldisadom.atlassian.net/browse/BIL-69). Epic: BIL-89 (Billeris).

## Goal (from the ticket)

- In Swagger, the document type and language are drop-downs built from the enums.
- The caller chooses what to generate.
- The file name gets a prefix that depends on the document type.
- Swagger examples and their tests are updated.

## Current state (master @ 125ae5e)

| Where | What |
|---|---|
| `InvoiceController.GeneratePDF` | `POST invoices/generate` with `[FromQuery] InvoiceGenerateRequest` (`Id`, `LanguageCode` = LT, `DocumentType` = Invoice). Returns `File(stream, "application/pdf", fileName)` |
| `[ProducesResponseType]` on it | **Wrong:** says 200 returns `InvoiceGenerateRequest`. No 400 response listed, though the validator can fail |
| `DocumentType` (BillerContracts) | `Invoice` → `InvoiceDocumentSF`, `JobDoneAct` → `InvoiceDocumentADA` (`InvoiceDocumentFactory`) |
| Choosing what to generate | **Already works** through `DocumentType` and `LanguageCode` |
| Swagger enums | `JsonStringEnumConverter` is registered in `Program.cs`, and Swashbuckle 9 normally reads it, so query enums should already be string drop-downs. Not checked in the UI yet |
| `InvoiceModel.GenerateFileLocation()` | `{Customer.InvoiceName}-{000005}.pdf`. **The same name for both document types and both languages** |
| Front end `Invoices_id.razor` | "Generate invoice" and "Generate job done act" buttons. Language is hard-coded to LT. It saves the file under the name from `Content-Disposition` into one folder, **so the act overwrites the invoice PDF** |
| `InvoiceGenerateValidator` | Only checks that `Id` is not empty. Invalid enum values are checked in `InvoiceService.GeneratePDF` |
| Tests | `InvoiceControllerTest.GeneratePDF_GivenValidRequest_ReturnsFileStreamResult`. `InvoiceServiceTest` checks `fileName == invoice.GenerateFileLocation()`. No Swagger example for generate |

## Design decisions

1. **File name: `{prefix}-{series}-{number}[-{LANG}].pdf`.**
   - Prefix by document type: `SF` (invoice, "sąskaita faktūra") and `ADA` (job done act, "atliktų darbų aktas"). These match the document class names.
   - The language is added only when it isn't LT, e.g. `SF-ABC-000005.pdf` and `ADA-ABC-000005-EN.pdf`. Existing LT downloads keep a short name, and an EN copy doesn't overwrite the LT one.
2. **The prefix lives in one place:** a `DocumentType` → prefix map in Application (e.g. `DocumentTypeExtensions.FilePrefix()`). A new document type without a prefix fails a test, not at runtime.
3. **`GenerateFileLocation` takes the document type and language** instead of guessing. It moves next to the factory, since the name now depends on what is generated.
4. **Keep `POST` and `[FromQuery]`.** `GET` would fit better, but changing it breaks the front end client for no user-visible gain.
5. **Enum validation moves to the validator** (`IsInEnum()` for both fields). The controller then returns 400 through the normal validation path, and the service checks become a guard only.

## Dependency

**Do after BIL-65.** BIL-65 changes `GenerateFileLocation()` and `GenerateInvoiceName()` to use the invoice's own number and the series name snapshot. Doing this first would mean writing that method twice. Add a Jira link: BIL-65 blocks BIL-69.

## Step 1: Check Swagger as it is

- Run the stack and open Swagger. If `LanguageCode` and `DocumentType` already show as drop-downs with names, nothing is needed there.
- If they show as numbers, add `c.UseInlineDefinitionsForEnums()` and make sure the enum schema uses names (Swashbuckle reads `JsonStringEnumConverter` from `JsonOptions`).
- Add `[Description]` or XML comments on the request properties if the drop-down needs explaining.

## Step 2: Application

- `DocumentTypeExtensions.FilePrefix(this DocumentType)`: `Invoice` → `"SF"`, `JobDoneAct` → `"ADA"`, anything else throws `ArgumentOutOfRangeException`.
- `InvoiceModel.GenerateFileLocation(DocumentType documentType, Language language)` builds the name from decision 1.
- `InvoiceService.GeneratePDF` passes both values through.
- `InvoiceModelExtensions.ToEntity` uses `GenerateFileLocation()` for `FilePath`. BIL-65 drops `file_path`. If BIL-65 is done first (recommended), nothing to do here. If not, pass `DocumentType.Invoice, Language.LT`.

## Step 3: Validators

`InvoiceGenerateValidator`: add `RuleFor(x => x.LanguageCode).IsInEnum()` and `RuleFor(x => x.DocumentType).IsInEnum()` with messages.

## Step 4: WebAPI

```csharp
[HttpPost("generate")]
[ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK, "application/pdf")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
```
Update the XML summary to list the two document types and the file name format.

Swagger examples: Swashbuckle.Filters doesn't apply request examples to query parameters, so there's no `InvoiceGenerateRequestExample`. The enum defaults (`LT`, `Invoice`) show as the drop-down defaults. If an error example is wanted, add a 400 `ErrorResponse` example and its test in `InvoiceSwaggerExampleTest`.

## Step 5: Tests (`write-tests` skill)

- `DocumentTypeExtensions`: a theory over **every** `DocumentType` value (`Enum.GetValues`) so a new type without a prefix fails. Plus an out-of-range value throws.
- `InvoiceModel.GenerateFileLocation`: theory with labels, e.g. SF/LT → `SF-ABC-000005.pdf`, ADA/EN → `ADA-ABC-000005-EN.pdf`, number padding at 9, 10, 99999, 100000.
- `InvoiceGenerateValidator`: invalid language and document type values fail. Valid ones pass.
- `InvoiceServiceTest`: `GeneratePDF` returns the name for the requested type and language.
- `InvoiceControllerTest`: update the existing test if the signature changes.
- Integration test: generate both types, check `Content-Disposition` has the expected names.

## Step 6: Front end (BillerFrontEnd, separate PR)

- Add a language selector (LT/EN) next to the generate buttons instead of hard-coded `Language.LT`.
- Nothing else is needed: the file name comes from `Content-Disposition`, so the overwrite problem is fixed by the API change.

## Out of scope

- Generating both documents in one call (zip).
- Switching to `GET`.

## Order of work

1. BIL-65 first.
2. Check Swagger (Step 1).
3. API change and tests in one PR, then the integration test.
4. Front end language selector.

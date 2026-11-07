# Plan: update invoice generate (BIL-69, with BIL-97)

Jira: [BIL-69](https://aldisadom.atlassian.net/browse/BIL-69). Epic: BIL-89 (Billeris).
Done together with [BIL-97](https://aldisadom.atlassian.net/browse/BIL-97) (download the PDFs in
the browser), which only changes BillerFrontEnd. See Step 6.

## Changes since the plan was written (checked at master @ 9ff9fb1)

- BIL-65 is done. `GenerateFileLocation()` is now `{SeriesName}-{000005}.pdf` (series name, not
  the customer's invoice name), and `file_path` is gone, so Step 2's last bullet needs nothing.
- BIL-73 is done, so BIL-97's `GeneratePdfClick` change no longer collides with it.
- `InvoiceDocumentFactory.GeneratePdf` still computes an unused `path` from `GenerateFileLocation()`.
  Remove it.
- BillerContracts is at 4.2.0. No contracts change is needed: `InvoiceGenerateRequest`,
  `DocumentType`, `Language` and `IInvoiceClient.GeneratePDF` stay as they are.
- Front end page is `src/FrontEnd/Components/Pages/Invoice/Invoices_id.razor`.

## Goal (from the ticket)

- In Swagger, the document type and language are drop-downs built from the enums.
- The caller chooses what to generate.
- ~~The file name gets a prefix that depends on the document type.~~ Dropped: the name stays
  `{series}-{number}.pdf`, and the front end lets the user edit it when saving (decision 1).
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

1. **File name stays `{series}-{number}.pdf`** (e.g. `ABC-000005.pdf`) for every document type and
   language: no `SF`/`ADA` prefix, no language suffix (owner's decision, 2026-11-07). The overwrite
   problem is solved in the front end instead: clicking a generate button opens a save dialog with
   this name suggested, and the user can edit it before saving (Step 6).
2. ~~Prefix map~~: dropped with decision 1.
3. ~~`GenerateFileLocation` takes the document type and language~~: dropped with decision 1.
   `GenerateFileLocation()` is unchanged.
4. **Keep `POST` and `[FromQuery]`.** `GET` would fit better, but changing it breaks the front end client for no user-visible gain.
5. **Enum validation moves to the validator** (`IsInEnum()` for both fields). The controller then returns 400 through the normal validation path, and the service checks become a guard only.

## Dependency

**Do after BIL-65.** BIL-65 changes `GenerateFileLocation()` and `GenerateInvoiceName()` to use the invoice's own number and the series name snapshot. Doing this first would mean writing that method twice. Add a Jira link: BIL-65 blocks BIL-69.

## Step 1: Check Swagger as it is

- Run the stack and open Swagger. If `LanguageCode` and `DocumentType` already show as drop-downs with names, nothing is needed there.
- If they show as numbers, add `c.UseInlineDefinitionsForEnums()` and make sure the enum schema uses names (Swashbuckle reads `JsonStringEnumConverter` from `JsonOptions`).
- Add `[Description]` or XML comments on the request properties if the drop-down needs explaining.

## Step 2: Application

- No file name change (decision 1). Only remove the unused `path` line in `InvoiceDocumentFactory`.

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

- `InvoiceGenerateValidator`: invalid language and document type values fail. Valid ones pass.
- `InvoiceServiceTest`: `GeneratePDF` returns `{series}-{number}.pdf` for an LT invoice and an EN act alike.
- `InvoiceControllerTest`: update the existing test if the signature changes.
- Integration test: generate both types, check `Content-Disposition` has the expected names.

## Step 6: Front end (BillerFrontEnd, branch `BIL-69-invoice-generate`, with BIL-97)

- Replace the two generate buttons with a document type drop-down (Invoice, Job done act), a
  language drop-down (LT/EN, instead of hard-coded `Language.LT`) and one Generate button.
- BIL-97: `GeneratePdfClick` writes the PDF to `MyDocuments` on the server (inside the container in
  Docker), so the user gets nothing. Add `wwwroot/js/download.js` with
  `downloadFileFromStream(fileName, streamRef)`, load it in `App.razor` after `blazor.web.js`, and
  have `GeneratePdfClick` pass `new DotNetStreamReference(fileResult.Content)` to it through
  `IJSRuntime`.
- The download suggests the `Content-Disposition` name and lets the user edit it:
  `showSaveFilePicker({ suggestedName })` (Chrome, Edge) opens the system save dialog; cancelling
  saves nothing. Where it's missing (Firefox, Safari) or refused (the click's user activation
  expired while the PDF was generated), `prompt("Save as", name)` asks for the name, then a
  temporary `<a download>` saves it.
- Check in the browser: both buttons, LT and EN: the dialog shows `{series}-{number}.pdf`, an edited
  name is used, cancel saves nothing, and the PDF opens.

## Out of scope

- Generating both documents in one call (zip).
- Switching to `GET`.

## Order of work

1. BIL-65 first. **Done.**
2. Check Swagger (Step 1). **Done:** `LanguageCode` and `DocumentType` already are string enums
   (`LT`/`EN`, `Invoice`/`JobDoneAct`) in `swagger.json`, so nothing changed there.
3. API change and tests in one PR, then the integration test. **Done** (Steps 2 to 5). Model binding
   already answers an undefined enum value (`999`) with 400 "The value '999' is invalid." before the
   validator runs, so the validator rules and the service checks are both guards. Integration
   tests on BillioIntegrationTest branch `BIL-69-invoice-generate`: every type and language gets
   `{series}-{number}.pdf`, undefined language and document type, empty id, not found.
4. Front end language selector and BIL-97 download. **Done** on BillerFrontEnd branch
   `BIL-69-invoice-generate`. Still to check by hand in a browser: both buttons, LT and EN.

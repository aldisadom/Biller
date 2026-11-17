# Plan: reject text the PDF font can't draw when it is saved; clean up AddPdfGenerator (BIL-105)

Jira: [BIL-105](https://aldisadom.atlassian.net/browse/BIL-105). Epic: BIL-89 (Billeris). Relates to BIL-94 (Done).

## Answers from the owner (2026-11-14)

1. A PDF that still hits a missing glyph returns **400 "Validation failure"**, with as much detail as
   possible: which record and field, and which characters (decision 6). *Replaced on 2026-11-16:
   PDF generation checks nothing, see below.*
2. **Integration tests are added** (step 8), on a BillioIntegrationTest branch with the same name.
3. Decisions 7 and 8 are the right reading of F7, F12 and F14.
4. **`State` gets no rule**: it isn't printed. *Replaced on 2026-11-16, see below.*

## Change from the owner (2026-11-16)

- **No check of the whole invoice before its PDF is generated.** `InvoiceService` has no
  `CheckDocumentTexts` and no checker again (as on master). The integration tests download every
  PDF, so a download that succeeds is enough. Step 4's `InvoiceService` line and step 7's
  `InvoiceServiceTest` line are undone.
- **No safety net at PDF generation either** (second change that day). Decision 6 is dropped:
  `InvoiceDocumentFactory` and `ErrorChecking` are as on master, and `DocumentTextException` is
  gone. A PDF that still hits a missing glyph (only data changed by hand in the database: production
  has no such character, BIL-94 pre-check, and the API can't store one any more) fails with
  QuestPDF's `DocumentDrawingException`, a 500 "General error", as on master. Step 3's
  `DocumentTextException`, step 4's factory line, step 6's `ErrorChecking` line and step 7's
  `InvoiceDocumentFactoryTest` line are undone; `ErrorCheckingTest` tests a `ValidationException`
  instead.
- **Every string field of the user, customer, seller, item and series requests gets the rule**,
  printed or not: also customer and seller `State` and user `Email`. Not `Password`: it is only
  hashed. Invoice and line comments keep the rule.
- Unit tests: 934 passed, line 93.8%, branch 89.4% (minimums 92.8 and 88.4). Integration tests: 3 new error cases (customer add `State`, seller update
  `State`, user add `Email`).

## Done on the branch (2026-11-15)

Steps 3 to 8 are done; nothing is committed or pushed yet. Unit tests: 949 passed, line 93.7%,
branch 88.4% (the CI minimums are raised to 92.7 and 87.4). Integration tests on the
`BIL-105-pdf-font-text-validation` branch of BillioIntegrationTest: 232 of 232, 3 runs, against a
throwaway stack (13 new error cases, add and update, and a seller saved with Lithuanian letters). On that stack: `POST /items` with `Dragon 🐉 鐵` gives 400 naming both characters;
`POST /invoices/generate` for an invoice whose seller name and comment were given an emoji in the
database gives 400 naming both fields, logged as a warning without a stack trace; with clean data it
gives the PDF. (Since 2026-11-16 PDF generation checks nothing: such data gives QuestPDF's 500
"General error", as on master.)

Done differently from the plan below:

- Decision 3: a character must be in **every configured font file**, not every file of the documents'
  family. Telling a file's family needs the font's name table; every configured file is simpler and
  stricter, and the same thing today (one file).
- A tab is not allowed: Calibri has no glyph for it. Line feed and carriage return are.
- `EnsureFontFamilyRegistered` stays in `DependencyInjectionInvoiceGeneration`; `PdfGeneratorSetup` calls it.
- The generate-time message names the line number, not the item's name ("Item name on line 2"),
  so no document text reaches the log.
- The classes that touch QuestPDF's global settings share one xUnit collection, so they don't run in
  parallel.
- After the review (2 rounds): every control character except line feed and carriage return is
  rejected whatever the font maps (Calibri maps U+0000); the reader uses the format 12 tables when
  a font has one, else format 4; startup fails when the configured files don't all have A-Z, a-z
  and 0-9; the generate-time exception carries no inner exception.
- Decision 2 names the zero-width joiner and the soft hyphen as rejected. Calibri has both, so they
  are allowed, and the render test shows they draw.
- Step 7 has no "agreement" test for a tab; the render test covers every character the checker allows.

Not done: step 9's look at a front-end page in the browser, and the comparison of the LT and EN
sample PDFs with master's (the document code and QuestPDF settings are unchanged; one LT invoice
rendered on the stack).

## Goal

- Saving an item, seller, customer, user, series or invoice with a character Calibri can't draw (an
  emoji, Chinese) returns 400 and names the character.
- A PDF that still hits such a character (old data) returns a clear 4xx, not a 500 "General error"
  with a stack trace in the log.
- `AddPdfGenerator` has no unused code, checks its arguments before it changes QuestPDF's global
  settings, and `FontSetting.Name` is gone.

## Current state (master @ ea8d434)

| Where | What |
|---|---|
| `Infrastructure/DependencyInjectionInvoiceGeneration.cs` `AddPdfGenerator` | Sets the five global `QuestPDF.Settings` first, then checks the arguments. Per font: checks `Path` and `Name`, `File.ReadAllBytes` into an unused variable (`aa`), then `FontManager.RegisterFontFromStream`. Ends with `EnsureFontFamilyRegistered`. Registers no services |
| `Domain/IOptions/FontSettings.cs` | `FontSetting.Path` and `Name`. `Name` is only checked for empty and used in the "file not found" message: QuestPDF 2026.9 registers a font under the family name stored in the file |
| `appsettings.LocalDocker.json` | The only `FontSettings` in the repo: `/Fonts/calibri.ttf`, `Name: calibri`. `docker-compose.yml` mounts `./Fonts` and runs as `LocalDocker` |
| `StartupInjection.cs:33` | Calls `AddPdfGenerator(services.BuildServiceProvider().GetRequiredService<IOptions<FontSettings>>())` |
| QuestPDF 2026.9.1 | No public API tells whether a font has a character. A missing one throws `DocumentDrawingException`; its message holds the whole text and a `Missing glyphs:` line |
| `InvoiceDocumentFactory.GeneratePdf` | Lets `DocumentDrawingException` through. `InvoiceDocumentFactoryTest` expects it for an emoji and for Chinese |
| `WebAPI/Middleware/ErrorChecking.cs` | No case for it, so it falls to `default`: 500 "General error", logged with the stack trace. The middleware has no unit test |
| Validators | Parameterless constructors, registered scoped. `InvoiceAddValidator` and `InvoiceUpdateValidator` build `new InvoiceItemValidator()` themselves |
| Text printed on the PDFs | Series `Name`; user `Name`, `LastName`; seller `CompanyName`, `CompanyNumber`, `Street`, `City`, `Phone`, `Email`, `BankName`, `BankNumber`; customer `CompanyName`, `CompanyNumber`, `Street`, `City`, `Phone`, `Email`; item `Name`; invoice `Comments`; invoice line `Comments`. `State` is not printed |

## Design decisions

1. **The character map is read from the font file's `cmap` table by a small hand-written reader**
   (formats 4 and 12), because QuestPDF exposes no lookup and SkiaSharp was removed in BIL-94. No new
   package. The alternatives were a font package (a new dependency and licence to watch) or a test
   render per saved text (slow, and control flow by exception).
2. **The rule may reject more than QuestPDF would, never less.** A character is allowed when the
   documents' font family has it, plus line breaks. Characters QuestPDF draws without a glyph
   (zero-width joiner, soft hyphen) are rejected: that is safe, and they are invisible anyway. A test
   renders every allowed character through QuestPDF and expects no exception, so the map can't allow
   something the PDF fails on.
3. **One service, `IDocumentTextChecker`** (`Application/Interfaces`), with
   `IReadOnlyList<string> MissingCharacters(string? text)`. `AddPdfGenerator` builds the implementation
   from the registered font files and registers it as a singleton. With several files of the family, a
   character must be in all of them (today there is one file, `calibri.ttf`).
4. **One shared FluentValidation rule, `.DrawableOnPdf(checker)`**, in `Validators`. Message:
   `Please use only characters the invoice PDF can show in <field>. Can't show: 🐉 (U+1F409)`. It
   lists each missing character once, at most 5, with its code point. Null and empty pass; the
   existing `NotEmpty` rules stay as they are.
5. **The rule goes on every field printed on a PDF** (the table's last row), in the add and update
   validators of item, seller, customer, user and series, and on `Comments` and line `Comments` in
   `InvoiceAddValidator`, `InvoiceUpdateValidator`, `InvoiceItemValidator` and
   `InvoiceModelUpdateValidator`. Validators take the checker in the constructor; the two invoice
   validators pass it on to `InvoiceItemValidator`.
6. *Dropped on 2026-11-16 (owner): PDF generation checks nothing and a missing glyph stays
   QuestPDF's 500, as on master. Kept below for the record.*
   **A PDF that still hits a missing glyph returns 400 "Validation failure".**
   `InvoiceService` checks the invoice's printed texts with the same checker before generating and
   throws `DocumentTextException` (new, `Domain/Exceptions`). Its message names the record, the field
   and the characters, and says how to fix it, e.g. `Item "Dragon" name has characters the invoice PDF
   can't show: 🐉 (U+1F409). Edit the item, then save the invoice again`. Every such field of the
   invoice is listed, not only the first.
   `InvoiceDocumentFactory` also turns a `DocumentDrawingException` whose message has
   `Missing glyphs` into that exception, without the document text, as a safety net for a text the
   check missed. Every other `DocumentDrawingException` (a layout error) stays a 500.
   `ErrorChecking` maps `DocumentTextException` to 400, so it is logged as a warning without a stack
   trace and without document text.
7. **F7, F12: the global setup moves to its own class.** `PdfGeneratorSetup.Apply(FontSettings,
   requiredFamily)` checks the arguments first, then sets the QuestPDF settings, registers the fonts,
   checks the family and returns the checker. `AddPdfGenerator` only calls it and registers the result.
   A bad argument then leaves QuestPDF's settings untouched.
8. **F14: remove `FontSetting.Name`**, from the class and from `appsettings.LocalDocker.json`. A
   settings file that still has `Name` keeps working: configuration binding ignores unknown keys, so
   the small PC needs no change before the deploy. The "file not found" message names the path.
9. **F4: delete the unused `File.ReadAllBytes`.**
10. **No data change.** Production has no such character (BIL-94 pre-check). Old rows are not
    cleaned.

## Dependencies

- None open. BIL-94 is Done.
- BIL-93 (READMEs) describes configuration; whichever of the two merges second mentions that
  `FontSettings` has only `Path`.

## Step 1: BillerContracts

No change: no new field, endpoint or status code type.

## Step 2: Migration

None.

## Step 3: Domain and Infrastructure

- [x] ~~`Domain/Exceptions/DocumentTextException`.~~ Removed on 2026-11-16 (owner).
- [x] `Domain/IOptions/FontSettings.cs`: remove `Name`.
- [x] `Infrastructure/FontCharacterReader`: reads a `.ttf` stream's `cmap` (format 12 when present,
      else format 4) into a set of code points. Throws `ArgumentException` naming the path for a file
      without a usable `cmap`.
- [x] `Infrastructure/DocumentTextChecker : IDocumentTextChecker`: the set, plus `\n` and `\r`.
      Whether a tab is allowed is decided by the render test in step 7.
- [x] `Infrastructure/PdfGeneratorSetup.Apply` (decision 7), without the `ReadAllBytes`.
- [x] `AddPdfGenerator`: call it, `services.AddSingleton<IDocumentTextChecker>(...)`.

## Step 4: Application

- [x] `Application/Interfaces/IDocumentTextChecker`.
- [x] ~~`InvoiceService` (PDF generation): check the printed texts first, throw `DocumentTextException`.~~
      Undone on 2026-11-16 (owner): `InvoiceService` is as on master.
- [x] ~~`InvoiceDocumentFactory.GeneratePdf`: missing-glyph `DocumentDrawingException` →
      `DocumentTextException`.~~ Undone on 2026-11-16 (owner): as on master.

## Step 5: Validators

- [x] `Validators/DocumentTextRule.cs`: the `DrawableOnPdf` extension.
- [x] The validators in decision 5: constructor parameter and one rule per printed field.
- [x] `Validators/DependencyInjection.cs`: no change (constructor injection). `InvoiceItemValidator`
      is registered there and also built by hand in two validators; both get the checker.

## Step 6: WebAPI

- [x] ~~`ErrorChecking`: `case DocumentTextException` → 400 "Validation failure".~~ Undone on
      2026-11-16 (owner): as on master.
- [x] `appsettings.LocalDocker.json`: remove `Name`.
- [x] `StartupInjection.cs`: no change beyond what compiles.

## Step 7: Tests (`_write-tests`)

- [x] `FontCharacterReaderTest`: Calibri has every Lithuanian letter and `€`; it has no emoji and no
      Chinese; a file that isn't a font throws.
- [x] `DocumentTextCheckerTest`: null, empty, plain text, a line break, an emoji (a surrogate pair is
      one character in the result), a lone surrogate, the same character twice listed once.
- [x] **Agreement with QuestPDF:** a document with every allowed character renders without an
      exception; `\t` is tried and then allowed or not, with the result written here.
- [x] Validator tests: per validator, one theory case per printed field with an emoji, checking the
      message; a case that passes with Lithuanian text.
- [x] ~~`InvoiceServiceTest`: a stored emoji throws `DocumentTextException` and the factory is not called.~~
      Undone on 2026-11-16 with the check.
- [x] ~~`InvoiceDocumentFactoryTest`: the three existing missing-glyph cases expect
      `DocumentTextException` and a message without the item name.~~ Undone on 2026-11-16: they
      expect `DocumentDrawingException`, as on master.
- [x] `ErrorCheckingTest` (new file): ~~`DocumentTextException`~~ a `ValidationException` (since
      2026-11-16) gives 400, "Validation failure" and the message, and logs a warning; an unknown
      exception gives 500 "General error".
- [x] `DependencyInjectionTests`: the checker is registered; a bad argument leaves the QuestPDF
      settings as they were (revert-check for decision 7); the `Name` cases go.
- [x] Coverage: compare with the CI minimums (`CLAUDE.md`).

## Step 8: Integration tests (BillioIntegrationTest, branch with the same name)

- [x] One error case per resource: add an item, seller, customer, user and series with an emoji, and
      an invoice with an emoji comment: 400, "Validation failure", the extended message.
- No test for the generate-time 400: the API can't store such text any more.

## Step 9: Front end

No change. The 400 shows through `AppErrorBoundary` like other validation errors. Check one page in
the browser against the test stack.

## Out of scope

- Other fonts, font fallback or new languages.
- Cleaning stored data.
- Replacing `services.BuildServiceProvider()` in `StartupInjection.cs`.
- A rule on fields that aren't printed (`State`, user `Email`, passwords).

## Order of work

1. Steps 3 and 4 with their tests: reader, checker, setup, the cleanup (F4, F7, F12, F14).
2. Step 5 and the validator tests.
3. Step 6 and the generate-time 400.
4. `_test-coverage`, then `_test-stack`: save an emoji item (400), render the LT and EN sample PDFs
   and compare them with master's.
5. Integration tests.

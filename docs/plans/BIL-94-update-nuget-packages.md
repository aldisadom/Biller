# Plan: update NuGet packages in every repo (BIL-94)

Jira: [BIL-94](https://aldisadom.atlassian.net/browse/BIL-94). Epic: BIL-89 (Billeris). Repos:
BillerContracts (step 1), Biller (step 2), BillerFrontEnd (step 3), BillioIntegrationTest (step 4).
It changes several repos, so it lives in Biller. Branch `BIL-94-update-nuget-packages` in each.

## Goal

- Every package is on its newest stable version that fits the limits below and `net8.0`.
- Packages shared between repos are on the same version.
- The package limits are in every repo's `CLAUDE.md`.
- Nothing changes for the user: same API responses, same Swagger, same PDFs, all tests pass.

## Current state (Biller master @ 3b81a8e, BillerContracts @ be7d36c, BillerFrontEnd @ a96800f, BillioIntegrationTest @ 8542785)

All four repos target `net8.0`. `dotnet list package --outdated` on 2026-11-08:

| Repo | Package | Now | Newest |
|---|---|---|---|
| Biller | BCrypt.Net-Next | 4.0.3 | 4.2.0 |
| Biller | Dapper | 2.1.66 | 2.1.89 |
| Biller | FluentValidation (2 projects) | 12.0.0 | 12.2.0 |
| Biller | Npgsql | 9.0.4 | 10.0.3 |
| Biller | QuestPDF | 2025.7.3 | 2026.9.1 |
| Biller | Serilog (5) / Serilog.Sinks.File (5) / Serilog.AspNetCore | 4.3.0 / 7.0.0 / 9.0.0 | 4.4.0 / 7.0.0 / 10.0.0 |
| Biller | Swashbuckle.AspNetCore, .Swagger / .Filters | 9.0.6 / 9.0.1 | 10.3.1 / 10.0.1 (Microsoft.OpenApi 1.6 → 2.x; 3 files use `Microsoft.OpenApi`) |
| Biller | Microsoft.Extensions.DependencyInjection (5 projects, one on 9.0.9), .Options | 9.0.10 | 10.0.14 |
| Biller | Microsoft.Data.Sqlite (tests) | 9.0.10 | 10.0.14. **No code uses it** |
| Biller | SkiaSharp.NativeAssets.Linux.NoDependencies, HarfBuzzSharp.NativeAssets.Linux | 3.119.1, 8.3.1.2 | 4.155.0, 14.4.0. **Leftovers from 2024**: no managed SkiaSharp in the graph, QuestPDF ships its own native library |
| Biller | xunit.v3 / xunit.runner.visualstudio | 3.2.2 / 3.1.5 | 4.0.2 / 4.0.1 |
| Biller | Microsoft.NET.Test.Sdk / coverlet.collector | 18.0.0 / 10.0.1 | 18.11.0 / 10.1.0 |
| Biller | FluentAssertions / Moq | 6.12.1 / 4.18.4 | 8.11.0 / 4.21.0 (FluentAssertions replaced, Moq limited; see decisions) |
| Biller | BillerContracts | 4.2.0 | 4.2.0 (latest on nuget.org) |
| Biller, FE, IT | Microsoft.VisualStudio.Azure.Containers.Tools.Targets | 1.22.1 / 1.21.0 / 1.22.1 | 1.23.0 |
| Contracts | Microsoft.Extensions.Configuration, .Http, .Options, .Options.ConfigurationExtensions | 8.0.x | 10.0.14 |
| Contracts | Newtonsoft.Json | 13.0.3 | 13.0.4 |
| Contracts tests | xunit 2.9.3, xunit.runner.visualstudio 2.8.2, Microsoft.NET.Test.Sdk 17.11.1, FluentAssertions 6.12.1, Moq 4.18.4 | | xUnit **v2**, while Biller is on v3 |
| FE | BillerContracts.Clients | 4.2.0 | 4.2.0 |
| FE | AutoMapper (2 projects) | 13.0.1 | 16.3.0. Removed by BIL-91 (To Do) |
| FE | Microsoft.Fast.Components.FluentUI | 3.7.8 | 3.8.0. Only an `@using` in `Invoices_add.razor`, no component used |
| FE | Microsoft.AspNetCore.Components.QuickGrid, Microsoft.Extensions.DependencyInjection | 8.0.10, 8.0.1 | 10.0.14 |
| FE | Serilog / Serilog.AspNetCore / Serilog.Sinks.File | 4.1.0 / 9.0.0 / 6.0.0 and 7.0.0 | 4.4.0 / 10.0.0 / 7.0.0 |
| IT | TUnit | 0.57.1 | 1.81.10 (major) |
| IT | Microsoft.Extensions.Configuration.EnvironmentVariables, .Json | 8.0.x | 10.0.14 |
| IT | Newtonsoft.Json | 13.0.3 | 13.0.4 |
| IT | System.Collections.Concurrent | 4.3.0 | part of .NET 8, not needed |

Differences from the issue: BillerContracts is at 4.2.0, not 2.0.0. BIL-92 and BIL-96 are Done.

## Design decisions

1. **Stay on `net8.0`.** For each package take the newest stable version whose package still has a
   `net8.0` (or `netstandard2.x`) target. Where the newest needs .NET 9/10 (likely QuickGrid 10),
   take the newest 8.x/9.x and write the reason in the table below. Moving .NET is a separate task.
2. **Moq stays 4.18.4**, pinned as `[4.18.4,4.20.0)` in every test project (SponsorLink in 4.20.0).
3. **FluentAssertions is replaced by AwesomeAssertions** (owner, 2026-11-08). FluentAssertions v8+
   needs a paid licence for commercial use. AwesomeAssertions is its Apache-2.0 fork: 9.6.0 on
   2026-08-20, 21 releases, targets `net8.0`. Swap the package and `using FluentAssertions` →
   `using AwesomeAssertions` in Biller and BillerContracts tests; fix any API differences from 6.x
   in a separate commit.
4. **QuestPDF updated** to the newest, on `LicenseType.Community` (Community Licence v3.0 accepted,
   per the issue). Read its release notes for layout changes; step 6 compares PDFs.
5. **AutoMapper is not touched.** BIL-91 removes it; 15.0 onwards also has a commercial licence.
6. **Remove unused packages**: Microsoft.Fast.Components.FluentUI (and its `@using`, and the Fluent UI
   line in FE `CLAUDE.md`), Microsoft.Data.Sqlite, SkiaSharp and HarfBuzzSharp native assets, and
   System.Collections.Concurrent. The native assets go only if a PDF still renders in the Linux
   container (step 6); otherwise they stay at their current versions with a comment why.
7. **BillerContracts tests move to xUnit v3** (`xunit.v3`, same runner and Test SDK as Biller), so
   shared test packages have one version. The test code changes only where v3 needs it.
8. **Major bumps** (Swashbuckle 10 / Microsoft.OpenApi 2, xUnit v3 4.0, TUnit 1.x, Npgsql 10,
   Serilog.AspNetCore 10): read the release notes and licence first. Swagger: compare
   `swagger/v1/swagger.json` before and after; any difference other than formatting is a bug.
9. **One commit per repo for the versions**, separate from code changes a bump needs, so a revert is
   one commit.

## Dependencies

BIL-92, BIL-96: Done. BIL-91 (AutoMapper) can go before or after this.

## Steps

### 1. BillerContracts
**Done on the branch, not merged or published yet.** Build: 0 warnings.
Tests: 83 passed, the same 83 `[Fact]`/`[InlineData]` cases as master. Only the `using` lines needed
changing for AwesomeAssertions and xUnit v3.
- Newtonsoft.Json 13.0.4; tests to xUnit v3, FluentAssertions → AwesomeAssertions, Moq pinned.
- **Changed in review (F1):** the `Clients` package keeps Microsoft.Extensions.Http 8.0.1,
  .Options 8.0.2 and .Options.ConfigurationExtensions 8.0.0 (the newest 8.0.x). A library's
  references are the floor for every consumer: 10.0.14 there gave NU1605 for any consumer pinning
  8.x (the FE pins DependencyInjection 8.0.1). `Clients.Tests` pins Microsoft.Extensions.Configuration
  8.0.0, which fails the build with NU1605 if the floor goes up. So FE and IT can take
  `BillerContracts.Clients` 4.2.1 on its own; their own Microsoft.Extensions updates are separate.
- Package limits in `CLAUDE.md`. Build, tests.
- The owner merges and runs `Publish` with the next version (4.2.1, no API change).

### 2. Biller
**Done on the branch**, BillerContracts 4.2.1 included (contracts PR #16 merged, published
2026-11-08). `dotnet list package --outdated` lists only Moq (pinned). Every package is on its
newest version; all support `net8.0` (BCrypt 4.2.0 through netstandard2.1). Build: 0 warnings.
Unit tests: 800 passed. Integration tests (BillioIntegrationTest master): 219/219, 3 runs.
What the updates needed:
- Swashbuckle 10: only `StartupSwagger.cs` used `Microsoft.OpenApi.Models` (namespace is now
  `Microsoft.OpenApi`). No filter or example changes.
- QuestPDF 2026.9: `RegisterFontWithCustomName` is obsolete. Calibri is registered with
  `RegisterFontFromStream` under the file's family name; family names match case-insensitively.
  2026.9 also changed defaults; `AddPdfGenerator` sets the old behaviour (`UseSystemFonts = true`,
  `ThrowOnMissingTextGlyphs = false`, `EnableDetailedLayoutErrors = false`), checked by
  `DependencyInjectionTests`. `ThrowOnMissingFontFamilies` keeps its new default (on): the app
  registers Calibri at startup and fails there if the file is missing, or (review F2, F8, F9) if no
  registered font has the family the documents use (`InvoiceDocumentFont.Family`, shared by both
  documents and the check). That made `InvoiceDocumentFactoryTest` depend on
  test order (it never registered the font); it now calls `AddPdfGenerator`, so the PDF tests get the
  app's font and settings. The tests find `Fonts/calibri.ttf` from the test dll; it isn't copied next
  to the dll, because QuestPDF registers fonts it finds in the app directory by itself.
- BCrypt 4.2.0: same hashes (`PasswordEncryptionServiceTest` checks an exact hash).
- Npgsql 10 reads `date` as `DateOnly` for untyped reads; `created_date`/`due_date` into `DateTime`
  still work through Dapper (invoice GET and the integration tests).
- AwesomeAssertions: only the `using` lines changed (58 files).
- Removed: Microsoft.Data.Sqlite, SkiaSharp and HarfBuzzSharp native assets. PDFs still render in the
  Linux container.
- Snapshot first, from master: `swagger.json` and an invoice and act PDF in LT and EN from the
  `_test-stack` stack (same seeded invoice).
- Update every package in the table, remove the unused ones, BillerContracts to the new version.
- Swashbuckle 10: fix the 3 files using `Microsoft.OpenApi` and any filter/example API changes.
- Package limits in `CLAUDE.md`. `_test-coverage`, then `_test-stack` + integration tests.

### 3. BillerFrontEnd
- Update QuickGrid, DI, Serilog packages, container tools, BillerContracts.Clients; remove
  Microsoft.Fast.Components.FluentUI and the second Serilog.Sinks.File version.
- Package limits in `CLAUDE.md`. Build; start it with `_test-stack` and open the main pages.

### 4. BillioIntegrationTest
- TUnit 1.x (read its migration notes), configuration packages, Newtonsoft.Json, container tools,
  BillerContracts.Clients; remove System.Collections.Concurrent.
- Package limits in `CLAUDE.md`. Run against the step 2 stack, `--repeat 3`.

### 5. Package limits text (each repo's `CLAUDE.md`)
- QuestPDF: Community licence only (`LicenseType.Community`). Before an update, check that the
  business still qualifies (under USD 1M revenue, not public sector or publicly traded) and read the
  new version's licence.
- Assertions: AwesomeAssertions (Apache 2.0). Don't add FluentAssertions back: v8+ needs a paid
  licence for commercial use.
- Moq: below 4.20.0, pinned as `[4.18.4,4.20.0)` (4.20.0 added SponsorLink build-time telemetry).
- Stay on `net8.0`; a package whose newest version needs a newer .NET stays on the last one that doesn't.

### 6. Check nothing changed
- `swagger.json` diff against the snapshot.
- Render the same invoice and act in LT and EN and compare with the snapshot PDFs page by page
  (images, not text).

**Biller result (2026-11-08):** master @ 3b81a8e against the branch, same data (the seed's POSTs
replayed with the same tag and dates), PyMuPDF in a throwaway container.
- API responses (invoice, users, sellers, customers, items, series): identical apart from ids.
- `swagger.json`: OpenAPI 3.0.4 both, same 16 paths and 36 schemas. Only difference: Swashbuckle 10
  adds a top-level `tags` list (Customer, Health, Invoice, Item, Seller, Series, User).
- PDFs (4): one page each, same text, fonts (Calibri), sizes and x positions. Some lines sit up to
  0.32 pt (0.1 mm) lower: new QuestPDF/Skia baseline placement, not visible. Files about 2% smaller
  (QuestPDF 2026.8 compression level).

## Versions kept back (fill in while working)

| Repo | Package | Kept at | Why |
|---|---|---|---|
| all | Moq | 4.18.4 | Decision 2 |
| FE | AutoMapper | 13.0.1 | Decision 5 |
| Contracts | Microsoft.Extensions.Http, .Options, .Options.ConfigurationExtensions (Clients), .Configuration (tests) | 8.0.x | Library floor, see step 1 (F1) |

## Out of scope

- Moving to .NET 9/10.
- Removing AutoMapper (BIL-91).
- Central package management (`Directory.Packages.props`).

## Order of work

1. Contracts (step 1), merged and published.
2. Biller (step 2), with steps 5 and 6.
3. BillerFrontEnd and BillioIntegrationTest (steps 3, 4), on the published contracts version.

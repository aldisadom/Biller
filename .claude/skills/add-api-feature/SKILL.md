---
name: add-api-feature
description: Add or extend a Biller REST API feature end-to-end across every layer — BillerContracts request/response records, FluentValidation validator, Swagger examples, controller, application service + model + mapping extensions, domain entity + repository interface, Dapper repository, Liquibase migration, DI registrations, and unit tests. Use this whenever the user wants a new entity/resource (e.g. "add payments", "CRUD for products"), a new endpoint, or a new field exposed through the API in the Biller repo, even if they only mention one layer ("add a Currency field to invoices", "new GET by seller endpoint") — fields and endpoints almost always ripple through several layers.
---

# Add an API feature end-to-end

Biller is layered. A request flows:2

`Controller` (WebAPI) → validator (`IValidator<TRequest>.CheckValidation`) → `request.ToModel()` →
`I<X>Service` (Application) → `model.ToEntity()` → `I<X>Repository` (Domain interface) →
`<X>Repository` (Infrastructure, Dapper + raw SQL) → PostgreSQL (Liquibase migrations).

**The Item feature is the reference implementation.** Before writing each layer, open the Item file
for that layer and mirror it, including XML doc comments, constructor style, naming, and blank lines.
That beats any template here, because it stays current with the code.

## 0. Pin down scope first

Work out which of these the user wants, and ask if it isn't clear:
- **New resource** (full CRUD): every step below.
- **New field on an existing resource**: contracts → migration → entity → repository SQL → model →
  mapping → validator → Swagger examples → tests. The controller and service usually don't change.
- **New endpoint/query**: contracts (maybe) → controller → service → repository → tests.

Also confirm the field names, types, which fields are required, and relationships (FK to `customers`, `sellers`, `users`?).

## 1. Contracts live in a separate repo + NuGet package

Request/response records and shared enums (`DocumentType`, `Language`, `InvoiceStatus`) **do not live
in this repo**. They are in the `BillerContracts` NuGet package (namespace `BillerContracts.*`),
built from https://github.com/aldisadom/BillerContracts (`Contracts/Requests/<X>/`, `Contracts/Responses/<X>/`).
The version is pinned in `src/Domain/Domain.csproj` (`<PackageReference Include="BillerContracts" ...>`)
and flows to every other project.

Steps:
1. Work in a clone at `../BillerContracts` (a sibling of this repo). If it doesn't exist, ask before
   cloning it (`git clone https://github.com/aldisadom/BillerContracts ../BillerContracts`). If it
   does, check it's on an up-to-date `master` and branch from there. Check that the contracts on
   `master` match the version pinned in Biller, because `master` may contain unreleased changes.
   Then add the records, mirroring `Contracts/Requests/Item/*` and `Contracts/Responses/Item/*`:
   `<X>AddRequest`, `<X>UpdateRequest` (has `Id`), `<X>GetRequest` (nullable filter props),
   `<X>Response`, `<X>ListResponse` (`List<<X>Response> <X>s { get; set; } = [];`).
   All are `public record` with `{ get; set; }` and `= string.Empty` / `= []` defaults.
   Create returns the shared `AddResponse { Guid Id }`.
2. **To build Biller against the unpublished contracts**, temporarily swap the PackageReference in
   `src/Domain/Domain.csproj` for
   `<ProjectReference Include="..\..\..\BillerContracts\Contracts\Contracts.csproj" />`.
   Tell the user you did this. It **must be reverted** to a PackageReference with the new version
   before committing, because CI clones only Biller.
3. Publishing, version choice, and rolling the new version into Biller and BillioIntegrationTest
   are covered by the `release-contracts` skill. Follow it, since the publish is the user's action.

## 2. Domain + database

- `src/Domain/Entities/<X>Entity.cs`: a `public record`. Property names must be the PascalCase form of
  the snake_case columns, because Dapper runs with `MatchNamesWithUnderscores = true`.
- `src/Domain/Repositories/I<X>Repository.cs`: `Get(Guid)` returning `<X>Entity?`, `Get()`,
  filter queries, `Add` returning `Guid`, `Update`, `Delete`.
- **Migration**: use the `add-db-migration` skill (next number in `migrations/sql/`, rollback line,
  snake_case plural table, `uuid DEFAULT gen_random_uuid()` PK).

## 3. Infrastructure

- `src/Infrastructure/Repository/<X>Repository.cs`: inject `IDbConnection`. Use verbatim `@"..."` SQL
  strings and `QuerySingleOrDefaultAsync` / `QueryAsync` / `ExecuteScalarAsync<Guid>` with
  `INSERT ... RETURNING id` / `ExecuteAsync`. Parameters are anonymous objects (`new { id }`) or the
  entity itself. Lists use `WHERE id=ANY(@Ids)`. Keep `INSERT` column lists and `UPDATE SET` lists in
  sync with the migration.
- Register it in `src/Infrastructure/DependencyInjection.cs` (`AddScoped<I<X>Repository, <X>Repository>()`).

## 4. Application

- `src/Application/Models/<X>Model.cs`: plain class mirroring the entity.
- `src/Application/MappingProfiles/<X>MappingProfile.cs`: `public static class <X>ModelExtensions`
  with hand-written extension methods (AutoMapper was removed on purpose; don't reintroduce a mapper):
  `ToEntity(this <X>Model)`, `ToModel(this <X>Entity)`, `ToModel(this <X>AddRequest)` (no Id),
  `ToModel(this <X>UpdateRequest)`, `ToResponse(this <X>Model)`, `ToModel(this <X>Response)`.
  Map **every** property, because the mapping tests check for missing ones.
- `src/Application/Interfaces/I<X>Service.cs` + `src/Application/Services/<X>Service.cs`:
  `Get(Guid)` throws `NotFoundException($"<X>:{id} not found")` when the repo returns null.
  `Update`/`Delete` call `Get(id)` first so missing ids become 404s. Cross-entity checks return
  `Result<T>`/`ErrorModel` (see `ItemService.GetWithValidation`).
- Register it in `src/Application/DependencyInjection.cs`.

## 5. Validators

- `src/Validators/<X>/<X>AddValidator.cs`, `<X>UpdateValidator.cs`: `AbstractValidator<TRequest>`
  with XML doc comments. Every rule gets `.WithMessage("Please specify ...")`. Use the shared
  `EmailValidator` for emails.
- Register both in `src/Validators/DependencyInjection.cs` once each. Watch for copy-paste
  duplicates, which have happened in this file before.

## 6. WebAPI

- `src/WebAPI/SwaggerExamples/<X>/`: `<X>AddRequestExample`, `<X>UpdateRequestExample`,
  `<X>ResponseExample`, `<X>ListResponseExample` implementing `IExamplesProvider<T>`. Fill **every**
  property with a non-empty value; a test enforces this. The repo's example data is Game of Thrones
  themed ("Iron throne", "Winterfell"); keep that flavour.
- `src/WebAPI/Controllers/<X>Controller.cs`: `[ApiController]`, `[Route("<x>s")]` lowercase plural,
  class-level `[ProducesResponseType(typeof(ErrorResponse), 500)]`. Every action has
  `ProducesResponseType` + `SwaggerRequestExample`/`SwaggerResponseExample`. Status codes:
  GET → 200 (+404 on by-id), POST → `CreatedAtAction(nameof(Add), new AddResponse{...})` 201,
  PUT/DELETE → `NoContent()` 204. Call `_validatorX.CheckValidation(request)` before mapping.
- WebAPI has `GenerateDocumentationFile=true`, so add `/// <summary>` docs on public members, or the
  build will warn.

## 7. Tests (`tests/xUnitTests`)

Mirror the Item tests one-for-one:
- `Application/Services/<X>ServiceTest.cs`: strict repository mock, `[Theory, AutoData]`, verify calls,
  plus a `NotFoundException` case for get/update/delete.
- `Application/MappingProfiles/<X>MappingProfileTest.cs`: one test per extension method via
  `MappingTestHelper.TestMapp` (`MappedAllTo` for model/entity/response, `UsedAllFrom` for requests).
- `Validators/<X>/<X>AddValidatorTest.cs` etc.: a `ValidRequest()` factory, a valid case, and one
  failing case per rule asserting on `PropertyName`.
- `WebAPI/Controllers/<X>ControllerTest.cs`: strict service and validator mocks. Validator mocks
  set up `Validate(request)` to return `new ValidationResult()`. Assert the result type and value,
  then `VerifyNoOtherCalls()`.
- `WebAPI/Swagger/<X>SwaggerExampleTest.cs`: one `NullChecker` test per example. Instantiate the
  **right** example class; copy-paste slips here have happened before.
- `Application/DependencyInjectionTests.cs`: add asserts for the new repository and service.

## 8. Verify

1. Run the `test-coverage` skill. Everything should pass, and the new classes should show coverage.
2. Optionally (ask first) see it live with the `run-local-stack` skill (create the entity via
   curl/Swagger), and run the `integration-tests` skill. Note that it wipes the local DB, and new
   endpoints need integration tests on a same-named branch in BillioIntegrationTest.
3. Summarize for the user: the files touched per layer, the migration name, and any pending
   BillerContracts publish/version bump, including a reminder if the temporary ProjectReference is
   still in place.

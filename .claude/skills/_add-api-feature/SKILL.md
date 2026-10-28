---
name: _add-api-feature
description: Add or extend a Biller REST API feature end-to-end across every layer — BillerContracts request/response records, FluentValidation validator, Swagger examples, controller, application service + model + mapping extensions, domain entity + repository interface, Dapper repository, Liquibase migration, DI registrations, and unit tests. Use this whenever the user wants a new entity/resource (e.g. "add payments", "CRUD for products"), a new endpoint, or a new field exposed through the API in the Biller repo, even if they only mention one layer ("add a Currency field to invoices", "new GET by seller endpoint") — fields and endpoints almost always ripple through several layers. Also use it when the user gives a Jira issue (BIL-<n> key or link) for API work: it reviews the issue, then works from its plan in docs/plans or writes one first. With no issue, it asks for the details and creates the BIL issue in Jira before starting.
---

# Add an API feature end-to-end

Biller is layered. A request flows:

`Controller` (WebAPI) → validator (`IValidator<TRequest>.CheckValidation`) → `request.ToModel()` →
`I<X>Service` (Application) → `model.ToEntity()` → `I<X>Repository` (Domain interface) →
`<X>Repository` (Infrastructure, Dapper + raw SQL) → PostgreSQL (Liquibase migrations).

**The Item feature is the reference implementation.** Before writing each layer, open the Item file
for that layer and mirror it, including XML doc comments, constructor style, naming, and blank lines.
That beats any template here, because it stays current with the code.

## 0. Start from the Jira issue and its plan

When the user gives a Jira link or key (`https://aldisadom.atlassian.net/browse/BIL-<n>`, `BIL-<n>`),
the issue and its plan are the spec. Without one, ask whether an issue exists. If it does, take its
key and go to step 1. If not, create it first:

- **Ask for the details** an issue needs: the goal (what the user can do afterwards), the
  resource, fields or endpoints, the rules and error cases, which repos it touches (front end?
  integration tests?), what it depends on, what is out of scope, and the parent epic. Ask the open
  points together, not one by one, and use "Pin down scope" below to see what is missing.
- **Draft it** in the style of the existing BIL issues (e.g. BIL-90, BIL-92): a summary, then
  **Goal**, **Found while planning** (what the code shows today), **Plan in short** (numbered
  steps), **Depends on** and **Risk**. Fill "Found" from the code, not from memory.
- **Show the draft and create it once the user agrees:** `createJiraIssue` in project `BIL`, type
  `Task`, `parent` = the epic, plus any priority the user gives. Link each blocker with
  `createJiraIssueLink` (`linkType: "Blocks"`, the blocker as `inwardIssue`).
- Then start again at step 1 with the new key. It has no plan yet, so step 5 writes one.

1. **Read the issue** with the Atlassian MCP tools (`getAccessibleAtlassianResources` once for the
   cloudId, then `getJiraIssue` with `view: "evidence"` for links and parent). Read the comments too
   (`discover` "list jira issue comments"). Note the summary, description, status, parent epic and
   every linked issue.
2. **Review it before writing anything.** Tell the user:
   - **Blockers:** each "is blocked by" issue and its status. If one isn't Done, ask before going on.
   - **Gaps:** what the issue doesn't say that the code needs (field types, required fields,
     relationships, status codes, error messages, which documents or languages are affected).
   - **Conflicts:** anything that breaks `CLAUDE.md` or `REVIEW.md` (invoices keep their own number,
     dates and lines, one SQL statement per write, no mapping library), or that clashes with another open
     issue or plan touching the same tables or requests.
3. **Find the plan.** Look, in order, for: a `Plan:` line in the description, then
   `docs/plans/BIL-<n>-*.md` in Biller. A plan that lives in a sibling repo needs `_sync-repos` first.
4. **There is a plan: work with it.**
   - Read all of it. Compare its "Current state" with the code now (it names the master commit it
     was written at; `git log <commit>..master` shows what changed since). Report the differences.
   - Follow its design decisions and step order. Don't re-decide them. If the code makes a decision
     wrong or a step impossible, stop, tell the user, and update the plan once they agree.
   - Migration numbers in a plan are guesses; take the next free one (`_add-db-migration`).
   - When the plan names other repos (BillerContracts, BillerFrontEnd, BillioIntegrationTest),
     those steps go on branches with Biller's branch name there.
5. **There is no plan: write one, then stop for approval.**
   - Path: `docs/plans/BIL-<n>-<slug>.md` in Biller (an API feature always changes Biller).
   - Mirror an existing plan, e.g. `docs/plans/BIL-90-seller-invoice-summary.md`: title with the key,
     `Jira:` link and epic, **Goal**, **Current state (master @ <short hash>)** as a table of the
     files, tables and endpoints involved, **Design decisions** (numbered, each with its reason),
     **Dependencies**, one **Step** per layer in the order of this skill (contracts, migration with
     its SQL and rollback, domain and infrastructure with the SQL, application, validators, WebAPI,
     tests, integration tests, front end), **Out of scope**, **Order of work**.
   - Fill "Current state" from the code, not from the issue. Put open questions at the top and ask
     them; don't guess the answers into decisions.
   - Show the user the plan and wait for approval before writing code.
   - Offer to add `Plan: docs/plans/BIL-<n>-<slug>.md in the Biller repo.` to the top of the issue
     description. Edit Jira only after the user agrees.
6. **Branch:** if on master, create `BIL-<n>-<slug>` (same slug as the plan) before changing files.
   Commit the plan on that branch, so the PR carries it.

As you work, tick off finished steps in the plan, or note what was done differently and why, so the
plan matches what is merged.

## Pin down scope

With a plan, the scope comes from it. Otherwise work out which of these the user wants, and ask if
it isn't clear:
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
   cloning it (`git clone https://github.com/aldisadom/BillerContracts ../BillerContracts`).
   Run the `_sync-repos` skill first, so it is on the branch with Biller's branch name or on
   up-to-date `master`. If it's on `master`, create a branch with Biller's branch name. Check that
   the contracts on `master` match the version pinned in Biller, because `master` may contain
   unreleased changes.
   Then add the records, mirroring `Contracts/Requests/Item/*` and `Contracts/Responses/Item/*`:
   `<X>AddRequest`, `<X>UpdateRequest` (has `Id`), `<X>GetRequest` (nullable filter props),
   `<X>Response`, `<X>ListResponse` (`List<<X>Response> <X>s { get; set; } = [];` plus
   `TotalCount`, `Page`, `PageSize`). All are `public record` with `{ get; set; }` and
   `= string.Empty` / `= []` defaults. Create returns the shared `AddResponse { Guid Id }`.
   For a new endpoint, also add the method to `Clients/Clients/<X>Client.cs` and
   `Clients/Interfaces/I<X>Client.cs`, with a test in `Clients.Tests/<X>ClientTest.cs`. The front
   end calls the API only through these clients. Follow `../BillerContracts/CLAUDE.md`.
2. **To build Biller against the unpublished contracts**, the user can run the `Publish` workflow
   on the contracts branch. From a branch other than master it publishes `<version>-preview.<run>`,
   which Biller can reference on its branch, and CI can restore it. For a quick local try before
   that, temporarily swap the PackageReference in `src/Domain/Domain.csproj` for
   `<ProjectReference Include="..\..\..\BillerContracts\Contracts\Contracts.csproj" />`.
   Tell the user you did this. It **must be reverted** before committing, because CI clones only Biller.
3. **Publishing is the user's action.** Once the contracts PR is merged, they run the `Publish`
   workflow on master (input `package_version`, the next version after the latest one on nuget.org;
   Biller may pin an older one). Both packages get that version. Run `_sync-repos`, then bump it in
   `src/Domain/Domain.csproj` (`BillerContracts`),
   `../BillioIntegrationTest/src/IntegrationTests/IntegrationTests.csproj` (`BillerContracts`), and
   `../BillerFrontEnd/src/FrontEnd/FrontEnd.csproj` (`BillerContracts.Clients`) when the front end
   needs the change. Replace any preview version.

## 2. Domain + database

- `src/Domain/Entities/<X>Entity.cs`: a `public record`. Property names must be the PascalCase form of
  the snake_case columns, because Dapper runs with `MatchNamesWithUnderscores = true`.
- `src/Domain/Repositories/I<X>Repository.cs`: `Get(Guid)` returning `<X>Entity?`, `Get()`,
  filter queries, `Add` returning `Guid`, `Update`, `Delete`.
- **Migration**: use the `_add-db-migration` skill (next number in `migrations/sql/`, rollback line,
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
  with hand-written extension methods (AutoMapper and other mapping libraries are not allowed in
  any Biller repo; don't add one):
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

Write them with the `_write-tests` skill, which has the conventions and per-layer patterns. The Item
tests show which files a feature needs. A new resource needs:
- `Application/Services/<X>ServiceTest.cs`, including a `NotFoundException` case for get/update/delete.
- `Application/MappingProfiles/<X>MappingProfileTest.cs`: one test per extension method.
- `Validators/<X>/<X>AddValidatorTest.cs`, `<X>UpdateValidatorTest.cs`.
- `WebAPI/Controllers/<X>ControllerTest.cs`.
- `WebAPI/Swagger/<X>SwaggerExampleTest.cs`: one test per example.
- Asserts for the new registrations in `Application/DependencyInjectionTests.cs` and
  `Validators/DependencyInjectionTests.cs`.

## 8. Verify

1. Run the `_test-coverage` skill. Everything should pass, and the new classes should show coverage.
2. See it live with the `_test-stack` skill. It doesn't touch the user's local DB:
   `test-stack.sh up` runs the migrations and starts this checkout's API on port 8091 (Swagger at
   http://localhost:8091/swagger). Run `_sync-repos` first, so `../BillioIntegrationTest` is on the
   branch with Biller's branch name or up-to-date master, then `run-integration.sh --repeat 3`.
   The tests delete all data through the API first. `test-stack.sh down` when done. New endpoints
   need integration tests on a branch with the same name in BillioIntegrationTest; CI uses that
   branch. The user's own stack (`docker compose up -d --build`, port 8090) is theirs; ask before
   using it.
3. Summarize for the user: the files touched per layer, the migration name, the plan steps that are
   done and any that are left, and any pending
   BillerContracts publish/version bump, including a reminder if the temporary ProjectReference is
   still in place.

Paste each command and the lines of its output that show the result (the `Passed!/Failed!` line,
the changesets Liquibase ran). Don't say it passed without the output.

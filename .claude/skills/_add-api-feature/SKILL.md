---
name: _add-api-feature
model: sonnet
effort: high
description: Add or extend a Biller REST API feature end-to-end across every layer — BillerContracts request/response records, FluentValidation validator, Swagger examples, controller, application service + model + mapping extensions, domain entity + repository interface, Dapper repository, Liquibase migration, DI registrations, and unit tests. Use this whenever the user wants a new entity/resource (e.g. "add payments", "CRUD for products"), a new endpoint, or a new field exposed through the API in the Biller repo, even if they only mention one layer ("add a Currency field to invoices", "new GET by seller endpoint") — fields and endpoints almost always ripple through several layers. Also use it when the user gives a Jira issue (BIL-<n> key or link) for API work: it has _write-plan review the issue and check or write its plan in docs/plans first. With no issue, it asks for the details and creates the BIL issue in Jira before starting.
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
- **Draft it short:** a summary, and a description of at most two short paragraphs. The first says
  what the user can do afterwards and why. The second gives the scope: the resource, fields or
  endpoints, the repos it touches and what is out of scope. No headings, step lists or risk
  sections. The details go in the plan (`_write-plan`, 4b), and blockers go in as issue links, not text.
- **Show the draft and create it once the user agrees:** `createJiraIssue` in project `BIL`, type
  `Task`, `parent` = the epic, plus any priority the user gives. Link each blocker with
  `createJiraIssueLink` (`linkType: "Blocks"`, the blocker as `inwardIssue`).
- Then go on at step 1 with the new key. It has no plan yet, so `_write-plan` writes one.

1. **Plan on Fable.** First run `_sync-repos` here, where the user can be asked: `_write-plan`
   can't ask, and reads only repos that are `ok`. Then run `_write-plan BIL-<n>`. It runs in its
   own context, doesn't see this conversation and remembers nothing between runs, so keep a list
   of what the user has decided (details, answers, "go on despite the blocker", "leave <repo> out")
   and pass **all of it on every run**. It reads and reviews the issue, checks the existing plan
   against the code or writes a new one, and returns the plan's path, the blockers, gaps and
   conflicts, the open questions and any repo it couldn't read.
2. **Tell the user** what it returned, before writing anything else, and gather their answers:
   - A blocker that isn't Done: `_write-plan` stops there without a plan. Ask whether to go on.
   - A repo it couldn't read that the plan needs: sort it out with `_sync-repos` (ask the user on
     `BLOCKED` or `MISSING`), or the user chooses to go on without it ("leave <repo> out"): the
     plan then has its steps, marked as not checked against the code. A repo the plan doesn't
     need is only reported.
   - Open questions: ask them together.

   Go to step 3 when it returned a plan and the user has nothing left to decide: every blocker that
   isn't Done is one the list says to go on despite, every unread repo the plan needs is one the
   list leaves out, and there are no open questions. Otherwise add the answers to the list, run `_write-plan` again with
   the whole list, and repeat this step with what that run returns. Don't start another run while
   answers are still missing, and stop when the user doesn't want to go on.
3. **Approval:** when `_write-plan` returned anything but `unchanged`, or the plan file isn't
   committed yet (`git status --porcelain docs/plans/BIL-<n>-*` shows it), show the plan and wait
   for approval before writing code. Offer to add
   `Plan: docs/plans/BIL-<n>-<slug>.md in the Biller repo.` to the top of the issue description.
   Edit Jira only after the user agrees.
4. **An approved plan:** report the differences `_write-plan` found. Follow the plan's design
   decisions and step order; don't re-decide them. If the code makes a decision wrong or a step
   impossible, stop and tell the user; once they agree, add the change to the list, run
   `_write-plan`, then step 3.
   - Migration numbers in a plan are guesses; take the next free one (`_add-db-migration`).
   - When the plan names other repos (BillerContracts, BillerFrontEnd, BillioIntegrationTest),
     those steps go on branches with Biller's branch name there.
5. **Branch:** if on master, create `BIL-<n>-<slug>` (same slug as the plan) before changing files.
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
- `src/Domain/Repositories/I<X>Repository.cs`: `Get(Guid)` returning `<X>Entity?`, the paged list
  `Get(Guid? <parent>Id, int page, int pageSize)` returning
  `(IEnumerable<<X>Entity> Items, int TotalCount, int Page)`, `Add` returning `Guid`, `Update`,
  `Delete`. There is no unpaged list query.
- **Migration**: use the `_add-db-migration` skill (next number in `migrations/sql/`, rollback line,
  snake_case plural table, `uuid DEFAULT gen_random_uuid()` PK).

## 3. Infrastructure

- `src/Infrastructure/Repository/<X>Repository.cs`: inject `IDbConnection`. Use verbatim `@"..."` SQL
  strings and `QuerySingleOrDefaultAsync` / `QueryAsync` / `ExecuteScalarAsync<Guid>` with
  `INSERT ... RETURNING id` / `ExecuteAsync`. Parameters are anonymous objects (`new { id }`) or the
  entity itself. Lists by id use `WHERE id=ANY(@Ids)`. Keep `INSERT` column lists and `UPDATE SET`
  lists in sync with the migration.
- The paged list goes through `_dbConnection.QueryPage<<X>Entity>(dataSql, countSql, filter, page,
  pageSize)` (`PagedQuery.cs`), like `ItemRepository.Get`: `dataSql` selects
  `*, COUNT(*) OVER() AS total_count` as the last column, has a stable `ORDER BY` (a name, then
  `id`) and ends with `LIMIT @PageSize OFFSET @Offset`; `countSql` is `SELECT COUNT(*)` with the
  same filter. A page below 1 returns page 1 and a page past the last returns the last page.
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
  `Update`/`Delete` call `Get(id)` first so missing ids become 404s. Ownership checks
  `GetFor<Owner>(id, ownerId)` return the model and throw `FluentValidation.ValidationException` (a 400)
  when the row is missing or another owner's (see `SellerService.GetForUser`). The list `Get(<X>GetRequest? query)`
  defaults to page 1 and `PageSize.p25` and passes the repository's `(Items, TotalCount, Page)` on,
  mapped to models.
- Register it in `src/Application/DependencyInjection.cs`.

## 5. Validators

- `src/Validators/<X>/<X>AddValidator.cs`, `<X>UpdateValidator.cs`: `AbstractValidator<TRequest>`
  with XML doc comments. Every rule gets `.WithMessage("Please specify ...")`. Use the shared
  `EmailValidator` for emails.
- `<X>GetValidator : PagedRequestValidator<<X>GetRequest>` for the list query: it checks the page
  size; add rules only for the filter ids.
- Register them in `src/Validators/DependencyInjection.cs` once each. Watch for copy-paste
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
  The list action builds `<X>ListResponse` and ends with `.WithPaging(query, totalCount, page)`,
  using the page the service returned (see `ItemController.Get`).
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
   using it. In a worktree, or while another feature's stack may be running, put
   `TEST_STACK_NAME=<lowercase branch key>` (e.g. `bil-90`) on every `test-stack.sh` and
   `run-integration.sh` command, so each feature gets its own stack and ports, and pass
   `run-integration.sh --repo` the feature's own BillioIntegrationTest worktree (see `_test-stack`).
3. Summarize for the user: the files touched per layer, the migration name, the plan steps that are
   done and any that are left, and any pending
   BillerContracts publish/version bump, including a reminder if the temporary ProjectReference is
   still in place.

Paste each command and the lines of its output that show the result (the `Passed!/Failed!` line,
the changesets Liquibase ran). Don't say it passed without the output.

# Biller

Invoicing backend: REST API plus invoice and act PDFs (LT, EN). One owner, runs in docker compose
on a small Linux PC.

## Stack
- .NET 8 Web API, Dapper with raw SQL on PostgreSQL 16, Liquibase SQL changesets in `migrations/sql/`.
- QuestPDF, FluentValidation, Serilog, Swagger with examples.
- Tests: xUnit v3, strict Moq mocks, FluentAssertions in `tests/xUnitTests`.
- Sibling repos, each with its own `CLAUDE.md`: `BillerContracts` (request/response records, enums
  and typed HTTP clients, published as the `BillerContracts` and `BillerContracts.Clients` NuGet
  packages), `BillerFrontEnd` (Blazor), `BillioIntegrationTest` (API tests; CI uses its branch with
  the same name as the Biller branch, else master).

## Where things go
- `src/WebAPI`: controllers, Swagger examples, middleware.
- `src/Validators`: request validators.
- `src/Application`: services, models, `*Extensions` mapping methods (`MappingProfiles/`), PDF documents, number-to-words.
- `src/Domain`: entities, repository interfaces, exceptions.
- `src/Infrastructure`: Dapper repositories, DI for the DB and PDF generation.

## Conventions
- A write that touches several rows or tables is one SQL statement (data-modifying CTE). All
  repositories share one scoped `IDbConnection`, so don't add transactions.
- Never edit a changeset that is already on master. Fix it with a new one. Every changeset has a `-- rollback`.
- Invoices store their user, seller and customer ids and show the current data. They keep their
  own number, dates and lines. Editing an item must not change an existing invoice or its PDF.
  Saving the invoice itself (`PUT /invoices`) copies line names and prices from the current items.
- Mirror the Item feature when adding a layer. Map every property by hand.
- Before reading, comparing or using a sibling repo, run the `_sync-repos` skill. Siblings must be
  on the branch with Biller's branch name, else on up-to-date master.
- Working copies on Windows have CRLF line endings (`.gitattributes` stores LF). Change files with
  the Edit tool, which keeps them. Don't rewrite source files with sed or Python scripts.

## Non-goals
- No ORM.
- No AutoMapper or any other mapping library, in any Biller repo. Map with hand-written extension methods.
- No AutoFixture or shared test-data builders.
- No seed data in migrations.

## Commands
- Tests and coverage: `bash .claude/skills/_test-coverage/scripts/run-coverage.sh`
- Build any repo, warnings in changed files only: `bash .claude/skills/_test-coverage/scripts/build.sh [repo]`
- Throwaway stack and integration tests: the `_test-stack` skill (`test-stack.sh up`, `run-integration.sh`, `test-stack.sh down`).
- The user's local stack: `docker compose up -d --build` (Swagger at http://localhost:8090/swagger, DB on port 5437). Don't use it for tests.

## More
- Plans: `docs/plans/BIL-<n>-<slug>.md` (Jira project BIL), in the repo the work is in. Only plans
  that change several repos, or Biller itself, live here.
- Review rules: `REVIEW.md`. Past bugs to test reviews against: `docs/review-evals.md`.
- Before opening a PR, use the `_pre-pr` skill.

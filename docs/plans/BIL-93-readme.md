# Plan: a README for every repo that matches it, and a rule to keep it so (BIL-93)

Jira: [BIL-93](https://aldisadom.atlassian.net/browse/BIL-93). Epic: BIL-89 (Billeris).

Docs only: no API, contract, migration or code change, so the layer steps of `_add-api-feature`
don't apply. The steps below are per repo.

## Answers from the owner (2026-11-14)

1. `make update`, `make backup` and `make restore` are **not run** for this task. The other commands
   run against a throwaway stack.
2. `docs/README_en.md` is **renamed to `docs/README_lt.md`** and becomes the Lithuanian translation of
   the main (English) README, in Biller and BillerContracts, the two repos that have the file.
3. The shields and the logo block are **removed**.

## Goal

- Every repo's README says what the repo is, how it fits with the other three, and how to build, run
  and test it today, with commands that were run.
- A change that makes a README wrong updates it in the same PR: a rule in each `CLAUDE.md` and
  `REVIEW.md`, and a check in the pre-PR steps.

## Current state (master @ ea8d434; siblings on up-to-date master, 2026-11-14)

| Where | What |
|---|---|
| Biller `README.md` (207 lines) | Template shields and logo, "English" link to `docs/README_en.md` ("To do"). Calls itself a "cloud-native" platform with "certificate support". Project structure lists `src/Common` and `src/Contracts`, which don't exist. Run: `docker-compose up --build`; test: `dotnet test tests/xUnitTests` (the repo's command is `run-coverage.sh`, Release). Right today: ports 8090 and 8888, Liquibase, the backup and restore section. Missing: DB port 5437, the `LocalDocker` settings and fonts, sibling repos, the test stack, CI and the coverage gate |
| BillerContracts `README.md` (297 lines) | Still the "Best-README-Template" text with `APIProjectTemplate` links. Nothing about the two packages, the clients or publishing |
| BillerFrontEnd `README.md` | 16 bytes: a title only |
| BillioIntegrationTest `README.md` | One line: "Integration tests for Biller" (the issue says there is none; one was added since) |
| `CLAUDE.md`, `REVIEW.md` (all four repos) | No README rule |
| Pre-PR | Biller: the `_pre-pr` skill (`_pr-scope` writes `scope.md`, `_pr-review` reads `REVIEW.md`). Siblings: a "Before a PR" list in `CLAUDE.md` |
| PR template (Biller) | Jira, What and why, Testing, Risk, How to revert. No docs line |

## Design decisions

1. **The README is for a person setting the repo up; `CLAUDE.md` stays the rule book.** The README
   doesn't repeat conventions, package limits or review rules. It links to `CLAUDE.md` for them, so
   one fact lives in one place and can't go stale twice.
2. **The same sections in every README**, left out when empty: what it is (2 or 3 sentences), how it
   fits with the other repos, prerequisites, run, test, configuration, where things go, deploy or
   publish. Plain Markdown: no HTML blocks, no table of contents, no "back to top" links.
3. **`README.md` is English; `docs/README_lt.md` is its Lithuanian translation** in Biller and
   BillerContracts, linked from the top of `README.md` ("Lietuviškai"). Commands, paths and names stay
   as they are; only the prose is translated. The README rule (decision 6) covers both files: a change
   to one changes the other. BillerFrontEnd and BillioIntegrationTest have no such file and get none.
4. **Only commands that were run go in.** Each README command is run on the branch and its result
   noted in the PR. `make update`, `make backup` and `make restore` are described, not run.
5. **Biller's backup and restore section stays as it is**: it is current and was tested in BIL-84.
6. **One rule, the same wording in all four `CLAUDE.md` files:** "Update `README.md` in the same
   change when the change makes it wrong: a new or changed command, setting, port, endpoint group,
   package, or project layout." Biller and BillerContracts add: "and `docs/README_lt.md`, its
   Lithuanian translation, with it."
7. **`REVIEW.md` in all four repos** gets "A change makes the README wrong and doesn't update it" in
   its failure list.
8. **The pre-PR check goes where the diff is already read.** Biller: `_pr-scope` adds a `README:`
   line to `scope.md` (needs an update: yes or no, and why), and `_pr-description` reports it.
   Siblings: one more line in their "Before a PR" list. No new skill and no new script.
9. **Four branches named `BIL-93-readme`, four PRs.** Nothing depends on a merge order.

## Dependencies

- None. BIL-105 removes `FontSetting.Name`; whichever of the two merges second writes the
  configuration section to match.

## Step 1: Biller

- [ ] Rewrite `README.md`:
  - What it is: invoicing REST API with invoice and act PDFs (LT, EN), one owner, docker compose.
  - The four repos and how a change travels (contracts → publish → version bump).
  - Prerequisites: .NET 10 SDK, Docker.
  - Run: `docker compose up -d --build`; the services (`billio-db` 5437, `billio-webapi` 8090 with
    Swagger, `billio-pgadmin4` 8888, `billio-liquibase`); `make run`, `make update`.
  - Test: `bash .claude/skills/_test-coverage/scripts/run-coverage.sh` (Release, as CI); integration
    tests through the test stack.
  - Configuration: `appsettings.LocalDocker.json` (connection string, `FontSettings`, password
    pepper), the `./Fonts` mount, the overdue job at 03:00 UTC.
  - Where things go: the five `src` projects and `migrations/sql/`, as in `CLAUDE.md`.
  - Migrations: Liquibase changesets, applied by the `liquibase` container.
  - Backup and restore: unchanged.
  - CI: build, unit tests with the coverage gate, integration tests.
- [ ] `git mv docs/README_en.md docs/README_lt.md` and write the translation. Remove `images/` files no README uses.
- [ ] `CLAUDE.md`: the rule (decision 6), under Conventions.
- [ ] `REVIEW.md`: the failure line (decision 7).
- [ ] `_pr-scope` and `_pr-description`: the `README:` line (decision 8).
- [ ] `docs/review-evals.md`: no entry, there is no past bug to test against.

## Step 2: BillerContracts

- [ ] Replace `README.md`: the two packages and what is in each, who uses them, build and test
      commands, how to publish (the `Publish` workflow, preview versions from a branch), and that a
      change reaches the other repos only after a publish and a version bump.
- [ ] `git mv docs/README_en.md docs/README_lt.md` and write the translation. Remove unused `images/`.
- [ ] `CLAUDE.md` rule and "Before a PR" line; `REVIEW.md` line.

## Step 3: BillerFrontEnd

- [ ] Write `README.md`: Blazor Server UI for the API, start Biller first, `make run` or
      `docker compose up -d --build`, UI at http://localhost:8080, `ClientsOptions` per environment,
      build and test commands, where pages go.
- [ ] `CLAUDE.md` rule and "Before a PR" line; `REVIEW.md` line.

## Step 4: BillioIntegrationTest

- [ ] Write `README.md`: what the tests cover, the warning that they delete all data through the
      API, how to run them against a Biller stack, how the URL is set (`ClientsOptions` and the
      environment variables), how CI pairs branches by name.
- [ ] `CLAUDE.md` rule and "Before a PR" line; `REVIEW.md` line.

## Step 5: Check

- [ ] Run every command each README shows (decision 4) and paste the result lines into the PR.
- [ ] Every link and file path in each README exists (a grep over the paths).
- [ ] Biller: `_pre-pr` on the branch shows the new `README:` line in `scope.md`.

## Out of scope

- API reference in the README: Swagger has it.
- Screenshots of the front end.
- A README for the NuGet packages' page on nuget.org.
- Rewriting any `CLAUDE.md` beyond the one rule and the pre-PR line.

## Order of work

1. Biller: README, rule, review line, the two skills.
2. BillerContracts, BillerFrontEnd, BillioIntegrationTest, in any order.
3. Step 5 in each repo, then the four PRs.

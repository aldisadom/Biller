# Plan: CI collects unit-test coverage in Biller and BillerContracts (BIL-106)

Jira: [BIL-106](https://aldisadom.atlassian.net/browse/BIL-106). Epic: BIL-89 (Billeris). Repos: Biller and
BillerContracts. It changes two repos, so it lives in Biller.

## Goal

- Every CI run in Biller and BillerContracts shows line and branch coverage per assembly in the job
  summary, and keeps the HTML report as an artifact.
- Biller's numbers are the same as `run-coverage.sh` locally (Release, Infrastructure left out).
- A gate fails the job when line or branch coverage drops more than 1 point below today's numbers.
- Coverage never goes down: after each PR's CI run its coverage is reviewed, and the minimums go up
  with it or stay, never down (design decision 9).
- Local builds and coverage runs use Release only, like CI (design decision 10).

**Scope change (2026-11-13, owner):** the issue lists coverage gates as out of scope. The owner asked
for a gate at today's numbers minus 1 point, so it only just passes now (design decisions 6 and 8),
for the never-down rule (9), and for Release-only local builds (10).

## Current state (Biller master @ 19f8a3a, BillerContracts master @ a79877f)

| Where | What |
|---|---|
| Biller `.github/workflows/CI.yaml`, job `unit-tests` | `dotnet test --configuration Release tests/xUnitTests /p:CollectCoverage=true /p:CoverletOutputFormat="opencover"`. These are coverlet.msbuild properties; the project has coverlet.collector, so nothing is collected. The last master run logs `Passed! ... 814` and no coverage line |
| Biller `tests/xUnitTests/xUnitTests.csproj` | `coverlet.collector` 10.1.0 (newest), `xunit.v3.mtp-off` 4.0.3 (VSTest) |
| Biller `run-coverage.sh` | No `--configuration`, so Debug. `--collect:"XPlat Code Coverage"`, then ReportGenerator `Html;TextSummary` with `-assemblyfilters:-Infrastructure` (Dapper repositories are covered by BillioIntegrationTest). Debug: 814 passed, 92.6% line, 85.8% branch. **Release** (what CI builds): 92.6% line, **85.4%** branch (300 of 351) |
| Biller `build.sh` | `CONFIG=Debug`; `--release` switches to Release. `_test-coverage/SKILL.md` shows `build.sh <repo> --release` for siblings. `run-integration.sh` already builds Release |
| BillerContracts `.github/workflows/CI.yaml` | One job: `dotnet build -c Release`, `dotnet test -c Release --no-build`. Also runs from `Publish.yaml` (`workflow_call`) and on releases |
| BillerContracts `Clients.Tests/Clients.Tests.csproj` | No coverlet package. `--collect "XPlat Code Coverage"` gives `Could not find data collector 'XPlat Code Coverage'`; 83 passed |
| ReportGenerator | Local global tool 5.4.12. nuget.org: 5.5.12 (2026-10-27), 5.6.0 (2026-11-10), Apache-2.0. No threshold option; `JsonSummary` writes `summary.linecoverage` / `summary.branchcoverage` after the assembly filters |
| BillerFrontEnd `build.yml` | Already runs `dotnet test ... --collect:"XPlat Code Coverage"` but reports nothing. 8 tests (mapping only; page tests come with BIL-87): 6.1% line, 1.2% branch |

Checked on a copy of BillerContracts in a `mcr.microsoft.com/dotnet/sdk:10.0` container (Release): with
coverlet.collector 10.1.0 the build has no warnings (no NU1605 from the Clients floor pins), 83 pass,
and ReportGenerator `MarkdownSummaryGithub` gives 83.2% line, 72.7% branch, 2 assemblies
(`BillerContracts.Clients` 95.6% line).

## Design decisions

1. **coverlet.collector with `--collect "XPlat Code Coverage"`**, as the issue says and `run-coverage.sh`
   does. Biller drops the `/p:CollectCoverage` properties, which did nothing.
2. **ReportGenerator as a dotnet tool, pinned to 5.5.12**, installed in the job
   (`dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.5.12`). No third-party
   GitHub Action. 5.5.12 is the newest version that has been out more than two weeks.
3. **Job summary: `MarkdownSummaryGithub`** appended to `$GITHUB_STEP_SUMMARY`. It has line and branch
   coverage per assembly. Its method coverage row says "Feature is only available for sponsors", so
   that row is dropped (`grep -v 'reportgenerator.io/pro'`).
4. **Artifact `coverage-report`**: the HTML report and the Cobertura file, like `coveragereport/` locally.
5. **Biller leaves out Infrastructure** (`-assemblyfilters:-Infrastructure`), the same filter as
   `run-coverage.sh`, so CI and local numbers match. BillerContracts reports every assembly
   (`Clients` and `Contracts`); the summary lists them separately.
6. **The report steps run only when the tests pass.** A red run already fails the PR, and a failed
   run's numbers would only mislead. The gate is the last step, after the summary is written, so a
   failed gate still shows the numbers.
7. **No automatic comparison with master.** `MarkdownDeltaSummary` needs the previous report stored
   somewhere; that is a later issue if wanted. Design decision 9 covers it by hand.
8. **Gate: today's Release numbers minus 1 point, line and branch**, as `COVERAGE_MIN_LINE` and
   `COVERAGE_MIN_BRANCH` in the workflow's `env`. The step reads ReportGenerator's `Summary.json` (after
   the Infrastructure filter, so the gate checks the numbers the summary shows) with `jq`, which the
   GitHub runners have. Fixed numbers, not a ratchet: raised by hand (design decision 9).

   | Repo | Line now | Branch now | `COVERAGE_MIN_LINE` | `COVERAGE_MIN_BRANCH` |
   |---|---|---|---|---|
   | Biller | 92.6 | 85.4 | 91.6 | 84.4 |
   | BillerContracts | 83.2 | 72.7 | 82.2 | 71.7 |

   Measured in Release: Biller with the Windows SDK, BillerContracts in a Linux container. If the first
   CI run (Linux, ReportGenerator 5.5.12) shows other numbers, the minimums become CI's numbers minus 1.
   The gate logic was checked on Biller's Release `Summary.json`: a minimum at or below the number
   passes, 0.1 above fails, and a missing number fails too (empty value, `awk` compares it as 0).
9. **Coverage never goes down (owner, 2026-11-13).** The minimums are always master's coverage minus 1
   point, so minimum + 1 is master's number. After each PR's CI run, compare the gate's line with that:
   - Lower than minimum + 1: the PR lowered coverage. Add tests in the same PR until it is back. The
     gate only stops drops of more than 1 point; this rule covers the smaller ones.
   - Higher: raise `COVERAGE_MIN_LINE` / `COVERAGE_MIN_BRANCH` in the same PR to the new numbers minus 1.
   - The same: leave them.
   - Never lower a minimum.
   The rule goes in each repo's `CLAUDE.md`, as a comment next to the minimums in `CI.yaml`, and in
   `REVIEW.md`, where a lowered minimum is a finding a reviewer can see in the diff.
10. **Release only, locally too (owner, 2026-11-13).** Debug and Release give different coverage
    (Biller branch 85.8% vs 85.4%), so `run-coverage.sh` runs `--configuration Release` and `build.sh`
    always builds Release; its `--release` option goes. Local numbers then match CI's.

## Dependencies

- None. The two PRs are independent and can merge in either order. Adding a test-only package to
  `Clients.Tests` (`IsPackable` false) needs no BillerContracts publish or version bump.

## Step 1: BillerContracts (branch `BIL-106-ci-coverage`)

- [x] `Clients.Tests/Clients.Tests.csproj`: `coverlet.collector` 10.1.0 with the same
  `IncludeAssets`/`PrivateAssets` as Biller's test project.
- [x] `.github/workflows/CI.yaml`, job `build`:
  ```yaml
      - name: Test
        run: dotnet test -c Release --no-build --collect "XPlat Code Coverage" --results-directory TestResults
      - name: Coverage report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.5.12
          reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:coveragereport \
            "-reporttypes:Html;MarkdownSummaryGithub;JsonSummary"
          grep -v 'reportgenerator.io/pro' coveragereport/SummaryGithub.md >> "$GITHUB_STEP_SUMMARY"
      - name: Upload coverage report
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: |
            coveragereport
            TestResults/*/coverage.cobertura.xml
      - name: Coverage gate
        run: |
          line=$(jq '.summary.linecoverage' coveragereport/Summary.json)
          branch=$(jq '.summary.branchcoverage' coveragereport/Summary.json)
          echo "Line $line% (minimum $COVERAGE_MIN_LINE%), branch $branch% (minimum $COVERAGE_MIN_BRANCH%)"
          awk -v l="$line" -v b="$branch" -v ml="$COVERAGE_MIN_LINE" -v mb="$COVERAGE_MIN_BRANCH" \
            'BEGIN { exit !(l >= ml && b >= mb) }' || { echo "::error::Coverage is below the minimum"; exit 1; }
  ```
  and in the workflow's `env`:
  ```yaml
    # Master's coverage minus 1 point. After each PR, raise them when coverage went up; never lower
    # them (CLAUDE.md, "Coverage").
    COVERAGE_MIN_LINE: 82.2
    COVERAGE_MIN_BRANCH: 71.7
  ```
- [x] `CLAUDE.md`, a `## Coverage` section before `## Before a PR` (it also says to build and measure
  in Release only):
  > CI reports unit-test coverage in the job summary and fails below `COVERAGE_MIN_LINE` /
  > `COVERAGE_MIN_BRANCH` in `.github/workflows/CI.yaml`, which are master's coverage minus 1 point.
  > Coverage never goes down. After each PR's CI run, compare its numbers with the minimums + 1: lower
  > means add tests in the same PR; higher means raise the minimums in the same PR to the new numbers
  > minus 1. Never lower a minimum.
- [x] `REVIEW.md`, under "Failure here looks like": `COVERAGE_MIN_LINE` or `COVERAGE_MIN_BRANCH` in
  `.github/workflows/CI.yaml` is lowered. `Skip:` also lists `coveragereport/` and `TestResults/`.
- [x] `.gitignore`: add `/coveragereport`, as Biller has, for running the same commands locally.
  `[Tt]est[Rr]esult*/` is already there.

## Step 2: Biller (branch `BIL-106-ci-coverage`)

- [x] `.github/workflows/CI.yaml`, job `unit-tests`: the same four steps, with
  `dotnet test --configuration Release tests/xUnitTests --collect "XPlat Code Coverage" --results-directory TestResults`,
  `"-assemblyfilters:-Infrastructure"` on the ReportGenerator line, and `COVERAGE_MIN_LINE: 91.6`,
  `COVERAGE_MIN_BRANCH: 84.4` in `env` with the same comment.
- [x] `run-coverage.sh`: `--configuration Release`. `build.sh`: always Release, no `--release` (an old
  call stops with `Unknown argument: --release`). `_test-coverage/SKILL.md`: no `--release`, says both
  scripts build Release like CI, and "Reporting back" compares the numbers with the minimums + 1, so
  the never-down rule is checked before the PR too.
- [x] `CLAUDE.md`: the same rule in `## Conventions`; `## Commands` says the scripts build Release.
- [x] `REVIEW.md`: the same "Failure here looks like" line.

## Step 3: Check

- [x] Locally with the SDK (Docker isn't reachable from WSL now), 2026-11-13: BillerContracts' workflow
  commands give 83 passed, 83.2% line, 72.7% branch (Clients 95.6%, Contracts 69.4%), the same as the
  Linux container; Biller's `run-coverage.sh` (`--configuration Release`) gives 814 passed, 92.6% line,
  85.4% branch. The gate step (with a `jq` stand-in) prints `Line 83.2% (minimum 82.2%), branch 72.7%
  (minimum 71.7%)` and `Line 92.6% (minimum 91.6%), branch 85.4% (minimum 84.4%)` and passes on both.
  The summary has no sponsors row. `build.sh` and `build.sh BillerContracts`: `-c Release`,
  `Warnings: 0`, `Build succeeded.` Both workflows parse.
- [ ] On each PR: the CI run's summary shows the coverage table, the artifact `coverage-report` is
  there, the gate prints its numbers, and the job is green. The gate's numbers match design decision 8.
- [ ] The gate fails when it should: one CI run with a minimum set 0.1 above the current number fails with
  `Coverage is below the minimum`; then set back.

## Out of scope

- A ratchet that raises the gate by itself, PR comments, badges, or an automatic comparison with
  master. Design decision 9 is a rule people follow; CI doesn't check it.
- BillerFrontEnd: at 6.1% line a gate guards nothing, and the report would only say the pages have no
  tests. Its report and gate belong with BIL-87's page tests, the same steps as here.
- BillioIntegrationTest: it tests the running API over HTTP, so coverlet can't see the API's code.
- The Node.js 20 deprecation warnings for `actions/checkout@v4` and `actions/setup-dotnet@v4`, and
  Biller's separate `build` job building the solution a second time.

## Order of work

1. This plan, on Biller's branch.
2. Step 1, PR in BillerContracts.
3. Step 2, PR in Biller.
4. Step 3 on both PRs.

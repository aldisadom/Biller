---
name: _test-coverage
description: Run Biller's xUnit test suite with code coverage, generate the HTML/text coverage report, and summarise failures and coverage gaps. Use this whenever the user wants to run tests, check that a change didn't break anything, see coverage numbers, find untested code, or asks "do the tests pass?" in the Biller repo — including after finishing a feature, migration, or invoice layout change, even if they don't say "coverage".
---

# Test + coverage for Biller

The repo is a .NET 8 solution (`Billio.sln`) with one unit-test project, `tests/xUnitTests`
(xUnit v3 + Moq + FluentAssertions, coverage via `coverlet.collector`).
There is no `make test` target; use the bundled script.

## Run it

```bash
bash .claude/skills/_test-coverage/scripts/run-coverage.sh                  # full suite + report
bash .claude/skills/_test-coverage/scripts/run-coverage.sh --filter "FullyQualifiedName~ItemService"   # subset
bash .claude/skills/_test-coverage/scripts/run-coverage.sh --no-report      # tests only, faster
bash .claude/skills/_test-coverage/scripts/build.sh [repo] [--release]      # build only: errors, warnings in changed files
```

`build.sh` also builds a sibling (`build.sh BillerFrontEnd --release`). It compares against
`master`, so old warnings elsewhere don't hide new ones.

The script picks `dotnet` or, from WSL, Windows `dotnet.exe` / `reportgenerator.exe`
(`C:\Users\<user>\.dotnet\tools`). It writes results to
`tests/xUnitTests/TestResults/latest/` and the report to `coveragereport/` (both gitignored),
prints `coveragereport/Summary.txt` (the `Infrastructure` assembly is filtered out), and exits with the test exit code. Console output is
minimal (failures + the `Passed!/Failed!` totals line). A full run takes ~15s.

If `reportgenerator` is missing, tell the user to install it with
`dotnet tool install -g dotnet-reportgenerator-globaltool` rather than installing it yourself.

## Reporting back

Keep it short and useful:

1. **Result line**: paste the `Passed!/Failed!` line exactly as the script printed it, and the
   overall line + branch coverage from the summary. Don't say the tests passed without that line.
2. **Failures** (if any): test name, the assertion message, and the likely cause in the source
   (open the test and the code under test — don't just paste the stack trace).
   A build error is not a test failure; report the compiler error with `file:line`.
3. **Coverage gaps worth acting on**: services, mapping extensions, helpers, validators,
   controllers, and Swagger examples are all unit-tested — gaps there are real.
   The `Infrastructure` assembly is excluded from the report on purpose. Its Dapper repositories and
   DB/PDF wiring are covered by the separate `BillioIntegrationTest` repo in CI, so don't report it
   as missing. Low or zero coverage is also expected and not worth flagging for: `Program`,
   `Capabilities/Startup*`, `Middleware/ErrorChecking`, and `HealthController`.
   If a class that should be covered shows 0%, either it has no test at all or a copy-pasted test
   instantiates the wrong class (e.g. an `Item...` test building an `Invoice...` example). Check
   which, and point it out.
4. If you made the code change being tested, compare against the previous numbers when you know
   them, and say whether new code is covered.

**Always end with a clickable link to the HTML report.** Use the exact `HTML report:` URL the script
printed (a `file:///C:/.../coveragereport/index.html` URL on WSL/Windows), formatted as a markdown
link, e.g. `[Open coverage report](file:///C:/Users/Aldis/source/repo2/Biller/coveragereport/index.html)`.
If the report wasn't generated (build failure, `--no-report`, reportgenerator missing), say so
instead of linking.

## Writing missing tests

When asked to close a gap, write the tests with the `_write-tests` skill, which holds the test
conventions (no AutoFixture, strict mocks with `Verify`, labelled theory cases, per-layer patterns).
Re-run the script afterwards to confirm the new tests pass and the number moved.

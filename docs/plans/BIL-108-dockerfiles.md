# Plan: Dockerfiles that build, and the integration tests built once in CI (BIL-108)

Jira: [BIL-108](https://aldisadom.atlassian.net/browse/BIL-108). Epic: BIL-89 (Billeris).

## Goal

- No Dockerfile in Biller or BillioIntegrationTest does work that fails or isn't needed.
- Biller's API image restores every project it builds, with no `Skipping project ... because it was
  not found` line.
- BillioIntegrationTest's CI builds the tests once.

## Current state (Biller master @ 7195d1b, BillioIntegrationTest master @ 4e2b996)

| Repo | Where | What |
|---|---|---|
| Biller | `src/WebAPI/DockerfileLocal` | Built by `docker-compose.yml` (so by CI and BillioIntegrationTest's CI) and by `_test-stack`. Copies `WebAPI`, `Infrastructure`, `Application`, `Domain` `.csproj` before `dotnet restore`, but not `Validators.csproj`, which `WebAPI.csproj` references. The restore skips it; the build works because `COPY . .` runs before it |
| Biller | `src/WebAPI/Dockerfile` | Only used by Visual Studio's `Docker` launch profile (`Properties/launchSettings.json`), which the owner doesn't use, and which sets ports 8080/8081 while the image exposes 8070/8071. Same as `DockerfileLocal` plus `RUN dotnet workload install wasm-tools` (Blazor WebAssembly; nothing in `src` uses it, added with `db1c979`), and the same missing `Validators.csproj` |
| Biller | `src/WebAPI/WebAPI.csproj`, `docker-compose.dcproj` | `DockerDefaultTargetOS`, `DockerfileContext`, `DockerComposeProjectPath` and `Microsoft.VisualStudio.Azure.Containers.Tools.Targets`: Visual Studio container tooling, shared by the single-container profile and the `docker-compose.dcproj` compose project |
| Biller | project references | `WebAPI` → `Infrastructure`, `Validators`; `Validators` → `Infrastructure` → `Application` → `Domain` |
| IntegrationTest | `Dockerfile`, `src/IntegrationTests/Dockerfile` | Both copy and build `BillioIntegrationTest.csproj`, which doesn't exist (the project is `src/IntegrationTests/IntegrationTests.csproj`), so neither builds. Nothing builds them: CI and `_test-stack`'s `run-integration.sh` run the tests with `dotnet` on the host |
| IntegrationTest | `src/IntegrationTests/IntegrationTests.csproj` | `DockerDefaultTargetOS` and `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` 1.23.0, only there for the Dockerfile |
| IntegrationTest | `src/IntegrationTests/Properties/launchSettings.json` | A `Container (Dockerfile)` profile, and a `BillioIntegrationTest` profile that is only `"commandName": "Project"`, what an IDE does without the file |
| IntegrationTest | `.dockerignore` | Only used by a docker build of this repo, which nothing does |
| IntegrationTest | `.github/workflows/ci.yml` | Job `build` builds the solution; job `integration-tests` (`needs: build`) runs on a new runner and restores and builds the same solution again before `dotnet test --no-build` |

## Design decisions

1. **Remove BillioIntegrationTest's Dockerfiles rather than fix them.** Nothing uses them, and a
   container couldn't reach the API at `http://localhost:8090` without network setup that is out of
   scope (running the tests in a container). With them go `.dockerignore`, `DockerDefaultTargetOS`,
   the `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` package and `launchSettings.json`:
   its container profile exists only for them, and its other profile does what an IDE does without it.
2. **One CI job builds the tests.** Drop the separate `build` job instead of passing its output to
   the test job as an artifact: jobs run on separate runners, so an artifact means an upload and a
   download for a build that takes seconds. The integration job restores and builds **before**
   starting Biller's compose stack, so a compile error fails fast, without waiting for the API.
3. **Biller keeps one API Dockerfile, `DockerfileLocal`.** `src/WebAPI/Dockerfile` and the
   `Docker` launch profile go: the owner doesn't use the profile, it was misconfigured (ports), and the
   file only differed by the unused `wasm-tools` workload. `DockerfileLocal` keeps its name, because
   compose, CI and `_test-stack` build it by that name. The `http`, `https` and `IIS Express` profiles
   stay (F5 and `dotnet run`).
4. **The Visual Studio container tooling in `WebAPI.csproj` stays** (`DockerDefaultTargetOS`,
   `DockerfileContext`, `DockerComposeProjectPath`, the `Containers.Tools.Targets` package):
   `docker-compose.dcproj` in `Billio.sln` uses it too, and whether that compose profile is still
   wanted is a separate question.
5. **Copy every project file the API references before the restore**: add
   `src/Validators/Validators.csproj` to `DockerfileLocal`. The restore layer then caches all
   packages, and the warning goes.

## Dependencies

None. BIL-94 (Done) moved these files to .NET 10 and left the fixes to this issue.

## Step 1: Biller Dockerfiles (branch `BIL-108-dockerfiles`)

- Delete `src/WebAPI/Dockerfile` (and with it the `wasm-tools` line).
- `src/WebAPI/Properties/launchSettings.json`: remove the `Docker` profile.
- `src/WebAPI/DockerfileLocal`: after the `Domain.csproj` copy, add
  `COPY ["src/Validators/Validators.csproj", "src/Validators/"]`.

## Step 2: BillioIntegrationTest (branch `BIL-108-dockerfiles`)

- Delete `Dockerfile`, `src/IntegrationTests/Dockerfile` and `.dockerignore`.
- `IntegrationTests.csproj`: remove `DockerDefaultTargetOS` and the
  `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` reference.
- Delete `src/IntegrationTests/Properties/launchSettings.json`.
- `ci.yml`: remove the `build` job and `needs: build`. In `integration-tests`, move
  `dotnet restore` and `dotnet build` into a `Build` step after `Set up .NET` and before
  `Start services`; `Run integration tests` keeps only `dotnet test --no-build`.

## Step 3: Check

- Biller: `docker build -f src/WebAPI/DockerfileLocal .` succeeds, and the restore output has no
  `Skipping project` line. `grep -rn "WebAPI/Dockerfile\b"` finds no reference to the deleted file.
- `_test-stack`: `test-stack.sh up` (builds `DockerfileLocal`), then `run-integration.sh --repeat 3`
  with BillioIntegrationTest on `BIL-108-dockerfiles`; all pass. `test-stack.sh down`.
- BillioIntegrationTest: `dotnet build BillioIntegrationTest.sln -c Release` passes without the
  containers package; `grep -rn Dockerfile` finds nothing outside `bin`/`obj`.
- CI: Biller's PR runs the integration tests against BillioIntegrationTest's branch with the same
  name; BillioIntegrationTest's PR shows one job that builds once.

## Out of scope

- Running the integration tests in a container in CI.
- Renaming `DockerfileLocal`, the Visual Studio compose project (`docker-compose.dcproj`) and its
  tooling in `WebAPI.csproj`, and BillerFrontEnd's Dockerfile.
- Biller's own CI (`CI.yaml`), which already builds BillioIntegrationTest once.

## Order of work

1. This plan, on Biller's branch.
2. Step 1 and step 2, one PR each, on branches named `BIL-108-dockerfiles`.
3. Step 3 before both PRs; merge BillioIntegrationTest's first or together (they don't depend on
   each other).

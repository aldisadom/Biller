# Biller

[Lietuviškai](docs/README_lt.md)

Biller is an invoicing backend: a REST API that keeps users, sellers, customers, items, invoice
series and invoices, and makes invoice and job-done-act PDFs in Lithuanian and English. It has one
owner and runs in Docker Compose on a small Linux PC.

## The four repos

| Repo | What it is |
|---|---|
| Biller (this one) | The API, the database migrations and the PDFs |
| [BillerContracts](https://github.com/aldisadom/BillerContracts) | Request and response records, enums and typed HTTP clients, published as the `BillerContracts` and `BillerContracts.Clients` NuGet packages |
| [BillerFrontEnd](https://github.com/aldisadom/BillerFrontEnd) | The Blazor UI. It calls this API through `BillerContracts.Clients` |
| [BillioIntegrationTest](https://github.com/aldisadom/BillioIntegrationTest) | Tests that call a running API over HTTP |

A change to a request or response goes into BillerContracts first. It reaches the other repos after
a new package version is published and each repo's package reference is updated.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started) with Docker Compose
- For the coverage report: `dotnet tool install -g dotnet-reportgenerator-globaltool`

## Run

```sh
docker compose up -d --build    # or: make run
```

| Container | What | Where |
|---|---|---|
| `billio-webapi` | The API | http://localhost:8090, Swagger at http://localhost:8090/swagger |
| `billio-db` | PostgreSQL 16, database `Data` | localhost:5437 |
| `billio-liquibase` | Applies the migrations, then stops | |
| `billio-pgadmin4` | pgAdmin | http://localhost:8888 |

`make update` is for the PC the app is deployed on, with `master` checked out: it takes a backup,
then resets the checked-out branch to `origin/master` (changes to tracked files and local commits
are lost, and so is an untracked file at a path `origin/master` or the local `master` tracks),
switches to `master` and starts the containers again. Other untracked files stay. One in a project
under `src/` is built into the API (an untracked `.cs` file is compiled in), unless `.dockerignore`
leaves it out (`bin/`, `obj/` and others). An untracked changeset file in `migrations/sql/`
(`.sql`, `.yaml`, `.yml`, `.xml` or `.json`) is run against the database, so don't leave one there. Known problem: it resets before it
fetches, so it deploys the master that was fetched the time before, and only a second run gets the
newest one. On another branch, the first run resets that branch and deploys the local `master` as it is.

## Test

```sh
bash .claude/skills/_test-coverage/scripts/run-coverage.sh    # unit tests and the coverage report
bash .claude/skills/_test-coverage/scripts/build.sh           # build only
```

Both build Release, as CI does. The coverage report goes to `coveragereport/`.

The integration tests are in BillioIntegrationTest and delete all data through the API, so they run
against a throwaway stack, never against the containers above. `run-integration.sh` takes the tests
from a BillioIntegrationTest checkout next to this one (`../BillioIntegrationTest`), or from the
folder given with `--repo`:

```sh
bash .claude/skills/_test-stack/scripts/test-stack.sh up
bash .claude/skills/_test-stack/scripts/run-integration.sh
bash .claude/skills/_test-stack/scripts/test-stack.sh down
```

CI (`.github/workflows/CI.yaml`) builds, then runs two jobs side by side: the unit tests, which fail
when coverage drops below its minimums, and the integration tests from the BillioIntegrationTest
branch with the same name as this branch, or from its master.

## Configuration

Docker Compose runs the API with `ASPNETCORE_ENVIRONMENT=LocalDocker`, so the settings come from
`src/WebAPI/appsettings.json` and `src/WebAPI/appsettings.LocalDocker.json`:

- `ConnectionStrings:PostgreConnection`: the database.
- `FontSettings`: the font files for the PDFs, each with only a `Path`. Compose mounts `./Fonts` at
  `/Fonts`. The API doesn't start when a file is missing or isn't a readable font, when none of them
  holds the documents' font family (Calibri), or when they don't all have A-Z, a-z and 0-9. The
  files also decide what text can be saved: a character that isn't in every configured file (an
  emoji, Chinese) gets 400 "Validation failure" naming it, in any string a user, customer, seller,
  item or series is saved with (except the password) and in invoice comments.
- `PasswordEncryption:Pepper`: mixed into password hashes. Changing it makes every stored password wrong.
- `Serilog`: logging.

Ports, database name and credentials are in `docker-compose.yml`.

Invoices that are past their due date become overdue when the API starts and every day at 03:00 UTC.

## Where things go

- `src/WebAPI`: controllers, Swagger examples, middleware, background jobs.
- `src/Validators`: request validators.
- `src/Application`: services, models, mapping methods, PDF documents, amounts in words.
- `src/Domain`: entities, repository interfaces, exceptions.
- `src/Infrastructure`: Dapper repositories, registration of the database and the PDF generator.
- `tests/xUnitTests`: unit tests, in the same folders as `src`.
- `migrations/sql`: Liquibase changesets.
- `docs/plans`: one plan per Jira issue.

The conventions, package limits and review rules are in [CLAUDE.md](CLAUDE.md) and [REVIEW.md](REVIEW.md).

## Migrations

The schema is managed by Liquibase. Every `.sql` file in `migrations/sql/` is a changeset:
`migrations/dbChangeLog.yaml` includes the whole folder, and the `billio-liquibase` container applies
the files in file-name order (the number at the start) each time the stack starts. A changeset that
is on master is never edited; a fix is a new changeset. A new changeset has a `-- rollback` line.
Four old ones (023, 024, 026 and 027) have none, so a rollback can't go back past them.

## Backup and restore

`make update` takes a backup first and stops if it fails. Run `make backup` yourself before any
other change that touches data, such as deploying a migration by hand.

```sh
make backup     # saves backup/billio-YYYYMMDD-HHMMSS.dump (UTC), keeps the newest 14
make restore    # lists the backups in backup/, asks which one, then restores it into Data
```

A restore goes into a temp database first, so a broken backup leaves `Data` as it was. Only when
that succeeds does it stop `webapi`, back up the current `Data` to
`backup/billio-YYYYMMDD-HHMMSS-before-restore.dump` (so picking the wrong file can be undone), swap
the restored database in, run Liquibase and start `webapi` again. If Liquibase fails, `webapi` is
left stopped. The backup includes
Liquibase's `databasechangelog` table, so after restoring an older backup only the newer migrations
are applied.

**Check a backup** without touching the live database: restore it into a scratch database, compare
its row counts with the live ones, then drop it:

```sh
docker exec billio-db psql -U postgres -d postgres -c "CREATE DATABASE \"Data_restore_test\" TEMPLATE template0;"
docker exec -i billio-db pg_restore -U postgres -d Data_restore_test --no-owner --exit-on-error < backup/billio-....dump
docker exec billio-db psql -U postgres -d Data_restore_test -c "SELECT count(*) FROM invoices;"
docker exec billio-db psql -U postgres -d Data -c "SELECT count(*) FROM invoices;"
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE \"Data_restore_test\";"
```

**Without the scripts:**

```sh
# backup
docker exec billio-db pg_dump -U postgres -d Data -Fc > backup/billio.dump

# restore: into a temp database first; go on only if pg_restore succeeds
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE IF EXISTS \"Data_restore_tmp\" WITH (FORCE);"
docker exec billio-db psql -U postgres -d postgres -c "CREATE DATABASE \"Data_restore_tmp\" TEMPLATE template0;"
docker exec -i billio-db pg_restore -U postgres -d Data_restore_tmp --no-owner --exit-on-error < backup/billio.dump
docker stop billio-webapi
# back up the current Data; go on only if both commands succeed
undo=backup/billio-$(date -u +%Y%m%d-%H%M%S)-before-restore.dump
docker exec billio-db pg_dump -U postgres -d Data -Fc > "$undo"
docker exec -i billio-db pg_restore --list < "$undo" | grep -m1 "TABLE DATA"
# swap, migrate, and drop the old database only once Liquibase succeeded
docker exec billio-db psql -U postgres -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'Data'; ALTER DATABASE \"Data\" RENAME TO \"Data_restore_old\"; ALTER DATABASE \"Data_restore_tmp\" RENAME TO \"Data\";"
docker start -a billio-liquibase
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE \"Data_restore_old\" WITH (FORCE);"
docker start billio-webapi
```

Keep a copy of the backups off the PC (USB drive or cloud folder). A backup on the same disk does
not survive a disk failure.

## Licence

MIT, see [LICENSE.txt](LICENSE.txt).

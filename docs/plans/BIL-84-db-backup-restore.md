# Plan: database backup and restore script

Jira: [BIL-84](https://aldisadom.atlassian.net/browse/BIL-84). Related: BIL-82 (production on the small PC). The series plan ([BIL-65-series.md](BIL-65-series.md)) needs a backup before migration 033.

## Goal

- One command saves the whole Billio database to a file.
- One command restores a file into the database.
- The README lists the raw commands too, so a restore works even without the scripts.
- A backup can be checked without touching the live database.

## Current state (master @ 125ae5e)

| Where | What |
|---|---|
| `docker-compose.yml` | `db` service, container `billio-db`, `postgres:16`, database `Data`, user/password `postgres`, volume `postgres_data`, host port 5437 |
| `makefile` | `run` and `update` targets only |
| Backups | None. No script and nothing in the README |
| Liquibase | Its tables (`databasechangelog`, `databasechangeloglock`) live in the same database, so a full dump also records which migrations have run |

## Design decisions

1. **`pg_dump -Fc` (custom format).** It is compressed, and `pg_restore` can restore it selectively and list its contents for checking.
2. **Stream the dump through `docker exec`** (`pg_dump -Fc > file`, `pg_restore < file`). The scripts only run on Linux (the production PC, and WSL for development), so there's no PowerShell to break binary output, and no temp file inside the container is needed. *(Changed during work: the first version dumped to `/tmp` in the container and used `docker cp`, for PowerShell.)*
3. **Restore into a fresh database, then swap it in.** This is cleaner than `--clean` on a live database, which leaves anything not in the dump behind. The dump goes into `Data_restore_tmp` first. Only when that succeeds is `webapi` stopped, `Data` dropped and the temp database renamed to `Data`, so a broken or wrong dump leaves `Data` as it was. *(Changed during work: the first version dropped `Data` before restoring. The local test showed why that's risky: `CREATE DATABASE` failed, see decision 8, and that version would have left no database.)*
4. **Restoring an older backup is fine.** The dump carries `databasechangelog`, so `docker compose up liquibase` afterwards applies only the newer migrations.
5. **Backups go to `./backup/` in the current folder (git-ignored)** as `billio-YYYYMMDD-HHMMSS.dump`, and restore reads from there: a bare file name is looked up in `backup/`, and with no file it lists every backup in `backup/` (newest first, numbered, with size) and asks which one to restore; `--yes` takes the newest. The script keeps the newest 14 by default (`KEEP=n` to change it). *(Changed during work: was `backups/`, and restore needed a full path.)*
6. **`make update` runs `make backup` first.** The backup is timestamped and needs no input, and `update` is the step that deploys new migrations, so it always has one to fall back on. If the backup fails, `update` stops. There is no scheduling; any other data change needs a `make backup` by hand. *(Changed during work: the first version only printed a reminder.)*
7. **Restore asks for confirmation** (type the database name). `--yes` skips the prompt for scripted use.
8. **New databases use `TEMPLATE template0`.** The `postgres:16` image moved from glibc 2.36 to 2.41, and Postgres 16 refuses `CREATE DATABASE` from `template1` when its recorded collation version doesn't match (`template database "template1" has a collation version mismatch`). `template0` records no version, and the Postgres docs recommend it for `pg_restore` targets anyway.

## Step 1: Scripts (`scripts/`) ✅

`scripts/db-backup.sh`
```bash
#!/usr/bin/env bash
set -euo pipefail
CONTAINER=${CONTAINER:-billio-db}; DB=${DB:-Data}; KEEP=${KEEP:-14}
BACKUP_DIR=${BACKUP_DIR:-backup}; NAME="billio-$(date +%Y%m%d-%H%M%S).dump"
mkdir -p "$BACKUP_DIR"
docker exec "$CONTAINER" pg_dump -U postgres -d "$DB" -Fc > "$BACKUP_DIR/$NAME" \
  || { rm -f "$BACKUP_DIR/$NAME"; echo "Backup failed" >&2; exit 1; }
# check: the archive must be readable and list at least one table
docker exec -i "$CONTAINER" pg_restore --list < "$BACKUP_DIR/$NAME" | grep -q "TABLE DATA" \
  || { echo "Backup check failed: $BACKUP_DIR/$NAME"; exit 1; }
ls -1t "$BACKUP_DIR"/billio-*.dump | tail -n +$((KEEP + 1)) | xargs -r rm --
echo "Saved $BACKUP_DIR/$NAME ($(du -h "$BACKUP_DIR/$NAME" | cut -f1))"
```

`scripts/db-restore.sh <file> [--yes] [--target <db>]`
- Without `--target`, restores into `Data`: asks for confirmation, restores into `Data_restore_tmp`, then (only on success) `docker stop billio-webapi` if it is running, drops `Data`, renames the temp database, and starts `webapi` again (also from an `EXIT` trap). *(Uses `docker stop`/`start` with the container name instead of `docker compose`, so it works from any directory, like the `docker exec` calls.)*
- With `--target Data_restore_test`, restores into a scratch database and leaves `Data` and `webapi` alone. Use this to check a backup.
- Prints row counts for `users`, `sellers`, `customers`, `items`, `invoices` after the restore.

Core commands:
```bash
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE IF EXISTS \"$TARGET\" WITH (FORCE);"
docker exec billio-db psql -U postgres -d postgres -c "CREATE DATABASE \"$TARGET\" TEMPLATE template0;"
docker exec -i billio-db pg_restore -U postgres -d "$TARGET" --no-owner --exit-on-error < "$FILE"
```

## Step 2: makefile and .gitignore ✅

```make
backup:
	bash scripts/db-backup.sh

restore:
	bash scripts/db-restore.sh
```
`make restore` takes no variables: it lists the backups and asks which one. Options (`<file>`, `--target <db>`, `--yes`) go through `bash scripts/db-restore.sh` directly. *(Changed during work: the first version passed `FILE` and `ARGS` through make.)*
Add both to `.PHONY`. Add `backup/` to `.gitignore`.

The existing `update` target runs `git reset --hard` and then applies new migrations. It now depends on `backup` (`update: backup`), so make takes a backup before the reset and stops if that fails.

## Step 3: README ("Backup and restore" section, in `README.md`) ✅

*(Only `README.md`: `docs/README_en.md` is an empty stub and `README.md` is already in English.)*

- `make backup` / `make restore` only. The README doesn't show how to run the scripts directly; their options are documented in the script headers. The backup check uses raw `docker exec` commands.
- Raw `docker exec` commands, so a restore works without the scripts.
- How to check a backup: restore into `Data_restore_test`, compare counts, then `DROP DATABASE "Data_restore_test"` (quoted, or Postgres folds it to lower case).
- After restoring an older backup: `docker compose up liquibase` to apply newer migrations.
- When to back up: `make update` does it itself. Back up by hand before any other change that touches data.
- Keep a copy off the PC (USB drive or cloud folder). A backup on the same disk doesn't survive a disk failure.

## Step 4: Manual checks

- [x] `make backup` creates a file and the check passes. (Ran `scripts/db-backup.sh` directly on WSL, which has no `make`: 28K file.)
- [x] Restore with `--target Data_restore_test`: counts match the live database (users 1, sellers 1, customers 6, items 18, invoices 46; 23 changesets in both), and `webapi` kept running.
- [x] Guards: no file, missing file, and a wrong confirmation all exit 1 without touching `Data`.
- [x] Full restore into `Data` on the local stack: the API starts and an old invoice PDF generates. (Owner checked backup and restore by hand on 2026-10-22.)
- [x] Restore a backup taken before the newest migration, run liquibase, and confirm it applies only the missing changesets. (Owner checked by hand.)
- ~~Run `make backup` and a scratch restore on the production PC (Linux).~~ Dropped: the owner decided not to run a separate check there.

## Out of scope

- Scheduled backups. Backups happen on `make update` or by hand.
- Point-in-time recovery (WAL archiving). Overkill for this data volume.
- Encrypting backups. Worth adding if copies go to cloud storage, since `users` holds password hashes.

## Order of work

1. Scripts and makefile targets, tested on the local stack.
2. README section.
3. ~~Run it on the production PC and take the backup that the series migration (BIL-65) needs.~~ Not needed as a separate step: once this change is deployed, the `make update` that deploys BIL-65 takes the backup itself. (The update that deploys this change still runs the old makefile, so it takes no backup.)

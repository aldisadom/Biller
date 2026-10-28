---
name: _add-db-migration
description: Create a Liquibase SQL migration (changeset) for Biller's PostgreSQL database — new tables, new/renamed/altered columns, indexes, constraints — following the repo's numbering, naming, and rollback conventions, and optionally apply it locally via docker compose. Use this whenever a change in the Biller repo needs a schema change, e.g. "add a column", "new table for X", "make email unique", "store invoice currency", or when an entity/repository change implies the DB must change, even if the user doesn't say "migration" or "Liquibase". Not for seeding initial, sample, or test data.
---

# Add a DB migration (Liquibase, PostgreSQL)

## How migrations work here

- `migrations/dbChangeLog.yaml` uses `includeAll: path: /migrations/sql/`. Liquibase runs every
  file in `migrations/sql/` **in alphabetical order**, so the zero-padded numeric prefix is what
  orders them. Nothing needs registering in the changelog.
- **Migrations are schema-only. Don't seed data in them.** No `INSERT`s of initial, sample, demo,
  or test rows. The old seed folder was removed on purpose: the integration tests create their own
  data through the API and wipe the tables. If the user asks for seed data, say this and suggest
  creating it through the API instead (Swagger at http://localhost:8090/swagger once
  `docker compose up -d --build` is running). The one exception is an
  `UPDATE` of existing rows needed to complete a schema change, such as the backfill before
  `SET NOT NULL` below.
- Liquibase stores a checksum of each applied changeset. **Never edit a migration file that has
  already been committed/applied** — it breaks `update` on every existing database. Fix mistakes
  with a new migration. (That's why the history has several `..._add_number_column` files.)
- The app talks to the DB with Dapper and `MatchNamesWithUnderscores = true`, so a C# property
  `InvoiceNumber` maps to column `invoice_number`. Column names must be snake_case for the
  mapping to work.

## Steps

1. **Find the next number**: `ls migrations/sql | sort | tail -3`. Take the highest prefix + 1,
   zero-padded to 3 digits (e.g. after `031-...` comes `032-...`).
2. **Name the file** `NNN-<Verb>-table-<table>[_<what>].sql`, matching history:
   - `026-Create-table-invoices.sql`
   - `030-Alter-table-user_add_salt_column.sql`
   - `029-Alter-table-user_unique_email.sql`
3. **Write the changeset** in Liquibase formatted SQL. The changeset id is the number without
   padding, author `Aldis` (matches every existing changeset — ask if the user wants another
   author):

   ```sql
   --liquibase formatted sql

   -- changeset Aldis:32
   -- comment: Alter invoices table, add currency column
   ALTER TABLE invoices ADD COLUMN currency character varying(3);

   UPDATE invoices SET currency = 'EUR';

   ALTER TABLE invoices ALTER COLUMN currency SET NOT NULL;

   -- rollback ALTER TABLE invoices DROP COLUMN currency;
   ```

   Conventions to follow:
   - Table names are plural snake_case (`users`, `invoices`, `items`).
   - Primary keys: `id uuid NOT NULL DEFAULT gen_random_uuid()` + `PRIMARY KEY(id)`.
   - Strings `character varying(255)`, long text `TEXT`, money `numeric`/`decimal`, dates `DATE`,
     enums stored as `INT` (C# enum ordinal).
   - Foreign keys as named constraints: `CONSTRAINT fk_<table>_<column> FOREIGN KEY(<column>) REFERENCES <other>(id)`,
     and add an index on FK columns: `CREATE INDEX idx_<table>_<column> ON <table> (<column>);`
   - **Adding a NOT NULL column to a table that has rows**: add it nullable, backfill with
     `UPDATE`, then `SET NOT NULL` (as in 030 above). A plain `ADD COLUMN ... NOT NULL` fails on
     non-empty tables unless it has a `DEFAULT`.
   - Always include a `-- rollback` line that truly reverses the change (multiple statements are
     fine on one line, separated by `;`).
   - Prefer one logical change per file.
   - **Destructive changes (drop or rename a column or table, narrow a type) go in two releases.**
     First release: add the new shape, copy the data, and switch the code to it. A later release
     drops the old column or table, once production has run on the new code. A rollback can't
     bring dropped data back, so the drop must never ship together with the code change.
4. **Update the code that depends on the schema** (if the user wants the full change):
   the `*Entity` record in `src/Domain/Entities`, SQL strings in
   `src/Infrastructure/Repository/*Repository.cs` (INSERT column lists, UPDATE SET lists),
   models + mapping extensions in `src/Application`. If the field must be exposed through the API,
   use the `_add-api-feature` skill — request/response types live in the separate BillerContracts
   NuGet package.
5. **Try it on a throwaway database (preferred)**: the `_test-stack` skill runs the migrations on its
   own Postgres. `test-stack.sh up --seed`, then `test-stack.sh rollback-check --table <table>`
   rolls the changeset back on a table with rows, prints `\d <table>` before and after, and runs
   `update` again. `test-stack.sh down` when done. This doesn't touch the user's local DB.

   **Or apply locally (ask first, it changes the user's local DB)**:
   `docker compose run --rm liquibase` starts the db and runs `liquibase update`, printing each
   changeset it runs. The compose file uses fixed container names (`billio-db`, ...), so if the
   start fails with a name conflict, another checkout's stack is running: ask before stopping it.
   To verify: `docker compose exec db psql -U postgres -d Data -c '\d <table>'`.
   To test the rollback: `docker compose run --rm liquibase --changeLogFile=/migrations/dbChangeLog.yaml --url=jdbc:postgresql://db:5432/Data --username=postgres --password=postgres rollback-count 1`
   then run `docker compose run --rm liquibase` again.

## Before finishing

Paste each command you ran and the lines of its output that show the result (the changesets
Liquibase ran, the `\d` output, the rollback). Don't say it worked without the output.

Tell the user the file name, what it does, how it rolls back, and any data implications
(backfill values chosen, rows that could violate a new constraint). If the change could fail on
production data (unique constraint, NOT NULL, type narrowing), say so explicitly and suggest a
pre-check query such as `SELECT email, count(*) FROM users GROUP BY email HAVING count(*) > 1;`.

If the migration changes or deletes existing data, remind the user to back up the production
database before deploying. Backups are manual: `make backup` once BIL-84 is merged, until then
`docker exec billio-db pg_dump -U postgres -d Data -Fc -f /tmp/billio.dump` and
`docker cp billio-db:/tmp/billio.dump .`.

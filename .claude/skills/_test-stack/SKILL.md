---
name: _test-stack
description: Start a throwaway Biller stack (Postgres, Liquibase migrations, the API and optionally BillerFrontEnd) from any branch or checkout, next to the user's own billio-* stack without touching it, then run BillioIntegrationTest against it, test a migration's rollback on real rows, seed one invoice, or print what a front-end page renders. Use this whenever the user wants the integration tests run, a migration or its rollback checked, the API or a page tried out against a branch, or "see it working", and whenever another skill (_add-api-feature, _add-db-migration, _pre-pr) reaches a step that needs a running Biller, even if they say "docker" or "local stack".
---

# A throwaway stack for tests

The user's own stack (`billio-db`, `billio-webapi`, `billio-pgadmin4`, port 8090) holds their local
data and may be on any branch. Don't use it for tests, don't restart it, and don't run migrations
against it. The integration tests delete **all** data through the API.

This stack has its own network, containers (`biller-test-*`) and ports: API 8091, DB 5439, front
end 8092. Change them with `TEST_STACK_API_PORT`, `TEST_STACK_DB_PORT` and `TEST_STACK_FRONTEND_PORT`.

## Run it

```bash
S=.claude/skills/_test-stack/scripts
bash $S/test-stack.sh up                      # this checkout's API + migrations
bash $S/test-stack.sh up --biller ../Biller --frontend --seed   # another checkout, front end, one invoice
bash $S/run-integration.sh --repeat 3        # BillioIntegrationTest against it, 3 runs
bash $S/test-stack.sh rollback-check --table invoices   # last changeset: roll back, \d, update again
bash $S/render-page.sh http://localhost:8092/invoices/update/<invoice id>
bash $S/test-stack.sh down                    # always, when done
```

- `up` builds the API image from the checkout's working tree, so uncommitted changes are included.
  Use `--biller` to test another branch's checkout (for example a git worktree).
- `run-integration.sh` takes `--url` for another API and `--repo` for another BillioIntegrationTest
  checkout. Put that repo on the branch that matches the API first (`_sync-repos`). The tests run in
  parallel, so use `--repeat 3` after changing tests that share data.
- `seed` (or `up --seed`) prints the ids it created. The integration tests delete that data, so
  render pages before running them, or seed again.
- `rollback-check` needs rows to be meaningful: seed first, so the rollback runs on a table with data.
- `render-page.sh` shows the server-rendered page (values, selected options, table cells, the error
  box). It can't click; say what still needs a browser.

## When done

Run `test-stack.sh down`, even after a failure, then check `docker ps -a` shows no `biller-test-*`
container. Paste the result lines of each command (the `Run n/N` lines, the Liquibase lines, the
page values). Don't say something passed without them.

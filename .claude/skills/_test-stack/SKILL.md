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

To run several stacks at once (a worktree each, or agents working in parallel), give each one a
name: `TEST_STACK_NAME=<name>`, such as the branch's key `bil-101`, on **every** `test-stack.sh`,
`run-integration.sh` and `render-page.sh` command for it. Its containers, network and images are then
`biller-test-<name>-*`, and Docker picks free ports unless the `TEST_STACK_*_PORT` variables set them.
`up` and `status` print the ports, and `run-integration.sh` finds the API's port by itself. Without
a name, a command works on the unnamed stack on the fixed ports.

## Run it

```bash
S=.claude/skills/_test-stack/scripts
bash $S/test-stack.sh up                      # this checkout's API + migrations
bash $S/test-stack.sh up --biller ../Biller --frontend --seed   # another checkout, front end, one invoice
bash $S/run-integration.sh --repeat 3        # BillioIntegrationTest against it, 3 runs
bash $S/test-stack.sh rollback-check --table invoices   # last changeset: roll back, \d, update again
bash $S/render-page.sh http://localhost:8092/invoices/update/<invoice id>
bash $S/test-stack.sh down                    # always, when done
TEST_STACK_NAME=bil-101 bash $S/test-stack.sh up   # a second stack next to the first
TEST_STACK_NAME=bil-101 bash $S/run-integration.sh --repeat 3
TEST_STACK_NAME=bil-101 bash $S/test-stack.sh down
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

## As a PR step

`_pre-pr` runs this (step 3) when `scope.md` says `Integration tests: yes`. CI takes the tests from
the BillioIntegrationTest branch named there, so put the local checkout on that branch first
(`_sync-repos`). Then save the result for the PR description:

```bash
P=.claude/skills/_pre-pr/scripts/pr-state.sh
if bash $S/test-stack.sh up; then
  OUT="$(bash $S/run-integration.sh --repeat 3 2>&1)"; CODE=$?; echo "$OUT"
  bash $S/test-stack.sh down
  { echo "BillioIntegrationTest branch: $(git -C ../BillioIntegrationTest branch --show-current)"
    echo "Command: run-integration.sh --repeat 3 (exit $CODE)"
    grep -E '^Run [0-9]+/[0-9]+|Build FAILED' <<< "$OUT"; } | bash $P write integration
else
  echo "test-stack.sh up failed: nothing saved. Fix the error, run test-stack.sh down, then run this again."
fi
```

Run the tests only when `up` succeeded. When it fails ("the test stack is already there", a build
or migration error), an older stack with the same name may still be running, and the tests would
pass for code that isn't in it.

## When done

Run `test-stack.sh down` (with the same `TEST_STACK_NAME`), even after a failure, then check
`docker ps -a` shows none of your stack's containers. A `biller-test-<name>-*` stack with another
name may belong to another agent; leave it alone. Paste the result lines of each command (the
`Run n/N` lines, the Liquibase lines, the page values). Don't say something passed without them.

Containers you start yourself (a throwaway Postgres, the API) get removed with their volumes too:
`docker rm -f -v <name>`, or start them with `docker run --rm`. Plain `docker rm` leaves the
postgres image's anonymous data volume behind. Then paste `docker ps -a` and
`docker volume ls -f dangling=true`: nothing you created may be left. Leave the compose stack
(`billio-*` containers) and volumes from before this session alone.

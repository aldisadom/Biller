---
name: _sync-repos
description: Put Biller's sibling repos (BillerContracts, BillerFrontEnd, BillioIntegrationTest) on the right branch before reading, comparing, building or changing them — the branch with the same name as Biller's current branch, or up-to-date master when there is none. Use this before any step that looks at or uses another repo — checking contracts or clients, bumping package versions, running integration tests, comparing code or conventions across repos — and whenever the user asks whether the repos are in sync, even if they don't mention branches.
---

# Sync sibling repos before using them

Local clones drift. A sibling can sit on an old feature branch, or on a master that is weeks behind
origin, and reading it then gives wrong answers: paths that no longer exist, versions that moved
on. So before reading, comparing, building or changing another repo, put it on its **target branch**:

- the branch with the same name as Biller's current branch, if the sibling has one (locally or on origin);
- otherwise `master`, fast-forwarded to origin.

This is also how CI pairs the repos: Biller and BillioIntegrationTest each test against the other's
branch with the same name, else master.

## Run it

```bash
bash .claude/skills/_sync-repos/scripts/sync-repos.sh                         # check only, changes nothing
bash .claude/skills/_sync-repos/scripts/sync-repos.sh --sync                  # switch and fast-forward
bash .claude/skills/_sync-repos/scripts/sync-repos.sh --sync BillerContracts  # only the repos you need
bash .claude/skills/_sync-repos/scripts/sync-repos.sh --sync --branch master  # Biller and every sibling on master
```

When the user gives a branch (`/_sync-repos master`), pass it as `--branch` and name no repos. Biller
(this checkout) then goes on that branch too, after the siblings. It switches only when it has no
uncommitted changes, new files included. When Biller has no such branch it should be on master,
but the script doesn't move it there: it reports `NOT READY`, and you ask the user. With repos
named, Biller is included only when `Biller` is one of them. In a worktree, a branch that another
worktree has checked out can't be switched to, so Biller reports `BLOCKED`.

It fetches every repo first, then prints one line per repo: `ok`, `NOT READY` (check mode only),
`BLOCKED` or `MISSING`. Exit code 0 means every repo is `ok`. From a sibling repo, pass its branch
and the repos to check, including Biller:
`bash ../Biller/.claude/skills/_sync-repos/scripts/sync-repos.sh --branch "$(git branch --show-current)" Biller BillerContracts`.

## What to do with the result

1. Run the check. If every line is `ok`, go on.
2. Otherwise run it with `--sync`. It only switches a repo whose tracked files have no uncommitted
   changes, and it only fast-forwards. Tell the user which repos it switched or updated.
3. `BLOCKED` (uncommitted changes on another branch, or a branch that has diverged from origin):
   stop and ask the user. Don't stash, reset, commit or discard their work.
4. `MISSING`: ask before cloning.
5. If it warns that Biller itself is behind origin, tell the user. Comparing against a stale Biller
   is just as wrong.

Paste the script's output. Don't say the repos are in sync without it.

When you create a branch in a sibling repo for the same piece of work, give it Biller's branch
name, so the skill and CI pair them.

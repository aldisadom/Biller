---
name: _cleanup-branches
model: sonnet
effort: high
description: Delete old local git branches in Biller and its sibling repos (BillerContracts, BillerFrontEnd, BillioIntegrationTest), and the worktrees that have them checked out, keeping master, the checked-out branch and the branch with Biller's current name. Local only, never touches branches on origin. Use this whenever the user wants to clean up, prune, tidy or remove old, stale, merged or leftover branches or worktrees in any Biller repo, even if they name only one repo or say "gone branches".
---

# Delete old local branches

Feature branches pile up locally after their PRs merge (squash merges leave them looking
unmerged, so `git branch -d` won't remove them). This skill deletes them in all four repos at once.

What it keeps in every repo:
- `master`;
- the branch the repo has checked out;
- the branch with Biller's current branch name, because `_sync-repos` and CI pair repos by it.

Everything else is force-deleted (`git branch -D`), including branches that were never pushed.
The user chose that on purpose. **Remote branches are never touched**: the script runs no `push`
and no `fetch`. Don't add a remote delete, even if asked in passing; that is a separate decision.

Worktrees go with their branch, since git won't delete a branch that a worktree has checked out.
The script decides each branch before it changes anything, so the dry run says what `--delete` does:
- A worktree that is there is removed (`git worktree remove`, never `--force`) before its branch
  is deleted, but only when it is clean. That means no uncommitted changes and no new files. It
  also means no ignored files other than build output (`bin/`, `obj/`, `TestResults/`,
  `coveragereport/`, `.vs/`, `node_modules/`), so a `backup/` dump or a `*.user` file keeps it.
  And no other worktree may be inside it.
- A worktree record whose folder is gone is removed with its branch. When the folder's parent
  can't be found either (a drive that isn't mounted), the branch is kept.
- `KEPT`, with the reason, in every other case: the worktree isn't clean, is locked
  (`git worktree lock`), or was made by Windows git and is seen from WSL (remove that one from
  Windows). Tell the user, and don't commit, stash, unlock or discard anything for them.

The script never runs `git worktree prune`. Prune drops every record whose folder it can't reach.
That includes a `C:\` path seen from WSL, a `/mnt/c` path seen from Windows and an unmounted
drive, and the branch would then be deleted unchecked. Don't run prune for the user either.

The worktree that this checkout is (when the skill runs from one) has Biller's current branch, so
it is kept.

## Run it

```bash
bash .claude/skills/_cleanup-branches/scripts/cleanup-branches.sh                  # dry run, changes nothing
bash .claude/skills/_cleanup-branches/scripts/cleanup-branches.sh --delete         # delete
bash .claude/skills/_cleanup-branches/scripts/cleanup-branches.sh --delete Biller  # only some repos
```

Each branch is printed with its last commit and state: `remote gone` (PR merged or branch deleted
on GitHub), `never pushed` (exists only here), or `on origin`, plus its worktree when it has one.

## Steps

1. Run the dry run and paste its output.
2. If any branch is `never pushed` or `on origin` with commits ahead, point those out: they may be
   work that exists nowhere else. Then run `--delete`. The user asked for this skill, so the
   deletion itself is expected; you don't need a second confirmation unless the list has
   unpushed branches the user hasn't seen yet. Name any `KEPT` worktree too.
3. Paste the `--delete` output. It lists each deleted branch's SHA, so a mistake can be undone with
   `git -C ../<repo> branch <name> <sha>` while the commit is still in the reflog. A removed
   worktree is recreated with `git -C ../<repo> worktree add <path> <name>` (see the worktree memory
   for the WSL steps); its folder held nothing uncommitted except build output.

To keep a different branch, check it out in that repo first; the script keeps whatever is checked out.

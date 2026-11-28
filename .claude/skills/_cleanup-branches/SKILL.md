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

Worktrees go with their branch, since git won't delete a branch that a worktree has checked out:
- `--delete` first prunes the records of worktrees whose folder is gone (`git worktree prune`).
- A worktree is removed (`git worktree remove`, never `--force`) before its branch is deleted.
- A worktree with uncommitted changes, new files or ignored files other than build output (`bin/`,
  `obj/`, `TestResults/`, `coveragereport/`, `.vs/`, `node_modules/`) is `KEPT` with its branch,
  so a `backup/` dump survives. Tell the user, and don't commit, stash or discard that work for them.
- Anything git itself refuses, such as a locked worktree, shows as a `FAILED` line.

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
   worktree held nothing uncommitted except build output.

To keep a different branch, check it out in that repo first; the script keeps whatever is checked out.

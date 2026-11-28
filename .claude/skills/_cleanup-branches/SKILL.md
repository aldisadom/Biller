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
- a worktree record whose folder is already gone is pruned (`git worktree prune`);
- a worktree that is still there is removed (`git worktree remove`) before its branch is deleted;
- a worktree with uncommitted changes, new files included, is `KEPT` with its branch. The script
  never uses `--force`. Tell the user, and don't commit, stash or discard that work for them.
- From WSL, a worktree made by Windows git has a `C:\` path that looks gone, so the script doesn't
  prune that repo's records and says so. Prune them from Windows.

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
Pruned worktree records are listed first, as `would prune` or `pruned`.

## Steps

1. Run the dry run and paste its output.
2. If any branch is `never pushed` or `on origin` with commits ahead, point those out: they may be
   work that exists nowhere else. Then run `--delete`. The user asked for this skill, so the
   deletion itself is expected; you don't need a second confirmation unless the list has
   unpushed branches the user hasn't seen yet. Name any `KEPT` worktree too.
3. Paste the `--delete` output. It lists each deleted branch's SHA, so a mistake can be undone with
   `git -C ../<repo> branch <name> <sha>` while the commit is still in the reflog. A removed
   worktree is recreated with `git -C ../<repo> worktree add <path> <name>` (see the worktree memory
   for the WSL steps); its folder held nothing uncommitted.

To keep a different branch, check it out in that repo first; the script keeps whatever is checked out.

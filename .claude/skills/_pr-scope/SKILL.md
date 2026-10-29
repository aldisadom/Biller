---
name: _pr-scope
description: First PR step — work out what a Biller branch's PR contains and needs (Jira key and plan, commits and uncommitted files, risk from migrations, data changes, the BillerContracts version, invoice numbering and PDFs, blockers, sibling repos with the same branch, whether integration tests are needed and from which branch) and save it as scope.md for the later steps. Use it as step 1 of _pre-pr, or alone when the user asks what a branch changes, how risky a change is, or which repos and tests a PR needs.
---

# What the PR contains

```bash
bash .claude/skills/_pr-scope/scripts/scope.sh [repo]
```

It saves `scope.md` in the PR state folder and prints it; paste the output. Exit 1 means it found a
blocker. It asks GitHub which sibling repos have the same branch, and changes nothing else.
Notes added to `scope.md` survive a re-run.

Then go through what it printed:

1. **Blockers**: stop and tell the user.
   - On `master`: the work needs its own `BIL-<n>-<slug>` branch.
   - A merge, rebase or cherry-pick in progress: finish or abort it first.
   - A `ProjectReference` to BillerContracts: CI clones only Biller, so the PR can't build. See
     `_add-api-feature`, section 1.
2. **Uncommitted files**: ask whether they belong to this PR. The review and the tests see them
   either way, because the steps work on the working tree. Leave the ones that don't belong out
   with `bash .claude/skills/_pre-pr/scripts/pr-state.sh exclude <path>`, then run `scope.sh`
   again. The ones that do belong get committed before the PR is opened (`_commit`).
3. **Other repos**: when `Same branch in:` lists repos, or the change touches contracts, endpoints
   or SQL, run `_sync-repos` and paste its output before you look at a sibling. Then run `scope.sh`
   for each repo in the PR.
4. **Risk the script can't see**: for each migration, the pre-check query against production data
   (from the plan or `_add-db-migration`). Record it, and any decision the user makes here, as a note:

   ```bash
   bash .claude/skills/_pre-pr/scripts/pr-state.sh note scope "035 pre-check: SELECT ... returns 0 rows on a copy of production"
   bash .claude/skills/_pre-pr/scripts/pr-state.sh note scope "docs/plans/BIL-65-series.md progress stays out of this PR (user)"
   ```

The notes go into the review pack and the PR description.

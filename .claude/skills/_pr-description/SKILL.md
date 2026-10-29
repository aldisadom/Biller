---
name: _pr-description
description: PR step — write a Biller PR's description from .github/pull_request_template.md using what the earlier steps left in the PR state folder (scope.md for Jira, plan and risk; tests.md and integration.md for Testing; findings.md for the review), after checking that none of them is missing or stale, and save it as description.md. Use it as step 6 of _pre-pr, or alone when the user asks for the PR text, description or summary of a branch.
---

# Write the PR description

```bash
P=.claude/skills/_pre-pr/scripts/pr-state.sh
bash $P status [repo]
```

`scope`, `tests` and `findings` must be `ok`, and `integration` too unless it's `not needed`. When
one is missing or `STALE`, run that step first, or tell the user which one is missing. Never write
the Testing section from a stale result: it would describe code that has changed since.

Read them with `bash $P read [repo] <step>`, then fill `.github/pull_request_template.md`:

- **Jira**: the key from `scope.md`.
- **What and why**: two to four lines, from the plan and the commits.
- **Testing**: the command and result lines from `tests.md` and `integration.md` (or "not needed"
  and why, from `scope.md`), and the BillioIntegrationTest branch CI will use. Then the review:
  rounds run and how many findings were fixed, answered, and pre-existing, from `findings.md`.
- **Risk**: the risk lines and notes in `scope.md`, written out. For migrations: what they change
  and whether they can fail on production data (give the pre-check query). Also data fixes, a
  BillerContracts version bump (a pre-release one must be replaced before merging), PDF layout
  changes, and the pre-existing findings left for later. "Low: code only, covered by unit tests"
  is fine when true.
- **How to revert**: code only → "revert the PR". Migration → its rollback, or "restore the backup
  taken before deploying". If a data change can't be rolled back, say so.
- **Other repos**: when `scope.md` lists repos under `Same branch in:`, name their PRs, or say that
  they follow.
- End with the line the session's attribution instructions give for PR descriptions.

Save it, then show it to the user:

```bash
bash $P write [repo] description <<'EOF'
Jira: BIL-...
...
EOF
```

Opening the PR is `_open-pr`'s job, and only when the user asks.

# Plan: apply "Solving code review at Anthropic" to Biller's skills

Jira: [BIL-88](https://aldisadom.atlassian.net/browse/BIL-88). Epic: BIL-89 (Billeris).

Source: https://claude.dev/blog/solving-code-review-at-anthropic/. Related: BIL-56 (pull request default texts).

## Summary of the article

**Problem.** Claude writes about 90% of Claude Code's code. Line-by-line human review doesn't scale. The goal became: *less human review time per change, with no more reverts or incidents.* Between June and September, merged PRs more than doubled and the revert rate fell by over 40%.

**Approach.** For each change, ask what could go wrong and which check (automated or human) should catch it. Review runs early and continuously, "like spellcheck while you write," so most changes are reviewed and fixed by Claude before a person sees them.

**Layers.**
1. Pre-PR: Claude runs tests, build and lint and must **show the output**, not claim success. A subagent reviews the diff in a fresh context.
2. Required CI.
3. AI review: several agents look for bugs. Verifier agents vote, and only findings with enough agreement are posted. Style and cleanup findings are filtered out.
4. Separate security review.
5. Risk-based approval: none, one or two humans depending on the change, not the author. An agent's approval never counts as a person's.
6. Merge queue, then flags and gradual release with fast rollback.

**Keeping review useful.**
- Findings are tagged **Important**, **Nit** or **Pre-existing**. Every finding is fixed or answered with a reason.
- Past incident PRs become an eval set. Reviewer changes are kept only if the score improves.
- Review must converge. One PR went 50+ rounds, and 59% of what the reviewer kept flagging was already there on the first pass. Fixes: cap at 3 rounds, then reconsider the approach. Re-review only the new diff. Flag the whole class of bug at once. Never repeat a finding. Result: mean reviews to pass dropped from 2.08 to 1.32.
- Giving the agent a way to verify its work makes results "two to three times better."

**Instruction files.**
- `CLAUDE.md`: short. Stack, conventions, goals and **non-goals**. Point to other docs.
- `REVIEW.md`: what failure looks like in this repo, paths to skip, a cap on nits, every finding cites file:line, the summary starts with a count and says plainly when there's nothing.
- When the same finding keeps coming back, move it into a skill, an instruction file or a lint rule. When review misses a bug, ask what context it was missing and add it.
- PR template with **Testing**, **Risk** and **How to revert**.

## What applies to Biller

Biller is a one-person repo, so the risk-tier and approval machinery doesn't apply. What does:

| Article idea | Biller today | Change |
|---|---|---|
| Short `CLAUDE.md` with conventions and non-goals | **No `CLAUDE.md`** | Add one (Step 1) |
| `REVIEW.md` with repo failure modes | None | Add one (Step 2) |
| Show verification output | Skills have "Verify" steps, but they don't require pasting the evidence | Add an evidence rule to every skill (Step 3) |
| Fresh-context review before the PR, severity tags, round cap | Nothing | New `_pre-pr` skill (Step 4) |
| PR template: Testing / Risk / How to revert | None (BIL-56) | Add it (Step 5) |
| Repeated findings go to the source | Skills exist, no loop | "Lessons" rule in `REVIEW.md` (Step 2) |
| Incidents become evals | Nothing | Small known-bugs list (Step 6) |
| Context the reviewer was missing | `add-api-feature` and `add-db-migration` point to `run-local-stack` and `integration-tests` skills that **don't exist in this repo** | Fix the references (Step 3) |

## Step 1: `CLAUDE.md` (under ~40 lines)

- Stack: .NET 8, Dapper + PostgreSQL 16, Liquibase SQL changesets, QuestPDF, xUnit v3 + Moq. Contracts live in the `BillerContracts` repo/NuGet. Front end lives in `BillerFrontEnd`. Integration tests live in `BillioIntegrationTest` (same branch name).
- Layers and where things go (one line each).
- Conventions: multi-statement writes are single SQL statements with CTEs (no transactions on the shared connection). Never edit an applied changeset. Invoices snapshot their data and must not change when master data changes.
- Non-goals: no ORM, no AutoMapper in Biller (`*Extensions` mapping methods instead).
- Pointers: `docs/plans/`, the skills list, how to run tests.

## Step 2: `REVIEW.md`

```markdown
# Review rules for Biller

Failure here looks like:
- An invoice total, line sum, or amount in words is wrong.
- Two invoices get the same number, or a number is skipped without a delete.
- An old invoice or its PDF changes after a seller, customer, item, or unit is edited.
- A migration loses data, can't roll back, or edits an already-applied changeset.
- A password, hash, or salt ends up in a response, log, or invoice.
- A request for one user's data returns another user's data.

Skip: bin/, obj/, coveragereport/, *.Designer.cs, migrations already on master (flag edits to them instead).
Tag each finding Important, Nit, or Pre-existing. At most 2 Nits. No naming or formatting comments.
Every finding cites file:line and a concrete failing input. No guesses from names.
Start with the number of Important findings. If there are none, say "No issues found."
When a finding repeats across reviews, propose a rule for CLAUDE.md, a skill, or an analyzer instead of repeating it.
```

## Step 3: Existing skills

- Add to the end of each skill's Verify/Finish section: *"Paste the command and its last lines of output (the `Passed!/Failed!` line, the liquibase changesets run). Don't say it passed without the output."*
- `add-api-feature`, `add-db-migration`: the `run-local-stack` and `integration-tests` skills they point to aren't in `.claude/skills`. Either add them to the repo (if they exist elsewhere) or replace the references with the actual `docker compose` commands.
- `add-db-migration`: add the "copy, then drop in a later release" rule for destructive changes and the backup step (from [BIL-84-db-backup-restore.md](BIL-84-db-backup-restore.md)).
- `write-tests`: add "the test fails if the bug is reintroduced" as a check for bug-fix tests.

## Step 4: New skill `_pre-pr`

`.claude/skills/_pre-pr/SKILL.md` (underscore prefix like the other repo skills), used before opening a PR:
1. Build and run the tests (`test-coverage` script). Paste the output. Stop if red.
2. Run `/code-review` on the branch diff (fresh context, reads `REVIEW.md`).
3. Fix Important findings. Answer the others with a reason. Leave Pre-existing ones alone and list them.
4. Re-review only the new diff. **After 3 rounds**, stop and summarise what keeps coming back, so the approach can be reconsidered.
5. Fill the PR template, including Risk and How to revert (for migrations: the rollback or "restore from backup").

## Step 5: PR template (closes BIL-56)

`.github/pull_request_template.md`:
```markdown
## What and why
## Testing
<!-- commands run and their result -->
## Risk
<!-- what could break; migration? data change? -->
## How to revert
```
Copy it to `BillerFrontEnd` and `BillioIntegrationTest`.

## Step 6: Known-bugs list (small eval set)

`docs/review-evals.md`: past real bugs with the commit that introduced them and the fix, e.g. `GenerateInvoiceName` using the customer's number instead of the invoice's, and the number-to-words grammar bugs. When changing `REVIEW.md` or the `_pre-pr` skill, run `/code-review` on a couple of those introducing commits and check the bug is still caught.

## Out of scope

- Multi-agent voting, risk classifiers, merge queues: too much for a one-person repo.

## Order of work

1. `CLAUDE.md` and `REVIEW.md`.
2. PR template.
3. Skill edits and the missing-skill references.
4. `_pre-pr` skill.
5. Known-bugs list. Grow it as bugs are found.

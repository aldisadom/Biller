---
name: _pre-pr
description: Get a Biller change ready for a pull request by running the PR steps in order — _pr-scope, _test-coverage, _test-stack when needed, _pr-review and _pr-fix (at most 3 rounds), then _pr-description — where each step leaves its result in the branch's PR state folder for the next one and runs again only when the code changed since. Use this whenever the user wants to prepare a PR, asks "is this ready?" or "can I merge this?", or has finished a feature or fix in any Biller repo and is about to push, even if they don't say "PR". For one step alone ("just review it", "write the PR text", "open the PR"), use that step's skill.
---

# Before opening a PR

The person reading the PR should only have to read it, not hunt for bugs. This skill runs the PR
steps in order. Each step is its own skill, can also run alone, and leaves its result in the
branch's state folder, where the next step reads it instead of working it out again. Every step
pastes the command and the output lines that show its result. Don't say a step passed without them.

## The steps

| # | Skill | Reads | Writes |
|---|---|---|---|
| 1 | `_pr-scope` | git | `scope.md`: Jira, plan, risk, blockers, repos, integration tests needed |
| 2 | `_test-coverage` | | `tests.md` |
| 3 | `_test-stack`, when `scope.md` says `Integration tests: yes` | `scope.md` | `integration.md` |
| 4 | `_pr-review` | `scope.md`, `findings.md` | a snapshot, new lines in `findings.md` |
| 5 | `_pr-fix` | `findings.md` | fixes, each finding's outcome in `findings.md` |
| 6 | `_pr-description` | all of the above | `description.md` |
| 7 | `_open-pr`, only when the user asks | `description.md` | the pushed branch and the PR |

## The state folder

```bash
P=.claude/skills/_pre-pr/scripts/pr-state.sh
bash $P status [repo]              # each step: ok, STALE, missing or not needed
bash $P read [repo] <step>         # exit 1 when missing, 3 when stale
bash $P write [repo] <step>        # stdin becomes <step>.md, stamped with the working tree
bash $P note [repo] <step> "text"  # a decision or detail for the later steps
bash $P exclude [repo] <path>      # leave an uncommitted path out of the PR
bash $P reset [repo]               # start over
```

It is `<repo>/.git/pre-pr/<branch>/`: never committed, and shared by all worktrees of the repo.
Each result is stamped with the working tree it describes (commits, uncommitted changes and new
files). When code changes after a step ran, `status` shows that step `STALE` and it runs again;
a change to docs only (`*.md`, `docs/`) doesn't. Committing doesn't change the tree, so results stay valid.

## Run

1. `bash $P status`. Start at the first step that is missing or `STALE`, and reuse the ones that
   are `ok`. On a new branch that is step 1.
2. Steps 1 to 3. Stop at a blocker in `scope.md`, a failed build, or a red test this change caused:
   fix it, or report it and ask, before going on.
3. Steps 4 and 5 are the review loop: review, fix, then step 2 again (and step 3 when a fix touched
   what it tests), then review again. At most 3 rounds; `_pr-review` skips a round when only docs
   changed. After round 3, stop even if findings remain. Tell the user what keeps coming back and
   whether the approach should change.
4. Step 6, then show the description.
5. Step 7 only when the user asks. Commit only when asked, with `_commit`.

When `scope.md` lists other repos under `Same branch in:`, each repo gets its own PR and its own
state folder. Run the steps for each one with the repo argument (`BillerFrontEnd`). Reviews of
different repos can run at the same time.

## After the PR

When a kind of finding keeps coming back across PRs, propose a rule for `REVIEW.md`, `CLAUDE.md`, a
skill or an analyzer. When a bug got past review, add it to `docs/review-evals.md`.

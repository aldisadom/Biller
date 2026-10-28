---
name: _pre-pr
description: Get a Biller change ready for a pull request — build and run the tests and show their output, review the branch diff in a fresh context against REVIEW.md, fix or answer every finding (at most 3 review rounds), then write the PR description from the repo template with Testing, Risk and How to revert. Use this whenever the user wants to open, create or prepare a PR, asks "is this ready?", "review my branch" or "can I merge this?", or has finished a feature or fix in the Biller repo and is about to push, even if they don't say "PR".
---

# Before opening a PR

The person reading the PR should only have to read it, not hunt for bugs. Every step leaves
evidence in the conversation: paste the command and the lines of output that show the result.
Don't say a step passed without that output.

## 1. Know the change

- `git status` and `git log --oneline master..HEAD`. Ask whether uncommitted changes belong to this PR.
- `git diff master...HEAD --stat`. Note whether it touches `migrations/sql/`,
  `src/Domain/Domain.csproj` (BillerContracts version), invoice numbering, or PDF documents. These
  decide the Risk section.
- If the change touches contracts, endpoints or SQL, run the `_sync-repos` skill and paste its output
  before you look at or run anything in a sibling repo.
- A `ProjectReference` to BillerContracts in `src/Domain/Domain.csproj` blocks the PR, because CI
  clones only Biller. Stop and tell the user (see `_add-api-feature`, section 1). A `-preview`
  BillerContracts version is fine on the branch, but put it under Risk: it must be replaced by a
  released version before merging.
- When sibling repos have a branch with the same name, each one gets its own PR. Run steps 1 to 5
  for each repo with that repo's `CLAUDE.md` and `REVIEW.md`. The scripts below take the repo
  folder or sibling name (`BillerFrontEnd`) as an argument.

## 2. Build and test

Run `bash .claude/skills/_test-coverage/scripts/run-coverage.sh` and paste the `Passed!/Failed!`
line. If the build fails or a test is red, stop: fix it if this change caused it, otherwise report it.
For a sibling repo, `bash .claude/skills/_test-coverage/scripts/build.sh <repo> --release` prints
its errors and the warnings in changed files.

If the change adds endpoints or changes SQL, the integration tests in CI use the BillioIntegrationTest
branch with the same name as this branch, or master if there isn't one. Check and paste the result
of `git ls-remote --heads https://github.com/aldisadom/BillioIntegrationTest.git <branch>`. Run
them before the review with the `_test-stack` skill (`test-stack.sh up`, then
`run-integration.sh --repeat 3`, then `test-stack.sh down`).

If you test against containers you start yourself (a throwaway Postgres for the migrations or the
integration tests, the API), remove them and their volumes when the testing is done:
`docker rm -f -v <name>`, or start them with `docker run --rm`. Plain `docker rm` leaves the
postgres image's anonymous data volume behind. Then paste `docker ps -a` and
`docker volume ls -f dangling=true`: nothing you created may be left. Leave the compose stack
(`billio-*` containers) and volumes from before this session alone.

## 3. Review in a fresh context

The reviewer must not share this conversation's assumptions. Every review starts cold and is the
most expensive step, so hand it one file with what it needs instead of letting it explore:

```bash
R=.claude/skills/_pre-pr/scripts/review-round.sh
bash $R save [repo] [--exclude .claude]   # snapshot of what is reviewed now (exclude untracked folders that aren't part of the change)
bash $R pack [repo] --full --plan docs/plans/BIL-<n>-<slug>.md   # prints the pack's path
```

Then run `/code-review` (the `code-review` skill) at its default level. Tell it to read the pack
first, to follow the repo's `REVIEW.md` (it's in the pack), and to open other files only to check
a finding. For a sibling repo, name the repo folder as the target. Reviews of different repos can
run at the same time. If that skill isn't available, start a general-purpose subagent with the
same prompt.

## 4. Handle every finding

- **Important**: fix it, with a test that fails without the fix (`_write-tests`). If the same
  kind of bug is in several places, fix all of them in this round.
- **Nit**: fix it, or answer in one line why not.
- **Pre-existing**: leave it out of this PR. List it for the user; it may deserve a Jira task.
- A finding you think is wrong: answer with the reason (`file:line`, the input that shows it's
  fine). Don't change code just to silence it.

Keep a list of every finding and what happened to it in the findings file
(`bash $R findings [repo]` prints its path), one line each: tag, `file:line`, what was done. It goes
into the next pack and the PR description.

## 5. Re-review, at most 3 rounds

After fixing, run step 2 again, then for each repo:
- `bash $R changed [repo]`. If only docs changed since the snapshot, skip this repo's round and say so.
- `bash $R pack [repo]` (the diff since the last snapshot, with the findings already handled), then
  `bash $R save [repo]` so the next round starts from here.
- Run `/code-review low` with the pack: a re-review only checks the fixes and what they touch.

Stop after round 3 even if findings remain. Tell the user what keeps coming back and whether the
approach should change. When a kind of finding keeps coming back across PRs, propose a rule for
`REVIEW.md`, `CLAUDE.md`, a skill or an analyzer. When a bug got past review, add it to
`docs/review-evals.md`.

## 6. Write the PR description

Fill `.github/pull_request_template.md`:
- **Jira**: the BIL key from the branch name or the plan.
- **What and why**: two to four lines.
- **Testing**: the commands from step 2 and their result lines, which integration-test branch CI
  will use, and the review: rounds run, findings fixed, answered, and pre-existing.
- **Risk**: what could break. Migrations: what they change and whether they can fail on production
  data (give the pre-check query). Also data fixes, a BillerContracts version bump, PDF layout
  changes. "Low: code only, covered by unit tests" is fine when true.
- **How to revert**: code only → "revert the PR". Migration → the rollback, or "restore the backup
  taken before deploying". If a data change can't be rolled back, say so.

Show the text to the user. Commit, push or create the PR only when they ask.

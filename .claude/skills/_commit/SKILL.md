---
name: _commit
description: Turn uncommitted changes in Biller or a sibling repo (BillerContracts, BillerFrontEnd, BillioIntegrationTest) into organised commits — one logical change per commit (tests with the code they cover; migrations, contract bumps, plans, skills and unrelated fixes on their own), whole files or single hunks staged, and messages in the repo's style (`BIL-<n>` subject, bullets saying what now happens, Co-Authored-By trailer). Use this whenever the user asks to commit, check in, save or split their changes, write or fix a commit message, or tidy up what is uncommitted in any Biller repo, even if they only say "commit this".
---

# Organised commits

Commits get read later: in the PR, in `git log` when hunting a bug, and when one has to be
reverted. Each commit is one change that a reader understands from its message alone and that can
be reverted without dragging something unrelated along.

## 1. See what is there

```bash
bash .claude/skills/_commit/scripts/commit-plan.sh                  # this repo
bash .claude/skills/_commit/scripts/commit-plan.sh BillerFrontEnd   # a sibling
```

It changes nothing. It prints the branch and its BIL key, commits not pushed yet, staged,
unstaged and untracked files with their area and size, package version changes, recent subjects to
match, and a `Check:` list. Files under `.claude/` show as "Hidden by .git/info/exclude": this
clone ignores `/.claude/` although the skills in it are tracked, so new skill files need `git add -f`.

Then read the diff itself (`git diff`, `git diff --cached`, new files in full). The message says
what the change does, and `--stat` doesn't tell you that.

Stop and ask before committing when:
- the branch is `master`: commits go on a `BIL-<n>-<slug>` branch. Ask for the key, then create it;
- a rebase or cherry-pick is in progress, or a merge you weren't asked to finish (section 5);
- `Check:` lists anything: build output, local settings, keys, conflict markers, large files, a
  `ProjectReference` to BillerContracts;
- some changes don't belong to this branch's issue, or you can't tell what they are for. The working
  tree may hold the user's own unfinished work: ask whether it goes in, waits, or gets its own
  commit. Don't stash, reset or discard it.

Files already staged were probably staged on purpose. Keep them together unless they plainly belong
in different commits, and say what you did with them.

## 2. Group the changes

One commit is one change: a feature step, a fix, a review round, a version bump.

- **Every commit builds and its tests pass.** Tests go in the commit with the code they cover. A
  package bump the code needs to compile goes with that code. A feature's validator, Swagger
  examples, mapping and DI registration go with the feature.
- **On their own**, in this order when several are present:
  1. a migration that stands alone, before the code that uses it
     (`BIL-65 Migrations 033/034: series table, move customers' series into it`);
  2. a BillerContracts bump (`BIL-65 BillerContracts 4.0.1: invoice responses carry seller and customer ids and names`),
     unless the code needs it to compile;
  3. the code, one commit per behaviour change; an unrelated fix found on the way, and renames or
     formatting with no behaviour change, each get their own so they don't hide the real change;
  4. fixes from a review round, one commit per round (`BIL-65 Review round 2: series update checks the stored next number`);
  5. plan progress and decisions (`BIL-65 Plan: ...`). A plan line that records the code change in
     the same commit may go with the code;
  6. skills, `CLAUDE.md`, `REVIEW.md`, `docs/review-evals.md` (`BIL-65 Skills: ...`).
- Don't split finer than that. Five commits that only build together are worse than one.

Show the plan as a short list (subject, files) before staging anything. If the user asked only for a
plan or a message, stop there.

## 3. Write the messages

```
BIL-65 Customers with invoices keep their series; invoices keep their customer

- PUT /customers returns 400 when it changes the series of a customer with
  invoices. The update locks the customer row, then its current series.
- POST /invoices stops at next_number 2147483647 instead of overflowing
  (400, not 500).

Co-Authored-By: <the line from the session's attribution instructions>
```

- **Subject**: the BIL key from the branch name, then what is true after the commit, in plain
  English. Add an area after the key when it isn't product code: `Plan:`, `Skills:`,
  `Review round N:`, `Migration 036:`, `BillerContracts 4.0.1:`. No trailing period; aim for under
  80 characters. A branch without a key gets no prefix.
- **Body**, after a blank line, only when the subject doesn't say it all: why, or bullets with what
  now happens: the endpoint, the status code, the rule, the edge case that is now handled. Don't list
  the files or classes touched; the diff shows those. Name what a reader would trip over: a data
  change, a contract version, a decision the user made. Wrap at about 90 characters.
- **Trailer**: end with the `Co-Authored-By:` line from the session's attribution instructions.
- Never `WIP`, `fix`, `changes`, `update files`, or a subject that only repeats the branch name.

## 4. Stage and commit

Stage by path, never `git add -A` or `git add .`:

```bash
git add -- src/Application/Services/SeriesService.cs tests/xUnitTests/Application/Services/SeriesServiceTest.cs
git add -f -- .claude/skills/_new-skill/SKILL.md   # hidden by .git/info/exclude
```

When one file holds parts of two commits, stage its hunks (`git add -p` is interactive and can't run here):

```bash
S=.claude/skills/_commit/scripts/stage-hunks.sh
bash $S list  src/WebAPI/Program.cs          # numbered hunks
bash $S stage src/WebAPI/Program.cs 1,3      # stage hunks 1 and 3
bash $S --fine list src/WebAPI/Program.cs    # no context lines: nearby changes become separate hunks
```

Numbers change after every stage, so list again before the next one, and pass `--fine` to both
`list` and `stage` or to neither. If one hunk mixes both changes even with `--fine`, commit it with
the change it mostly belongs to and say so in the body. Don't edit the file to split it.

If the commits contain C# or project files and nothing in this conversation built the current
tree, run `bash .claude/skills/_test-coverage/scripts/build.sh [repo]` once first and paste its last
line. Running the tests is the job of `_test-coverage` and the PR steps (`_pre-pr`).

Before each commit, check that exactly the planned files are staged (`git diff --cached --stat`),
then commit with the message on stdin so the body and trailer keep their line breaks:

```bash
git commit -F - <<'EOF'
BIL-65 ...

- ...

Co-Authored-By: ...
EOF
```

Never `--no-verify`.

## 5. Merge commits

When finishing a merge of `origin/master` into the branch, keep git's subject
(`Merge remote-tracking branch 'origin/master' into BIL-65-series`) and add a body: what the merge
brings in, and how each conflict was resolved: renumbered changesets, which side won, what was
rewritten.

## 6. Report

Paste `git log --stat --format='%h %s' -n <new commits>` and `git status --short`. Name anything left
uncommitted on purpose and why. `git reset HEAD~<n>` undoes the new commits and keeps their
changes in the working tree.

## Limits

- **Push** only when the user asks.
- **History**: never amend, rebase or squash pushed commits. Unpushed ones are reworded or amended
  only when the user asks.
- **Sibling repos**: same rules and the same BIL key. The branch must have Biller's branch name, so
  run `_sync-repos` first. Pass the repo to the scripts (`commit-plan.sh BillerFrontEnd`,
  `stage-hunks.sh --repo BillerFrontEnd ...`) and run git as `git -C ../<repo> ...`. A change
  across repos, such as a contract and the code that uses it, is one commit in each repo.

---
name: _pr-review
description: PR step — review a Biller branch in a fresh context against REVIEW.md, handing the reviewer one pack (scope.md, REVIEW.md, earlier findings, the diff) instead of letting it explore. Round 1 reviews the whole branch at the default level; later rounds review only what changed since the last snapshot, at low effort, and are skipped when only docs changed. Every finding goes into findings.md for _pr-fix. Use it as step 4 of _pre-pr, or alone when the user asks to review their branch or changes before a PR.
---

# Review in a fresh context

The reviewer must not share this conversation's assumptions. Every review starts cold and is the
most expensive step, so hand it one file with what it needs instead of letting it explore.

```bash
R=.claude/skills/_pr-review/scripts/review-round.sh
```

**Round 1:**

```bash
bash $R save [repo]          # snapshot of what this round reviews
bash $R pack [repo] --full   # prints the pack's path: scope.md, REVIEW.md, findings so far, the branch's diff
```

**Round 2 and 3**, after `_pr-fix`:

```bash
bash $R changed [repo]       # exit 1: only docs or nothing changed, so skip this round and say so
bash $R pack [repo]          # the diff since the last snapshot, with the findings already handled
bash $R save [repo]          # the next round starts from here
```

`save` lists files that aren't committed yet. If one isn't part of the PR, leave it out with
`bash .claude/skills/_pre-pr/scripts/pr-state.sh exclude <path>` and save again. The pack takes the
plan from `scope.md`; pass `--plan <file>` when there is no `scope.md`.

Then run `/code-review` (the `code-review` skill): the default level in round 1, `low` in later
rounds. Tell it to read the pack first, to follow the `REVIEW.md` in it, and to open other files
only to check a finding. For a sibling repo, name the repo folder as the target. Reviews of
different repos can run at the same time. If that skill isn't available, start a general-purpose
subagent with the same prompt.

## Record the findings

Add every finding to `findings.md` (`bash $R findings [repo]` prints its path), one line each,
with the reviewer's tag (`Important`, `Nit` or `Pre-existing`, see `REVIEW.md`) and the round:

```
- [ ] Important src/Infrastructure/Repository/SeriesRepository.cs:88 next_number overflows at int.MaxValue (round 2)
- [ ] Nit src/Validators/SeriesValidator.cs:14 message doesn't name the field (round 2)
```

Don't drop, merge or soften findings here; `_pr-fix` decides what happens to each. Paste the
reviewer's result and the lines you added. No findings: say so, and `_pr-fix` has nothing to do.

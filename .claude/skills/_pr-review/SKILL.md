---
name: _pr-review
description: PR step — review a Biller branch in a fresh context against REVIEW.md, handing the reviewer one pack (scope.md, REVIEW.md, earlier findings, the diff) instead of letting it explore. Round 1 reviews the whole branch; later rounds review only what changed since the last snapshot, and run only when the round before had an Important finding that got fixed, or code changed for another reason. Every round runs at medium effort. Every finding goes into findings.md for _pr-fix. Use it as step 4 of _pre-pr, or alone when the user asks to review their branch or changes before a PR.
---

# Review in a fresh context

The reviewer must not share this conversation's assumptions. Every review starts cold and is the
most expensive step, so hand it one file with what it needs instead of letting it explore.

```bash
R=.claude/skills/_pr-review/scripts/review-round.sh
```

**Round 1:**

```bash
bash $R pack [repo] --full   # prints the pack's path: scope.md, REVIEW.md, findings so far, the branch's diff
bash $R save [repo]          # snapshot of what the pack shows: the next round starts from here
```

**Round 2 and 3**, after `_pr-fix`, only when the round before had an **Important** finding that
got fixed: the fix needs a look. Nits, answers and pre-existing findings don't start a round.
Otherwise the review ends: say so and go on to the description. If code changed later for another
reason, review that change as a new round.

```bash
bash $R changed [repo]       # exit 0: code changed, review it; 1: only docs or nothing, skip and say so;
                             # 3: the last snapshot can't be read, review the whole branch (pack --full)
bash $R pack [repo]          # the diff since the last snapshot, with every finding listed so far
bash $R save [repo]          # snapshot of what the pack shows
```

Run `save` right after `pack`: it records the tree the pack showed, so an edit made while the
review runs isn't counted as reviewed. `save` lists files that aren't committed yet, new or
changed. If one isn't part of the PR, leave it out with
`bash .claude/skills/_pre-pr/scripts/pr-state.sh exclude <path>`, then pack and save again. The
pack takes the plan from `scope.md`; pass `--plan <file>` when there is no `scope.md`. Skill
files, `CLAUDE.md` and `REVIEW.md` count as code: a fix that only changes them is still reviewed.

Then run `/code-review medium` (the `code-review` skill) in every round. Higher levels are told to
surface more findings, which pads later rounds with Nits against REVIEW.md's "at most 2 Nits".
Tell it to read the pack first, to follow the `REVIEW.md` in it, and to open other files only to
check a finding. For
a sibling repo, name the repo folder as the target. Reviews of different repos can run at the
same time. If that skill isn't available, start a general-purpose subagent with the same prompt
and the same effort.

## Record the findings

Add every finding to `findings.md` (`save` creates it; `bash $R findings [repo]` prints its path), one line each,
with its number, the reviewer's tag (`Important`, `Nit` or `Pre-existing`, see `REVIEW.md`) and the round:

```
- [ ] F4 Important src/Infrastructure/Repository/SeriesRepository.cs:88 next_number overflows at int.MaxValue (round 2)
- [ ] F5 Nit src/Validators/SeriesValidator.cs:14 message doesn't name the field (round 2)
```

Numbers go on from the file's last one, across rounds (`echo $(( $(grep -c '^- \[' "$F") + 1 ))`
gives the next), and never change, so the user can say "fix F4, answer F5". Use `F<n>`, not `#<n>`:
on GitHub `#4` links to issue 4.

Don't drop, merge or soften findings here; `_pr-fix` decides what happens to each. Paste the
reviewer's result and the lines you added, and name findings by number when you report them. No
findings: say so, and `_pr-fix` has nothing to do.

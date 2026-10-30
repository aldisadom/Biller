---
name: _pr-fix
description: PR step — work through every open finding in a Biller branch's findings.md (fix important bugs with a test that fails without the fix, fix or answer nits, list pre-existing problems for Jira, answer findings that are wrong with the reason) and record each outcome, so the next review round doesn't report it again and the PR description can count it. Use it as step 5 of _pre-pr, or alone when the user asks to work through or fix review findings.
---

# Handle every finding

```bash
F="$(bash .claude/skills/_pr-review/scripts/review-round.sh findings [repo])"
grep -n '^- \[ \]' "$F"    # the open findings
```

For each open line:

- **Important**: fix it, with a test that fails without the fix (`_write-tests`; its
  `revert-check.sh` shows the test failing with the fix undone). If the same kind of bug is in
  several places, fix all of them in this round.
- **Nit**: fix it, or answer in one line why not.
- **Pre-existing**: leave it out of this PR. List it for the user; it may deserve a Jira task.
- **Wrong**: answer with the reason (`file:line`, the input that shows it's fine). Don't change
  code just to silence it.

Then tick the line, keep its number, and add what happened after `->`:

```
- [x] F4 Important src/Infrastructure/Repository/SeriesRepository.cs:88 next_number overflows at int.MaxValue (round 2) -> fixed: stops at int.MaxValue with 400; SeriesRepositoryTest.Add_AtMaxNumber fails without it
- [x] F5 Nit src/Validators/SeriesValidator.cs:14 message doesn't name the field (round 2) -> not fixed: FluentValidation adds the property name
- [x] F1 Pre-existing src/Application/Services/ItemService.cs:40 no paging limit (round 1) -> pre-existing: listed for the user
```

The next review round reads these lines and doesn't report them again, and the PR description
counts them. When the user names findings by number ("fix F2, leave F3"), do what they say for
those and handle the rest as above. Report each outcome with its number.

After fixing, `tests.md` is stale: the tests run again (`_test-coverage`) before the next review
round. Commit the fixes only when the user wants commits, with `_commit` and a
`BIL-<n> Review round <N>: ...` subject. Otherwise leave them uncommitted; the next snapshot
includes them.

# Review rules for Biller

Failure here looks like:
- An invoice total, line sum, or amount in words is wrong.
- Two invoices get the same number, or a number is skipped without a delete.
- An old invoice or its PDF changes after an item or unit is edited.
- A migration loses data, can't roll back, or edits an already-applied changeset.
- A password, hash, or salt ends up in a response, log, or invoice.
- A request for one user's data returns another user's data.
- A validator or ownership check always passes or always fails.
- A test passes whatever the code does (wrong class under test, missing `Verify`, expected value copied from the output).
- A mapping library (AutoMapper or similar) is added, or a mapping method misses a property.

Skip: `bin/`, `obj/`, `coveragereport/`, `TestResults/`, `*.Designer.cs`, migrations already on master (flag edits to them instead).

Tag each finding **Important**, **Nit**, or **Pre-existing** (the bug was there before this change). At most 2 Nits. No naming or formatting comments.

Every finding cites `file:line` and a concrete input that fails. No guesses from names.

Start with the number of Important findings. If there are none, say "No issues found."

When the same kind of bug appears in several places, report it once and list every place.

When a finding repeats across reviews, propose a rule for `CLAUDE.md`, a skill, or an analyzer instead of repeating it.

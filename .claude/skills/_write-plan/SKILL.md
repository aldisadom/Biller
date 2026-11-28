---
name: _write-plan
model: opus
effort: xhigh
context: fork
description: Plan a Biller Jira issue on Opus, in its own context — read the BIL issue with its links and comments, review it (blockers, gaps, conflicts with CLAUDE.md, REVIEW.md and other open plans), then check its existing plan against the code or write a new one in docs/plans/BIL-<n>-<slug>.md with the open questions at the top, and return the review, the plan's path and the questions. Use it whenever a BIL issue needs a plan, an existing plan needs checking against the code, or a plan needs the user's answers or a changed decision folded in — from _add-api-feature step 1, before any other change across several layers or repos, or alone when the user asks to plan an issue.
---

# Plan a Jira issue

This skill runs on Opus at xhigh effort, in a fork that doesn't see the conversation. Everything it
knows comes from the arguments, the issue and the repos. It writes or updates the plan file and
reports back; the caller talks to the user, edits Jira and creates the branch.

**Arguments:** the issue key or link (`BIL-<n>`, `https://aldisadom.atlassian.net/browse/BIL-<n>`),
plus everything the user has said about it: details, decisions, answers to earlier open questions,
a decision the code made wrong. Without a key, stop and say so: the caller creates the issue first
(`_add-api-feature`, step 0).

## 1. Read the issue

With the Atlassian MCP tools: `getAccessibleAtlassianResources` once for the cloudId, then
`getJiraIssue` with `view: "evidence"` for links and parent, and `listJiraIssueComments`. Note the
summary, description, status, parent epic and every linked issue, with each one's status.

## 2. Review it

- **Blockers:** each "is blocked by" issue and its status.
- **Gaps:** what the issue doesn't say that the code needs (field types, required fields,
  relationships, status codes, error messages, which documents or languages are affected).
- **Conflicts:** anything that breaks `CLAUDE.md` or `REVIEW.md` (invoices keep their own number,
  dates and lines, one SQL statement per write, no mapping library), or that clashes with another
  open issue or plan touching the same tables or requests (`docs/plans/`).

A blocker that isn't Done, unless the arguments say the user chose to go on: stop here, write no
plan, and return only the review (step 5).

## 3. Find the plan

In order: a `Plan:` line in the issue description, then `docs/plans/BIL-<n>-*.md` in Biller.

Sibling repos, on every run: check all three (BillerContracts, BillerFrontEnd,
BillioIntegrationTest) with `bash .claude/skills/_sync-repos/scripts/sync-repos.sh BillerContracts
BillerFrontEnd BillioIntegrationTest` (check mode only: switching may need the user, and the caller
runs `_sync-repos` for that), and decide for each whether the plan **needs** it: it already has
steps there, or the issue needs them (BillerContracts for a new field or request,
BillioIntegrationTest for an endpoint or rule, BillerFrontEnd for a page). Read only the repos the
check reports `ok`. The rules for a needed repo, whichever of 4a or 4b runs:

- `ok`: its steps are checked against the code like the rest. Steps marked "not checked against
  the code" are checked now and lose the marker; missing steps are written now.
- Not `ok`: don't read it. When the arguments say to leave it out, the plan has its steps anyway,
  marked "not checked against the code": existing ones are kept, missing ones are written from the
  issue and the repos that were read, and "Current state" says that repo wasn't read. Otherwise
  write no steps for it yet; the result asks the user (step 5, item 6).

## 4a. There is a plan: check it

- Read all of it. Compare its "Current state" with the code now. It names the master commit it was
  written at; `git log <commit>..master` shows what changed since. List the differences.
- List any design decision the code now makes wrong, or any step it makes impossible. Don't
  re-decide the plan: change a decision only when the arguments say the user agreed to the change.
- Migration numbers in a plan are guesses; the next free one is taken when the migration is written
  (`_add-db-migration`).
- Apply step 3's rules for needed siblings: steps added or no longer marked "not checked" are
  differences too.

## 4b. There is no plan: write one

- Path: `docs/plans/BIL-<n>-<slug>.md` in Biller when the work changes Biller or several repos;
  otherwise in the repo the work is in (`CLAUDE.md`, "More").
- Mirror an existing plan, e.g. `docs/plans/BIL-90-seller-invoice-summary.md`: title with the key,
  `Jira:` link and epic, **Goal**, **Current state (master @ <short hash>)** as a table of the
  files, tables and endpoints involved, **Design decisions** (numbered, each with its reason),
  **Dependencies**, one **Step** per layer in the order `_add-api-feature` uses (contracts,
  migration with its SQL and rollback, domain and infrastructure with the SQL, application,
  validators, WebAPI, tests, integration tests, front end), **Out of scope**, **Order of work**.
  Work that isn't an API feature (docs, tooling, CI) gets one step per repo or area instead.
- Fill "Current state" from the code, not from the issue.
- Put **Open questions** at the top. Don't guess an answer into a decision.

## 4c. Either way: use what the user decided

The answers and decisions in the arguments go into the plan, new or existing: each replaces the
matching open question, and a decision gets its reason. Don't ask again what the arguments answer.

## 4d. Make it simpler

Before returning, ask of every decision and step: can this be made simpler (`CLAUDE.md`, "Keep it
simple")? Reuse an existing type, query or endpoint before adding one. Merge steps and drop
options, checks and edge cases the issue doesn't need. A case that needs an unusual setup or
concurrent requests goes under **Out of scope** in one line, not into a step. Do this for an
existing plan too, but change its decisions only as step 4a allows: list the simplification as an
open question instead.

Write the file in the working tree. Don't commit, don't create a branch, don't edit Jira and don't
write code: the caller does those once the user has approved the plan.

## 5. Return

Reply with, in this order:

1. **Plan:** its path, and `new`, `updated` or `unchanged`; or "none: stopped at a blocker".
2. **Blockers:** each one with its status, or "none".
3. **Gaps** and **conflicts** from step 2.
4. For an existing plan: the differences from step 4a, and every decision the code makes wrong.
5. **Open questions** the user has to answer, numbered as in the plan.
6. **Repos not read:** each sibling the check didn't report `ok`, with its line, whether the plan
   needs it (step 3), and whether the arguments said to leave it out.
7. **Simpler:** what step 4d dropped, merged or moved out of scope, or proposes as a question; or
   "nothing found".

The caller tells the user, gathers the answers, and runs this skill again with everything the user
has decided so far, not only the new answers: this skill keeps nothing between runs.

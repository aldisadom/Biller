---
name: _open-pr
description: Last PR step, only when the user asks — push a Biller branch and create its GitHub pull request with the description saved by _pr-description, or update an open PR's description, after checking that the PR steps' results are there and not stale. Use it whenever the user asks to push, open, create or update the PR in any Biller repo; if the steps haven't run or the code changed since, run _pre-pr first.
---

# Push and open the PR

```bash
P=.claude/skills/_pre-pr/scripts/pr-state.sh
bash $P status [repo]
```

Every step must be `ok` (or `not needed`). When one is missing or `STALE`, run `_pre-pr` from that
step, show the new description, and continue only once the user has seen it.

1. **Commit what belongs.** A PR only has commits. When `git status` shows changes that are part
   of the PR (see `scope.md`), commit them with `_commit`. Leave excluded paths alone. Committing
   doesn't change the tree, so the results stay `ok`.
2. **Push**: `git push -u origin <branch>`. Never force-push unless the user asks.
3. **Create the PR**, with the body from `description.md` (`read` leaves out the stamp line):

   ```bash
   bash $P read [repo] description > "$(bash $P dir [repo])/body.md"
   gh pr create --base master --head <branch> --title "BIL-<n> <what it does>" --body-file "$(bash $P dir [repo])/body.md"
   ```

   Run it from the repo's folder. The title is the Jira key and one line saying what the change
   does, like the merged PRs in `git log origin/master`.
4. **An open PR already exists** (`gh pr view <branch> --json number,url`): update its body with
   `gh api -X PATCH repos/aldisadom/<repo>/pulls/<n> -F body=@<body.md>`. `gh pr edit` fails with
   this machine's gh 2.46.
5. **Several repos**: one PR per repo. Open BillerContracts first, because Biller can't merge
   before the package it needs is published. Then add links to the other PRs in each description
   and update the bodies with `gh api`.

Paste each PR's URL and record it: `bash $P note [repo] description "PR: <url>"`.

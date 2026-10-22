#!/usr/bin/env bash
# Delete local branches in Biller and its sibling repos, keeping master, the branch each repo has
# checked out, and the branch with Biller's current branch name (so CI pairing keeps working).
# Remote branches are never touched: no push, no fetch, only `git branch -D` on local refs.
#
# Usage: cleanup-branches.sh [--delete] [repo ...]
#   (default)  list what would be deleted, change nothing
#   --delete   force-delete those branches, unmerged and unpushed ones included
#   repo       folder names (default: Biller BillerContracts BillerFrontEnd BillioIntegrationTest)
# Every deleted branch is printed with its last commit, so it can be restored with
# `git -C ../<repo> branch <name> <sha>` while that commit is still in the reflog.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"

DELETE=0
REPOS=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --delete) DELETE=1; shift ;;
    -*) echo "Unknown argument: $1" >&2; exit 2 ;;
    *) REPOS+=("$1"); shift ;;
  esac
done
[[ ${#REPOS[@]} -eq 0 ]] && REPOS=(Biller BillerContracts BillerFrontEnd BillioIntegrationTest)
BILLER_BRANCH="$(git -C "$REPO_ROOT" symbolic-ref --short -q HEAD || echo master)"

[[ $DELETE -eq 1 ]] && echo "Deleting local branches." || echo "Dry run. Pass --delete to delete."
echo "Keeping in every repo: master, the checked-out branch, '$BILLER_BRANCH'."

FAILED=0
for repo in "${REPOS[@]}"; do
  dir="$PARENT/$repo"
  if [[ ! -d "$dir/.git" ]]; then
    echo "$repo: MISSING"; FAILED=1; continue
  fi
  current="$(git -C "$dir" symbolic-ref --short -q HEAD || echo '')"
  echo "$repo: on '${current:-(detached HEAD)}'"

  count=0
  while IFS='|' read -r name sha upstream track; do
    [[ "$name" == master || "$name" == "$current" || "$name" == "$BILLER_BRANCH" ]] && continue
    if [[ -z "$upstream" ]]; then state="never pushed"
    elif [[ "$track" == *gone* ]]; then state="remote gone"
    else state="on origin${track:+ $track}"; fi
    count=$((count + 1))
    if [[ $DELETE -eq 0 ]]; then
      echo "  would delete $name ($sha, $state)"
    elif out="$(git -C "$dir" branch -D "$name" 2>&1)"; then
      echo "  deleted $name ($sha, $state)"
    else
      echo "  FAILED $name: $out"; FAILED=1
    fi
  done < <(git -C "$dir" for-each-ref refs/heads \
             --format='%(refname:short)|%(objectname:short)|%(upstream:short)|%(upstream:track)')
  [[ $count -eq 0 ]] && echo "  nothing to delete"
done
exit $FAILED

#!/usr/bin/env bash
# Delete local branches in Biller and its sibling repos, keeping master, the branch each repo has
# checked out, and the branch with Biller's current branch name (so CI pairing keeps working).
# Remote branches are never touched: no push, no fetch, only `git branch -D` on local refs.
# Worktrees go with their branch: a record whose folder is gone is pruned, and a worktree that is
# still there is removed (`git worktree remove`, never --force) unless it has uncommitted changes,
# new files included; then the branch and its worktree are both kept.
#
# Usage: cleanup-branches.sh [--delete] [repo ...]
#   (default)  list what would be deleted, change nothing
#   --delete   force-delete those branches, unmerged and unpushed ones included, and their worktrees
#   repo       folder names (default: Biller BillerContracts BillerFrontEnd BillioIntegrationTest)
# Every deleted branch is printed with its last commit, so it can be restored with
# `git -C ../<repo> branch <name> <sha>` while that commit is still in the reflog. A removed
# worktree's folder is gone for good, but it held nothing that wasn't committed.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
# The siblings sit next to the main checkout, also when this runs from a worktree inside it
PARENT="$(common="$(git -C "$REPO_ROOT" rev-parse --path-format=absolute --git-common-dir 2>/dev/null)" && dirname "$(dirname "$common")" || dirname "$REPO_ROOT")"

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

  # Worktree records whose folder is gone. Git won't delete a branch such a record still holds.
  # From WSL, a worktree made by Windows git has a C:\ path that looks gone here, so its record is
  # left alone: prune those from Windows.
  pruning=1
  if [[ -n "${WSL_DISTRO_NAME:-}" ]] && grep -qsE '^[A-Za-z]:[\\/]' "$dir"/.git/worktrees/*/gitdir; then
    echo "  not pruning worktree records: some were made by Windows git; run 'git worktree prune' from Windows"
    pruning=0
  else
    if [[ $DELETE -eq 1 ]]; then pruned="$(git -C "$dir" worktree prune -v 2>&1)"; verb="pruned"
    else pruned="$(git -C "$dir" worktree prune -n -v 2>&1)"; verb="would prune"; fi
    while IFS= read -r line; do
      [[ -n "$line" ]] && echo "  $verb ${line#Removing }"
    done <<< "$pruned"
  fi

  # Which branch each worktree has checked out (the main checkout's is $current, kept anyway)
  unset worktree_of; declare -A worktree_of=()
  wt=""
  while IFS= read -r line; do
    case "$line" in
      "worktree "*) wt="${line#worktree }" ;;
      "branch refs/heads/"*) worktree_of["${line#branch refs/heads/}"]="$wt" ;;
    esac
  done < <(git -C "$dir" worktree list --porcelain)

  count=0
  while IFS='|' read -r name sha upstream track; do
    [[ "$name" == master || "$name" == "$current" || "$name" == "$BILLER_BRANCH" ]] && continue
    if [[ -z "$upstream" ]]; then state="never pushed"
    elif [[ "$track" == *gone* ]]; then state="remote gone"
    else state="on origin${track:+ $track}"; fi
    count=$((count + 1))
    wt="${worktree_of[$name]:-}"
    if [[ -n "$wt" && ! -d "$wt" && $pruning -eq 0 ]]; then
      echo "  KEPT $name ($sha, $state): its worktree record $wt isn't pruned (see above)"; continue
    fi
    [[ -n "$wt" && ! -d "$wt" ]] && wt=""   # folder gone: its record is pruned above
    if [[ -n "$wt" ]] && [[ -n "$(git -C "$wt" status --porcelain 2>&1)" ]]; then
      echo "  KEPT $name ($sha, $state): worktree $wt has uncommitted changes, or git can't read it"
    elif [[ $DELETE -eq 0 ]]; then
      echo "  would delete $name ($sha, $state)${wt:+, and its worktree $wt}"
    elif [[ -n "$wt" ]] && ! out="$(git -C "$dir" worktree remove "$wt" 2>&1)"; then
      echo "  FAILED $name: $out"; FAILED=1
    elif out="$(git -C "$dir" branch -D "$name" 2>&1)"; then
      echo "  deleted $name ($sha, $state)${wt:+, and its worktree $wt}"
    else
      echo "  FAILED $name: $out"; FAILED=1
    fi
  done < <(git -C "$dir" for-each-ref refs/heads \
             --format='%(refname:short)|%(objectname:short)|%(upstream:short)|%(upstream:track)')
  [[ $count -eq 0 ]] && echo "  nothing to delete"
done
exit $FAILED

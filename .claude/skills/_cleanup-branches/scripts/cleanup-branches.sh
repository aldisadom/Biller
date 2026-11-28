#!/usr/bin/env bash
# Delete local branches in Biller and its sibling repos, keeping master, the branch each repo has
# checked out, and the branch with Biller's current branch name (so CI pairing keeps working).
# Remote branches are never touched: no push, no fetch, only `git branch -D` on local refs.
# Worktrees go with their branch: records whose folder is gone are pruned, and a worktree is
# removed (`git worktree remove`, never --force) unless it has uncommitted changes, new files or
# ignored files other than build output; then the branch and its worktree are both KEPT.
#
# Usage: cleanup-branches.sh [--delete] [repo ...]
#   (default)  list what would be deleted, change nothing
#   --delete   force-delete those branches, unmerged and unpushed ones included, and their worktrees
#   repo       folder names (default: Biller BillerContracts BillerFrontEnd BillioIntegrationTest)
# Every deleted branch is printed with its last commit, so it can be restored with
# `git -C ../<repo> branch <name> <sha>` while that commit is still in the reflog.
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

# Ignored files a removed worktree may lose: build output that a build makes again
BUILD_OUTPUT='^!! (.*/)?(bin|obj|TestResults|coveragereport|\.vs|node_modules)/$'

FAILED=0
for repo in "${REPOS[@]}"; do
  dir="$PARENT/$repo"
  if [[ ! -d "$dir/.git" ]]; then
    echo "$repo: MISSING"; FAILED=1; continue
  fi
  current="$(git -C "$dir" symbolic-ref --short -q HEAD || echo '')"
  echo "$repo: on '${current:-(detached HEAD)}'"

  # Git won't delete a branch that a worktree record holds, even when the folder is gone
  [[ $DELETE -eq 1 ]] && git -C "$dir" worktree prune

  # Which branch each worktree has checked out (the main checkout's is $current, kept anyway)
  unset worktree_of; declare -A worktree_of=()
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
    [[ -n "$wt" && ! -d "$wt" ]] && wt=""   # folder gone: its record is pruned
    unsaved=""
    [[ -n "$wt" ]] && unsaved="$(git -C "$wt" status --porcelain --ignored 2>&1 | grep -vE "$BUILD_OUTPUT" | head -3 | tr '\n' ' ')"
    if [[ -n "$unsaved" ]]; then
      echo "  KEPT $name ($sha, $state): worktree $wt has uncommitted, new or ignored files: ${unsaved% }"
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

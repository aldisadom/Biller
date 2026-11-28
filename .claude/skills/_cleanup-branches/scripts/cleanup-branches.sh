#!/usr/bin/env bash
# Delete local branches in Biller and its sibling repos, keeping master, the branch each repo has
# checked out, and the branch with Biller's current branch name (so CI pairing keeps working).
# Remote branches are never touched: no push, no fetch, only `git branch -D` on local refs.
# Worktrees go with their branch. A worktree that is there is removed (`git worktree remove`, never
# --force) only when it is clean: no uncommitted changes, no new files, no ignored files other than
# build output, no worktree inside it, not locked, not made by Windows git (seen from WSL). A
# record whose folder is gone goes, unless the folder's parent can't be found either. Otherwise the
# branch and its worktree are both KEPT.
#
# Usage: cleanup-branches.sh [--delete] [repo ...]
#   (default)  list what would be deleted, change nothing
#   --delete   force-delete those branches, unmerged and unpushed ones included, and their worktrees
#   repo       folder names (default: Biller BillerContracts BillerFrontEnd BillioIntegrationTest)
# Every deleted branch is printed with its last commit, so it can be restored with
# `git -C ../<repo> branch <name> <sha>` while that commit is still in the reflog. A removed
# worktree's folder is gone for good, but it held nothing that wasn't committed except build output.
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

# Ignored files a worktree may lose: build output and IDE caches that a build makes again
BUILD_OUTPUT='^!! (.*/)?(bin|obj|TestResults|coveragereport|\.vs|node_modules)/$'

# Decides what happens to the worktree of branch $1, before anything is changed, so the dry run
# says what --delete will do. Sets keep (why the branch and its worktree stay; empty when they can
# go) and wt_action: remove (a clean worktree), record (its folder is gone: only the record goes)
# or empty (no worktree).
check_worktree() {
  local path="${wt_path[$1]:-}" flag="${wt_flag[$1]:-}" other ignored
  keep=""; wt_action=""
  [[ -z "$path" ]] && return
  if [[ "$flag" == locked ]]; then
    keep="worktree $path is locked (git worktree unlock)"; return
  fi
  if [[ ! -d "$path" ]]; then
    if [[ ! -d "$(dirname "$path")" ]]; then
      keep="worktree $path and its parent folder can't be found (a drive not mounted?)"; return
    fi
    wt_action="record"; return
  fi
  if [[ "$flag" == windows ]]; then
    keep="worktree $path was made by Windows git; remove it from Windows"; return
  fi
  for other in "${all_paths[@]}"; do
    if [[ "$other" == "$path/"* ]]; then keep="worktree $other is inside worktree $path"; return; fi
  done
  if [[ -n "$(git -C "$path" status --porcelain 2>&1)" ]]; then
    keep="worktree $path has uncommitted changes or new files, or git can't read it"; return
  fi
  ignored="$(git -C "$path" status --porcelain --ignored 2>/dev/null | grep -E '^!! ' | grep -vE "$BUILD_OUTPUT" \
             | head -3 | sed 's/^!! //' | tr '\n' ' ')"
  if [[ -n "$ignored" ]]; then
    keep="worktree $path has ignored files that aren't build output: ${ignored% }"; return
  fi
  wt_action="remove"
}

FAILED=0
for repo in "${REPOS[@]}"; do
  dir="$PARENT/$repo"
  if [[ ! -d "$dir/.git" ]]; then
    echo "$repo: MISSING"; FAILED=1; continue
  fi
  current="$(git -C "$dir" symbolic-ref --short -q HEAD || echo '')"
  echo "$repo: on '${current:-(detached HEAD)}'"

  # Every worktree record, read from .git/worktrees/<id>/ instead of `git worktree prune`: prune
  # drops any record whose folder it can't reach (an unmounted drive, a C:\ path seen from WSL, a
  # /mnt/c path seen from Windows), and the branch would then be deleted unchecked.
  unset wt_path wt_admin wt_flag; declare -A wt_path=() wt_admin=() wt_flag=()
  all_paths=()
  for admin in "$dir"/.git/worktrees/*/; do
    [[ -f "$admin/gitdir" && -f "$admin/HEAD" ]] || continue
    gitdir="$(tr -d '\r' < "$admin/gitdir")"
    flag=""
    if [[ "$gitdir" =~ ^([A-Za-z]):[\\/](.*)$ && ! -e "$gitdir" ]]; then
      # Made by Windows git and read from WSL: C:\x is /mnt/c/x
      rest="${BASH_REMATCH[2]//\\//}"; gitdir="/mnt/${BASH_REMATCH[1],,}/$rest"; flag="windows"
    elif [[ "$gitdir" != /* && ! "$gitdir" =~ ^[A-Za-z]: ]]; then
      gitdir="$admin$gitdir"   # a relative record (worktree.useRelativePaths)
    fi
    path="${gitdir%/.git}"
    all_paths+=("$path")
    head="$(tr -d '\r' < "$admin/HEAD")"
    [[ "$head" == "ref: refs/heads/"* ]] || continue   # detached HEAD: no branch to delete
    branch="${head#ref: refs/heads/}"
    [[ -e "$admin/locked" ]] && flag="locked"
    wt_path[$branch]="$path"; wt_admin[$branch]="$admin"; wt_flag[$branch]="$flag"
  done

  count=0
  while IFS='|' read -r name sha upstream track; do
    [[ "$name" == master || "$name" == "$current" || "$name" == "$BILLER_BRANCH" ]] && continue
    if [[ -z "$upstream" ]]; then state="never pushed"
    elif [[ "$track" == *gone* ]]; then state="remote gone"
    else state="on origin${track:+ $track}"; fi
    count=$((count + 1))
    check_worktree "$name"
    path="${wt_path[$name]:-}"
    case "$wt_action" in
      remove) also=", and its worktree $path" ;;
      record) also=", and the record of its worktree $path (folder already gone)" ;;
      *) also="" ;;
    esac
    if [[ -n "$keep" ]]; then
      echo "  KEPT $name ($sha, $state): $keep"
    elif [[ $DELETE -eq 0 ]]; then
      echo "  would delete $name ($sha, $state)$also"
    elif [[ "$wt_action" == remove ]] && ! out="$(git -C "$dir" worktree remove "$path" 2>&1)"; then
      echo "  FAILED $name: $out"; FAILED=1
    elif [[ "$wt_action" == record ]] && ! out="$(rm -rf -- "${wt_admin[$name]}" 2>&1)"; then
      echo "  FAILED $name: can't remove the worktree record ${wt_admin[$name]}: $out"; FAILED=1
    elif out="$(git -C "$dir" branch -D "$name" 2>&1)"; then
      echo "  deleted $name ($sha, $state)$also"
    else
      echo "  FAILED $name: $out"; FAILED=1
    fi
  done < <(git -C "$dir" for-each-ref refs/heads \
             --format='%(refname:short)|%(objectname:short)|%(upstream:short)|%(upstream:track)')
  [[ $count -eq 0 ]] && echo "  nothing to delete"
done
exit $FAILED

#!/usr/bin/env bash
# Delete local branches in Biller and its sibling repos, keeping master, the branch each repo has
# checked out, and the branch with Biller's current branch name (so CI pairing keeps working).
# Remote branches are never touched: no push, no fetch, only `git branch -D` on local refs.
# Worktrees go with their branch. A worktree that is there is removed (`git worktree remove`, never
# --force) only when it is clean: no uncommitted changes, no new files, no ignored files other than
# build output, no skip-worktree or assume-unchanged files, no submodules, no worktree inside it,
# not locked, not made by Windows git (seen from WSL). A record whose folder is gone goes, unless
# the folder's parent is missing, empty or a mount point. A branch being rebased or bisected, or
# held by two records, stays. Otherwise the branch and its worktree are both KEPT.
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

# A branch being rebased or bisected has a detached HEAD, so neither git nor the records show it as
# checked out. $1: a git dir (the main .git or .git/worktrees/<id>/), $2: its folder, for the message.
mark_busy() {
  local f b
  for f in "$1/rebase-merge/head-name" "$1/rebase-apply/head-name"; do
    if [[ -f "$f" ]]; then b="$(tr -d '\r' < "$f")"; busy[${b#refs/heads/}]="it is being rebased in $2"; fi
  done
  if [[ -f "$1/BISECT_START" ]]; then b="$(tr -d '\r' < "$1/BISECT_START")"; busy[$b]="it is being bisected in $2"; fi
}

# Decides what happens to the worktree of branch $1, before anything is changed, so the dry run
# says what --delete will do. Sets keep (why the branch and its worktree stay; empty when they can
# go) and wt_action: remove (a clean worktree), record (its folder is gone: only the record goes)
# or empty (no worktree).
check_worktree() {
  local path="${wt_path[$1]:-}" flag="${wt_flag[$1]:-}" admin="${wt_admin[$1]:-}" parent other ignored
  keep=""; wt_action=""
  if [[ -n "${busy[$1]:-}" ]]; then keep="${busy[$1]}"; return; fi
  [[ -z "$path" ]] && return
  case "$flag" in
    locked) keep="worktree $path is locked (git worktree unlock)"; return ;;
    duplicate) keep="more than one worktree record has it checked out"; return ;;
  esac
  if [[ ! -d "$path" ]]; then
    # Gone only when its parent is there and isn't an empty folder or a mount point: an unmounted
    # drive often leaves its mount point behind as an empty folder.
    parent="$(dirname "$path")"
    if [[ ! -d "$parent" || -z "$(ls -A "$parent" 2>/dev/null)" ]] \
       || [[ "$(stat -c %d "$parent" 2>/dev/null)" != "$(stat -c %d "$parent/.." 2>/dev/null)" ]]; then
      keep="worktree $path can't be found, and neither can its parent folder, or it is empty or a mount point (a drive not mounted?). If the folder was deleted on purpose, remove the record $admin by hand"
      return
    fi
    wt_action="record"; return
  fi
  if [[ "$flag" == windows ]]; then
    keep="worktree $path was made by Windows git; remove it from Windows"; return
  fi
  for other in "${all_paths[@]}"; do
    if [[ "$other" == "$path/"* ]]; then keep="worktree $other is inside worktree $path"; return; fi
  done
  if [[ -e "$path/.gitmodules" ]]; then
    keep="worktree $path has submodules, which git worktree remove refuses"; return
  fi
  # -u and --ignored are given so that status.showUntrackedFiles=no in config can't hide new files
  if [[ -n "$(git -C "$path" status --porcelain -unormal 2>&1)" ]]; then
    keep="worktree $path has uncommitted changes or new files, or git can't read it"; return
  fi
  # Edits to skip-worktree (S) and assume-unchanged (lowercase) files don't show in status
  if git -C "$path" ls-files -v 2>/dev/null | grep -qE '^(S|[a-z]) '; then
    keep="worktree $path has skip-worktree or assume-unchanged files, whose edits status doesn't show"; return
  fi
  ignored="$(git -C "$path" status --porcelain -unormal --ignored=traditional 2>/dev/null | grep -E '^!! ' \
             | grep -vE "$BUILD_OUTPUT" | head -3 | sed 's/^!! //' | tr '\n' ' ')"
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
  unset wt_path wt_admin wt_flag busy; declare -A wt_path=() wt_admin=() wt_flag=() busy=()
  all_paths=()
  mark_busy "$dir/.git" "$dir"
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
    mark_busy "$admin" "$path"
    head="$(tr -d '\r' < "$admin/HEAD")"
    [[ "$head" == "ref: refs/heads/"* ]] || continue   # detached HEAD: no branch to delete
    branch="${head#ref: refs/heads/}"
    [[ -e "$admin/locked" ]] && flag="locked"
    [[ -n "${wt_path[$branch]:-}" ]] && flag="duplicate"
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
      echo "  FAILED $name: $out${also:+ (already removed$also)}"; FAILED=1
    fi
  done < <(git -C "$dir" for-each-ref refs/heads \
             --format='%(refname:short)|%(objectname:short)|%(upstream:short)|%(upstream:track)')
  [[ $count -eq 0 ]] && echo "  nothing to delete"
done
exit $FAILED

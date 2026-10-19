#!/usr/bin/env bash
# Check that Biller's sibling repos are on the branch with the same name as Biller's current
# branch, or on up-to-date master when they have no such branch.
#
# Usage: sync-repos.sh [--sync] [--branch <name>] [repo ...]
#   (default)  report only, change nothing
#   --sync     switch repos without uncommitted changes to their target branch and fast-forward them
#   --branch   use this branch name instead of Biller's current branch
#   repo       sibling folder names (default: BillerContracts BillerFrontEnd BillioIntegrationTest)
# Exit code: 0 when every repo is on its target branch and not behind origin, 1 otherwise.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"
export GIT_TERMINAL_PROMPT=0

SYNC=0
BRANCH=""
REPOS=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --sync) SYNC=1; shift ;;
    --branch) BRANCH="$2"; shift 2 ;;
    -*) echo "Unknown argument: $1" >&2; exit 2 ;;
    *) REPOS+=("$1"); shift ;;
  esac
done
[[ ${#REPOS[@]} -eq 0 ]] && REPOS=(BillerContracts BillerFrontEnd BillioIntegrationTest)
BILLER_BRANCH="$(git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD)"
[[ -z "$BRANCH" ]] && BRANCH="$BILLER_BRANCH"

# Prints "<behind> <ahead>" of HEAD against its upstream, or nothing when there is no upstream.
behind_ahead() { git -C "$1" rev-list --left-right --count '@{upstream}...HEAD' 2>/dev/null; }

git -C "$REPO_ROOT" fetch --quiet --prune origin 2>/dev/null || echo "WARNING: fetch failed for Biller"
read -r b_behind b_ahead <<< "$(behind_ahead "$REPO_ROOT")"
echo "Biller is on '$BILLER_BRANCH'${b_behind:+ (behind origin: $b_behind, ahead: $b_ahead)}."
[[ "$BRANCH" == master ]] && echo "Target: master." || echo "Target: '$BRANCH' where it exists, else master."
[[ "${b_behind:-0}" -gt 0 ]] && echo "WARNING: Biller is behind origin. Pull it before comparing."

FAILED=0
for repo in "${REPOS[@]}"; do
  dir="$PARENT/$repo"
  if [[ ! -d "$dir/.git" ]]; then
    echo "$repo: MISSING. Clone it: git clone https://github.com/aldisadom/$repo ../$repo"
    FAILED=1; continue
  fi
  git -C "$dir" fetch --quiet --prune origin 2>/dev/null || echo "$repo: WARNING fetch failed, using the last fetched state"

  target=master
  if [[ "$BRANCH" != master ]] && { git -C "$dir" show-ref --verify --quiet "refs/heads/$BRANCH" \
      || git -C "$dir" show-ref --verify --quiet "refs/remotes/origin/$BRANCH"; }; then
    target="$BRANCH"
  fi
  current="$(git -C "$dir" symbolic-ref --short -q HEAD || echo '(detached HEAD)')"
  dirty="$(git -C "$dir" status --porcelain --untracked-files=no)"
  note=""

  if [[ "$current" != "$target" ]]; then
    if [[ $SYNC -eq 0 ]]; then
      echo "$repo: NOT READY. On '$current', should be on '$target'${dirty:+ (has uncommitted changes)}"
      FAILED=1; continue
    fi
    if [[ -n "$dirty" ]]; then
      echo "$repo: BLOCKED. On '$current' with uncommitted changes, should be on '$target'. Ask the user."
      FAILED=1; continue
    fi
    if ! out="$(git -C "$dir" switch --quiet "$target" 2>&1)"; then
      echo "$repo: BLOCKED. Switching to '$target' failed: $out"
      FAILED=1; continue
    fi
    note="switched from '$current', "
  fi

  read -r behind ahead <<< "$(behind_ahead "$dir")"
  extra="${dirty:+, has uncommitted changes}"
  if [[ -z "${behind:-}" ]]; then
    echo "$repo: ok. On '$target' (${note}local branch, not on origin$extra)"
  elif [[ "$behind" -gt 0 && "$ahead" -gt 0 ]]; then
    echo "$repo: BLOCKED. '$target' has diverged from origin (behind $behind, ahead $ahead). Ask the user."
    FAILED=1
  elif [[ "$behind" -gt 0 ]]; then
    if [[ $SYNC -eq 1 && -z "$dirty" ]] && git -C "$dir" merge --quiet --ff-only '@{upstream}' 2>/dev/null; then
      echo "$repo: ok. On '$target' (${note}fast-forwarded $behind commits)"
    else
      echo "$repo: NOT READY. On '$target' but $behind commits behind origin${dirty:+, has uncommitted changes}"
      FAILED=1
    fi
  else
    [[ "$ahead" -gt 0 ]] && extra=", $ahead unpushed commits$extra"
    echo "$repo: ok. On '$target' (${note}up to date$extra)"
  fi
done
exit $FAILED

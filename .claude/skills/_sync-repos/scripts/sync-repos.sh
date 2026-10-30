#!/usr/bin/env bash
# Check that Biller's sibling repos are on the branch with the same name as Biller's current
# branch, or on up-to-date master when they have no such branch.
#
# Usage: sync-repos.sh [--sync] [--branch <name>] [repo ...]
#   (default)  report only, change nothing
#   --sync     switch repos without uncommitted changes to their target branch and fast-forward them
#   --branch   use this branch name instead of Biller's current branch, and put Biller on it too
#              when Biller has it (Biller stays where it is otherwise)
#   repo       folder names (default: BillerContracts BillerFrontEnd BillioIntegrationTest, plus
#              Biller when --branch is given). Biller means the checkout this script runs from.
# Exit code: 0 when every repo is on its target branch and not behind origin, 1 otherwise.
set -uo pipefail

REPO_ROOT="${SYNC_REPOS_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)}"

# Switching Biller can replace this file while bash still reads it, so run from a copy.
if [[ -z "${SYNC_REPOS_ROOT:-}" ]]; then
  copy="$(mktemp)"
  cp "${BASH_SOURCE[0]}" "$copy"
  SYNC_REPOS_ROOT="$REPO_ROOT" bash "$copy" "$@"
  code=$?
  rm -f "$copy"
  exit $code
fi

PARENT="$(dirname "$REPO_ROOT")"
export GIT_TERMINAL_PROMPT=0

SYNC=0
BRANCH=""
BILLER=0
REPOS=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --sync) SYNC=1; shift ;;
    --branch) BRANCH="$2"; shift 2 ;;
    -*) echo "Unknown argument: $1" >&2; exit 2 ;;
    Biller) BILLER=1; shift ;;
    *) REPOS+=("$1"); shift ;;
  esac
done
if [[ ${#REPOS[@]} -eq 0 && $BILLER -eq 0 ]]; then
  REPOS=(BillerContracts BillerFrontEnd BillioIntegrationTest)
  [[ -n "$BRANCH" ]] && BILLER=1
fi
BILLER_BRANCH="$(git -C "$REPO_ROOT" symbolic-ref --short -q HEAD || echo '(detached HEAD)')"
[[ -z "$BRANCH" ]] && BRANCH="$BILLER_BRANCH"

# Prints "<behind> <ahead>" of HEAD against its upstream, or nothing when there is no upstream.
behind_ahead() { git -C "$1" rev-list --left-right --count '@{upstream}...HEAD' 2>/dev/null; }

has_branch() {
  git -C "$1" show-ref --verify --quiet "refs/heads/$2" || git -C "$1" show-ref --verify --quiet "refs/remotes/origin/$2"
}

# Puts one repo on its target branch and reports it. $4 is git status's --untracked-files mode:
# "no" for siblings, "normal" for Biller, where new files may be the user's work in progress.
# Returns 1 when the repo isn't ready.
sync_repo() {
  local repo="$1" dir="$2" target="$3" untracked="$4" current dirty note="" out behind ahead extra
  current="$(git -C "$dir" symbolic-ref --short -q HEAD || echo '(detached HEAD)')"
  dirty="$(git -C "$dir" status --porcelain --untracked-files="$untracked")"

  if [[ "$current" != "$target" ]]; then
    if [[ $SYNC -eq 0 ]]; then
      echo "$repo: NOT READY. On '$current', should be on '$target'${dirty:+ (has uncommitted changes)}"
      return 1
    fi
    if [[ -n "$dirty" ]]; then
      echo "$repo: BLOCKED. On '$current' with uncommitted changes, should be on '$target'. Ask the user."
      return 1
    fi
    if ! out="$(git -C "$dir" switch --quiet "$target" 2>&1)"; then
      echo "$repo: BLOCKED. Switching to '$target' failed: $out"
      return 1
    fi
    note="switched from '$current', "
  fi

  read -r behind ahead <<< "$(behind_ahead "$dir")"
  extra="${dirty:+, has uncommitted changes}"
  if [[ -z "${behind:-}" ]]; then
    echo "$repo: ok. On '$target' (${note}local branch, not on origin$extra)"
  elif [[ "$behind" -gt 0 && "$ahead" -gt 0 ]]; then
    echo "$repo: BLOCKED. '$target' has diverged from origin (behind $behind, ahead $ahead). Ask the user."
    return 1
  elif [[ "$behind" -gt 0 ]]; then
    if [[ $SYNC -eq 1 && -z "$dirty" ]] && git -C "$dir" merge --quiet --ff-only '@{upstream}' 2>/dev/null; then
      echo "$repo: ok. On '$target' (${note}fast-forwarded $behind commits)"
    else
      echo "$repo: NOT READY. On '$target' but $behind commits behind origin${dirty:+, has uncommitted changes}"
      return 1
    fi
  else
    [[ "$ahead" -gt 0 ]] && extra=", $ahead unpushed commits$extra"
    echo "$repo: ok. On '$target' (${note}up to date$extra)"
  fi
}

git -C "$REPO_ROOT" fetch --quiet --prune origin 2>/dev/null || echo "WARNING: fetch failed for Biller"
read -r b_behind b_ahead <<< "$(behind_ahead "$REPO_ROOT")"
echo "Biller is on '$BILLER_BRANCH'${b_behind:+ (behind origin: $b_behind, ahead: $b_ahead)}."
[[ "$BRANCH" == master ]] && echo "Target: master." || echo "Target: '$BRANCH' where it exists, else master."
# With Biller in the list, its own line below reports whether it is behind.
[[ $BILLER -eq 0 && "${b_behind:-0}" -gt 0 ]] && echo "WARNING: Biller is behind origin. Pull it before comparing."

FAILED=0
for repo in "${REPOS[@]}"; do
  dir="$PARENT/$repo"
  if [[ ! -d "$dir/.git" ]]; then
    echo "$repo: MISSING. Clone it: git clone https://github.com/aldisadom/$repo ../$repo"
    FAILED=1; continue
  fi
  git -C "$dir" fetch --quiet --prune origin 2>/dev/null || echo "$repo: WARNING fetch failed, using the last fetched state"

  target=master
  [[ "$BRANCH" != master ]] && has_branch "$dir" "$BRANCH" && target="$BRANCH"
  sync_repo "$repo" "$dir" "$target" no || FAILED=1
done

# Biller last: it is the checkout the user works in. It goes on the given branch only when it has
# one; otherwise it stays on its own branch rather than falling back to master.
if [[ $BILLER -eq 1 ]]; then
  if has_branch "$REPO_ROOT" "$BRANCH"; then
    sync_repo Biller "$REPO_ROOT" "$BRANCH" normal || FAILED=1
  else
    echo "Biller: has no branch '$BRANCH', stays on '$BILLER_BRANCH'."
    sync_repo Biller "$REPO_ROOT" "$BILLER_BRANCH" normal || FAILED=1
  fi
fi
exit $FAILED

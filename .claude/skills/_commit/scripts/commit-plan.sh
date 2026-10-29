#!/usr/bin/env bash
# Show what is uncommitted in a Biller repo, so it can be split into commits: the branch and its BIL
# key, commits not pushed yet, staged, unstaged and untracked files with their area and size, package
# version changes, and files that must not be committed. Changes nothing.
#
# Usage: commit-plan.sh [repo-dir]
#   repo-dir  any Biller repo (default: this repo); a sibling name such as BillerFrontEnd also works
# Exit code: 0, or 2 for a bad argument.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"

DIR="$REPO_ROOT"
case $# in
  0) ;;
  1) if [[ -d "$1" ]]; then DIR="$(cd "$1" && pwd)"; elif [[ -d "$PARENT/$1" ]]; then DIR="$PARENT/$1"; else echo "No such repo: $1" >&2; exit 2; fi ;;
  *) echo "Usage: commit-plan.sh [repo-dir]" >&2; exit 2 ;;
esac
cd "$DIR" || exit 2
git rev-parse --git-dir >/dev/null 2>&1 || { echo "Not a git repo: $DIR" >&2; exit 2; }
GIT_DIR="$(git rev-parse --git-dir)"
# safecrlf=false: no "CRLF will be replaced by LF" warning for every CRLF working copy.
g() { git -c core.quotePath=false -c core.safecrlf=false "$@"; }

CHECKS=()

# Branch, key, upstream.
BRANCH="$(git branch --show-current)"
KEY="$(grep -oE '^BIL-[0-9]+' <<< "$BRANCH" || true)"
echo "Repo:     $(basename "$DIR")"
echo "Branch:   ${BRANCH:-(detached HEAD)}   key: ${KEY:-none}"
[[ -z "$BRANCH" ]] && CHECKS+=("detached HEAD: check out a branch before committing")
[[ ( "$BRANCH" == master || "$BRANCH" == main ) && -n "$(g status --porcelain)" ]] \
  && CHECKS+=("on $BRANCH: create a BIL-<n>-<slug> branch before committing")

for f in MERGE_HEAD CHERRY_PICK_HEAD REVERT_HEAD; do
  [[ -f "$GIT_DIR/$f" ]] && CHECKS+=("$f exists: a ${f%_HEAD} is in progress")
done
[[ -d "$GIT_DIR/rebase-merge" || -d "$GIT_DIR/rebase-apply" ]] && CHECKS+=("a rebase is in progress")

if UPSTREAM="$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>/dev/null)"; then
  read -r BEHIND AHEAD < <(git rev-list --left-right --count "$UPSTREAM...HEAD")
  echo "Upstream: $UPSTREAM, $AHEAD ahead, $BEHIND behind"
  RANGE="$UPSTREAM..HEAD"
else
  echo "Upstream: none (branch never pushed)"
  RANGE=""
  git rev-parse --verify -q master >/dev/null && [[ "$BRANCH" != master ]] && RANGE="master..HEAD"
fi
if [[ -n "$RANGE" ]]; then
  UNPUSHED="$(git log --format='  %h %s' "$RANGE")"
  [[ -n "$UNPUSHED" ]] && { echo "Not pushed ($RANGE):"; echo "$UNPUSHED"; }
fi

echo "Recent subjects:"
git log --no-merges --format='  %s' -6

# Area of a path, to see which commit a file belongs to.
area() {
  case "$1" in
    .claude/*|CLAUDE.md|REVIEW.md|docs/review-evals.md) echo skills ;;
    migrations/*) echo migration ;;
    docs/plans/*) echo plan ;;
    docs/*|*.md) echo docs ;;
    .github/*) echo ci ;;
    tests/*) echo tests ;;
    *.csproj|*.sln|*.props) echo project ;;
    src/*/*) local p="${1#src/}"; echo "code:${p%%/*}" ;;
    */*) echo "${1%%/*}" ;;
    *) echo other ;;
  esac
}

# One "status +added -deleted hunks area path" line per file of a diff side.
show_side() {  # $1 = title, rest = git diff options
  local title="$1"; shift
  local lines
  lines="$(paste <(g diff "$@" --no-renames --name-status) <(g diff "$@" --no-renames --numstat))"
  [[ -z "$lines" ]] && return
  echo "$title"
  while IFS=$'\t' read -r st path add del _; do
    local hunks
    hunks="$(g diff "$@" --no-color --no-ext-diff -- "$path" | grep -c '^@@')"
    [[ "$add" == - ]] && { add=bin; del=bin; hunks=-; }
    printf '  %-2s %6s %6s %3s %-5s  %-16s %s\n' "$st" "+$add" "-$del" "$hunks" "$([[ "$hunks" == 1 ]] && echo hunk || echo hunks)" "$(area "$path")" "$path"
  done <<< "$lines"
}

show_side "Staged (goes into the next commit as it is):" --cached
show_side "Not staged:"

UNTRACKED="$(g ls-files --others --exclude-standard)"
if [[ -n "$UNTRACKED" ]]; then
  echo "Untracked:"
  while IFS= read -r path; do
    printf '  ?? %6s %6s %3s %-5s  %-16s %s\n' "+$(wc -l < "$path" | tr -d ' ')" "" "" "" "$(area "$path")" "$path"
  done <<< "$UNTRACKED"
fi

# Biller's clone excludes /.claude/ in .git/info/exclude, yet the skills in it are tracked: a new
# skill file is hidden from git status and needs git add -f.
EXCLUDED=""
[[ -f "$GIT_DIR/info/exclude" ]] \
  && EXCLUDED="$(g ls-files --others --ignored --exclude-from="$GIT_DIR/info/exclude" 2>/dev/null)"
if [[ -n "$EXCLUDED" ]]; then
  echo "Hidden by .git/info/exclude (git add -f, if they belong in the repo):"
  while IFS= read -r path; do
    printf '  !! %6s %6s %3s %-5s  %-16s %s\n' "+$(wc -l < "$path" | tr -d ' ')" "" "" "" "$(area "$path")" "$path"
  done <<< "$EXCLUDED"
fi

if [[ -z "$(g status --porcelain)" && -z "$EXCLUDED" ]]; then
  echo "Nothing to commit: the working tree is clean."
fi

# Package versions changed in project files, staged or not.
PACKAGES="$(g diff HEAD --no-color --no-ext-diff -U0 -- '*.csproj' '*.props' | awk '
  /^\+\+\+ b\// { file = substr($0, 7) }
  /^[-+] *<(PackageReference|ProjectReference)/ {
    if (!match($0, /Include="[^"]+"/)) next
    name = substr($0, RSTART + 9, RLENGTH - 10)
    ver = "none"
    if (match($0, /Version="[^"]+"/)) ver = substr($0, RSTART + 9, RLENGTH - 10)
    k = file "  " name
    if (substr($0, 1, 1) == "-") old[k] = ver; else new[k] = ver
    keys[k] = 1
  }
  END {
    for (k in keys) {
      if (!(k in old)) print "  " k " added" (new[k] == "none" ? "" : " " new[k])
      else if (!(k in new)) print "  " k " removed"
      else if (old[k] != new[k]) print "  " k " " old[k] " -> " new[k]
    }
  }' | sort)"
[[ -n "$PACKAGES" ]] && { echo "Packages:"; echo "$PACKAGES"; }
g diff HEAD --no-color --no-ext-diff -U0 -- '*.csproj' '*.props' | grep -qE '^\+ *<ProjectReference[^>]*BillerContracts' \
  && CHECKS+=("ProjectReference to BillerContracts: CI clones only Biller, so it can't build this")

# Files that must not be committed, and conflict markers left in.
CHANGED="$( { g diff HEAD --name-only --no-renames --diff-filter=d; echo "$UNTRACKED"; echo "$EXCLUDED"; } | grep -v '^$' | sort -u)"
while IFS= read -r path; do
  [[ -z "$path" ]] && continue
  if grep -qiE '(^|/)(bin|obj|TestResults|coverage[^/]*|\.vs|node_modules)/|\.(user|suo|pfx|p12|key|pem)$|(^|/)\.env|appsettings\.[^/]*local[^/]*\.json$|settings\.local\.json$|secret' <<< "$path"; then
    CHECKS+=("$path: build output, local settings or a secret?")
  fi
  if [[ -f "$path" ]]; then
    size=$(wc -c < "$path")
    (( size > 1048576 )) && CHECKS+=("$path: large file ($((size / 1024)) KB)")
  fi
done <<< "$CHANGED"

# Staged and working-tree versions both: a marker can be staged and already gone from the file.
MARKERS="$( { { g diff --cached --no-color --no-ext-diff; g diff HEAD --no-color --no-ext-diff; } | awk '
    /^\+\+\+ b\// { file = substr($0, 7) }
    /^\+(<<<<<<<|>>>>>>>)( |$)/ { print file }'
  [[ -n "$UNTRACKED" ]] && while IFS= read -r p; do grep -lIE '^(<<<<<<<|>>>>>>>)( |$)' -- "$p" 2>/dev/null; done <<< "$UNTRACKED"
  } | sort -u)"
while IFS= read -r path; do
  [[ -n "$path" ]] && CHECKS+=("$path: conflict markers")
done <<< "$MARKERS"

echo "Check:"
if [[ ${#CHECKS[@]} -eq 0 ]]; then echo "  (nothing)"; else printf '  %s\n' "${CHECKS[@]}"; fi
exit 0

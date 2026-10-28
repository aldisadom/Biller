#!/usr/bin/env bash
# Review bookkeeping for _pre-pr, per repo and branch: snapshots of what was reviewed, the list of
# findings already handled, and one context file ("pack") the reviewer reads instead of exploring.
# State lives in <repo>/.git/review-rounds/<branch>/ and is never committed.
#
# Usage: review-round.sh <command> [repo] [options]
#   repo       a repo folder or a sibling name such as BillerFrontEnd (default: this repo)
#   save [--exclude PATH]...
#              snapshot the working tree (commits, uncommitted changes and new files) as the
#              reviewed state; --exclude leaves out untracked folders that aren't part of the change
#   changed    list the files changed since the last snapshot; exit 0 when code changed,
#              1 when only docs (*.md, docs/) or nothing changed, so the next round can be skipped
#   pack [--full] [--plan FILE]
#              write the reviewer's context file and print its path: REVIEW.md, the handled
#              findings, and the diff since the last snapshot (--full: against master)
#   findings   print the path of the handled-findings file; add one line per finding and outcome
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"

COMMAND="${1:-}"; [[ $# -gt 0 ]] && shift
DIR="$REPO_ROOT"
if [[ $# -gt 0 && "$1" != -* ]]; then
  if [[ -d "$1/.git" || -f "$1/.git" ]]; then DIR="$(cd "$1" && pwd)"
  elif [[ -d "$PARENT/$1" ]]; then DIR="$PARENT/$1"
  else echo "No such repo: $1" >&2; exit 2; fi
  shift
fi
cd "$DIR" || exit 2

BRANCH="$(git branch --show-current)"
STATE="$(git rev-parse --absolute-git-dir)/review-rounds/${BRANCH//\//_}"
mkdir -p "$STATE"
FINDINGS="$STATE/findings.md"

pathspec() {
  printf '%s\0' .
  [[ -f "$STATE/excludes" ]] && while read -r ex; do [[ -n "$ex" ]] && printf ':(exclude)%s\0' "$ex"; done < "$STATE/excludes"
}

# New files in the change. Honours .gitignore but not the local .git/info/exclude, which in Biller
# hides new files under .claude/ (files already tracked there are not affected).
new_files() { pathspec | xargs -0 git ls-files --others --exclude-per-directory=.gitignore --; }

# The working tree as a tree object, without touching the real index.
current_tree() {
  local index; index="$(mktemp)"
  cp "$(git rev-parse --absolute-git-dir)/index" "$index"
  pathspec | GIT_INDEX_FILE="$index" xargs -0 git add -u -- 2>/dev/null
  new_files | tr '\n' '\0' | GIT_INDEX_FILE="$index" xargs -0 -r git add -f -- 2>/dev/null
  GIT_INDEX_FILE="$index" git write-tree
  rm -f "$index"
}

last_round() { ls "$STATE"/round-*.tree 2>/dev/null | sed -E 's#.*/round-([0-9]+)\.tree#\1#' | sort -n | tail -n1; }

case "$COMMAND" in
  save)
    : > "$STATE/excludes"
    while [[ $# -gt 0 ]]; do
      case "$1" in
        --exclude) echo "$2" >> "$STATE/excludes"; shift 2 ;;
        *) echo "Unknown argument: $1" >&2; exit 2 ;;
      esac
    done
    tree="$(current_tree)"
    previous="$(last_round)"
    round=$(( ${previous:-0} + 1 ))
    echo "$tree" > "$STATE/round-$round.tree"
    echo "Round $round snapshot of $(basename "$DIR") on '$BRANCH': $tree"
    git diff --stat master "$tree" | tail -n1
    untracked="$(new_files)"
    if [[ -n "$untracked" ]]; then
      echo "New files in the snapshot (use --exclude if they aren't part of the change):"
      sed 's/^/  /' <<< "$untracked"
    fi
    ;;

  changed)
    round="$(last_round)"
    [[ -n "$round" ]] || { echo "No snapshot yet. Run: review-round.sh save" >&2; exit 2; }
    files="$(git diff --name-only "$(cat "$STATE/round-$round.tree")" "$(current_tree)")"
    if [[ -z "$files" ]]; then echo "Nothing changed since round $round."; exit 1; fi
    echo "Changed since round $round:"; sed 's/^/  /' <<< "$files"
    if grep -qvE '(\.md$|^docs/)' <<< "$files"; then echo "Code changed: review again."; exit 0; fi
    echo "Only docs changed: another review round isn't needed."; exit 1
    ;;

  pack)
    full=0 plan=""
    while [[ $# -gt 0 ]]; do
      case "$1" in
        --full) full=1; shift ;;
        --plan) plan="$2"; shift 2 ;;
        *) echo "Unknown argument: $1" >&2; exit 2 ;;
      esac
    done
    tree="$(current_tree)"
    round="$(last_round)"
    if [[ $full -eq 1 || -z "$round" ]]; then
      from=master; what="against master"
    else
      from="$(cat "$STATE/round-$round.tree")"; what="since the round $round snapshot ($from)"
    fi
    pack="$STATE/pack.md"
    {
      echo "# Review pack: $(basename "$DIR") on '$BRANCH'"
      echo
      echo "Diff $what. Read this file first. Open other files only to check a finding."
      echo "Repo: $DIR. Also read its CLAUDE.md.${plan:+ Plan: $plan.}"
      echo
      echo "## Files"; echo '```'; git diff --stat "$from" "$tree"; echo '```'
      echo
      echo "## Review rules (REVIEW.md)"
      if [[ -f REVIEW.md ]]; then cat REVIEW.md; else echo "(no REVIEW.md)"; fi
      echo
      echo "## Already handled, don't report again"
      if [[ -s "$FINDINGS" ]]; then cat "$FINDINGS"; else echo "(none yet)"; fi
      echo
      echo "## Diff"; echo '```diff'; git diff "$from" "$tree"; echo '```'
    } > "$pack"
    echo "$pack ($(wc -l < "$pack") lines, diff $what)"
    ;;

  findings)
    touch "$FINDINGS"
    echo "$FINDINGS"
    ;;

  *) sed -n '2,16p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 2 ;;
esac

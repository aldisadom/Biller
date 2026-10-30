#!/usr/bin/env bash
# Review bookkeeping for _pr-review, per repo and branch: snapshots of what was reviewed, the
# findings and what happened to them, and one context file ("pack") the reviewer reads instead of
# exploring. Everything lives in the PR state folder (_pre-pr/scripts/pr-state.sh dir), next to
# scope.md, whose plan and risk notes go into the pack.
#
# Usage: review-round.sh <command> [repo] [options]
#   repo       a repo folder or a sibling name such as BillerFrontEnd (default: this repo)
#   save       snapshot the working tree (commits, uncommitted changes and new files) as the
#              reviewed state, without the paths left out with pr-state.sh exclude
#   changed    list the files changed since the last snapshot; exit 0 when code changed,
#              1 when only docs (*.md, docs/) or nothing changed, so the next round can be skipped
#   pack [--full] [--plan FILE]
#              write the reviewer's context file and print its path: scope.md, REVIEW.md, the
#              findings so far, and the diff since the last snapshot (--full: since the branch
#              left master). The plan defaults to the one named in scope.md.
#   findings   print the path of findings.md: one "- [ ] F<n> <tag> <file:line> <finding> (round N)" line
#              per finding; _pr-fix ticks it and adds "-> <what was done>"
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
PR_STATE="$REPO_ROOT/.claude/skills/_pre-pr/scripts/pr-state.sh"
STATE="$(bash "$PR_STATE" dir "$DIR")" || exit 2
FINDINGS="$STATE/findings.md"

# The working tree as a tree object (honouring the excludes saved in the state folder).
current_tree() { bash "$PR_STATE" tree "$DIR"; }

last_round() { ls "$STATE"/round-*.tree 2>/dev/null | sed -E 's#.*/round-([0-9]+)\.tree#\1#' | sort -n | tail -n1; }

case "$COMMAND" in
  save)
    [[ $# -eq 0 ]] || { echo "Unknown argument: $1 (leave paths out with pr-state.sh exclude)" >&2; exit 2; }
    tree="$(current_tree)"
    previous="$(last_round)"
    round=$(( ${previous:-0} + 1 ))
    echo "$tree" > "$STATE/round-$round.tree"
    echo "Round $round snapshot of $(basename "$DIR") on '$BRANCH': $tree"
    git diff --stat "$(bash "$PR_STATE" base "$DIR")" "$tree" | tail -n1
    untracked="$(git diff --name-only --diff-filter=A HEAD "$tree")"
    if [[ -n "$untracked" ]]; then
      echo "Files not committed yet (leave out those that aren't part of the PR with pr-state.sh exclude):"
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
    [[ -z "$plan" && -f "$STATE/scope.md" ]] \
      && plan="$(sed -nE 's/^Plan: ([^ ]+\.md).*/\1/p' "$STATE/scope.md" | head -n1)"
    tree="$(current_tree)"
    round="$(last_round)"
    if [[ $full -eq 1 || -z "$round" ]]; then
      from="$(bash "$PR_STATE" base "$DIR")"; what="against master (from the branch's base $from)"
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
      echo "## Scope (from _pr-scope)"
      if [[ -f "$STATE/scope.md" ]]; then sed '1{/^<!-- tree: /d}' "$STATE/scope.md"; else echo "(no scope.md)"; fi
      echo
      echo "## Review rules (REVIEW.md)"
      if [[ -f REVIEW.md ]]; then cat REVIEW.md; else echo "(no REVIEW.md)"; fi
      echo
      echo "## Findings so far: [x] lines are handled, don't report them again"
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

  *) sed -n '2,18p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 2 ;;
esac

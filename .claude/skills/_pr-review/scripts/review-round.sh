#!/usr/bin/env bash
# Review bookkeeping for _pr-review, per repo and branch: snapshots of what was reviewed, the
# findings and what happened to them, and one context file ("pack") the reviewer reads instead of
# exploring. Everything lives in the PR state folder (_pre-pr/scripts/pr-state.sh dir), next to
# scope.md, whose plan and risk notes go into the pack.
#
# Usage: review-round.sh <command> [repo] [options]
#   repo       a repo folder or a sibling name such as BillerFrontEnd (default: this repo)
#   save       record the reviewed state as the next round's snapshot: the tree the last pack
#              showed, or the working tree when there was no pack (commits, uncommitted changes
#              and new files, without the paths left out with pr-state.sh exclude). Pins it, so
#              git gc keeps it, and creates findings.md
#   changed    list the files changed since the last snapshot; exit 0 when code changed,
#              1 when only docs or nothing changed, so the next round can be skipped, 3 when the
#              snapshot can't be compared. Skill files, CLAUDE.md and REVIEW.md count as code.
#   pack [--full] [--plan FILE]
#              write the reviewer's context file and print its path: scope.md, REVIEW.md, the
#              findings so far, and the diff since the last snapshot (--full: since the branch
#              left master), without the paths REVIEW.md says to skip. The plan defaults to the
#              one named in scope.md. Run save right after it.
#   findings   print the path of findings.md: one "- [ ] F<n> <tag> <file:line> <finding> (round N)" line
#              per finding; _pr-fix ticks it and adds "-> <what was done>"
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
# The siblings sit next to the main checkout, also when this runs from a worktree inside it
PARENT="$(common="$(git -C "$REPO_ROOT" rev-parse --path-format=absolute --git-common-dir 2>/dev/null)" && dirname "$(dirname "$common")" || dirname "$REPO_ROOT")"

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

# REVIEW.md's "Skip:" paths, left out of the pack. Migrations stay in: an edit to one already on
# master must be seen.
SKIP=(':(exclude,glob)**/bin/**' ':(exclude,glob)**/obj/**' ':(exclude,glob)**/coveragereport/**'
      ':(exclude,glob)**/TestResults/**' ':(exclude,glob)**/*.Designer.cs')

# save pins its snapshot, so this only catches one taken before snapshots were pinned. Stop
# instead of reading the failed diff as "nothing changed" or packing an empty one.
check_tree() {
  git cat-file -e "$1^{tree}" 2>/dev/null && return
  echo "Can't read $2 ($1): git may have pruned it. Review the whole branch: review-round.sh pack --full" >&2
  exit 3
}

case "$COMMAND" in
  save)
    [[ $# -eq 0 ]] || { echo "Unknown argument: $1 (leave paths out with pr-state.sh exclude)" >&2; exit 2; }
    # The tree the reviewer was shown, so an edit made while the review ran isn't recorded as reviewed
    if [[ -f "$STATE/pack.tree" ]]; then tree="$(cat "$STATE/pack.tree")"; rm -f "$STATE/pack.tree"
    else tree="$(current_tree)"; fi
    previous="$(last_round)"
    round=$(( ${previous:-0} + 1 ))
    echo "$tree" > "$STATE/round-$round.tree"
    bash "$PR_STATE" pin "$DIR" "round-$round" "$tree"
    touch "$FINDINGS"
    echo "Round $round snapshot of $(basename "$DIR") on '$BRANCH': $tree"
    git diff --stat "$(bash "$PR_STATE" base "$DIR")" "$tree" | tail -n1
    uncommitted="$(git diff --name-status HEAD "$tree")"
    if [[ -n "$uncommitted" ]]; then
      echo "Not committed yet, new (A) or changed (M, D) (leave out those that aren't part of the PR with pr-state.sh exclude):"
      sed 's/^/  /' <<< "$uncommitted"
    fi
    ;;

  changed)
    round="$(last_round)"
    [[ -n "$round" ]] || { echo "No snapshot yet. Run: review-round.sh save" >&2; exit 2; }
    from="$(cat "$STATE/round-$round.tree")"
    check_tree "$from" "the round $round snapshot"
    files="$(git diff --name-only "$from" "$(current_tree)")" || { echo "git diff failed" >&2; exit 3; }
    if [[ -z "$files" ]]; then echo "Nothing changed since round $round."; exit 1; fi
    echo "Changed since round $round:"; sed 's/^/  /' <<< "$files"
    if bash "$PR_STATE" has-code "$DIR" <<< "$files"; then echo "Code changed: review again."; exit 0; fi
    echo "Only docs changed: another review round isn't needed."; exit 1
    ;;

  pack)
    full=0 plan=""
    while [[ $# -gt 0 ]]; do
      case "$1" in
        --full) full=1; shift ;;
        --plan) [[ $# -ge 2 && -n "$2" ]] || { echo "--plan needs a file" >&2; exit 2; }; plan="$2"; shift 2 ;;
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
      check_tree "$from" "the round $round snapshot"
    fi
    git diff --quiet "$from" "$tree" 2>/dev/null; [[ $? -le 1 ]] || { echo "git diff $from $tree failed" >&2; exit 3; }
    echo "$tree" > "$STATE/pack.tree"
    pack="$STATE/pack.md"
    {
      echo "# Review pack: $(basename "$DIR") on '$BRANCH'"
      echo
      echo "Diff $what. Read this file first. Open other files only to check a finding."
      echo "Repo: $DIR. Also read its CLAUDE.md.${plan:+ Plan: $plan.}"
      echo
      echo "## Files"; echo '```'; git diff --stat "$from" "$tree" -- . "${SKIP[@]}"; echo '```'
      echo
      echo "## Scope (from _pr-scope)"
      if [[ -f "$STATE/scope.md" ]]; then sed '1{/^<!-- tree: /d}' "$STATE/scope.md"; else echo "(no scope.md)"; fi
      echo
      echo "## Review rules (REVIEW.md)"
      if [[ -f REVIEW.md ]]; then cat REVIEW.md; else echo "(no REVIEW.md)"; fi
      echo
      echo "## Findings so far: don't report any of them again, open ([ ]) or handled ([x])"
      if [[ -s "$FINDINGS" ]]; then cat "$FINDINGS"; else echo "(none yet)"; fi
      echo
      echo "## Diff"; echo '```diff'; git diff "$from" "$tree" -- . "${SKIP[@]}"; echo '```'
    } > "$pack"
    echo "$pack ($(wc -l < "$pack") lines, diff $what)"
    ;;

  findings)
    touch "$FINDINGS"
    echo "$FINDINGS"
    ;;

  *) sed -n '2,22p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 2 ;;
esac

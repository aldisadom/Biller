#!/usr/bin/env bash
# The state folder of the PR steps (_pr-scope, _test-coverage, _test-stack, _pr-review, _pr-fix,
# _pr-description, _open-pr): each step leaves its result there for the next one, per repo and branch.
# It lives in <repo>/.git/pre-pr/<branch>/, is shared by every worktree of the repo and is never
# committed. Each result is stamped with the working tree it describes, so a later step can tell
# when the code changed after it was written.
#
# Usage: pr-state.sh <command> [repo] [args]
#   repo          a repo folder or a sibling name such as BillerFrontEnd (default: this repo)
#   dir           print the folder, creating it
#   base          print the commit the PR's diff starts from: where the branch left origin/master
#                 (master when there is no origin/master), so master's newer commits aren't in it
#   tree          print the working tree (commits, uncommitted changes and new files) as a tree id,
#                 without touching the index; leaves out the excluded paths
#   exclude [path...]
#                 leave paths that aren't part of the PR (uncommitted files or folders) out of the
#                 tree every step sees; without paths, list them. --clear forgets them all
#   write <step> save stdin as <step>.md, stamped with the current tree
#   note <step> <text>
#                 add a "- <text>" line to <step>.md, keeping its stamp
#   read <step>   print <step>.md; exit 1 when missing, 3 when code changed since it was written
#                 (when only docs changed it prints a note and exits 0)
#   status        one line per step: ok, docs changed, STALE, missing or not needed
#   reset         delete every result, snapshot and finding of this branch, to start over
# Steps: scope, tests, integration, findings, description. Docs are *.md files and docs/.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"
STEPS=(scope tests integration findings description)

usage() { sed -n '8,25p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//' >&2; exit 2; }

COMMAND="${1:-}"; [[ $# -gt 0 ]] && shift
DIR="$REPO_ROOT"
if [[ $# -gt 0 && ( -d "$1/.git" || -f "$1/.git" || -d "$PARENT/$1" ) && ! " ${STEPS[*]} " =~ " $1 " ]]; then
  if [[ -d "$1/.git" || -f "$1/.git" ]]; then DIR="$(cd "$1" && pwd)"; else DIR="$PARENT/$1"; fi
  shift
fi
cd "$DIR" || exit 2

BRANCH="$(git branch --show-current)"
[[ -n "$BRANCH" ]] || { echo "Detached HEAD in $DIR: check out the PR branch first." >&2; exit 2; }
STATE="$(git rev-parse --path-format=absolute --git-common-dir)/pre-pr/${BRANCH//\//_}"
mkdir -p "$STATE"

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
  GIT_INDEX_FILE="$index" git -c core.safecrlf=false write-tree
  rm -f "$index"
}

stamp_of() { sed -nE '1s/^<!-- tree: ([0-9a-f]+) .*/\1/p' "$1"; }

# "ok", "docs" or "stale" for a file stamped with a tree, against the current tree.
freshness() {
  local stamp="$1" now="$2" files
  [[ "$stamp" == "$now" ]] && { echo ok; return; }
  files="$(git diff --name-only "$stamp" "$now" 2>/dev/null)" || { echo stale; return; }
  if [[ -z "$files" ]]; then echo ok
  elif grep -qvE '(\.md$|^docs/)' <<< "$files"; then echo stale
  else echo docs; fi
}

step_arg() {
  [[ -n "${1:-}" ]] || usage
  [[ " ${STEPS[*]} " =~ " $1 " ]] || { echo "Unknown step: $1 (steps: ${STEPS[*]})" >&2; exit 2; }
}

case "$COMMAND" in
  dir) echo "$STATE" ;;

  base)
    upstream=origin/master
    git rev-parse --verify -q "$upstream" >/dev/null || upstream=master
    git merge-base "$upstream" HEAD
    ;;

  tree) current_tree ;;

  exclude)
    if [[ "${1:-}" == --clear ]]; then rm -f "$STATE/excludes"; echo "No paths excluded."; exit 0; fi
    for p in "$@"; do grep -qxF -- "$p" "$STATE/excludes" 2>/dev/null || echo "$p" >> "$STATE/excludes"; done
    if [[ -s "$STATE/excludes" ]]; then echo "Left out of the PR:"; sed 's/^/  /' "$STATE/excludes"; else echo "No paths excluded."; fi
    ;;

  write)
    step_arg "${1:-}"
    tree="$(current_tree)"
    { echo "<!-- tree: $tree written: $(date '+%Y-%m-%d %H:%M') -->"; cat; } > "$STATE/$1.md"
    echo "Wrote $1.md for tree $tree" >&2
    ;;

  note)
    step_arg "${1:-}"
    step="$1"; shift
    [[ $# -gt 0 ]] || usage
    [[ -f "$STATE/$step.md" ]] || { echo "No $step.md yet." >&2; exit 1; }
    echo "- $*" >> "$STATE/$step.md"
    ;;

  read)
    step_arg "${1:-}"
    file="$STATE/$1.md"
    [[ -f "$file" ]] || { echo "No $1.md for $(basename "$DIR") on '$BRANCH': run its step first." >&2; exit 1; }
    sed '1{/^<!-- tree: /d}' "$file"
    stamp="$(stamp_of "$file")"
    [[ -z "$stamp" ]] && exit 0
    case "$(freshness "$stamp" "$(current_tree)")" in
      ok) ;;
      docs) echo "(only docs changed since $1.md was written)" >&2 ;;
      stale) echo "STALE: code changed since $1.md was written. Run its step again." >&2; exit 3 ;;
    esac
    ;;

  status)
    now="$(current_tree)"
    echo "PR state of $(basename "$DIR") on '$BRANCH' ($STATE):"
    for step in "${STEPS[@]}"; do
      file="$STATE/$step.md"
      if [[ "$step" == integration ]] && grep -qE '^Integration tests: no' "$STATE/scope.md" 2>/dev/null; then
        state="not needed"
      elif [[ ! -f "$file" ]]; then
        state=missing
      elif [[ "$step" == findings ]]; then
        rounds="$(ls "$STATE"/round-*.tree 2>/dev/null | wc -l)"
        open="$(grep -c '^- \[ \]' "$file")"
        state="$rounds round(s), $open open"
      else
        case "$(freshness "$(stamp_of "$file")" "$now")" in
          ok) state=ok ;; docs) state="ok (docs changed since)" ;; stale) state="STALE (code changed since)" ;;
        esac
      fi
      printf '  %-12s %s\n' "$step" "$state"
    done
    ;;

  reset)
    rm -rf "$STATE"
    echo "Deleted $STATE"
    ;;

  *) usage ;;
esac

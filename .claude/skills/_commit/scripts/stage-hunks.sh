#!/usr/bin/env bash
# Stage some of a file's changes, for when one file holds parts of two commits (git add -p is
# interactive and can't run here). Hunks are numbered from the file's unstaged diff.
#
# Usage: stage-hunks.sh [--repo <dir>] [--fine] list  <file>
#        stage-hunks.sh [--repo <dir>] [--fine] stage <file> <n>[,<n>...]
#   --repo  any Biller repo (default: this repo); a sibling name such as BillerFrontEnd also works
#   --fine  no context lines, so changes a few lines apart become separate hunks. It changes the
#           numbering, so pass it to both list and stage.
#   list    print each hunk with its number
#   stage   stage only the given hunks, then show what is staged and how many hunks are left
# The file path is relative to the repo root. Numbers change after each stage: list again first.
# Exit code: 0, 1 when there is nothing to stage or git apply refuses the hunks, 2 for a bad argument.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"

usage() { sed -n '5,6p' "${BASH_SOURCE[0]}" | sed 's/^# //' >&2; exit 2; }

DIR="$REPO_ROOT"
CTX=-U3
APPLY=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo) [[ $# -ge 2 ]] || usage
            if [[ -d "$2" ]]; then DIR="$(cd "$2" && pwd)"; elif [[ -d "$PARENT/$2" ]]; then DIR="$PARENT/$2"; else echo "No such repo: $2" >&2; exit 2; fi
            shift 2 ;;
    --fine) CTX=-U0; APPLY=(--unidiff-zero); shift ;;
    -*) echo "Unknown argument: $1" >&2; usage ;;
    *) break ;;
  esac
done
[[ $# -ge 2 ]] || usage
CMD="$1"; FILE="$2"; NUMS="${3:-}"
[[ "$CMD" == list || "$CMD" == stage ]] || usage
[[ "$CMD" == stage && ! "$NUMS" =~ ^[0-9]+(,[0-9]+)*$ ]] && { echo "stage needs hunk numbers such as 1,3" >&2; exit 2; }

cd "$DIR" || exit 2
git ls-files --error-unmatch -- "$FILE" >/dev/null 2>&1 \
  || { echo "$FILE is not tracked: stage the whole file with git add, or check the path (relative to $(basename "$DIR"))" >&2; exit 2; }

# safecrlf=false: no "CRLF will be replaced by LF" warning for every CRLF working copy.
diff_of() { git -c core.quotePath=false -c core.safecrlf=false diff--no-color --no-ext-diff --src-prefix=a/ --dst-prefix=b/ "$CTX" -- "$FILE"; }

DIFF="$(diff_of)"
[[ -n "$DIFF" ]] || { echo "$FILE has no unstaged changes."; exit 1; }
grep -q '^Binary files' <<< "$DIFF" && { echo "$FILE is binary: stage the whole file with git add." >&2; exit 2; }
TOTAL="$(grep -c '^@@' <<< "$DIFF")"

if [[ "$CMD" == list ]]; then
  awk '/^@@/ { n++; print ""; print "== hunk " n " =="; } n > 0 { print }' <<< "$DIFF"
  echo
  echo "$TOTAL hunk(s) in $FILE"
  exit 0
fi

for n in ${NUMS//,/ }; do
  (( n >= 1 && n <= TOTAL )) || { echo "No hunk $n: $FILE has $TOTAL" >&2; exit 2; }
done

PATCH="$(mktemp)"
trap 'rm -f "$PATCH"' EXIT
# The file header, then only the chosen hunks ("\ No newline at end of file" stays with its hunk).
awk -v want=",$NUMS," '
  /^@@/ { n++; keep = index(want, "," n ",") > 0 }
  n == 0 || keep { print }' <<< "$DIFF" > "$PATCH"

git apply --cached --whitespace=nowarn "${APPLY[@]}" "$PATCH" || { echo "git apply refused the hunks; nothing was staged." >&2; exit 1; }

echo "Staged hunk(s) $NUMS of $FILE. Staged now in this file:"
git diff --cached --stat -- "$FILE"
LEFT="$(diff_of | grep -c '^@@')"
echo "Left unstaged: $LEFT hunk(s)"

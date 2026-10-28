#!/usr/bin/env bash
# Check that a bug-fix test fails when the fix is undone: replace one exact piece of text in a
# source file, run the filtered tests, then restore the file byte for byte.
#
# Usage: revert-check.sh --file <path> --old <text> --new <text> --filter "<dotnet test filter>"
#   --file    the source file with the fix (relative to the Biller repo root, or absolute)
#   --old     the fixed code, exactly as in the file; it must occur exactly once
#   --new     the code with the bug back (may be empty)
#   --filter  which tests to run, e.g. "FullyQualifiedName~InvoiceServiceTest"
# Line endings in the file (LF or CRLF) don't matter; write --old and --new with plain newlines.
# Exit code: 0 when the tests failed with the fix undone and the file was restored,
#            1 when they still passed (the test doesn't cover the fix), 2 on any other problem.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"

FILE="" OLD="" NEW="" FILTER="" HAVE_OLD=0 HAVE_NEW=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --file) FILE="$2"; shift 2 ;;
    --old) OLD="$2"; HAVE_OLD=1; shift 2 ;;
    --new) NEW="$2"; HAVE_NEW=1; shift 2 ;;
    --filter) FILTER="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done
[[ -n "$FILE" && $HAVE_OLD -eq 1 && $HAVE_NEW -eq 1 && -n "$FILTER" ]] \
  || { sed -n '2,13p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 2; }
[[ "$FILE" == /* ]] || FILE="$REPO_ROOT/$FILE"
[[ -f "$FILE" ]] || { echo "ERROR: no such file: $FILE" >&2; exit 2; }

BACKUP="$(mktemp)"
cp -p "$FILE" "$BACKUP"
restore() {
  cp -p "$BACKUP" "$FILE"
  if cmp -s "$BACKUP" "$FILE"; then rm -f "$BACKUP"; else echo "ERROR: restore failed, the original is in $BACKUP" >&2; exit 2; fi
}
trap restore EXIT

python3 -I - "$FILE" "$OLD" "$NEW" <<'EOF' || exit 2
import sys
path, old, new = sys.argv[1], sys.argv[2], sys.argv[3]
data = open(path, "rb").read()
if b"\r\n" in data:
    old, new = old.replace("\n", "\r\n"), new.replace("\n", "\r\n")
old_b, new_b = old.encode("utf-8"), new.encode("utf-8")
count = data.count(old_b)
if count != 1:
    sys.exit(f"ERROR: --old occurs {count} times in {path}, it must occur exactly once")
open(path, "wb").write(data.replace(old_b, new_b))
EOF

echo ">> fix undone in ${FILE#"$REPO_ROOT"/}, running: $FILTER"
OUT="$(bash "$REPO_ROOT/.claude/skills/_test-coverage/scripts/run-coverage.sh" --no-report --filter "$FILTER" 2>&1)"
CODE=$?
grep -E " error |\[FAIL\]|Passed!|Failed!" <<< "$OUT" | sed -E 's#^\[xUnit\.net [0-9:.]+\] +##' | head -20

restore
trap - EXIT
echo ">> file restored: ${FILE#"$REPO_ROOT"/}"
if grep -q "Failed!" <<< "$OUT"; then
  echo "OK: the tests fail without the fix."
  exit 0
elif [[ $CODE -eq 0 ]]; then
  echo "PROBLEM: the tests still pass without the fix, so they don't cover it."
  exit 1
else
  echo "ERROR: the run failed for another reason (build error?), exit $CODE. Output above."
  exit 2
fi

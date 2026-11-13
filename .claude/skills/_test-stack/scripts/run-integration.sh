#!/usr/bin/env bash
# Run BillioIntegrationTest against any Biller API and print only the result lines.
#
# Usage: run-integration.sh [--url URL] [--repeat N] [--repo DIR]
#   --url     API base URL (default: the test stack's API, or with TEST_STACK_NAME that named stack's)
#   --repeat  run the suite N times (default 1). The tests run in parallel, so repeats catch races
#   --repo    BillioIntegrationTest checkout (default: ../BillioIntegrationTest)
# The tests delete ALL data through the API first. Never point --url at production.
# Exit code: 0 when the build and every run passed.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
# The siblings sit next to the main checkout, also when this runs from a worktree inside it
PARENT="$(common="$(git -C "$REPO_ROOT" rev-parse --path-format=absolute --git-common-dir 2>/dev/null)" && dirname "$(dirname "$common")" || dirname "$REPO_ROOT")"

URL=""
REPEAT=1
REPO="$PARENT/BillioIntegrationTest"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --url) URL="${2%/}"; shift 2 ;;
    --repeat) REPEAT="$2"; shift 2 ;;
    --repo) REPO="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

# The API to test: --url, else the test stack's API on the port Docker published it on
# (test-stack.sh names its containers so). Docker can give the IPv4 and IPv6 bindings different
# random ports, so take the IPv4 one. Either way it must answer /health.
GIVEN_URL="$URL"
find_api() {
  if [[ -z "$GIVEN_URL" ]]; then
    local api="biller-test${TEST_STACK_NAME:+-$TEST_STACK_NAME}-api" bindings addr
    bindings="$(docker port "$api" 8070 2>/dev/null)"
    addr="$({ grep -m1 '^0\.0\.0\.0:' <<< "$bindings" || head -n1 <<< "$bindings"; } \
      | sed 's/^0\.0\.0\.0:/127.0.0.1:/; s/^\[::\]:/localhost:/')"
    [[ -n "$addr" ]] || { echo "ERROR: $api is not running. Run test-stack.sh up, or pass --url" >&2; exit 2; }
    URL="http://$addr"
  fi
  # A time limit: after a wait this runs while holding the lock
  curl -sf --max-time 10 "$URL/health" >/dev/null || { echo "ERROR: $URL/health does not answer" >&2; exit 2; }
}
find_api

pick() { for c in "$@"; do command -v "$c" >/dev/null 2>&1 && { echo "$c"; return 0; }; done; return 1; }
DOTNET="$(pick dotnet dotnet.exe)" || { echo "ERROR: neither dotnet nor dotnet.exe found on PATH" >&2; exit 2; }

# Windows dotnet.exe can't read /mnt/... paths, so run from the repo with a relative solution path.
cd "$REPO" || { echo "ERROR: no such folder: $REPO" >&2; exit 2; }
SLN="$(ls -- *.sln 2>/dev/null | head -n1)"
[[ -n "$SLN" ]] || { echo "ERROR: no solution in $REPO" >&2; exit 2; }

# Two runs on one checkout build into the same bin/obj, so a second run waits for the first.
# Stacks that test in parallel each pass their own checkout (a worktree) with --repo.
# The lock lives in the checkout's git folder, or in /tmp for a folder that isn't the top of its own
# checkout: a copy inside another repo must not take that repo's lock, branch or commit.
GIT_FOLDER=""
TOP="$(git rev-parse --show-toplevel 2>/dev/null)"
[[ -n "$TOP" && "$(cd "$TOP" && pwd -P)" == "$(pwd -P)" ]] && GIT_FOLDER="$(git rev-parse --absolute-git-dir)"
if [[ -n "$GIT_FOLDER" ]]; then LOCK="$GIT_FOLDER/run-integration.lock"
else LOCK="/tmp/run-integration-$(pwd | md5sum | cut -c1-12).lock"; fi
exec 9>"$LOCK" || { echo "ERROR: cannot open $LOCK" >&2; exit 2; }
if ! flock -n 9; then
  echo ">> Waiting for another run-integration.sh on $REPO (lock: $LOCK)"
  flock 9
  # While this run waited, its stack may have stopped or come back on another port
  find_api
fi

# Every client URL, or a missing one falls back to appsettings.json, which points at the user's own stack.
export ClientsOptions__UserUrl="$URL/users" ClientsOptions__SellerUrl="$URL/sellers" \
  ClientsOptions__CustomerUrl="$URL/customers" ClientsOptions__ItemUrl="$URL/items" \
  ClientsOptions__InvoiceUrl="$URL/invoices" ClientsOptions__SeriesUrl="$URL/series"
# A Windows dotnet.exe started from WSL only sees the variables listed in WSLENV.
[[ "$DOTNET" == *.exe ]] && export WSLENV="${WSLENV:+$WSLENV:}ClientsOptions__UserUrl:ClientsOptions__SellerUrl:ClientsOptions__CustomerUrl:ClientsOptions__ItemUrl:ClientsOptions__InvoiceUrl:ClientsOptions__SeriesUrl"

# Which checkout, branch and commit ran. A worktree checked out with --detach lists every remote
# branch at its commit, since git can't tell which one it came from.
BRANCH="" COMMIT=""
if [[ -n "$GIT_FOLDER" ]]; then
  BRANCH="$(git branch --show-current 2>/dev/null)"
  [[ -n "$BRANCH" ]] || BRANCH="$(git branch -r --points-at HEAD 2>/dev/null | grep -v -- ' -> ' | sed 's/^ *//' | paste -sd' ' -)"
  COMMIT="$(git rev-parse --short HEAD 2>/dev/null)"
fi
echo ">> $SLN in $PWD on '${BRANCH:-no branch}' at ${COMMIT:-no commit} against $URL"
# dotnet leaves build servers running, so its commands get fd 9 closed: they must not keep the lock.
BUILD="$("$DOTNET" build "$SLN" -c Release -nologo 9>&- 2>&1)"
if ! grep -q "Build succeeded" <<< "$BUILD"; then
  grep -E " error " <<< "$BUILD" | sort -u
  echo "Build FAILED"
  exit 1
fi

# The failed tests come from a TRX report: in the console text, log lines can look like failed tests.
# Relative to the repo, because Windows dotnet.exe can't write to /mnt/... paths.
TRX_DIR="TestResults/run-integration"
TRX_FILE="run-integration.trx"

# Print the first 10 failed tests of a TRX report, each with its message and first stack frame.
failed_tests() {
python3 -I - "$1" <<'EOF'
import sys
import xml.etree.ElementTree as ET

ns = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
try:
    results = ET.parse(sys.argv[1]).getroot().iter(ns + "UnitTestResult")
except ET.ParseError as error:
    sys.exit(f"Unreadable TRX report {sys.argv[1]}: {error}")
# The outcomes of a test that failed, or didn't finish or run.
failing = ("Failed", "Error", "Timeout", "Aborted", "Disconnected", "NotRunnable", "InProgress")
failed = [r for r in results if r.get("outcome") in failing]
for result in failed[:10]:
    outcome = result.get("outcome")
    print(f"failed {result.get('testName')}" + ("" if outcome == "Failed" else f" ({outcome})"))
    message = result.findtext(f"{ns}Output/{ns}ErrorInfo/{ns}Message") or ""
    stack = result.findtext(f"{ns}Output/{ns}ErrorInfo/{ns}StackTrace") or ""
    lines = [line.strip() for line in message.splitlines() if line.strip()]
    frames = [line.strip() for line in stack.splitlines() if line.strip()]
    # TUnit leaves its own frames out; a package frame, such as the API client's, has no source line.
    frame = ([f for f in frames if ":line " in f] or frames)[:1]
    for line in lines[:5] + frame:
        print("  " + line)
if len(failed) > 10:
    print(f"... and {len(failed) - 10} more failed tests")
EOF
}

FAILED=0
for run in $(seq 1 "$REPEAT"); do
  # A report left from the run before must not be read as this run's.
  rm -rf -- "$TRX_DIR"
  OUT="$("$DOTNET" test "$SLN" -c Release --no-build \
    --report-trx --report-trx-filename "$TRX_FILE" --results-directory "$TRX_DIR" 2>&1)"
  CODE=$?
  if [[ -f "$TRX_DIR/$TRX_FILE" ]]; then failed_tests "$TRX_DIR/$TRX_FILE"; else echo "No TRX report at $REPO/$TRX_DIR/$TRX_FILE"; fi
  TOTALS="$(grep -E "^\s+(total|failed|succeeded|skipped):" <<< "$OUT" | tr -s ' ' | tr '\n' ' ')"
  echo "Run $run/$REPEAT: ${TOTALS:-no summary (see: $DOTNET test $SLN -c Release --no-build)}"
  [[ $CODE -ne 0 ]] && FAILED=1
done 9>&-
exit $FAILED

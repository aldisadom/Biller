#!/usr/bin/env bash
# Run BillioIntegrationTest against any Biller API and print only the result lines.
#
# Usage: run-integration.sh [--url URL] [--repeat N] [--repo DIR]
#   --url     API base URL (default: the test stack, http://localhost:${TEST_STACK_API_PORT:-8091})
#   --repeat  run the suite N times (default 1). The tests run in parallel, so repeats catch races
#   --repo    BillioIntegrationTest checkout (default: ../BillioIntegrationTest)
# The tests delete ALL data through the API first. Never point --url at production.
# Exit code: 0 when the build and every run passed.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"

URL="http://localhost:${TEST_STACK_API_PORT:-8091}"
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

pick() { for c in "$@"; do command -v "$c" >/dev/null 2>&1 && { echo "$c"; return 0; }; done; return 1; }
DOTNET="$(pick dotnet dotnet.exe)" || { echo "ERROR: neither dotnet nor dotnet.exe found on PATH" >&2; exit 2; }

# Windows dotnet.exe can't read /mnt/... paths, so run from the repo with a relative solution path.
cd "$REPO" || { echo "ERROR: no such folder: $REPO" >&2; exit 2; }
SLN="$(ls -- *.sln 2>/dev/null | head -n1)"
[[ -n "$SLN" ]] || { echo "ERROR: no solution in $REPO" >&2; exit 2; }
curl -sf "$URL/health" >/dev/null || { echo "ERROR: $URL/health does not answer" >&2; exit 2; }

export ClientsOptions__UserUrl="$URL/users" ClientsOptions__SellerUrl="$URL/sellers" \
  ClientsOptions__CustomerUrl="$URL/customers" ClientsOptions__ItemUrl="$URL/items" \
  ClientsOptions__InvoiceUrl="$URL/invoices"
# A Windows dotnet.exe started from WSL only sees the variables listed in WSLENV.
[[ "$DOTNET" == *.exe ]] && export WSLENV="${WSLENV:+$WSLENV:}ClientsOptions__UserUrl:ClientsOptions__SellerUrl:ClientsOptions__CustomerUrl:ClientsOptions__ItemUrl:ClientsOptions__InvoiceUrl"

echo ">> $SLN on '$(git branch --show-current)' against $URL"
BUILD="$("$DOTNET" build "$SLN" -c Release -nologo 2>&1)"
if ! grep -q "Build succeeded" <<< "$BUILD"; then
  grep -E " error " <<< "$BUILD" | sort -u
  echo "Build FAILED"
  exit 1
fi

FAILED=0
for run in $(seq 1 "$REPEAT"); do
  OUT="$("$DOTNET" test "$SLN" -c Release --no-build 2>&1)"
  CODE=$?
  # Failed test names with the next lines (assertion and first stack frame), then the totals.
  grep -E -A3 "^\s+failed " <<< "$OUT" | grep -v "^--$" | head -40
  TOTALS="$(grep -E "^\s+(total|failed|succeeded|skipped):" <<< "$OUT" | tr -s ' ' | tr '\n' ' ')"
  echo "Run $run/$REPEAT: ${TOTALS:-no summary (see: $DOTNET test $SLN -c Release --no-build)}"
  [[ $CODE -ne 0 ]] && FAILED=1
done
exit $FAILED

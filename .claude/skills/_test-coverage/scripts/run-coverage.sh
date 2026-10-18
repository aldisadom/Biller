#!/usr/bin/env bash
# Run the xUnit suite with coverage and build an HTML + text report.
# Works from WSL (uses Windows dotnet.exe / reportgenerator.exe) or native Linux/CI.
#
# Usage: run-coverage.sh [--filter "<dotnet test filter>"] [--no-report]
# Outputs:
#   tests/xUnitTests/TestResults/latest/            raw results (trx + cobertura)
#   coveragereport/index.html                       HTML report
#   coveragereport/Summary.txt                      text summary (printed at end)
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
cd "$REPO_ROOT" || exit 2

FILTER=""
MAKE_REPORT=1
while [[ $# -gt 0 ]]; do
  case "$1" in
    --filter) FILTER="$2"; shift 2 ;;
    --no-report) MAKE_REPORT=0; shift ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

pick() { for c in "$@"; do command -v "$c" >/dev/null 2>&1 && { echo "$c"; return 0; }; done; return 1; }

DOTNET="$(pick dotnet dotnet.exe)" || { echo "ERROR: neither dotnet nor dotnet.exe found on PATH" >&2; exit 2; }

RESULTS_DIR="tests/xUnitTests/TestResults/latest"
rm -rf "$RESULTS_DIR"

TEST_ARGS=(test tests/xUnitTests/xUnitTests.csproj
  --collect:"XPlat Code Coverage"
  --results-directory "$RESULTS_DIR"
  --logger "trx;LogFileName=results.trx"
  --logger "console;verbosity=minimal")
[[ -n "$FILTER" ]] && TEST_ARGS+=(--filter "$FILTER")

echo ">> $DOTNET ${TEST_ARGS[*]}"
"$DOTNET" "${TEST_ARGS[@]}"
TEST_EXIT=$?

if [[ $MAKE_REPORT -eq 0 ]]; then exit $TEST_EXIT; fi

COBERTURA="$(find "$RESULTS_DIR" -name coverage.cobertura.xml 2>/dev/null | head -n1)"
if [[ -z "$COBERTURA" ]]; then
  echo "WARNING: no coverage.cobertura.xml produced (build failure?) - skipping report" >&2
  exit $TEST_EXIT
fi

RG="$(pick reportgenerator reportgenerator.exe)"
if [[ -z "$RG" ]]; then
  for p in /mnt/c/Users/*/.dotnet/tools/reportgenerator.exe; do [[ -x "$p" ]] && RG="$p" && break; done
fi
if [[ -z "$RG" ]]; then
  echo "WARNING: reportgenerator not found. Install: dotnet tool install -g dotnet-reportgenerator-globaltool" >&2
  echo "Raw coverage: $COBERTURA"
  exit $TEST_EXIT
fi

rm -rf coveragereport
# Infrastructure (Dapper repositories, DB/PDF wiring) is covered by BillioIntegrationTest, not unit tests.
"$RG" -reports:"$COBERTURA" -targetdir:coveragereport "-reporttypes:Html;TextSummary" \
  "-assemblyfilters:-Infrastructure" >/dev/null
echo
echo "================ COVERAGE SUMMARY ================"
cat coveragereport/Summary.txt
echo "=================================================="
REPORT="$REPO_ROOT/coveragereport/index.html"
# file:// URL that opens in the user's browser; on WSL point at the Windows path.
if command -v wslpath >/dev/null 2>&1 && [[ "$REPORT" == /mnt/* ]]; then
  REPORT_URL="file:///$(wslpath -m "$REPORT")"
else
  REPORT_URL="file://$REPORT"
fi
echo "HTML report: $REPORT_URL"
echo "TRX results: $REPO_ROOT/$RESULTS_DIR/results.trx"
exit $TEST_EXIT

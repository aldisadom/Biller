#!/usr/bin/env bash
# Build a Biller repo's solution and print only what matters: errors, the warning count, and the
# warnings in files changed against the base branch, so old warnings elsewhere don't hide new ones.
# Works from WSL (Windows dotnet.exe) or native Linux/CI.
#
# Usage: build.sh [repo-dir] [--release] [--base <branch>]
#   repo-dir   any Biller repo (default: this repo); a sibling name such as BillerFrontEnd also works
#   --release  build Release (default Debug)
#   --base     branch to compare against for "changed files" (default master)
# The build is --no-incremental: an incremental build skips unchanged projects and hides their warnings.
# Exit code: the build's exit code.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"

DIR="$REPO_ROOT"
CONFIG=Debug
BASE=master
while [[ $# -gt 0 ]]; do
  case "$1" in
    --release) CONFIG=Release; shift ;;
    --base) BASE="$2"; shift 2 ;;
    -*) echo "Unknown argument: $1" >&2; exit 2 ;;
    *) if [[ -d "$1" ]]; then DIR="$(cd "$1" && pwd)"; elif [[ -d "$PARENT/$1" ]]; then DIR="$PARENT/$1"; else echo "No such repo: $1" >&2; exit 2; fi; shift ;;
  esac
done

pick() { for c in "$@"; do command -v "$c" >/dev/null 2>&1 && { echo "$c"; return 0; }; done; return 1; }
DOTNET="$(pick dotnet dotnet.exe)" || { echo "ERROR: neither dotnet nor dotnet.exe found on PATH" >&2; exit 2; }

# Windows dotnet.exe can't read /mnt/... paths, so run from the repo with a relative solution path.
cd "$DIR" || exit 2
SLN="$(ls -- *.sln 2>/dev/null | head -n1)"
[[ -n "$SLN" ]] || { echo "ERROR: no solution in $DIR" >&2; exit 2; }

echo ">> $DOTNET build $SLN -c $CONFIG --no-incremental ($(git branch --show-current))"
OUT="$("$DOTNET" build "$SLN" -c "$CONFIG" -nologo --no-incremental 2>&1)"
CODE=$?

# Diagnostics as "path(line,col): kind CODE: message [project]"; make paths repo-relative with /.
normalize() { tr -d '\r' | tr '\\' '/' | sed -E 's# \[[^]]*\]$##; s#^ *[^(]*/'"$(basename "$DIR")"'/##; s#^ +##'; }
ERRORS="$(grep -E ": error [A-Za-z]+[0-9]+" <<< "$OUT" | normalize | sort -u)"
WARNINGS="$(grep -E ": warning [A-Za-z]+[0-9]+" <<< "$OUT" | normalize | sort -u)"

CHANGED="$( { git diff --name-only "$BASE"...HEAD 2>/dev/null; git diff --name-only HEAD; \
  git ls-files --others --exclude-standard; } | sort -u)"
IN_CHANGED=""
if [[ -n "$WARNINGS" && -n "$CHANGED" ]]; then
  # "file(line,col): warning" for code, "file : warning" for project files such as NU1903.
  IN_CHANGED="$(grep -F -f <(sed -e 's/$/(/' -e 'p' -e 's/($/ :/' <<< "$CHANGED") <<< "$WARNINGS")"
fi

[[ -n "$ERRORS" ]] && { echo "Errors:"; echo "$ERRORS"; }
count() { [[ -z "$1" ]] && echo 0 || wc -l <<< "$1"; }
echo "Warnings: $(count "$WARNINGS") (unique), $(count "$IN_CHANGED") in files changed against $BASE"
[[ -n "$IN_CHANGED" ]] && echo "$IN_CHANGED"
if [[ $CODE -eq 0 ]]; then echo "Build succeeded."; else echo "Build FAILED (exit $CODE)."; fi
exit $CODE

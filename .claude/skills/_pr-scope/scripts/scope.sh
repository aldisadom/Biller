#!/usr/bin/env bash
# Work out what a PR contains and what it needs, write it to scope.md in the PR state folder
# (_pre-pr/scripts/pr-state.sh) and print it: Jira key and plan, commits and uncommitted files,
# risk flags, blockers, sibling repos with the same branch, and whether integration tests are needed
# and from which BillioIntegrationTest branch CI takes them.
#
# Usage: scope.sh [repo]
#   repo  a repo folder or a sibling name such as BillerFrontEnd (default: this repo)
# Asks GitHub (git ls-remote) which repos have the branch. Changes nothing outside the state folder.
# Exit code: 0, 1 when there is a blocker, 2 for a bad argument.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"
PR_STATE="$REPO_ROOT/.claude/skills/_pre-pr/scripts/pr-state.sh"
SIBLINGS=(Biller BillerContracts BillerFrontEnd BillioIntegrationTest)

DIR="$REPO_ROOT"
case $# in
  0) ;;
  1) if [[ -d "$1/.git" || -f "$1/.git" ]]; then DIR="$(cd "$1" && pwd)"; elif [[ -d "$PARENT/$1" ]]; then DIR="$PARENT/$1"; else echo "No such repo: $1" >&2; exit 2; fi ;;
  *) echo "Usage: scope.sh [repo]" >&2; exit 2 ;;
esac
cd "$DIR" || exit 2

# The repo's name on GitHub, which is also its folder name next to Biller (a worktree folder isn't).
REPO="$(basename "$(git remote get-url origin 2>/dev/null)" .git)"
[[ -n "$REPO" ]] || REPO="$(basename "$DIR")"
BRANCH="$(git branch --show-current)"
KEY="$(grep -oE '^BIL-[0-9]+' <<< "$BRANCH" || true)"
TREE="$(bash "$PR_STATE" tree "$DIR")" || exit 2
BASE="$(bash "$PR_STATE" base "$DIR")" || exit 2
CHANGED="$(git diff --name-only "$BASE" "$TREE")"
BLOCKERS=()
RISKS=()

[[ -z "$BRANCH" || "$BRANCH" == master ]] && BLOCKERS+=("on ${BRANCH:-a detached HEAD}: a PR needs its own BIL-<n>-<slug> branch")
GIT_DIR="$(git rev-parse --absolute-git-dir)"
for f in MERGE_HEAD CHERRY_PICK_HEAD REVERT_HEAD rebase-merge rebase-apply; do
  [[ -e "$GIT_DIR/$f" ]] && BLOCKERS+=("a ${f%_HEAD} is in progress")
done

PLAN=none
if [[ -n "$KEY" ]]; then
  for d in "$DIR" "$PARENT/Biller"; do
    p="$(cd "$d" 2>/dev/null && ls docs/plans/"$KEY"-*.md 2>/dev/null | head -n1)"
    [[ -n "$p" ]] && { PLAN="$p"; [[ "$d" != "$DIR" ]] && PLAN="$p (in Biller)"; break; }
  done
fi

# Uncommitted: modified tracked files plus new files (new skill files too, which .git/info/exclude hides).
UNCOMMITTED="$(git diff --name-only HEAD "$TREE")"
EXCLUDED="$(bash "$PR_STATE" exclude "$DIR" | sed -n 's/^  //p')"

# Notes from an earlier run (decisions the user made) survive a re-run.
OLD="$(bash "$PR_STATE" dir "$DIR")/scope.md"
NOTES="$([[ -f "$OLD" ]] && sed -n '/^Notes:$/,$p' "$OLD" | sed 1d)"

# Risk flags, by path (Biller's layout; siblings mostly have none).
migrations="$(grep -E '^migrations/' <<< "$CHANGED" | xargs -r -n1 basename | paste -sd, - | sed 's/,/, /g')"
[[ -n "$migrations" ]] && RISKS+=("migrations: $migrations (pre-check queries for production data, rollback tested?)")
contracts="$(git diff -U0 "$BASE" "$TREE" -- '*.csproj' | grep -E '^[-+] *<PackageReference Include="BillerContracts' \
  | sed -E 's/.*Version="([^"]+)".*/\1/' | paste -sd' ' -)"
if [[ -n "$contracts" ]]; then
  read -r old new <<< "$contracts"
  RISKS+=("BillerContracts ${old:-?} -> ${new:-removed}")
  [[ "${new:-}" == *-* ]] && RISKS+=("BillerContracts $new is a pre-release: replace it with a released version before merging")
fi
git diff -U0 "$BASE" "$TREE" -- '*.csproj' | grep -qE '^\+ *<ProjectReference[^>]*BillerContracts' \
  && BLOCKERS+=("ProjectReference to BillerContracts: CI clones only this repo, so it can't build it")
numbering="$(grep -E '^src/Infrastructure/Repository/(Invoice|Series)Repository\.cs$' <<< "$CHANGED" | xargs -r -n1 basename | paste -sd, - | sed 's/,/, /g')"
[[ -n "$numbering" ]] && RISKS+=("invoice numbering or series: $numbering")
pdf="$(grep -E '^src/Application/(Models/Invoice/|Services/InvoiceDocument|Helpers/)' <<< "$CHANGED" | wc -l)"
(( pdf > 0 )) && RISKS+=("PDF documents or their texts: $pdf file(s) (render a sample)")
data="$(grep -E '^migrations/.*\.sql$' <<< "$CHANGED" | while read -r f; do
    [[ -f "$f" ]] && grep -qiE '^\s*(update|delete|insert)\b' "$f" && basename "$f"
  done | paste -sd, - | sed 's/,/, /g')"
[[ -n "$data" ]] && RISKS+=("data changes in $data: can the rollback restore the data?")

# Integration tests: Biller's endpoints, validators, SQL or contracts changed, or the tests themselves.
INTEGRATION=no
WHY=""
if [[ "$REPO" == Biller ]]; then
  for pair in src/WebAPI/Controllers/:controllers src/Validators/:validators src/Infrastructure/:repositories migrations/:migrations; do
    grep -q "^${pair%%:*}" <<< "$CHANGED" && WHY="${WHY:+$WHY, }${pair#*:}"
  done
  [[ -n "$contracts" ]] && WHY="${WHY:+$WHY, }contracts"
  [[ -n "$WHY" ]] && INTEGRATION=yes
elif [[ "$REPO" == BillioIntegrationTest && -n "$CHANGED" ]]; then
  INTEGRATION=yes; WHY="the tests changed"
fi

# Which repos have this branch on GitHub (CI and _sync-repos pair them by name).
OTHERS=()
IT_BRANCH=master
if [[ -n "$BRANCH" && "$BRANCH" != master ]]; then
  for r in "${SIBLINGS[@]}"; do
    [[ "$r" == "$REPO" ]] && continue
    if [[ -n "$(git ls-remote --heads "https://github.com/aldisadom/$r.git" "$BRANCH" 2>/dev/null)" ]]; then
      OTHERS+=("$r")
      [[ "$r" == BillioIntegrationTest ]] && IT_BRANCH="$BRANCH"
    fi
  done
fi
[[ "$REPO" == BillioIntegrationTest ]] && IT_BRANCH="$BRANCH"

{
  echo "# Scope: $REPO on '$BRANCH'"
  echo "Jira: ${KEY:-none}${KEY:+ (https://aldisadom.atlassian.net/browse/$KEY)}"
  echo "Plan: $PLAN"
  echo "Base: $(git log -1 --format='%h %s' "$BASE")"
  echo "Commits: $(git rev-list --count --no-merges "$BASE"..HEAD) since the base, merges not counted"
  echo "Uncommitted: $(grep -c . <<< "$UNCOMMITTED") file(s)"
  echo "Diff: $(git diff --shortstat "$BASE" "$TREE" | sed 's/^ *//')"
  echo "Same branch in: $(if [[ ${#OTHERS[@]} -gt 0 ]]; then echo "${OTHERS[*]} (one PR each)"; else echo none; fi)"
  if [[ "$INTEGRATION" == yes ]]; then
    echo "Integration tests: yes ($WHY), CI takes them from BillioIntegrationTest '$IT_BRANCH'"
  else
    echo "Integration tests: no"
  fi
  echo "Risk:"
  if [[ ${#RISKS[@]} -eq 0 ]]; then echo "- none found by path: code only"; else printf -- '- %s\n' "${RISKS[@]}"; fi
  echo "Blockers:"
  if [[ ${#BLOCKERS[@]} -eq 0 ]]; then echo "- none"; else printf -- '- %s\n' "${BLOCKERS[@]}"; fi
  if [[ -n "$UNCOMMITTED" ]]; then
    echo "Uncommitted files:"
    sed 's/^/- /' <<< "$UNCOMMITTED"
  fi
  if [[ -n "$EXCLUDED" ]]; then
    echo "Left out of the PR (pr-state.sh exclude):"
    sed 's/^/- /' <<< "$EXCLUDED"
  fi
  echo "Notes:"
  [[ -n "$NOTES" ]] && echo "$NOTES"
} | bash "$PR_STATE" write "$DIR" scope 2>/dev/null

bash "$PR_STATE" read "$DIR" scope
[[ ${#BLOCKERS[@]} -eq 0 ]]

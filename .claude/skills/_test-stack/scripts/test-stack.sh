#!/usr/bin/env bash
# A throwaway Biller stack (Postgres, migrations, API, optionally the front end) that runs next to the
# user's own billio-* stack without touching it: its own network, container names and ports.
#
# Usage: test-stack.sh <command> [options]
#   up [--biller DIR] [--frontend] [--frontend-repo DIR] [--seed]
#              start the stack from the Biller checkout DIR (default: this repo) and the front end
#              from DIR (default: ../BillerFrontEnd); --seed also runs `seed`
#   seed       add one user, seller, series, customer, item and invoice through the API and print their ids
#   rollback-check [--count N] [--table NAME]
#              roll back the last N changesets (default 1), then update again; with --table, print
#              `\d NAME` before, after the rollback, and after the update
#   status     show the stack's containers and whether the API answers
#   down       remove the containers, their volumes, the network and the images
# Ports: TEST_STACK_API_PORT (8091), TEST_STACK_DB_PORT (5439), TEST_STACK_FRONTEND_PORT (8092).
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
PARENT="$(dirname "$REPO_ROOT")"

API_PORT="${TEST_STACK_API_PORT:-8091}"
DB_PORT="${TEST_STACK_DB_PORT:-5439}"
FRONTEND_PORT="${TEST_STACK_FRONTEND_PORT:-8092}"
API_URL="http://localhost:$API_PORT"

NET=biller-test-net
DB=biller-test-db
API=biller-test-api
FRONTEND=biller-test-frontend
API_IMAGE=biller-test-api
FRONTEND_IMAGE=biller-test-frontend
LIQUIBASE_IMAGE=liquibase/liquibase:4.26.0
POSTGRES_IMAGE=postgres:16

die() { echo "ERROR: $*" >&2; exit 1; }

port_in_use() { ss -ltn 2>/dev/null | grep -qE "[:.]$1\s"; }

wait_for() { # <seconds> <command...>
  local seconds="$1"; shift
  for _ in $(seq 1 "$seconds"); do "$@" >/dev/null 2>&1 && return 0; sleep 1; done
  return 1
}

# The Biller checkout the running stack was built from, saved as a label on the database container.
stack_biller() { docker inspect -f '{{ index .Config.Labels "biller-test.biller" }}' "$DB" 2>/dev/null; }

liquibase() { # <biller dir> <liquibase command...>
  local biller="$1"; shift
  docker run --rm --network "$NET" -v "$biller/migrations:/liquibase/migrations" "$LIQUIBASE_IMAGE" \
    --changeLogFile=/migrations/dbChangeLog.yaml --url=jdbc:postgresql://db:5432/Data \
    --username=postgres --password=postgres "$@" 2>&1
}

cmd_up() {
  local biller="$REPO_ROOT" frontend=0 frontend_repo="$PARENT/BillerFrontEnd" seed=0
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --biller) biller="$(cd "$2" && pwd)" || die "no such folder: $2"; shift 2 ;;
      --frontend) frontend=1; shift ;;
      --frontend-repo) frontend_repo="$(cd "$2" && pwd)" || die "no such folder: $2"; frontend=1; shift 2 ;;
      --seed) seed=1; shift ;;
      *) die "unknown option for up: $1" ;;
    esac
  done
  [[ -f "$biller/src/WebAPI/DockerfileLocal" ]] || die "$biller is not a Biller checkout"

  if docker ps -a --format '{{.Names}}' | grep -qx "$DB\|$API\|$FRONTEND"; then
    die "the test stack is already there. Run: test-stack.sh down"
  fi
  for port in "$API_PORT" "$DB_PORT"; do
    port_in_use "$port" && die "port $port is in use. Set TEST_STACK_API_PORT / TEST_STACK_DB_PORT"
  done
  [[ $frontend -eq 1 ]] && port_in_use "$FRONTEND_PORT" && die "port $FRONTEND_PORT is in use. Set TEST_STACK_FRONTEND_PORT"

  echo ">> Biller from $biller ($(git -C "$biller" branch --show-current))"
  docker network create "$NET" >/dev/null || die "could not create network $NET"
  docker run -d --name "$DB" --network "$NET" --network-alias db --label "biller-test.biller=$biller" \
    -e POSTGRES_DB=Data -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
    -p "$DB_PORT:5432" "$POSTGRES_IMAGE" >/dev/null || die "could not start $DB"
  wait_for 60 docker exec "$DB" pg_isready -U postgres -d Data || die "Postgres did not start"

  echo ">> Migrations"
  local out
  out="$(liquibase "$biller" update)" || { echo "$out" | tail -20; die "liquibase update failed"; }
  echo "$out" | grep -E "Run:|Previously run:|Update has been successful|ERROR"

  echo ">> API image"
  docker build -q -t "$API_IMAGE" -f "$biller/src/WebAPI/DockerfileLocal" "$biller" >/dev/null \
    || die "API image build failed (run: docker build -f $biller/src/WebAPI/DockerfileLocal $biller)"
  docker run -d --name "$API" --network "$NET" -e ASPNETCORE_ENVIRONMENT=LocalDocker \
    -e ASPNETCORE_HTTP_PORTS=8070 -e ASPNETCORE_HTTPS_PORTS= -v "$biller/Fonts:/Fonts" \
    -p "$API_PORT:8070" "$API_IMAGE" >/dev/null || die "could not start $API"
  wait_for 60 curl -sf "$API_URL/health" || { docker logs --tail 30 "$API"; die "API did not answer $API_URL/health"; }
  echo "API: $API_URL (Swagger $API_URL/swagger), DB: localhost:$DB_PORT"

  if [[ $frontend -eq 1 ]]; then
    echo ">> Front end from $frontend_repo ($(git -C "$frontend_repo" branch --show-current))"
    docker build -q -t "$FRONTEND_IMAGE" -f "$frontend_repo/src/FrontEnd/Dockerfile" "$frontend_repo" >/dev/null \
      || die "front-end image build failed"
    local inner="http://$API:8070"
    docker run -d --name "$FRONTEND" --network "$NET" -e ASPNETCORE_ENVIRONMENT=Development \
      -e ASPNETCORE_HTTP_PORTS=8080 -e ASPNETCORE_HTTPS_PORTS= \
      -e "ClientsOptions__UserUrl=$inner/users" -e "ClientsOptions__SellerUrl=$inner/sellers" \
      -e "ClientsOptions__CustomerUrl=$inner/customers" -e "ClientsOptions__ItemUrl=$inner/items" \
      -e "ClientsOptions__InvoiceUrl=$inner/invoices" -e "ClientsOptions__SeriesUrl=$inner/series" \
      -p "$FRONTEND_PORT:8080" "$FRONTEND_IMAGE" >/dev/null \
      || die "could not start $FRONTEND"
    wait_for 60 curl -s -o /dev/null "http://localhost:$FRONTEND_PORT/" || die "front end did not answer"
    echo "Front end: http://localhost:$FRONTEND_PORT"
  fi

  [[ $seed -eq 1 ]] && cmd_seed
  echo "Remove it when done: bash ${BASH_SOURCE[0]} down"
}

post() { # <path> <json>; prints the new id
  curl -sf -X POST "$API_URL/$1" -H 'Content-Type: application/json' -d "$2" \
    | python3 -I -c 'import sys,json; print(json.load(sys.stdin)["id"])'
}

cmd_seed() {
  curl -sf "$API_URL/health" >/dev/null || die "the API at $API_URL does not answer. Run: test-stack.sh up"
  local tag user seller series customer item invoice today due
  tag="$(date +%s)"
  today="$(date +%F)"; due="$(date -d '+14 days' +%F)"
  user="$(post users "{\"email\":\"tyrion$tag@casterlyrock.com\",\"name\":\"Tyrion\",\"lastName\":\"Lannister\",\"password\":\"StrongPassword1!\"}")" || die "adding the user failed"
  seller="$(post sellers "{\"userId\":\"$user\",\"email\":\"seller$tag@casterlyrock.com\",\"companyName\":\"Lannister gold\",\"companyNumber\":\"123\",\"street\":\"Lion st\",\"city\":\"Casterly Rock\",\"state\":\"Westerlands\",\"phone\":\"+1\",\"bankName\":\"Iron bank\",\"bankNumber\":\"LT01\"}")" || die "adding the seller failed"
  series="$(post series "{\"sellerId\":\"$seller\",\"name\":\"WF\"}")" || die "adding the series failed"
  customer="$(post customers "{\"sellerId\":\"$seller\",\"email\":\"ned$tag@winterfell.com\",\"companyName\":\"Winterfell\",\"companyNumber\":\"456\",\"street\":\"North st\",\"city\":\"Winterfell\",\"state\":\"North\",\"phone\":\"+2\",\"seriesId\":\"$series\"}")" || die "adding the customer failed"
  item="$(post items "{\"customerId\":\"$customer\",\"name\":\"Iron throne\",\"price\":10,\"quantity\":5}")" || die "adding the item failed"
  invoice="$(post invoices "{\"userId\":\"$user\",\"sellerId\":\"$seller\",\"customerId\":\"$customer\",\"items\":[{\"id\":\"$item\",\"quantity\":2}],\"comments\":\"Lannisters pay their debts\",\"dueDate\":\"$due\",\"createdDate\":\"$today\"}")" || die "adding the invoice failed"
  echo "user=$user"
  echo "seller=$seller"
  echo "series=$series"
  echo "customer=$customer"
  echo "item=$item"
  echo "invoice=$invoice"
}

cmd_rollback_check() {
  local count=1 table=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --count) count="$2"; shift 2 ;;
      --table) table="$2"; shift 2 ;;
      *) die "unknown option for rollback-check: $1" ;;
    esac
  done
  local biller; biller="$(stack_biller)"
  [[ -n "$biller" ]] || die "the test stack is not running. Run: test-stack.sh up"
  describe() { [[ -n "$table" ]] && { echo "-- \\d $table ($1)"; docker exec "$DB" psql -U postgres -d Data -c "\\d $table" | sed -n '1,/^Indexes:/p'; }; }

  describe "before"
  echo ">> rollback-count $count"
  local out
  out="$(liquibase "$biller" rollback-count "$count")" || { echo "$out" | tail -20; die "rollback failed"; }
  echo "$out" | grep -E "Rolling Back Changeset|executed successfully|ERROR"
  describe "after the rollback"
  echo ">> update"
  out="$(liquibase "$biller" update)" || { echo "$out" | tail -20; die "update after the rollback failed"; }
  echo "$out" | grep -E "Running Changeset|Run:|Update has been successful|ERROR"
  describe "after the update"
}

cmd_status() {
  docker ps -a --filter "name=biller-test-" --format '{{.Names}}\t{{.Status}}\t{{.Ports}}'
  local biller; biller="$(stack_biller)"
  [[ -n "$biller" ]] && echo "Built from: $biller"
  if curl -sf "$API_URL/health" >/dev/null 2>&1; then echo "API: $API_URL answers"; else echo "API: $API_URL does not answer"; fi
}

cmd_down() {
  for name in "$FRONTEND" "$API" "$DB"; do
    docker ps -a --format '{{.Names}}' | grep -qx "$name" && docker rm -f -v "$name" >/dev/null && echo "removed container $name"
  done
  docker network ls --format '{{.Name}}' | grep -qx "$NET" && docker network rm "$NET" >/dev/null && echo "removed network $NET"
  for image in "$API_IMAGE" "$FRONTEND_IMAGE"; do
    docker image inspect "$image" >/dev/null 2>&1 && docker rmi "$image" >/dev/null && echo "removed image $image"
  done
  local left; left="$(docker ps -a -q --filter "name=biller-test-")"
  [[ -z "$left" ]] || die "containers are left: $left"
  echo "Test stack removed."
}

command="${1:-}"; [[ $# -gt 0 ]] && shift
case "$command" in
  up) cmd_up "$@" ;;
  seed) cmd_seed ;;
  rollback-check) cmd_rollback_check "$@" ;;
  status) cmd_status ;;
  down) cmd_down ;;
  *) sed -n '2,15p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 2 ;;
esac

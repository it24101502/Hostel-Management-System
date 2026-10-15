#!/usr/bin/env bash
# =========================================================
# SE3112 load testing - run one load profile end to end
# Owner: Prashasthi
#
#   ./testing/load/run-load.sh <profile> [extra JMeter -J flags]
#
#   profile = smoke | load | stress | spike
#   e.g.  ./testing/load/run-load.sh load
#         ./testing/load/run-load.sh load -Jhost=20.212.39.185
#
# What it does:
#   1. checks JMeter is installed and the services answer
#   2. runs JMeter in NON-GUI mode with the profile's settings
#   3. builds the HTML dashboard   -> testing/load/report/<profile>/
#   4. prints the summary table and applies the quality gate
# The services must already be running (docker compose up -d)
# and seed.sql must have been applied once.
# =========================================================
set -euo pipefail

PROFILE="${1:-smoke}"
shift || true

HERE="$(cd "$(dirname "$0")" && pwd)"
PROFILE_FILE="$HERE/profiles/$PROFILE.properties"
RUN_NAME="${RUN_NAME:-$PROFILE}"      # stress-steps.sh sets this per step
RESULTS_DIR="$HERE/report"          # git-ignored by the root .gitignore
REPORT_DIR="$HERE/report/$RUN_NAME"
JTL="$RESULTS_DIR/$RUN_NAME.jtl"
HOST="localhost"

for arg in "$@"; do
  case "$arg" in -Jhost=*) HOST="${arg#-Jhost=}" ;; esac
done

if [[ ! -f "$PROFILE_FILE" ]]; then
  echo "Unknown profile '$PROFILE'. Use one of: $(ls "$HERE/profiles" | sed 's/.properties//' | tr '\n' ' ')"
  exit 2
fi

if ! command -v jmeter >/dev/null 2>&1; then
  echo "jmeter is not on PATH (Mac: brew install jmeter)"
  exit 2
fi

# Fail fast if a service is down: a load test against a dead
# service only measures "connection refused".
echo "Checking services on $HOST ..."
for port in 8080 8081 8082 8083 8084; do
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "http://$HOST:$port/" || true)
  if [[ "$code" == "000" ]]; then
    echo "  port $port: NOT RESPONDING - start the stack first (docker compose up -d)"
    exit 2
  fi
  echo "  port $port: up (HTTP $code)"
done

# Optional: wipe the leave rows left by earlier runs so every
# run starts from the same data (RESET_DB=1 ./run-load.sh load).
if [[ "${RESET_DB:-0}" == "1" ]]; then
  echo "Resetting load-test leave rows ..."
  docker exec -i hostel-mysql sh -c 'mysql -uroot -p"$MYSQL_ROOT_PASSWORD" 2>/dev/null' \
    < "$HERE/reset-leave-data.sql"
fi

# JMeter refuses to write a dashboard into a non-empty folder,
# and an old .jtl would be appended to, so start clean.
mkdir -p "$RESULTS_DIR"
rm -rf "$REPORT_DIR" "$JTL"

echo
echo "Running profile '$PROFILE':"
grep -v '^#' "$PROFILE_FILE" | sed '/^$/d; s/^/  /'
echo

jmeter -n \
  -t "$HERE/leave-load.jmx" \
  -q "$PROFILE_FILE" \
  -l "$JTL" \
  -j "$RESULTS_DIR/$RUN_NAME-jmeter.log" \
  -e -o "$REPORT_DIR" \
  "$@"

SLA_MS=$(grep '^sla_ms=' "$PROFILE_FILE" | cut -d= -f2)
echo
echo "================ SUMMARY ($RUN_NAME) ================"
STATUS=0
python3 "$HERE/summarize.py" "$REPORT_DIR" "$JTL" \
  --sla-ms "${SLA_MS:-3000}" \
  --markdown "$RESULTS_DIR/$RUN_NAME-summary.md" || STATUS=$?

echo
echo "HTML dashboard: $REPORT_DIR/index.html"
exit $STATUS

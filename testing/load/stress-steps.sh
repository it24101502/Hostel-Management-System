#!/usr/bin/env bash
# =========================================================
# SE3112 load testing - step-stress to find the breaking point
# Owner: Prashasthi
#
#   ./testing/load/stress-steps.sh [levels...]
#   default levels: 25 50 100 200 400 concurrent students
#
# Runs the "stress" profile once per level (60 s each, data
# reset between steps) and prints one line per level, so you
# can see the exact point where p95 crosses the 3 s target
# (NFR-01) or errors start to appear.
# =========================================================
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
LEVELS=("$@")
[[ ${#LEVELS[@]} -eq 0 ]] && LEVELS=(25 50 100 200 400)

OUT="$HERE/report/stress-steps.md"
mkdir -p "$HERE/report"
{
  echo "| Students | Samples | Avg ms | p95 ms | p99 ms | Req/s | Error % | p95 within 3 s? |"
  echo "|---|---|---|---|---|---|---|---|"
} > "$OUT"

for users in "${LEVELS[@]}"; do
  echo
  echo "########## STEP: $users concurrent students ##########"
  RUN_NAME="step-$users" RESET_DB="${RESET_DB:-1}" \
    "$HERE/run-load.sh" stress \
      -Jstudent_users="$users" \
      -Jlogin_users=$(( users / 10 > 0 ? users / 10 : 1 )) \
      -Jramp_up=20 -Jduration=60 || true

  python3 - "$HERE/report/step-$users/statistics.json" "$users" >> "$OUT" <<'PY'
import json, sys
t = json.load(open(sys.argv[1]))["Total"]
ok = "yes" if t["pct2ResTime"] <= 3000 else "**NO**"
print(f'| {sys.argv[2]} | {t["sampleCount"]} | {t["meanResTime"]:.0f} | '
      f'{t["pct2ResTime"]:.0f} | {t["pct3ResTime"]:.0f} | '
      f'{t["throughput"]:.1f} | {t["errorPct"]:.2f} | {ok} |')
PY
done

echo
echo "================ STEP-STRESS RESULTS ================"
cat "$OUT"
echo
echo "Saved to $OUT"

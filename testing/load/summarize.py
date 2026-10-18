#!/usr/bin/env python3
"""
SE3112 load testing - results summary and quality gate.
Owner: Prashasthi

Reads the JMeter outputs of one run and:
  1. prints a per-endpoint table (samples, avg, p90, p95, p99,
     throughput, error %) using the numbers from the HTML
     dashboard's statistics.json, so they match the dashboard;
  2. splits failures into "functional" (wrong status code,
     timeout, connection refused) and "SLA" (correct response,
     but slower than NFR-01) using results.jtl;
  3. applies a quality gate: every endpoint's p95 must be within
     the SLA and its functional error rate within the limit.
     Exit code 1 = gate failed, so the CI job goes red.

Usage:
  python3 summarize.py <report_dir> <results.jtl> [--sla-ms 3000]
                       [--max-error-pct 1.0] [--markdown out.md]
"""
import argparse
import csv
import json
import os
import sys
from collections import defaultdict

SLA_FAILURE_TEXT = "The operation lasted too long"


def load_stats(report_dir):
    path = os.path.join(report_dir, "statistics.json")
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def split_failures(jtl_path):
    """Return {label: {"functional": n, "sla": n}} from the raw JTL."""
    counts = defaultdict(lambda: {"functional": 0, "sla": 0})
    with open(jtl_path, newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            if row.get("success", "true").lower() == "true":
                continue
            message = row.get("failureMessage") or ""
            code = row.get("responseCode") or ""
            # A row can fail BOTH ways; a wrong status code wins
            # because that is the more serious problem.
            expected_ok = code.startswith("2")
            if SLA_FAILURE_TEXT in message and expected_ok:
                counts[row["label"]]["sla"] += 1
            else:
                counts[row["label"]]["functional"] += 1
    return counts


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("report_dir")
    parser.add_argument("jtl")
    parser.add_argument("--sla-ms", type=float, default=3000)
    parser.add_argument("--max-error-pct", type=float, default=1.0)
    parser.add_argument("--markdown", help="also write a Markdown table here")
    args = parser.parse_args()

    stats = load_stats(args.report_dir)
    failures = split_failures(args.jtl)

    header = ("Endpoint", "Samples", "Avg ms", "p90 ms", "p95 ms",
              "p99 ms", "Req/s", "Func err %", "SLA miss %", "Gate")
    rows = []
    gate_failed = False

    labels = sorted(label for label in stats if label != "Total")
    for label in labels + ["Total"]:
        s = stats[label]
        samples = s["sampleCount"] or 1
        f = failures.get(label, {"functional": 0, "sla": 0})
        if label == "Total":
            f = {
                "functional": sum(v["functional"] for v in failures.values()),
                "sla": sum(v["sla"] for v in failures.values()),
            }
        func_pct = 100.0 * f["functional"] / samples
        sla_pct = 100.0 * f["sla"] / samples

        # setUp logins run once and are not part of the SLA.
        gated = label != "Total" and not label.startswith("SETUP")
        verdict = "-"
        if gated:
            ok = s["pct2ResTime"] <= args.sla_ms and func_pct <= args.max_error_pct
            verdict = "PASS" if ok else "FAIL"
            gate_failed = gate_failed or not ok

        rows.append((
            label, s["sampleCount"],
            f'{s["meanResTime"]:.0f}', f'{s["pct1ResTime"]:.0f}',
            f'{s["pct2ResTime"]:.0f}', f'{s["pct3ResTime"]:.0f}',
            f'{s["throughput"]:.1f}', f"{func_pct:.2f}", f"{sla_pct:.2f}",
            verdict,
        ))

    widths = [max(len(str(r[i])) for r in rows + [header]) for i in range(len(header))]
    line = "  ".join(h.ljust(w) for h, w in zip(header, widths))
    print(line)
    print("-" * len(line))
    for r in rows:
        print("  ".join(str(c).ljust(w) for c, w in zip(r, widths)))

    print()
    print(f"Quality gate: p95 <= {args.sla_ms:.0f} ms (NFR-01) and "
          f"functional errors <= {args.max_error_pct}% for every endpoint")
    print("RESULT:", "FAILED" if gate_failed else "PASSED")

    if args.markdown:
        with open(args.markdown, "w", encoding="utf-8") as md:
            md.write("| " + " | ".join(header) + " |\n")
            md.write("|" + "---|" * len(header) + "\n")
            for r in rows:
                md.write("| " + " | ".join(str(c) for c in r) + " |\n")
            md.write(f"\n**Quality gate:** p95 ≤ {args.sla_ms:.0f} ms and functional "
                     f"errors ≤ {args.max_error_pct}% → "
                     f"**{'FAILED' if gate_failed else 'PASSED'}**\n")

    sys.exit(1 if gate_failed else 0)


if __name__ == "__main__":
    main()

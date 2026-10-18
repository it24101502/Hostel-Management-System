import json
import sys
import time
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent
ARTIFACTS = ROOT / "artifacts"
ARTIFACTS.mkdir(exist_ok=True)


class ScreenshotResult(unittest.TextTestResult):
    def capture(self, test):
        driver = getattr(test, "driver", None)
        if driver:
            try:
                filename = test.id().replace(".", "_") + ".png"
                driver.save_screenshot(str(ARTIFACTS / filename))
            except Exception as error:
                print(f"Screenshot unavailable: {error}")

    def addFailure(self, test, err):
        self.capture(test)
        super().addFailure(test, err)

    def addError(self, test, err):
        self.capture(test)
        super().addError(test, err)

    def addSubTest(self, test, subtest, err):
        if err is not None:
            self.capture(test)
        super().addSubTest(test, subtest, err)


def main():
    suite = unittest.defaultTestLoader.discover(
        str(ROOT), pattern="test_*.py"
    )
    started = time.perf_counter()

    result = unittest.TextTestRunner(
        verbosity=2,
        resultclass=ScreenshotResult,
    ).run(suite)

    report = {
        "successful": result.wasSuccessful(),
        "tests_run": result.testsRun,
        "failure_entries": len(result.failures),
        "error_entries": len(result.errors),
        "skipped": len(result.skipped),
        "duration_seconds": round(time.perf_counter() - started, 2),
        "failures": [
            {"test": str(test), "traceback": details}
            for test, details in result.failures
        ],
        "errors": [
            {"test": str(test), "traceback": details}
            for test, details in result.errors
        ],
    }

    report_path = ARTIFACTS / "report.json"
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"\nReport: {report_path}")

    return 0 if result.wasSuccessful() else 1


if __name__ == "__main__":
    sys.exit(main())
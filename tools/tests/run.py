#!/usr/bin/env python3
"""Runs every tools test (`test_*.py` here), standard library only.

    python3 tools/tests/run.py           # all of them
    python3 tools/tests/run.py l10n      # the ones whose file name contains "l10n"
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent


def main(argv: list[str]) -> int:
    sys.path.insert(0, str(HERE.parent))
    sys.path.insert(0, str(HERE))
    loader = unittest.TestLoader()
    suite = unittest.TestSuite()
    for path in sorted(HERE.glob("test_*.py")):
        if argv and not any(word in path.stem for word in argv):
            continue
        try:
            suite.addTests(loader.loadTestsFromName(path.stem))
        except unittest.SkipTest as skip:  # a file that can't run on this OS (test_vm on Windows)
            suite.addTest(skipped(path.stem, str(skip)))
    result = unittest.TextTestRunner(verbosity=1).run(suite)
    return 0 if result.wasSuccessful() else 1


def skipped(module: str, reason: str) -> unittest.TestCase:
    """One skipped test standing for a test file that skipped itself as it loaded, so the run reports it and why."""
    case = type(module, (unittest.TestCase,), {"test_file": lambda self: self.skipTest(reason)})
    return case("test_file")


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

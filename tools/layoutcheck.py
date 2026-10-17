#!/usr/bin/env python3
"""What the device layout checkers (check-layout-ios.py, check-layout-android.py) share: a progress line from any of
their threads, and the report they end with. Not a command of its own."""

from __future__ import annotations

import sys
import threading
import time
from pathlib import Path

PRINT_LOCK = threading.Lock()


def log(message: str) -> None:
    """A progress line on stderr, whole even when several threads log at once."""
    with PRINT_LOCK:
        print(message, file=sys.stderr, flush=True)


def report(results: dict[tuple[str, str], list[str]], platform: str, devices: list[str], languages: list[str],
           out_root: Path, started: float) -> int:
    """Prints each device and language's problems (`== <platform>-<device> <language>`, "not run" for one missing
    from `results`), then which failed or that every screen fits, with the minutes since `started` (a
    `time.monotonic()`); 1 when any failed."""
    failed = []
    for device in devices:
        for language in languages:
            problems = results.get((device, language), ["not run"])
            print(f"== {platform}-{device} {language}")
            for problem in problems:
                print(f"  {problem}")
            if problems:
                failed.append(f"{platform}-{device}/{language}")
    minutes = (time.monotonic() - started) / 60
    if failed:
        print(f"Layout problems in: {' '.join(failed)} (shots in {out_root}, {minutes:.1f} min)", file=sys.stderr)
        return 1
    print(f"Every screen fits on {', '.join(devices)} in: {' '.join(languages)} ({minutes:.1f} min)")
    return 0

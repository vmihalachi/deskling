#!/usr/bin/env python3
"""Renders the iPhone and iPad app's screens in each language on several simulators and fails when one doesn't fit:
content taller than the screen outside a scroll view, text cut short or pushed off the screen, a screen that couldn't
be drawn (ScreenshotCapture's checks, `LayoutCheck+iOS.swift`). The iOS twin of check-layout.sh.

    python3 laching/tools/check-layout-ios.py [--devices se,promax,...] [--only name,name] [--jobs N]
        [--text-size SIZE] [--app PATH] [--out DIR] [--keep] [language ...]

Defaults: every device below, every language in languages.all, LAYOUT_JOBS (else 4; at least one per device type)
simulators at a time. Shots and problems go to <mac.dir>/build/layout-check/ios-<device>/<language>. It builds the Debug
app for the Simulator once (`mac.iosScheme`; --app runs one already built), then clones a simulator per job from a
template it keeps ("<name> layout: <device type>", made once with accessibility on, which the text checks read), boots
the clones, and runs the languages on them side by side, each one launch of the app in screenshot mode
(`mac.screenshotEnv`) with -AppleLanguages and -AppleLocale: no reboot between languages. The clones are deleted at the
end (--keep leaves them).

Devices: se (iPhone SE 3rd generation, the smallest), promax (the largest iPhone), ipad (iPad Pro 13" upright),
ipad-landscape (the same turned, drawn at the turned screen's size: iPadOS won't turn a windowed app), ipad-split (a
375 pt third of the screen, compact as in Split View), duo (iPhone Duo, closed: nothing opens it from the command
line). --text-size sets Dynamic Type on every clone (`xcrun simctl ui <device> content_size`), such as
accessibility-extra-extra-extra-large, to see what the largest text breaks. A SIMCTL_CHILD_<NAME> variable in the
environment reaches the app as <NAME>. Leftover clones of a run that was killed are deleted by the next run; the
templates stay (`xcrun simctl delete <udid>` removes one).
"""

import argparse
import json
import os
import plistlib
import queue
import re
import shutil
import subprocess
import sys
import threading
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import companion  # noqa: E402

TYPE_PREFIX = "com.apple.CoreSimulator.SimDeviceType."
# Device: the simulator types to use, the first one a runtime supports, and how the app shows its window.
DEVICES = {
    "se": {"types": ["iPhone-SE-3rd-generation"]},
    "promax": {"types": ["iPhone-18-Pro-Max", "iPhone-17-Pro-Max", "iPhone-16-Pro-Max"]},
    "ipad": {"types": ["iPad-Pro-13-inch-M5-12GB", "iPad-Pro-13-inch-M4-8GB"]},
    "ipad-landscape": {"types": ["iPad-Pro-13-inch-M5-12GB", "iPad-Pro-13-inch-M4-8GB"], "orientation": "landscape"},
    "ipad-split": {"types": ["iPad-Pro-13-inch-M5-12GB", "iPad-Pro-13-inch-M4-8GB"], "width": 375},
    "duo": {"types": ["iPhone-Duo"]},
}
LAUNCH_TIMEOUT = 600  # seconds for one language's run
PRINT_LOCK = threading.Lock()


def log(message: str) -> None:
    with PRINT_LOCK:
        print(message, file=sys.stderr, flush=True)


def simctl(*args: str, timeout: float = 300, check: bool = True) -> str:
    result = subprocess.run(["xcrun", "simctl", *args], capture_output=True, text=True, timeout=timeout)
    if check and result.returncode != 0:
        raise RuntimeError(f"simctl {' '.join(args)}: {(result.stderr or result.stdout).strip()}")
    return result.stdout


def simulators() -> list[dict]:
    """Every simulator, with its runtime."""
    found = []
    for runtime, devices in json.loads(simctl("list", "-j", "devices"))["devices"].items():
        found += [dict(device, runtime=runtime) for device in devices]
    return found


def pick_type(device: str, runtimes: list[dict]) -> tuple[str, dict]:
    """The first of `device`'s simulator types an installed runtime supports, and the newest such runtime."""
    for short in DEVICES[device]["types"]:
        supporting = [r for r in runtimes if any(t["identifier"] == TYPE_PREFIX + short for t in r["supportedDeviceTypes"])]
        if supporting:
            return short, max(supporting, key=lambda r: [int(n) for n in re.findall(r"\d+", r["version"])])
    sys.exit(f"check-layout-ios: no installed iOS runtime has a simulator for {device} ({', '.join(DEVICES[device]['types'])})")


def accessibility_on(udid: str) -> None:
    """The text checks read fonts from the accessibility tree, which apps build only with accessibility on."""
    for key in ("ApplicationAccessibilityEnabled", "AccessibilityEnabled"):
        simctl("spawn", udid, "defaults", "write", "com.apple.Accessibility", key, "-bool", "true")


def boot(udid: str) -> None:
    simctl("boot", udid, check=False)  # already booted is fine
    simctl("bootstatus", udid, "-b", timeout=600)


def template(app_name: str, short: str, runtime: dict) -> str:
    """The template simulator for a type: made once (booted, accessibility on, shut down), kept for later runs."""
    name = f"{app_name} layout: {short}"
    for device in simulators():
        if device["name"] == name and device["runtime"] == runtime["identifier"] and device["isAvailable"]:
            if device["state"] != "Shutdown":
                simctl("shutdown", device["udid"], check=False)
            return device["udid"]
    log(f"Making the {name} simulator ({runtime['name']}), once")
    udid = simctl("create", name, TYPE_PREFIX + short, runtime["identifier"]).strip()
    boot(udid)
    accessibility_on(udid)
    simctl("shutdown", udid)
    return udid


def clean_stale_clones(app_name: str) -> None:
    """Deletes the clones of runs that are no longer running (killed before they could)."""
    pattern = re.compile(rf"^{re.escape(app_name)} layout clone .* \((\d+)\)$")
    for device in simulators():
        match = pattern.match(device["name"])
        if not match:
            continue
        try:
            os.kill(int(match.group(1)), 0)
            continue  # that run is still going
        except ProcessLookupError:
            pass
        except PermissionError:
            continue
        simctl("shutdown", device["udid"], check=False)
        simctl("delete", device["udid"], check=False)


def build(config: companion.Config) -> Path:
    mac_dir = config.mac_dir
    derived = mac_dir / "build" / "ios-layout.noindex"
    log("Building the iOS app (Debug, Simulator)")
    result = subprocess.run(
        ["xcodebuild", "-project", config.require("mac.project"), "-scheme", config.require("mac.iosScheme"),
         "-configuration", "Debug", "-destination", "generic/platform=iOS Simulator", "-derivedDataPath", str(derived),
         "build", "-quiet"],
        cwd=mac_dir, capture_output=True, text=True)
    if result.returncode != 0:
        sys.exit(f"check-layout-ios: the build failed\n{result.stdout[-4000:]}{result.stderr[-4000:]}")
    apps = sorted((derived / "Build" / "Products" / "Debug-iphonesimulator").glob("*.app"))
    if not apps:
        sys.exit("check-layout-ios: the build made no .app")
    named = [a for a in apps if a.stem == config.get("mac.app")]
    return (named or apps)[0]


def run_language(udid: str, bundle: str, device: str, language: str, only: str, env_name: str, out: Path) -> list[str]:
    """One launch in screenshot mode: copies the shots and problems.txt to `out`, returns the problems."""
    env = dict(os.environ)
    env[f"SIMCTL_CHILD_{env_name}"] = only or "all"
    if orientation := DEVICES[device].get("orientation"):
        env["SIMCTL_CHILD_LACHING_SCREENSHOT_ORIENTATION"] = orientation
    if width := DEVICES[device].get("width"):
        env["SIMCTL_CHILD_LACHING_SCREENSHOT_WIDTH"] = str(width)
    command = ["xcrun", "simctl", "launch", "--console-pty", "--terminate-running-process", udid, bundle,
               "-AppleLanguages", f"({language})", "-AppleLocale", language]
    try:
        result = subprocess.run(command, env=env, capture_output=True, text=True, timeout=LAUNCH_TIMEOUT)
    except subprocess.TimeoutExpired:
        try:  # a simulator too busy to launch may be too busy to answer this
            simctl("terminate", udid, bundle, timeout=60, check=False)
        except subprocess.TimeoutExpired:
            pass
        return [f"the run didn't finish in {LAUNCH_TIMEOUT} s"]
    # The app renders into its container's temp folder and prints that path last.
    lines = [line.strip() for line in result.stdout.replace("\r", "").splitlines()]
    folders = [Path(line) for line in lines if line.startswith("/") and Path(line).is_dir()]
    if not folders:
        tail = "\n".join(lines[-5:])
        return [f"no screenshots were rendered (exit {result.returncode}){': ' + tail if tail else ''}"]
    source = folders[-1]
    out.mkdir(parents=True, exist_ok=True)
    for old in list(out.glob("*.png")) + [out / "problems.txt"]:
        old.unlink(missing_ok=True)
    shots = sorted(source.glob("*.png"))
    for shot in shots:
        shutil.copy2(shot, out / shot.name)
    problems_file = source / "problems.txt"
    problems = problems_file.read_text(encoding="utf-8").splitlines() if problems_file.exists() else []
    if problems:
        shutil.copy2(problems_file, out / "problems.txt")
    shutil.rmtree(source, ignore_errors=True)
    if not shots:
        problems.append(f"no shots matched --only {only}" if only else "no shots were written")
    return [p for p in problems if p.strip()]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0], formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("languages", nargs="*", help="default: languages.all")
    parser.add_argument("--devices", default=",".join(DEVICES), help=f"comma-separated, of: {', '.join(DEVICES)}")
    parser.add_argument("--only", default="", help="just the shots whose names start with one of these (comma-separated)")
    parser.add_argument("--jobs", type=int, default=int(os.environ.get("LAYOUT_JOBS", "4")), help="simulators at a time")
    parser.add_argument("--text-size", help="Dynamic Type on every simulator, as `simctl ui content_size` names it")
    parser.add_argument("--app", type=Path, help="a Debug Simulator build of the app to run instead of building one")
    parser.add_argument("--out", type=Path, help="default: <mac.dir>/build/layout-check")
    parser.add_argument("--keep", action="store_true", help="leave the clones, booted")
    args = parser.parse_args()

    config = companion.load()
    app_name = config.require("name")
    env_name = config.require("mac.screenshotEnv")
    languages = args.languages or config.languages
    devices = [d.strip() for d in args.devices.split(",") if d.strip()]
    if unknown := [d for d in devices if d not in DEVICES]:
        sys.exit(f"check-layout-ios: unknown device {', '.join(unknown)} (one of {', '.join(DEVICES)})")
    out_root = args.out or config.work_dir / "layout-check"
    started = time.monotonic()

    app = args.app.resolve() if args.app else build(config)
    bundle = plistlib.loads((app / "Info.plist").read_bytes())["CFBundleIdentifier"]

    runtimes = [r for r in json.loads(simctl("list", "-j", "runtimes"))["runtimes"]
                if r.get("isAvailable") and r.get("platform", "iOS") == "iOS"]
    clean_stale_clones(app_name)
    # Devices on the same simulator type share its clones: the iPad's three ways are launches of one app.
    types: dict[str, dict] = {}
    for device in devices:
        short, runtime = pick_type(device, runtimes)
        entry = types.setdefault(short, {"runtime": runtime, "tasks": queue.Queue(), "count": 0})
        for language in languages:
            entry["tasks"].put((device, language))
            entry["count"] += 1
    # Clones per type in proportion to its runs, at least one each.
    total = sum(t["count"] for t in types.values())
    jobs = max(args.jobs, len(types))
    for t in types.values():
        t["workers"] = max(1, round(jobs * t["count"] / total))
    while sum(t["workers"] for t in types.values()) > jobs:
        busiest = max((t for t in types.values() if t["workers"] > 1), key=lambda t: t["workers"] / t["count"])
        busiest["workers"] -= 1
    for short, t in types.items():
        t["workers"] = min(t["workers"], t["count"])
        t["template"] = template(app_name, short, t["runtime"])

    results: dict[tuple[str, str], list[str]] = {}
    clones: list[str] = []
    clones_lock = threading.Lock()

    def worker(short: str, number: int) -> None:
        entry = types[short]
        try:
            name = f"{app_name} layout clone {short} #{number} ({os.getpid()})"
            udid = simctl("clone", entry["template"], name).strip()
            with clones_lock:
                clones.append(udid)
            boot(udid)
            accessibility_on(udid)
            if args.text_size:
                simctl("ui", udid, "content_size", args.text_size)
            simctl("install", udid, str(app), timeout=600)
        except Exception as error:  # noqa: BLE001 - the runs it would have made report it
            log(f"check-layout-ios: the {short} simulator #{number} didn't start ({error})")
            while True:
                try:
                    device, language = entry["tasks"].get_nowait()
                except queue.Empty:
                    return
                # Another clone of the type may still take it; this one can't.
                if entry["workers"] > 1:
                    entry["tasks"].put((device, language))
                    entry["workers"] -= 1
                    return
                results[(device, language)] = [f"the simulator didn't start ({error})"]
        while True:
            try:
                device, language = entry["tasks"].get_nowait()
            except queue.Empty:
                return
            began = time.monotonic()
            try:
                problems = run_language(udid, bundle, device, language, args.only, env_name,
                                        out_root / f"ios-{device}" / language)
            except Exception as error:  # noqa: BLE001
                problems = [f"the run failed ({error})"]
            results[(device, language)] = problems
            log(f"  ios-{device} {language}: {len(problems)} problem(s), {time.monotonic() - began:.0f} s")

    threads = [threading.Thread(target=worker, args=(short, n + 1), daemon=True)
               for short, t in types.items() for n in range(t["workers"])]
    log(f"Running {total} launch(es) on {len(threads)} simulator(s): {', '.join(devices)} × {' '.join(languages)}")
    try:
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join()
    finally:
        if not args.keep:
            for udid in clones:
                simctl("shutdown", udid, check=False)
                simctl("delete", udid, check=False)

    failed = []
    for device in devices:
        for language in languages:
            problems = results.get((device, language), ["not run"])
            print(f"== ios-{device} {language}")
            for problem in problems:
                print(f"  {problem}")
            if problems:
                failed.append(f"ios-{device}/{language}")
    minutes = (time.monotonic() - started) / 60
    if failed:
        print(f"Layout problems in: {' '.join(failed)} (shots in {out_root}, {minutes:.1f} min)", file=sys.stderr)
        return 1
    print(f"Every screen fits on {', '.join(devices)} in: {' '.join(languages)} ({minutes:.1f} min)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

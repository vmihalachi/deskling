#!/bin/sh
# Renders the Mac app's windows in light and dark to PNGs, with sample state and throwaway settings: a Debug build run
# in screenshot mode (Deskling's ScreenshotCapture, started by `mac.screenshotEnv`). Your real settings aren't touched.
# Usage: deskling/tools/screenshots.sh [--only name,name] [--hidden|--front] [--no-build] [output-dir] [language]
#        (defaults: screenshots.out, the source language)
#   --only      just the shots whose file names start with one of the names, for checking a change. Pair it with a
#               scratch output-dir so the committed screenshots stay as they are. Implies --hidden.
#   --hidden    the windows open transparent, behind everything and without taking the focus, so you can keep working;
#               the shots show them inactive (grey traffic lights and controls). For checks, never the committed shots.
#   --front     the windows come forward and are shot key, as in use (the default without --only); leave the Mac alone.
#   --no-build  run the last build (check-layout.sh builds once, then runs the languages side by side).
#   --build-only  just build.
# Fails, after copying the shots, when the run reports problems (ScreenshotCapture's problems.txt, left next to the
# shots): a window that couldn't be drawn, a shot that wasn't written, content taller than its window. check-layout.sh
# runs it per language.
set -e
ONLY=""
HIDDEN=""
BUILD=1
RUN=1
while [ $# -gt 0 ]; do
  case "$1" in
    --only) ONLY="$2"; [ -n "$HIDDEN" ] || HIDDEN=1; shift 2 ;;
    --hidden) HIDDEN=1; shift ;;
    --front) HIDDEN=0; shift ;;
    --no-build) BUILD=""; shift ;;
    --build-only) RUN=""; shift ;;
    *) break ;;
  esac
done
[ "$HIDDEN" = 1 ] || HIDDEN=0
TOOLS=$(cd "$(dirname "$0")" && pwd)
get() { python3 "$TOOLS/companion.py" get "$1"; }
ROOT=$(python3 "$TOOLS/companion.py" root)
OUT="${1:-$ROOT/$(get screenshots.out)}"
LANG_CODE="${2:-$(get languages.source)}"
APP=$(get mac.app)
ENV_NAME=$(get mac.screenshotEnv)
cd "$ROOT/$(get mac.dir)"
DERIVED="build/screenshots.noindex"

if [ -n "$BUILD" ]; then
  xcodebuild -project "$(get mac.project)" -scheme "$(get mac.scheme)" -configuration Debug \
    -destination 'platform=macOS' -derivedDataPath "$DERIVED" build -quiet 2>&1 | grep -v 'warning:' || true
fi
[ -n "$RUN" ] || exit 0
# A sandboxed app renders into its container's temp folder and prints that path last.
SRC=$(env "$ENV_NAME=${ONLY:-all}" DESKLING_SCREENSHOT_HIDDEN="$HIDDEN" "$DERIVED/Build/Products/Debug/$APP.app/Contents/MacOS/$APP" \
  -AppleLanguages "($LANG_CODE)" -AppleLocale "$LANG_CODE" | tail -1)
[ -d "$SRC" ] || { echo "No screenshots were rendered" >&2; exit 1; }
mkdir -p "$OUT"
rm -f "$OUT/problems.txt"
COUNT=0
for file in "$SRC"/*.png; do
  [ -e "$file" ] || continue
  name=$(basename "$file")
  cp "$file" "$OUT"/
  echo "$OUT/$name"
  COUNT=$((COUNT + 1))
done
[ "$COUNT" -gt 0 ] || { echo "No screenshots matched --only $ONLY" >&2; exit 1; }
echo "$COUNT screenshots in $OUT"
PROBLEMS=""
if [ -s "$SRC/problems.txt" ]; then
  PROBLEMS=$(sed 's/^/  /' "$SRC/problems.txt")
  cp "$SRC/problems.txt" "$OUT/problems.txt"
fi
# Each run renders into a folder of its own in the app's temporary folder; one named by DESKLING_SCREENSHOT_DIR stays.
[ -n "$DESKLING_SCREENSHOT_DIR" ] || rm -rf "$SRC"
if [ -n "$PROBLEMS" ]; then
  printf 'Problems (%s):\n%s\n' "$LANG_CODE" "$PROBLEMS" >&2
  exit 1
fi

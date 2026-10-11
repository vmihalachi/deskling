#!/bin/sh
# Renders the Mac app's windows in light and dark to PNGs, with sample state and throwaway settings: a Debug build run
# in screenshot mode (Deskling's ScreenshotCapture, started by `mac.screenshotEnv`). Your real settings aren't touched.
# Usage: deskling/tools/screenshots.sh [--only name,name] [output-dir] [language]   (defaults: screenshots.out, the
#        source language)
#   --only   just the shots whose file names start with one of the names, for checking a change. Pair it with a
#            scratch output-dir so the committed screenshots stay as they are.
set -e
ONLY=""
if [ "$1" = "--only" ]; then
  ONLY="$2"
  shift 2
fi
TOOLS=$(cd "$(dirname "$0")" && pwd)
get() { python3 "$TOOLS/companion.py" get "$1"; }
ROOT=$(python3 "$TOOLS/companion.py" root)
OUT="${1:-$ROOT/$(get screenshots.out)}"
LANG_CODE="${2:-$(get languages.source)}"
APP=$(get mac.app)
ENV_NAME=$(get mac.screenshotEnv)
cd "$ROOT/$(get mac.dir)"
DERIVED="build/screenshots.noindex"

xcodebuild -project "$(get mac.project)" -scheme "$(get mac.scheme)" -configuration Debug \
  -destination 'platform=macOS' -derivedDataPath "$DERIVED" build -quiet 2>&1 | grep -v 'warning:' || true
# A sandboxed app renders into its container's temp folder and prints that path last.
SRC=$(env "$ENV_NAME=${ONLY:-all}" "$DERIVED/Build/Products/Debug/$APP.app/Contents/MacOS/$APP" \
  -AppleLanguages "($LANG_CODE)" -AppleLocale "$LANG_CODE" | tail -1)
[ -d "$SRC" ] || { echo "No screenshots were rendered" >&2; exit 1; }
mkdir -p "$OUT"
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

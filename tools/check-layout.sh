#!/bin/sh
# Renders the Mac app's windows in each language and fails when one doesn't fit: content taller than its window, a
# window that couldn't be drawn, a shot that wasn't written (the checks screenshots.sh fails on). Longer translations
# are where a window runs out of room, so run it after changing text or a window's layout.
# Usage: deskling/tools/check-layout.sh [--only name,name] [language ...]   (default: every language in languages.all)
# Shots go to <mac.dir>/build/layout-check/<language>, to look at what failed. Windows appear on screen for each
# language; leave the Mac alone until it's done.
TOOLS=$(cd "$(dirname "$0")" && pwd)
get() { python3 "$TOOLS/companion.py" get "$1"; }
ROOT=$(python3 "$TOOLS/companion.py" root)
ONLY=""
if [ "$1" = "--only" ]; then
  ONLY="$2"
  shift 2
fi
# shellcheck disable=SC2046
[ $# -gt 0 ] || set -- $(get languages.all | python3 -c 'import json, sys; print(" ".join(json.load(sys.stdin)))')
OUT="$ROOT/$(get mac.dir)/build/layout-check"
FAILED=""
for lang in "$@"; do
  echo "== $lang"
  if [ -n "$ONLY" ]; then
    "$TOOLS/screenshots.sh" --only "$ONLY" "$OUT/$lang" "$lang" > /dev/null || FAILED="$FAILED $lang"
  else
    "$TOOLS/screenshots.sh" "$OUT/$lang" "$lang" > /dev/null || FAILED="$FAILED $lang"
  fi
done
if [ -n "$FAILED" ]; then
  echo "Layout problems in:$FAILED (shots in $OUT)" >&2
  exit 1
fi
echo "Every window fits in: $*"

#!/bin/sh
# Renders the Mac App Store screenshots for one language or all of them (`screenshots.store` in companion.json).
# Usage: laching/tools/store-screenshots.sh [language...|all]   (default: the source language)
# Every language lands in <store.out>/<lang>/<n>-<shot>-<appearance>.png: upload them from there.
#   source   composes the committed captures in screenshots.out (run screenshots.sh first if the windows changed).
#   xx       captures the windows it needs in that language into build/store-captures/apple/<xx> first.
# Captions: store.captions, one list per language. Needs Pillow (pip3 install pillow).
set -e
TOOLS=$(cd "$(dirname "$0")" && pwd)
get() { python3 "$TOOLS/companion.py" get "$1"; }
ROOT=$(python3 "$TOOLS/companion.py" root)
SOURCE=$(get languages.source)
CAPTIONS="$ROOT/$(get screenshots.store.captions)"
OUT="$ROOT/$(get screenshots.store.out)"
LANGS="${*:-$SOURCE}"
if [ "$LANGS" = all ]; then
  LANGS=$(python3 -c "import json, sys; print(' '.join(json.load(open(sys.argv[1]))))" "$CAPTIONS")
fi
for lang in $LANGS; do
  if [ "$lang" = "$SOURCE" ]; then
    python3 "$TOOLS/store-screenshots.py" "$ROOT/$(get screenshots.out)" "$OUT/$lang" "$lang"
    continue
  fi
  shots=$(python3 -c "import json, sys; print(','.join(dict.fromkeys(c['shot'] for c in json.load(open(sys.argv[1]))[sys.argv[2]])))" "$CAPTIONS" "$lang")
  captures="$ROOT/build/store-captures/apple/$lang"
  "$TOOLS/screenshots.sh" --only "$shots" --front "$captures" "$lang"
  python3 "$TOOLS/store-screenshots.py" "$captures" "$OUT/$lang" "$lang"
done

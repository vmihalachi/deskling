#!/bin/sh
# Regenerates the app's conformance vectors from its Swift core package: the app's ConformanceTests write them when
# `conformance.writeFlag` is set (SwiftPM tests aren't sandboxed). The core is platform-neutral, so this runs on
# Linux too. Names come from companion.json.
# Usage: deskling/tools/conformance.sh
set -eu
TOOLS=$(cd "$(dirname "$0")" && pwd)
get() { python3 "$TOOLS/companion.py" get "$1"; }
ROOT=$(python3 "$TOOLS/companion.py" root)
if ! get conformance.dir >/dev/null; then
  echo "companion.json has no conformance section: this app has no vectors to regenerate" >&2
  exit 1
fi
FLAG=$(get conformance.writeFlag)
PACKAGE=$(get conformance.corePackagePath)
cd "$ROOT"
env "$FLAG=1" swift test --package-path "$PACKAGE" --filter 'ConformanceTests/testWriteVectors'
echo "Updated $(get conformance.dir)/ from $PACKAGE"

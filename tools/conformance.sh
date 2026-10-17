#!/bin/sh
# Regenerates the app's conformance vectors from its Swift core package: the app's ConformanceTests write them when
# `conformance.writeFlag` is set (SwiftPM tests aren't sandboxed). The core is platform-neutral, so this runs on
# Linux too. Names come from companion.json.
# Usage: laching/tools/conformance.sh
set -eu
TOOLS=$(cd "$(dirname "$0")" && pwd)
env_file=$(python3 "$TOOLS/companion.py" env)
eval "$env_file"
if [ -z "$CONFORMANCE_DIR" ]; then
  echo "companion.json has no conformance section: this app has no vectors to regenerate" >&2
  exit 1
fi
cd "$ROOT"
env "$CONFORMANCE_FLAG=1" swift test --package-path "$CORE_PACKAGE" --filter 'ConformanceTests/testWriteVectors'
echo "Updated $CONFORMANCE_DIR/ from $CORE_PACKAGE"

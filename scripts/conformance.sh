#!/bin/sh
# Regenerates the conformance vectors in conformance/ from the Swift code.
# SwiftPM tests aren't sandboxed, so ConformanceTests writes straight into the repo when asked to.
set -eu
cd "$(dirname "$0")/.."
DESKLING_WRITE_CONFORMANCE=1 swift test --filter 'ConformanceTests/testWriteVectors'
echo "Updated conformance/ from the Swift code"

#!/usr/bin/env bash
# Compiles the fixture skins (Tests/Fixtures/characters/) with tools/character.py:
# the Swift art into Sources/DesklingTesting/Fixtures/ and the JSON into conformance/companion/characters/.
#
#   scripts/fixture-art.sh            # write what changed
#   scripts/fixture-art.sh --check    # write nothing; exit 1 if an output is stale
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"
check=()
[ "${1:-}" = "--check" ] && check=(--check)
design=Tests/Fixtures/characters
out=conformance/companion/characters
swift=Sources/DesklingTesting/Fixtures
python3 tools/character.py --design "$design" --skin fixture-vector --out "$out/fixture-vector.json" \
  --swift-out "$swift/FixtureVectorArt.swift" "${check[@]+"${check[@]}"}"
python3 tools/character.py --design "$design" --skin fixture-raster --mode raster --out "$out/fixture-raster.json" \
  --swift-out "$swift/FixtureRasterArt.swift" "${check[@]+"${check[@]}"}"

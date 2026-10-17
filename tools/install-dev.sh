#!/bin/bash
# Copies a finished Debug build to ~/Applications/<mac.devAppName>.app, replacing the previous copy, so there's one
# current Dev app next to the TestFlight/App Store one (bundle ID devBundleId).
#
# Runs as the app scheme's build post-action (after signing): `"$PROJECT_DIR/../laching/tools/install-dev.sh"` (or the
# app's two-line wrapper), so Xcode, xcodebuild, verify.sh and screenshots.sh all refresh it. Release builds and CI are
# skipped. A running Dev app keeps running the old copy until you quit and reopen it.
set -euo pipefail

[[ "${CONFIGURATION:-}" == "Debug" ]] || exit 0
[[ -z "${CI:-}" ]] || exit 0

src="${BUILT_PRODUCTS_DIR:?}/${FULL_PRODUCT_NAME:?}"
[[ -d "$src" ]] || exit 0

TOOLS=$(cd "$(dirname "$0")" && pwd)
name=$(cd "${PROJECT_DIR:-.}" && python3 "$TOOLS/companion.py" get mac.devAppName)
dest_dir="$HOME/Applications"
dest="$dest_dir/$name.app"
tmp="$dest_dir/.$name.app.new"

mkdir -p "$dest_dir"
rm -rf "$tmp"
ditto "$src" "$tmp"
rm -rf "$dest"
mv "$tmp" "$dest"

lsregister=/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister
"$lsregister" -f "$dest" || true
# The build registers its product with Launch Services, so every build folder showed up as another copy of the app in
# Spotlight, Launchpad and Open With. Keep only the installed copy registered. Spotlight re-registers any app it finds
# on disk, so the scripts build into `*.noindex` folders, which it skips.
"$lsregister" -u "$src" || true

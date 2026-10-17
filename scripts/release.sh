#!/bin/sh
# Laching's releases. release.yml runs it on every push to master, so whatever lands there ships; it works the same
# by hand.
# Usage: scripts/release.sh plan          what is waiting, on one line:
#                                           tag X.Y.Z <commit>  the version strings name X.Y.Z and it has no tag yet (a
#                                                               change bumped it): release the commit that did;
#                                           bump X.Y.Z          CHANGELOG.md's Unreleased section has entries: the next
#                                                               patch, or the next minor while 0.x (major after) when
#                                                               one says **Breaking**;
#                                           none                nothing.
#                                         Reads the tags, so fetch them first.
#        scripts/release.sh pending       exits 0 when the Unreleased section has entries.
#        scripts/release.sh bump X.Y.Z    moves the Unreleased entries under "## X.Y.Z - <today, UTC>" and puts X.Y.Z in
#                                         the ten version strings (Laching.swift, LachingInfo.cs, Directory.Build.props,
#                                         site/index.html, README.md, kotlin/gradle.properties, Kotlin's Laching.kt), so
#                                         scripts/verify.sh --version X.Y.Z passes. Committing and tagging are the
#                                         caller's.
set -eu
cd "$(dirname "$0")/.."

usage() {
  sed -n 's/^# \{0,1\}//; 4,18p' "$0" >&2
  exit 2
}

current() { sed -n 's/.*public static let version = "\([^"]*\)".*/\1/p' Sources/LachingCore/Laching.swift; }

# CHANGELOG.md's Unreleased section, without its heading.
unreleased() { awk '/^## / { on = ($2 == "Unreleased"); next } on' CHANGELOG.md; }

pending() { unreleased | grep -q '[^[:space:]]'; }

# The version after $1: a breaking change ($2 = 1) is a minor while 0.x and a major after; anything else a patch.
next() {
  echo "$1" | awk -F. -v breaking="$2" '{
    if (breaking == 1 && $1 == 0) print $1 "." ($2 + 1) ".0"
    else if (breaking == 1) print ($1 + 1) ".0.0"
    else print $1 "." $2 "." ($3 + 1)
  }'
}

plan() {
  version=$(current)
  if [ -z "$(git tag -l "v$version")" ]; then
    commit=$(git log -1 --format=%H -S "version = \"$version\"" -- Sources/LachingCore/Laching.swift)
    echo "tag $version ${commit:-$(git rev-parse HEAD)}"
  elif pending; then
    breaking=0
    if unreleased | grep -q '\*\*Breaking'; then breaking=1; fi
    echo "bump $(next "$version" "$breaking")"
  else
    echo none
  fi
}

bump() {
  new=$1
  old=$(current)
  echo "$new" | grep -Eq '^[0-9]+\.[0-9]+\.[0-9]+$' || { echo "bump takes X.Y.Z, not $new" >&2; exit 2; }
  pending || { echo "CHANGELOG.md's Unreleased section is empty: nothing to release" >&2; exit 1; }
  re=$(printf '%s' "$old" | sed 's/[.]/\\./g')
  # "X.Y.Z" in the sources and the install snippets, >X.Y.Z< in Directory.Build.props, vX.Y.Z in the site's label and
  # the README's tag example, lachingVersion=X.Y.Z in kotlin/gradle.properties.
  for file in Sources/LachingCore/Laching.swift dotnet/Laching.Core/LachingInfo.cs dotnet/Directory.Build.props \
    site/index.html README.md kotlin/gradle.properties kotlin/core/src/main/kotlin/laching/core/Laching.kt; do
    sed "s/\"$re\"/\"$new\"/g; s/>$re</>$new</g; s/v$re\([^0-9]\)/v$new\1/g; s/v$re\$/v$new/
         s/^lachingVersion=$re\$/lachingVersion=$new/" "$file" > "$file.new"
    mv "$file.new" "$file"
  done
  awk -v heading="## $new - $(date -u +%Y-%m-%d)" '
    { print }
    !done && /^## Unreleased[[:space:]]*$/ { print ""; print heading; done = 1 }' CHANGELOG.md > CHANGELOG.md.new
  mv CHANGELOG.md.new CHANGELOG.md
  echo "Bumped $old to $new"
}

case "${1:-}" in
  plan) [ $# -eq 1 ] || usage; plan ;;
  pending) [ $# -eq 1 ] || usage; pending ;;
  bump) [ $# -eq 2 ] || usage; bump "$2" ;;
  *) usage ;;
esac

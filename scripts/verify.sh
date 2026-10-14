#!/usr/bin/env bash
# Runs the checks that apply to a set of changed paths.
#
#   scripts/verify.sh                    # working-tree changes vs HEAD, plus untracked files
#   scripts/verify.sh --staged           # staged files
#   scripts/verify.sh --all              # every check, regardless of what changed
#   scripts/verify.sh --fast ...         # only the checks that take seconds (no swift test, dotnet test)
#   scripts/verify.sh --version X.Y.Z    # only release.yml's gate: the strings are X.Y.Z, CHANGELOG.md has its section
#
# Path → checks:
#   Package.swift, Sources/, Tests/     swift-format lint, swift build + test (macOS), vectors current
#   dotnet/**, conformance/**           dotnet format whitespace, dotnet build + test
#   tools/**, Tests/Fixtures/**         the tools' Python tests, fixture art current
#   tools/landing/**                    @deskling/landing: types, lint, format, tests (Node 24)
#   version sources, site/**, README.md  every version string agrees (always checked)
set -uo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

fast=0 all=0 mode=worktree version=
while [ $# -gt 0 ]; do
  case "$1" in
    --fast) fast=1 ;;
    --all) all=1 ;;
    --staged) mode=staged ;;
    --version) [ $# -ge 2 ] || { echo "--version needs X.Y.Z (see --help)" >&2; exit 2; }; version="$2"; shift ;;
    -h|--help) sed -n '2,15p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "unknown argument: $1 (see --help)" >&2; exit 2 ;;
  esac
  shift
done
case "$mode" in
  worktree) changed="$(git diff --name-only HEAD; git ls-files --others --exclude-standard)" ;;
  staged) changed="$(git diff --cached --name-only)" ;;
esac
changed="$(printf '%s\n' "$changed" | sed '/^$/d' | sort -u)"
touched() { [ "$all" = 1 ] || printf '%s\n' "$changed" | grep -Eq "$1"; }
darwin=0; [ "$(uname)" = Darwin ] && darwin=1
has() { command -v "$1" >/dev/null 2>&1; }
logdir="$(mktemp -d "${TMPDIR:-/tmp}/verify.XXXXXX")"
failed=() notes=()
check() {
  local name="$1"; shift
  local log="$logdir/${name//[^A-Za-z0-9]/_}.log"
  if "$@" >"$log" 2>&1; then printf 'ok    %s\n' "$name"
  else printf 'FAIL  %s\n' "$name"; tail -n 30 "$log" | sed 's/^/      /'; failed+=("$name"); fi
}
skip() { notes+=("$1"); }
finish() {
  rm -rf "$logdir"
  if [ ${#failed[@]} -gt 0 ]; then echo "${#failed[@]} check(s) failed."; exit 1; fi
  exit 0
}

swift='^Package\.swift$|^Sources/|^Tests/|^\.swift-format$'
dotnet='^dotnet/|^conformance/|^Tests/DesklingCompanionTests/Resources/|^Tests/DesklingShellTests/Resources/'
tools='^tools/|^Tests/Fixtures/'

vectors_current() {
  scripts/conformance.sh || return 1
  if ! git diff --quiet -- conformance || [ -n "$(git ls-files --others --exclude-standard -- conformance)" ]; then
    git status --short -- conformance
    echo "The vectors were stale and have been regenerated. Commit them with your change."
    return 1
  fi
}

landing_check() {
  (
    cd tools/landing || exit 1
    [ -d node_modules ] || npm ci --silent || exit 1
    npx tsc --noEmit && npx vp check && npx vp test run
  )
}

# With an argument, the agreed version must also be that one (release.yml checks the tag's against it).
versions_agree() {
  local expected="${1:-}" found distinct
  found="$( {
    sed -n 's/.*public static let version = "\([^"]*\)".*/\1/p' Sources/DesklingCore/Deskling.swift
    sed -n 's/.*public const string Version = "\([^"]*\)".*/\1/p' dotnet/Deskling.Core/DesklingInfo.cs
    sed -n 's/.*<DesklingVersion Condition=[^>]*>\([^<]*\)<.*/\1/p' dotnet/Directory.Build.props
    grep -oE 'class="version"><a [^>]*>v[0-9]+\.[0-9]+\.[0-9]+|exact: (<span class="s">)?"[0-9]+\.[0-9]+\.[0-9]+"' site/index.html |
      grep -oE '[0-9]+\.[0-9]+\.[0-9]+'
    grep -oE 'exact: "[0-9]+\.[0-9]+\.[0-9]+"|git tag v[0-9]+\.[0-9]+\.[0-9]+' README.md |
      grep -oE '[0-9]+\.[0-9]+\.[0-9]+'
  } )"
  # Deskling.swift, DesklingInfo.cs, Directory.Build.props, the site's version label and two install snippets, and the
  # README's install pin and release example.
  if [ "$(printf '%s\n' "$found" | grep -c .)" -ne 8 ]; then
    echo "expected 8 version strings, found:"; printf '%s\n' "$found" | sed 's/^/  /'; return 1
  fi
  distinct="$(printf '%s\n' "$found" | sort -u)"
  if [ "$(printf '%s\n' "$distinct" | wc -l)" -ne 1 ]; then
    echo "version strings disagree (Deskling.swift, DesklingInfo.cs, Directory.Build.props, site/index.html, README.md):"
    printf '%s\n' "$found" | sort | uniq -c | sed 's/^/  /'; return 1
  fi
  if [ -n "$expected" ] && [ "$distinct" != "$expected" ]; then
    echo "the version strings say $distinct, not $expected (Deskling.swift, DesklingInfo.cs, Directory.Build.props," \
      "site/index.html, README.md)"
    return 1
  fi
}

# `## X.Y.Z - date` (or `## [X.Y.Z] …`): release.yml takes the release notes from that section.
changelog_has() {
  local re; re="$(printf '%s' "$1" | sed 's/[.]/\\./g')"
  grep -Eq "^## \[?$re\]? " CHANGELOG.md || { echo "CHANGELOG.md has no '## $1' section"; return 1; }
}

if [ -n "$version" ]; then
  check "version strings are $version" versions_agree "$version"
  check "CHANGELOG.md has a '## $version' section" changelog_has "$version"
  finish
fi
check "versions agree" versions_agree

if touched "$swift"; then
  if [ "$darwin" = 1 ]; then
    check "swift-format lint" xcrun swift-format lint -r --strict --configuration .swift-format Sources Tests
  else skip "swift-format: needs macOS"; fi
fi
if touched "$dotnet"; then
  if has dotnet; then check "dotnet format whitespace" dotnet format whitespace dotnet --folder --verify-no-changes
  else skip "dotnet format: needs the .NET SDK"; fi
fi
if touched "$tools"; then
  if has python3; then
    check "tools tests" python3 tools/tests/run.py
    check "fixture art current" scripts/fixture-art.sh --check
  else skip "tools tests: needs python3"; fi
fi
if touched '^tools/landing/'; then
  if has npm; then check "@deskling/landing (types, lint, tests)" landing_check
  else skip "@deskling/landing: needs Node 24 and npm"; fi
fi
if [ "$fast" = 0 ]; then
  if touched "$swift|^conformance/"; then
    if [ "$darwin" = 1 ]; then
      check "swift build + test" swift test
      check "conformance vectors current" vectors_current
    else skip "swift test: needs macOS (push and run swift.yml)"; fi
  fi
  if touched "$dotnet"; then
    if has dotnet; then
      check "dotnet build + test (Core)" dotnet test dotnet/Deskling.Core.Tests/Deskling.Core.Tests.csproj -v q --nologo
      check "dotnet build + test (Companion)" dotnet test dotnet/Deskling.Companion.Tests/Deskling.Companion.Tests.csproj -v q --nologo
      check "dotnet build + test (Companion.Shell)" dotnet test dotnet/Deskling.Companion.Shell.Tests/Deskling.Companion.Shell.Tests.csproj -v q --nologo
    else skip "dotnet test: needs the .NET SDK"; fi
  fi
fi
for n in "${notes[@]+"${notes[@]}"}"; do printf 'skip  %s\n' "$n"; done
[ -z "$changed" ] && [ "$all" = 0 ] && echo "No changed paths."
finish

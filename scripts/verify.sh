#!/usr/bin/env bash
# Runs the checks that apply to a set of changed paths.
#
#   scripts/verify.sh                    # working-tree changes vs HEAD, plus untracked files
#   scripts/verify.sh --staged           # staged files
#   scripts/verify.sh --all              # every check, regardless of what changed
#   scripts/verify.sh --fast ...         # only the checks that take seconds (no swift test, dotnet test)
#
# Path → checks:
#   Package.swift, Sources/, Tests/     swift-format lint, swift build + test (macOS), vectors current
#   dotnet/**, conformance/**           dotnet format whitespace, dotnet build + test
#   version sources, site/**            every version string agrees (always checked)
set -uo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

fast=0 all=0 mode=worktree
while [ $# -gt 0 ]; do
  case "$1" in
    --fast) fast=1 ;;
    --all) all=1 ;;
    --staged) mode=staged ;;
    -h|--help) sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
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

swift='^Package\.swift$|^Sources/|^Tests/|^\.swift-format$'
dotnet='^dotnet/|^conformance/'

vectors_current() {
  scripts/conformance.sh || return 1
  if ! git diff --quiet -- conformance || [ -n "$(git ls-files --others --exclude-standard -- conformance)" ]; then
    git status --short -- conformance
    echo "The vectors were stale and have been regenerated. Commit them with your change."
    return 1
  fi
}

versions_agree() {
  local found distinct
  found="$( {
    sed -n 's/.*public static let version = "\([^"]*\)".*/\1/p' Sources/DesklingCore/Deskling.swift
    sed -n 's/.*public const string Version = "\([^"]*\)".*/\1/p' dotnet/Deskling.Core/DesklingInfo.cs
    sed -n 's/.*<DesklingVersion Condition=[^>]*>\([^<]*\)<.*/\1/p' dotnet/Directory.Build.props
    grep -oE 'class="version"><a [^>]*>v[0-9]+\.[0-9]+\.[0-9]+|exact: (<span class="s">)?"[0-9]+\.[0-9]+\.[0-9]+"' site/index.html |
      grep -oE '[0-9]+\.[0-9]+\.[0-9]+'
  } )"
  # Deskling.swift, DesklingInfo.cs, Directory.Build.props, and the site's version label and two install snippets.
  if [ "$(printf '%s\n' "$found" | grep -c .)" -ne 6 ]; then
    echo "expected 6 version strings, found:"; printf '%s\n' "$found" | sed 's/^/  /'; return 1
  fi
  distinct="$(printf '%s\n' "$found" | sort -u)"
  if [ "$(printf '%s\n' "$distinct" | wc -l)" -ne 1 ]; then
    echo "version strings disagree (Deskling.swift, DesklingInfo.cs, Directory.Build.props, site/index.html):"
    printf '%s\n' "$found" | sort | uniq -c | sed 's/^/  /'; return 1
  fi
}

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
if [ "$fast" = 0 ]; then
  if touched "$swift|^conformance/"; then
    if [ "$darwin" = 1 ]; then
      check "swift build + test" swift test
      check "conformance vectors current" vectors_current
    else skip "swift test: needs macOS (push and run swift.yml)"; fi
  fi
  if touched "$dotnet"; then
    if has dotnet; then check "dotnet build + test" dotnet test dotnet/Deskling.Core.Tests/Deskling.Core.Tests.csproj -v q --nologo
    else skip "dotnet test: needs the .NET SDK"; fi
  fi
fi
for n in "${notes[@]+"${notes[@]}"}"; do printf 'skip  %s\n' "$n"; done
[ -z "$changed" ] && [ "$all" = 0 ] && echo "No changed paths."
rm -rf "$logdir"
if [ ${#failed[@]} -gt 0 ]; then echo "${#failed[@]} check(s) failed."; exit 1; fi
exit 0

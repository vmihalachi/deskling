#!/usr/bin/env bash
# Runs the checks that apply to a set of changed paths in a companion app, so nobody has to remember which ones. Every
# path and name comes from the app's companion.json (`companion.py env`); the app keeps a two-line scripts/verify.sh
# that execs this one.
#
#   scripts/verify.sh                    # working-tree changes vs HEAD, plus untracked files
#   scripts/verify.sh --staged           # staged files (the pre-commit hook)
#   scripts/verify.sh --base origin/master
#   scripts/verify.sh -- path/a path/b   # explicit paths (the agent Stop hook)
#   scripts/verify.sh --fast ...         # only the checks that take seconds (no xcodebuild, swift/dotnet test, landing build)
#   scripts/verify.sh --all              # every check, regardless of what changed
#
# Path → checks:
#   mac.app/tests Swift, mac.projectSpec   xcodegen (fails if project.pbxproj was stale), swift-format lint, xcodebuild test
#   mac.core                               swift test on the core package
#   mac.core sources and Conformance/      deskling/tools/conformance.sh (fails if the vectors were stale)
#   strings (app Swift, .xcstrings, landing i18n)   l10n.py check, phrases.py check, .resw export is current
#   voices, voice config, catalog, phrases.json     voice.py check, the voices pointer is pushed
#   windows/**/*.cs                        dotnet format whitespace, dotnet test (windows.coreTests)
#   landing/, phrases/settings vectors     <landing.tool> check and build
#   always                                 the Deskling pins agree (submodule tag = deskling.tag = SwiftPM = XcodeGen = NuGet)
#
# Prints one line per check and the tail of the log for failures. Exit 1 if anything failed.
set -uo pipefail

TOOLS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
env_file="$(python3 "$TOOLS/companion.py" env)" || exit 2
eval "$env_file"
cd "$ROOT"

fast=0 all=0 mode=worktree base="" paths=()
while [ $# -gt 0 ]; do
  case "$1" in
    --fast) fast=1 ;;
    --all) all=1 ;;
    --staged) mode=staged ;;
    --base) mode=base; base="$2"; shift ;;
    --) shift; mode=paths; paths=("$@"); break ;;
    -h|--help) sed -n '2,24p' "$TOOLS/verify.sh" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "unknown argument: $1 (see --help)" >&2; exit 2 ;;
  esac
  shift
done

case "$mode" in
  worktree) changed="$(git diff --name-only HEAD 2>/dev/null; git ls-files --others --exclude-standard 2>/dev/null)" ;;
  staged) changed="$(git diff --cached --name-only)" ;;
  base) changed="$(git diff --name-only "$base"...HEAD; git diff --name-only HEAD; git ls-files --others --exclude-standard)" ;;
  paths) changed="$(printf '%s\n' "${paths[@]}" | sed "s|^$ROOT/||")" ;;
esac
changed="$(printf '%s\n' "$changed" | sed '/^$/d' | sort -u)"

# touched <extended regex>: true when a changed path matches (always true with --all).
touched() { [ "$all" = 1 ] || printf '%s\n' "$changed" | grep -Eq "$1"; }

darwin=0; [ "$(uname)" = Darwin ] && darwin=1
has() { command -v "$1" >/dev/null 2>&1; }
# The landing tool from PATH, or the one its node_modules brings (CI, cloud containers: setup-linux.sh).
landing_tool="$LANDING_TOOL"
if [ -n "$landing_tool" ] && ! has "$landing_tool" && [ -x "$ROOT/$LANDING_DIR/node_modules/.bin/$landing_tool" ]; then
  landing_tool="$ROOT/$LANDING_DIR/node_modules/.bin/$landing_tool"
fi
logdir="$(mktemp -d "${TMPDIR:-/tmp}/verify.XXXXXX")"
failed=() notes=()

# check <name> <command...>: runs quietly, prints ok/FAIL, keeps the log.
check() {
  local name="$1"; shift
  local log="$logdir/${name//[^A-Za-z0-9]/_}.log"
  if "$@" >"$log" 2>&1; then
    printf 'ok    %s\n' "$name"
  else
    printf 'FAIL  %s\n' "$name"
    tail -n 30 "$log" | sed 's/^/      /'
    failed+=("$name")
  fi
}
skip() { notes+=("$1"); }

xcodegen_in_sync() {
  (cd "$MAC_DIR" && xcodegen generate --quiet) || return 1
  if ! git diff --quiet -- "$MAC_DIR/$MAC_PROJECT"; then
    echo "project.pbxproj was stale and has been regenerated; include it in your commit."
    git diff --stat -- "$MAC_DIR/$MAC_PROJECT"
    [ "$mode" = staged ] && return 1
  fi
  return 0
}

conformance_current() {
  local before after
  snapshot() { { git diff -- "$CONFORMANCE_DIR"; git ls-files --others --exclude-standard -- "$CONFORMANCE_DIR"; } | shasum; }
  before="$(snapshot)"
  "$TOOLS/conformance.sh" || return 1
  after="$(snapshot)"
  if [ "$before" != "$after" ]; then
    git status --short -- "$CONFORMANCE_DIR"
    echo "The vectors were stale and have been regenerated. Commit them with your change,"
    echo "then run the Windows Core tests to see what the port needs."
    return 1
  fi
  return 0
}

# The voices pointer this commit records (staged, or the work tree's) must already be on GitHub, or every other clone
# breaks: `voice.py publish` pushes before it stages. Offline: it asks the local remote-tracking refs.
voices_pointer_pushed() {
  local sha
  sha="$(git ls-files --stage "$VOICE_DIR" | awk '{print $2}')"
  [ -n "$sha" ] || return 0
  [ -e "$VOICE_DIR/.git" ] || { echo "$VOICE_DIR/ isn't checked out: git submodule update --init"; return 1; }
  git -C "$VOICE_DIR" fetch -q origin 2>/dev/null || true
  if [ -z "$(git -C "$VOICE_DIR" branch -r --contains "$sha" 2>/dev/null)" ]; then
    echo "$VOICE_DIR pointer ${sha:0:7} isn't pushed: run python3 $DESKLING_PATH/tools/voice.py publish -m \"…\""
    return 1
  fi
}

resw_current() {
  python3 "$TOOLS/l10n.py" export-windows --out "$logdir/resw" >/dev/null &&
    diff -r --strip-trailing-cr "$logdir/resw" "$WINDOWS_STRINGS"
}

swift_format() {
  local config="$MAC_DIR/.swift-format"
  # Fix with: xcrun swift-format format -i --configuration <mac.dir>/.swift-format <files>
  if [ "$all" = 1 ]; then
    local dirs=()
    for d in "$MAC_APP" "$MAC_TESTS" "$MAC_CORE/Sources" "$MAC_CORE/Tests"; do [ -n "$d" ] && [ -d "$d" ] && dirs+=("$d"); done
    xcrun swift-format lint -r --strict --configuration "$config" "${dirs[@]}"
    return
  fi
  local files
  files="$(printf '%s\n' "$changed" | grep -E "^$MAC_DIR/.*\.swift$" | while read -r f; do [ -f "$f" ] && echo "$f"; done)"
  [ -n "$files" ] || return 0
  # shellcheck disable=SC2086
  xcrun swift-format lint --strict --configuration "$config" $files
}

csharp_format() {
  local files=()
  while read -r f; do [ -f "$f" ] && files+=("${f#"$WINDOWS_DIR"/}"); done < <(printf '%s\n' "$changed" | grep -E "$RE_CSHARP")
  if [ "$all" = 1 ]; then
    dotnet format whitespace "$WINDOWS_DIR" --folder --verify-no-changes
  elif [ ${#files[@]} -gt 0 ]; then
    dotnet format whitespace "$WINDOWS_DIR" --folder --verify-no-changes --include "${files[@]}"
  fi
}

# --- always ------------------------------------------------------------------------------------

check "Deskling pins agree" python3 "$TOOLS/companion.py" pins

# --- fast checks -------------------------------------------------------------------------------

if touched "$RE_SWIFT|$RE_COREPKG"; then
  if [ -n "$MAC_SPEC" ]; then
    if [ "$darwin" = 1 ] && has xcodegen; then check "xcodegen (project.pbxproj in sync)" xcodegen_in_sync
    else skip "xcodegen: needs macOS with xcodegen"; fi
  fi
  if [ "$darwin" = 1 ] && [ -f "$MAC_DIR/.swift-format" ]; then check "swift-format lint" swift_format; fi
fi

if touched "$RE_STRINGS"; then
  check "l10n.py check" python3 "$TOOLS/l10n.py" check
fi
if touched "$RE_PHRASES"; then
  check "phrases.py check" python3 "$TOOLS/phrases.py" check
fi
if [ -n "$VOICE_DIR" ]; then
  # Pending lines are fine (the app never plays them); this fails only on a broken manifest, orphans or a missing
  # required clip of a shipping language, and prints the pending summary either way.
  if touched "^$VOICE_DIR\$"; then
    check "voices pointer pushed" voices_pointer_pushed
  fi
  if touched "$RE_VOICE"; then
    check "voice.py check" python3 "$TOOLS/voice.py" check
  fi
fi
if [ -n "$WINDOWS_STRINGS" ] && [ -d "$WINDOWS_STRINGS" ] && touched "\.xcstrings$|^$WINDOWS_STRINGS/"; then
  check ".resw files match the catalog" resw_current
fi

if touched "$RE_CSHARP"; then
  if has dotnet; then check "dotnet format whitespace" csharp_format
  else skip "dotnet format: needs the .NET SDK"; fi
fi

if touched "$RE_LANDING" && [ -n "$LANDING_DIR" ] && [ -d "$ROOT/$LANDING_DIR" ]; then
  if [ -n "$landing_tool" ] && { has "$landing_tool" || [ -x "$landing_tool" ]; }; then
    check "landing: $LANDING_TOOL check" bash -c "cd \"$LANDING_DIR\" && \"$landing_tool\" check"
  else skip "landing: needs $LANDING_TOOL"; fi
fi

# --- slow checks -------------------------------------------------------------------------------

if [ "$fast" = 0 ]; then
  if touched "$RE_COREPKG"; then
    if has swift; then check "swift test ($MAC_CORE)" swift test --package-path "$MAC_CORE"
    else skip "swift test ($MAC_CORE): needs a Swift toolchain"; fi
  fi
  if touched "$RE_PURE"; then
    if [ "$darwin" = 1 ]; then check "conformance vectors current" conformance_current
    else skip "conformance.sh: needs macOS + Xcode"; fi
  fi
  if touched "$RE_SWIFT|^$CONFORMANCE_DIR/"; then
    if [ "$darwin" = 1 ]; then
      check "xcodebuild test" bash -c "cd \"$MAC_DIR\" && xcodebuild -project \"$MAC_PROJECT\" -scheme \"$MAC_SCHEME\" \
        -destination 'platform=macOS' -derivedDataPath \"$ROOT/$DERIVED\" -quiet test"
    else skip "xcodebuild test: needs macOS + Xcode"; fi
  fi
  if touched "$RE_WINDOWS_CORE" && [ -n "$WINDOWS_CORE_TESTS" ]; then
    if has dotnet; then
      check "dotnet test ($WINDOWS_CORE_TESTS)" dotnet test "$WINDOWS_DIR/$WINDOWS_CORE_TESTS/$WINDOWS_CORE_TESTS.csproj" -v q --nologo
    else skip "dotnet test: needs the .NET SDK"; fi
  fi
  if touched "$RE_WINDOWS_APP"; then
    skip "WinUI app: builds only on Windows; push the branch and run the Windows workflow"
  fi
  if touched "$RE_LANDING" && [ -n "$LANDING_DIR" ] && [ -d "$ROOT/$LANDING_DIR" ] && [ -n "$landing_tool" ] &&
    { has "$landing_tool" || [ -x "$landing_tool" ]; }; then
    check "landing: $LANDING_TOOL build" bash -c "cd \"$LANDING_DIR\" && \"$landing_tool\" build"
  fi
fi

for n in "${notes[@]+"${notes[@]}"}"; do printf 'skip  %s\n' "$n"; done
if [ -z "$changed" ] && [ "$all" = 0 ]; then echo "No changed paths."; fi
rm -rf "$logdir"
if [ ${#failed[@]} -gt 0 ]; then
  echo "${#failed[@]} check(s) failed."
  exit 1
fi
exit 0

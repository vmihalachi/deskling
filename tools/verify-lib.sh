#!/usr/bin/env bash
# The harness tools/verify.sh (a companion app's checks) and Laching's own scripts/verify.sh share. Sourced, not run:
# it needs bash (3.2 is enough) and the sourcing script's $changed (the changed paths, one per line) and $all (1 for
# --all).
#
#   check <name> <command...>   runs the command quietly: `ok    <name>`, or `FAIL  <name>` and the tail of its log
#   skip <note>                 a check that can't run here, listed at the end
#   changed_files <regex>...    the changed paths matching every regex that are files, in the caller's `files` array
#   summary                     the skipped checks, and "No changed paths." when there were none
#   finish                      removes the logs; exits 1 when a check failed

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
  echo "verify-lib.sh is sourced by verify.sh, not run" >&2
  exit 2
fi

has() { command -v "$1" >/dev/null 2>&1; }
logdir="$(mktemp -d "${TMPDIR:-/tmp}/verify.XXXXXX")"
failed=() notes=()

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

changed_files() {
  local f re list="$changed"
  files=()
  for re in "$@"; do list="$(printf '%s\n' "$list" | grep -E "$re")"; done
  while read -r f; do [ -n "$f" ] && [ -f "$f" ] && files+=("$f"); done <<< "$list"
}

summary() {
  local n
  for n in "${notes[@]+"${notes[@]}"}"; do printf 'skip  %s\n' "$n"; done
  if [ -z "$changed" ] && [ "$all" = 0 ]; then echo "No changed paths."; fi
}

finish() {
  rm -rf "$logdir"
  if [ ${#failed[@]} -gt 0 ]; then
    echo "${#failed[@]} check(s) failed."
    exit 1
  fi
  exit 0
}

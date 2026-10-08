#!/usr/bin/env bash
# Stop hook for coding agents: runs `scripts/verify.sh --fast` (format and version checks, seconds) on the
# working-tree changes. On failure it prints the output to stderr and exits 2, which sends the agent back to fix it.
# Once per turn: when the agent is already continuing because of this hook (stop_hook_active), it lets it stop.
set -uo pipefail
cd "$(dirname "$0")/.."
input="$(cat 2>/dev/null || true)"
case "$input" in *'"stop_hook_active":true'* | *'"stop_hook_active": true'*) exit 0 ;; esac
if ! out="$(scripts/verify.sh --fast 2>&1)"; then
  printf 'scripts/verify.sh --fast failed. Fix it before finishing:\n%s\n' "$out" >&2
  exit 2
fi
exit 0

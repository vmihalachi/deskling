#!/usr/bin/env bash
# Prepares a fresh Linux container (Claude Code on the web, Copilot's coding agent, a dev container) to run what a
# companion app can run off a Mac: the Windows Core tests (`dotnet test`), C# formatting, the l10n and voice checks and
# the landing build. Names come from companion.json. Idempotent and quiet when everything is already there.
#
#   deskling/tools/setup-linux.sh        (the app's scripts/setup-linux.sh execs it)
#
# Claude Code runs it at session start in cloud sessions (CLAUDE_CODE_REMOTE=true, .claude/settings.json). It never
# touches an existing landing node_modules, so it's safe in a shared checkout.
set -uo pipefail
TOOLS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
has() { command -v "$1" >/dev/null 2>&1; }
has python3 || { echo "error: python3 is required (the tools are Python)" >&2; exit 1; }
eval "$(python3 "$TOOLS/companion.py" env)" || exit 1
cd "$ROOT"

# The Deskling tools and the recorded voices are submodules.
git submodule update --init ${DESKLING_PATH:+"$DESKLING_PATH"} ${VOICE_DIR:+"$VOICE_DIR"} >/dev/null 2>&1 ||
  echo "warning: couldn't fetch the submodules; voice.py check won't run" >&2
# A landing page kept in its own repository is a submodule too.
if [ -n "$LANDING_DIR" ] && git ls-files -s -- "$LANDING_DIR" | grep -q '^160000'; then
  git submodule update --init "$LANDING_DIR" >/dev/null 2>&1 || echo "warning: couldn't fetch $LANDING_DIR/" >&2
fi
git config submodule.recurse true; git config push.recurseSubmodules on-demand
[ -d .githooks ] && git config core.hooksPath .githooks

# .NET 10 SDK, into ~/.dotnet.
if [ -n "$WINDOWS_DIR" ]; then
  if ! has dotnet || ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
    echo "Installing the .NET 10 SDK into ~/.dotnet"
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${TMPDIR:-/tmp}/dotnet-install.sh" &&
      bash "${TMPDIR:-/tmp}/dotnet-install.sh" --channel 10.0 --install-dir "$HOME/.dotnet" >/dev/null ||
      echo "warning: .NET install failed; dotnet test and dotnet format won't run" >&2
  fi
  if [ -x "$HOME/.dotnet/dotnet" ]; then
    export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$PATH"
    # Hand PATH to the rest of a Claude Code session (SessionStart hooks get CLAUDE_ENV_FILE).
    if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
      echo "export DOTNET_ROOT=\"$HOME/.dotnet\" PATH=\"$HOME/.dotnet:\$PATH\" DOTNET_CLI_TELEMETRY_OPTOUT=1" >>"$CLAUDE_ENV_FILE"
    fi
  fi
  tests="$WINDOWS_DIR/$WINDOWS_CORE_TESTS/$WINDOWS_CORE_TESTS.csproj"
  [ -n "$WINDOWS_CORE_TESTS" ] && [ -f "$tests" ] && has dotnet && dotnet restore "$tests" -v q --nologo >/dev/null 2>&1
fi

# Node 24 and the landing dependencies, which bring its build tool.
if [ -n "$LANDING_DIR" ] && [ -d "$LANDING_DIR" ]; then
  if ! has node; then
    echo "warning: Node isn't installed; the landing checks won't run" >&2
  elif [ "$(node -p 'process.versions.node.split(".")[0]')" -lt 24 ]; then
    echo "warning: Node $(node -v) is older than Node 24" >&2
  fi
  if has npm && [ ! -d "$LANDING_DIR/node_modules" ]; then
    echo "Installing landing dependencies"
    npm ci --prefix "$LANDING_DIR" --no-audit --no-fund >/dev/null || echo "warning: npm ci failed in $LANDING_DIR/" >&2
  fi
fi

echo "Linux setup done: $(has dotnet && dotnet --version || echo 'no dotnet'), node $(node -v 2>/dev/null || echo none)"

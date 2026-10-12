# Deskling tools

Python tools (standard library only, unless noted), shell scripts and agent skills for companion apps built on
Deskling: localization, phrase sync, recorded voices, character art, sounds, fonts, screenshots, verification and the
agent hook. Every tool reads the app's `companion.json` at its root (found by walking up from the current directory,
or `$COMPANION_ROOT`); the schema is [`companion.schema.json`](companion.schema.json). No tool names an app.

| Tool | What |
|---|---|
| `companion.py` | Loads and validates `companion.json`; `get`, `path`, `root`, `validate`, `env` (paths and path patterns for shell), `pins` (the Deskling version everywhere it's pinned). |
| `l10n.py` | String Catalog gaps, work files for translators, apply with validation, `.resw`/`.po` exports; language-scoped phrases. |
| `phrases.py` | Keeps the catalog's `phrase.<id>` (and `.n` for jokes) keys in step with `phrases.json`. |
| `voice.py`, `voice-review.html` | Records, reviews and publishes the character's voices with ElevenLabs; a clip per joke part. |
| `character.py` | Compiles the app's skins (vector or raster; `characters` in `companion.json`) to the JSON and Swift both cores draw from; `--check`. |
| `synth.py` | The sound engine; the app's recipes module defines `sounds(engine)`. |
| `fonts.py` | Builds the display face from a pinned variable font (needs fontTools). |
| `screenshots.sh`, `store-screenshots.sh`, `store-screenshots.py` | Window captures in light and dark (failing on the problems `ScreenshotCapture` reports: content taller than its window, a shot not drawn or written); App Store images (needs Pillow). |
| `check-layout.sh` | `screenshots.sh` in every language (or the ones named), naming the languages whose windows don't fit. |
| `install-dev.sh`, `dev-icon.swift` | The Debug build in `~/Applications/<devAppName>.app`; its DEV-banded icon. |
| `conformance.sh` | Regenerates the app's vectors from its Swift core. |
| `verify.sh` | Runs the checks that apply to the changed paths, plus the pin check. |
| `setup-linux.sh` | Prepares a Linux container (cloud agents): submodules, .NET, landing dependencies. |
| `hooks/hook.py` | Agent guardrails: generated files, the stop-time checks, the Windows parity reminder. |
| `skills/` | `localization`, `add-phrase`, `add-setting`, `conformance`, `voice`, `windows-port`, `release-mac`, `review-invariants`. |

Tests: `python3 tools/tests/run.py` (over `tools/tests/fixture-app`, a minimal app; `tools.yml` runs them).

## Adopting the tools in an app

1. **Submodule, pinned to the release.** Vendor Deskling at the tag the app pins in SwiftPM and NuGet:

   ```sh
   git submodule add https://github.com/vmihalachi/deskling deskling
   git -C deskling checkout v0.5.0 && git add deskling
   ```

   The submodule is tools and skills only; the code still comes from the package registries. `scripts/verify.sh`
   fails whenever the submodule's tag, `deskling.tag`, the core `Package.swift`'s `exact:`, the XcodeGen spec's
   `exactVersion` and the `Deskling.*` NuGet versions disagree (`python3 deskling/tools/companion.py pins` shows them).

2. **`companion.json`** at the app's root, every section the app has (see the schema and the drafts in
   `tools/tests/*.companion.json`). Check it: `python3 deskling/tools/companion.py validate`.

3. **Two-line wrappers**, so the documented commands keep working, e.g. `scripts/verify.sh`:

   ```sh
   #!/bin/sh
   exec "$(dirname "$0")/../deskling/tools/verify.sh" "$@"
   ```

   and the same for `setup-linux.sh`, `conformance.sh`, `screenshots.sh`, `check-layout.sh`, `install-dev.sh` (the scheme's post-action);
   Python tools are called directly (`python3 deskling/tools/l10n.py status`).

4. **The hook.** Claude Code, `.claude/settings.json`:

   ```json
   {
     "hooks": {
       "PreToolUse": [{ "matcher": "Read|Edit|MultiEdit|Write|NotebookEdit|Grep|Bash", "hooks": [
         { "type": "command", "command": "python3 \"$CLAUDE_PROJECT_DIR/deskling/tools/hooks/hook.py\" pre", "timeout": 10 }] }],
       "PostToolUse": [{ "matcher": "Edit|MultiEdit|Write|NotebookEdit|Bash", "hooks": [
         { "type": "command", "command": "python3 \"$CLAUDE_PROJECT_DIR/deskling/tools/hooks/hook.py\" post", "timeout": 60 }] }],
       "Stop": [{ "hooks": [
         { "type": "command", "command": "python3 \"$CLAUDE_PROJECT_DIR/deskling/tools/hooks/hook.py\" stop", "timeout": 180 }] }],
       "SessionStart": [{ "matcher": "startup", "hooks": [
         { "type": "command", "command": "[ \"$CLAUDE_CODE_REMOTE\" != true ] || \"$CLAUDE_PROJECT_DIR/scripts/setup-linux.sh\"", "timeout": 600 }] }]
     }
   }
   ```

   Codex, `.codex/config.toml` (`[features] hooks = true`): `PreToolUse`/`PostToolUse` with matcher
   `^(Bash|apply_patch)$` and `Stop`, each running
   `python3 "$(git rev-parse --show-toplevel)/deskling/tools/hooks/hook.py" <pre|post|stop>`. GitHub Copilot,
   `.github/hooks/<app>.json`: `PreToolUse` and `PostToolUse` running `python3 deskling/tools/hooks/hook.py pre|post`.
   The hook reads the guards, the slow-check table and the parity patterns (`hooks.*`) from `companion.json`, and
   fails open when the file is missing or invalid.

5. **Skills.** Symlink the generic ones next to the app's own:

   ```sh
   for s in localization add-phrase add-setting conformance voice windows-port release-mac review-invariants; do
     ln -s "../../deskling/tools/skills/$s" ".agents/skills/$s"
   done
   ```

   They read the app's prose from its `DESIGN.md` (the character's voice, the glossary, the palette) and from
   `.agents/notes/<skill>.md` when present (voices chosen, the team ID, who reviews which language);
   `review-invariants` adds the app's `.agents/review-appendix.md` to its checklist.

6. **The app's own files** the tools read: `l10n.rulesFile` (transcreation rules for this app), the voice config and
   direction (`voice.config`, `voice.direction`), the sounds recipes (`sounds.recipes`), the store captions, the
   character design folder (`characters.design`).

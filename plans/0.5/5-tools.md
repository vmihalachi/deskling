# 5: `tools/`: companion.json, the scripts, the hook, the skills

> Sequence 5 of `README.md` (read *`tools/`* and *Language-scoped phrases*). Needs 2 (`character.py` is already here).
> One session; Python 3, no dependencies.

## Read only these

- `README.md` here; `tools/character.py` (from sequence 2); `scripts/verify.sh`; `.agents/skills/*/SKILL.md` (the house style).
- From sunnysays: `macos/scripts/{l10n,phrases,voice,synth-sounds,fonts,store-screenshots}.py`, `macos/scripts/test_{l10n,voice}.py`,
  `macos/scripts/{screenshots,store-screenshots,install-dev,conformance}.sh`, `macos/scripts/dev-icon.swift`, `scripts/{verify,setup-linux}.sh`,
  `.claude/hooks/{hook,test_hook}.py`, `.claude/settings.json`, `.codex/config.toml`, `.github/hooks/sunnysays.json`, `.agents/skills/*/SKILL.md`,
  `.github/review-checklist.md`, `macos/scripts/voice-config.json` (structure only). `../pagliacci/companion.json` (the second instance).
  Never the `.xcstrings`, `.resw`, voices manifest or `conformance/*` files.

## Steps

| Step | Commit | What |
|---|---|---|
| 5.1 | `tools: companion.json loader and schema` | `tools/companion.py` (find the root by walking up from cwd or `$COMPANION_ROOT`; load, validate against `tools/companion.schema.json`, `get <dotted.path>`; importable by the other scripts), the schema with every section in the README, `tools/tests/fixture-app/` (a minimal app: `companion.json`, a tiny String Catalog, `conformance/phrases.json` with a line, a sequence and an `it`-only line, a rig, a voice config) and `tools/tests/run.py`. Check both `../sunnysays/companion.json` (written in its M9; draft one here under `tools/tests/`) and `../pagliacci/companion.json` validate. |
| 5.2 | `tools: l10n.py, phrases.py` | Parameterized by `companion.json` (paths, catalogs, languages, glossary, keep-names, rules file, the xcodebuild project/scheme and the stringsdata filter, `.resw` and `.po` outputs); **language scoping** (a `phrase.<id>` or `.n` key is required only in its phrase's `languages`; other languages marked "do not translate" and skipped by `check`/`status`/`export`; `export-windows` omits them); `phrases.py` syncs sequence parts as separate keys with part comments and the `languages`. `test_l10n.py` over the fixture app. |
| 5.3 | `tools: voice.py` | Paths, voices list (`voice.voices`, e.g. `["male","female"]` or `["clown"]`), keychain service, site copy from `companion.json`; clip ids per part (`<id>.<n>` from `keys[n-1]`), only in the phrase's languages; the review page lists parts under their joke. `test_voice.py` over the fixture app. |
| 5.4 | `tools: synth, fonts, screenshots, store screenshots, install-dev, dev-icon, conformance.sh` | `synth.py` = the engine (wav writer, tone and envelope helpers) importing the app's `sounds.recipes` module; **safety net:** render sunnysays' five recipes (copied into the fixture app) and `cmp` against `../sunnysays/macos/SunnySays/Resources/Sounds/*.wav`. The shell scripts read names and env variables from `companion.json` (`screenshotEnv`, the derived-data path, the Dev app name); `dev-icon.swift` takes the catalog path and band color as arguments. |
| 5.5 | `tools: verify.sh, setup-linux.sh, hooks` | `verify.sh`: the path→checks table from `companion.json` (`mac.*`, `windows.*`, `landing`, `conformance`), the existing checks (formatting, tests, vectors current, voices pointer pushed, `l10n.py check`, …), and the always-on pin check (`git -C <laching.path> describe --tags --exact-match` = `laching.tag` = the `exact:` in `<mac.core>/Package.swift` and `<mac.dir>/project.yml` = every `Laching.*` NuGet version in `<windows.dir>/**/*.csproj`; prints the four values on disagreement). `setup-linux.sh`: `git submodule update --init <laching.path> <voice.dir>`, restores from the config. `hooks/hook.py`: the base guards built from the config (catalogs, `.resw`, `pbxproj`, voice manifest and clips, conformance), `guardedExtra`, the slow-check table and the xcodegen trigger from `mac.*`, parity regexes from `hooks.*`; `test_hook.py` over the fixture app. |
| 5.6 | `tools: skills` | `tools/skills/{localization,add-phrase,add-setting,conformance,voice,windows-port,release-mac,review-invariants}/SKILL.md` from sunnysays', written against `companion.json` names and reading app prose (the voice rubric, the glossary) from the app's `DESIGN.md` and a per-app notes file; `review-invariants/checklist.md` = the generic items of sunnysays' `.github/review-checklist.md` (privacy, stable ids, pure logic, text, UI tokens, parity, generated files, concurrency) with an "app appendix" hook; `add-phrase` covers sequences and `languages`. `tools/README.md`: how an app adopts the tools (submodule, wrappers, hook wiring, skill symlinks). |

## Finish

`python3 tools/tests/run.py` green; `scripts/verify.sh --all` ok; both `companion.json` instances validate. Hand to
sequence 6 and sunnysays' M9: `tools/README.md`'s adoption steps.

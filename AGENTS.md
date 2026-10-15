# AGENTS.md — Deskling

Guide for coding agents (Claude Code reads it through `CLAUDE.md`). Humans start at `README.md`.

**What it is:** an MIT Swift package plus .NET packages with the generic parts of a menu bar / tray app:
pure scheduling (`DesklingCore` / `Deskling.Core`), permission-free system signals, app-shell plumbing, a
StoreKit store, and the layer a desk companion app is built on (`DesklingCompanion` / `Deskling.Companion`, its UI
and `tools/`). Used by mybackhurts, sunnysays and Mister Pagliacci. The Swift code is the reference; the C# port replays its
conformance vectors.

## Doc map

| Question | Read |
|---|---|
| Where is X, what does each product import, what runs on which OS | `docs/layout.md` |
| The full conventions, with their examples | `docs/conventions.md` |
| How an issue is written and worked, the labels | `docs/issues.md` |
| Vector formats | `conformance/README.md` |
| The companion apps' tools | `tools/README.md` |
| Why the companion layer, shell and Windows shell are as they are | `plans/0.5/`, `plans/0.7/`, `plans/0.8/` |

## Where things live

`Sources/Deskling{Core,System,Shell,Companion,CompanionUI,Store,Testing}/` are the Swift products, one folder each,
with `Tests/<Product>Tests/`; `dotnet/` holds their C# mirrors (`Deskling.Core`, `Deskling.Companion`,
`Deskling.Companion.Shell`, `Deskling.Testing`, `Deskling.Windows`, `Deskling.Windows.Companion`, their tests and
`Fixture.App`); `conformance/` the generated vectors (never hand-edit); `tools/` the companion apps' tools;
`scripts/` the repo's own scripts; `plans/` the released designs' record. The folder-by-folder map, with what each
product imports and what runs where, is `docs/layout.md`.

## Commands

```sh
swift build && swift test                      # needs macOS (DesklingSystem/Shell/Store import AppKit, CoreAudio, StoreKit)
DESKLING_WRITE_CONFORMANCE=1 swift test --filter 'ConformanceTests/testWriteVectors'   # or scripts/conformance.sh
xcrun swift-format lint -r --strict --configuration .swift-format Sources Tests
cd dotnet && dotnet test Deskling.Core.Tests   # anywhere with the .NET 10 SDK (Deskling.Windows builds on Windows only)
cd dotnet && dotnet test Deskling.Companion.Tests   # the companion vectors, the same anywhere
cd dotnet && dotnet test Deskling.Companion.Shell.Tests   # the companion shell's logic, the same anywhere
cd dotnet && dotnet test Deskling.Windows.Companion.Tests   # Windows only: the speaker, the chime and the Windows shell's services
dotnet run --project dotnet/Fixture.App        # Windows only: the fixture app, for the checks by hand
scripts/fixture-screenshots.ps1 [-Only home]   # Windows only: the fixture's windows to PNGs, light and dark; fails on layout problems
python3 tools/windows/check-winui.py dotnet/Fixture.App dotnet/Deskling.Windows.Companion   # the WinUI traps under Native AOT
dotnet format whitespace dotnet --folder       # C# style (dotnet/.editorconfig)
python3 tools/tests/run.py                     # the tools' tests
scripts/verify.sh                              # runs what applies to your changes; --all for everything
```

In a Linux container only the .NET side runs. Verify Swift through `swift.yml` (push a branch, dispatch the workflow,
read the failed job's log). Vectors need the Swift code to regenerate: without a Mac, dispatch `regenerate-vectors.yml`
on your branch and pull the commit it pushes. It refuses the default branch (its first step fails there), so a vector
change reaches `master` only through a pull request.

## Planned work

Planned work lives in [GitHub Issues](https://github.com/vmihalachi/deskling/issues), not in files (`gh issue list`,
`gh issue view N`). Read the issue before starting and work only on what it says; a `decision` issue waits for the
owner's choice. Comment what you found as you go, tick the checkboxes, close it from the commit (`Fixes #N`).
Something found along the way is a new issue, not a note in a file. The labels and the shape of an issue:
`docs/issues.md`.

## Finding code

The map in `docs/layout.md`: every Swift file has a C# twin, at the same relative path unless the map says otherwise,
so `grep -rn` on a type name finds both sides.

## Skills

Step-by-step procedures for the multi-step changes live in `.agents/skills/` (symlinked as `.claude/skills/`, `.junie/skills/` and `.github/skills/`). Read the matching `SKILL.md` before starting:

- `add-scheduler-behavior`: new or changed `ReminderScheduler` behavior, from config field to C# port.
- `port-to-csharp`: mirroring any Swift change in `dotnet/`.
- `release`: cutting `vX.Y.Z`.

## Conventions

The short form; the full rules with their examples are in `docs/conventions.md`.

- **Public API is deliberate:** every public type gets a doc comment (what it does, needs and never does); keep the
  type names mybackhurts already uses.
- **Swift 6, strict concurrency, macOS 14+, no dependencies.** `DesklingCore` is Foundation-only and
  `DesklingCompanion` Foundation and Observation only (both build on Linux); AppKit, SwiftUI, CoreAudio,
  AVFoundation, IOKit and Carbon stay in `DesklingSystem`, `DesklingShell` or `DesklingCompanionUI`.
- **Package code never names an app** and ships no strings: apps inject their bank, defaults, skins, palette,
  voices and every label (`Text`, string or copy struct); `tools/` reads everything app-specific from `companion.json`.
- **No permission prompts, no network.**
- **Behavior changes are additive and vector-tested:** a config field whose default keeps every vector
  byte-identical, a unit test, a vector case and the C# port in the same change. `conformance/companion` pins
  mechanism over fixtures; each app pins its own content.
- **Rasters never shear:** rotation, translation and uniform scale only; pose vectors pin affines, never pixels.
- **C# mirrors Swift** folder for folder and file for file; the deviations (the store under `Deskling.Core/Store/`,
  `Deskling.Testing`'s own layout, the companion shell mirrored by role) are marked in `docs/layout.md`. Source-generated
  JSON only: the apps publish with Native AOT.
- **Versioning:** SemVer, one `vX.Y.Z` tag for both sides, a CHANGELOG line per user-visible change under
  *Unreleased*, the version bumped in all five places in the same change (`scripts/verify.sh` checks they agree).
- Formatting is enforced in CI: `.swift-format` (4 spaces, 140 columns) and `dotnet/.editorconfig`.

## Sessions

One task per session: start a new one for unrelated work and hand off follow-ups instead of continuing here. Before searching for code you don't know the location of, use the `context-explorer` subagent (or `jbcontext search`) so the reads stay out of this context. Read big files by outline (`grep -n '^\(func\|class\|struct\|extension\|public\|##\)'`) and then a range, never whole; the hook refuses a whole read over 600 lines. `/compact` at a milestone in a long session.

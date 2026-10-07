# AGENTS.md — Deskling

Guide for coding agents (Claude Code reads it through `CLAUDE.md`). Humans start at `README.md`.

**What it is:** an MIT Swift package plus .NET packages with the generic parts of a menu bar / tray app:
pure scheduling (`DesklingCore` / `Deskling.Core`), permission-free system signals, app-shell plumbing and a
StoreKit store. Used by mybackhurts and sunnysays. The Swift code is the reference; the C# port replays its
conformance vectors.

## Layout

| Path | Contents |
|---|---|
| `Package.swift`, `Sources/Deskling{Core,System,Shell,Store,Testing}/` | The Swift products (one folder each). `DesklingCore` imports Foundation only: `Scheduling/`, `Random/`, `Input/`, `Localization/`. |
| `Tests/<Product>Tests/` | XCTest, one test target per product. `DesklingCoreTests/Conformance/` generates and replays `conformance/`. |
| `conformance/` | Generated vectors (`scheduler/`). Never hand-edit; `scripts/conformance.sh`. Formats in `conformance/README.md`. |
| `dotnet/Deskling.Core/` | Pure C# mirror of `DesklingCore`, file for file. `IsAotCompatible`. |
| `dotnet/Deskling.Windows/` | Win32 / Windows App SDK services (`net10.0-windows`, no XAML). Compiles on Linux and macOS with `EnableWindowsTargeting`. |
| `dotnet/Deskling.Core.Tests/` | xUnit; `Conformance/` replays every vector. |
| `scripts/` | `verify.sh` (checks for what changed), `conformance.sh`. |
| `.github/workflows/` | `swift.yml` (macos-26), `dotnet.yml` (ubuntu + windows), `release.yml` (tags → NuGet + GitHub release). |

## Commands

```sh
swift build && swift test                      # needs macOS (DesklingSystem/Shell/Store import AppKit, CoreAudio, StoreKit)
DESKLING_WRITE_CONFORMANCE=1 swift test --filter Conformance   # or scripts/conformance.sh
xcrun swift-format lint -r --strict --configuration .swift-format Sources Tests
cd dotnet && dotnet build && dotnet test       # anywhere with the .NET 10 SDK
dotnet format whitespace dotnet --folder       # C# style (dotnet/.editorconfig)
scripts/verify.sh                              # runs what applies to your changes; --all for everything
```

In a Linux container only the .NET side runs. Verify Swift through `swift.yml` (push a branch, dispatch the workflow,
read the failed job's log).

## Conventions

- **Public API is deliberate.** Every public type gets a doc comment saying what it does, what it needs and what it
  never does (for example "never prompts for a permission"). Keep type names that mybackhurts already uses
  (`ReminderScheduler`, `SchedulerConfig`, `Clock`, `BusyStateProvider`…) so adopting the package is an import swap.
- **Swift 5 language mode**, macOS 14+, no dependencies. `DesklingCore` stays Foundation-only; anything that imports
  AppKit, CoreAudio, AVFoundation, IOKit or Carbon goes in `DesklingSystem` or `DesklingShell`.
- **No permission prompts, no network.** System signals read device and window state. If an API would prompt the user
  or touch the network, it doesn't belong here.
- **Behavior changes are additive and vector-tested.** New scheduler behavior gets a config field whose default keeps
  every existing vector byte-identical, a unit test, a vector case, and the C# port in the same change
  (`cd dotnet && dotnet test` tells you what to port).
- **C# mirrors Swift** folder for folder and file for file (`Sources/DesklingCore/Scheduling/ReminderScheduler.swift` ↔
  `dotnet/Deskling.Core/Scheduling/ReminderScheduler.cs`). Core folders: `Scheduling/`, `Random/`, `Input/`. `DesklingSystem`
  and `DesklingShell` map onto `Deskling.Windows/System/` and `Deskling.Windows/Shell/`. Tests mirror the same folders, with
  `Conformance/` in each Core test project.
  No reflection-based JSON (source-generated contexts only): the apps publish with Native AOT and warnings are errors.
- **Strings:** the package ships none. Titles for notification actions and the like are passed in already localized.
- **Versioning:** SemVer, one `vX.Y.Z` tag for both sides; `CHANGELOG.md` gets a line per user-visible change under
  *Unreleased*. Bump `Deskling.version` and `DesklingInfo.Version` with the tag.
- Formatting is enforced in CI: `.swift-format` (4 spaces, 140 columns) and `dotnet/.editorconfig`.

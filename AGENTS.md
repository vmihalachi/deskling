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
| `dotnet/Deskling.Windows/` | Win32 / Windows App SDK services (`net10.0-windows`, no XAML). Restores on Linux and macOS with `EnableWindowsTargeting`, but its Windows App SDK PRI step needs Windows: build it through `dotnet.yml`'s windows job. `System/` and `Shell/` (with `Shell/Localization/`). |
| `dotnet/Deskling.Core.Tests/` | xUnit; `Conformance/` replays every vector. |
| `scripts/` | `verify.sh` (checks for what changed), `conformance.sh`. |
| `.github/workflows/` | `swift.yml` (macos-26), `dotnet.yml` (ubuntu + windows), `release.yml` (tags → NuGet + GitHub release). |

## Commands

```sh
swift build && swift test                      # needs macOS (DesklingSystem/Shell/Store import AppKit, CoreAudio, StoreKit)
DESKLING_WRITE_CONFORMANCE=1 swift test --filter 'ConformanceTests/testWriteVectors'   # or scripts/conformance.sh
xcrun swift-format lint -r --strict --configuration .swift-format Sources Tests
cd dotnet && dotnet test Deskling.Core.Tests   # anywhere with the .NET 10 SDK (Deskling.Windows builds on Windows only)
dotnet format whitespace dotnet --folder       # C# style (dotnet/.editorconfig)
scripts/verify.sh                              # runs what applies to your changes; --all for everything
```

In a Linux container only the .NET side runs. Verify Swift through `swift.yml` (push a branch, dispatch the workflow,
read the failed job's log).

## Conventions

- **Public API is deliberate.** Every public type gets a doc comment saying what it does, what it needs and what it
  never does (for example "never prompts for a permission"). Keep type names that mybackhurts already uses
  (`ReminderScheduler`, `SchedulerConfig`, `Clock`, `BusyStateProvider`…) so adopting the package is an import swap.
- **Swift 6 language mode** (strict concurrency; public value types are `Sendable`), macOS 14+, no dependencies. `DesklingCore` stays Foundation-only; anything that imports
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
  *Unreleased*. Bump the version in the same change as the tag, in all four places: `Deskling.version`
  (`Sources/DesklingCore/Deskling.swift`), `DesklingInfo.Version` (`dotnet/Deskling.Core/DesklingInfo.cs`),
  the `DesklingVersion` default in `dotnet/Directory.Build.props`, and `site/index.html` (the `vX.Y.Z` label in the
  header and both `exact:` install snippets). Move *Unreleased* to the new section. `scripts/verify.sh` fails when
  these disagree, so run it before committing.
- Formatting is enforced in CI: `.swift-format` (4 spaces, 140 columns) and `dotnet/.editorconfig`.

<!-- jbcontext-instructions-start -->
# Tools

## Semantic Code Search (jbcontext)

You have access to `jbcontext search` for searching the codebase semantically.
Use the `/context-search` skill or run `jbcontext search "<query>"` to find code by meaning, not just keywords.

### Query Tips

- Be descriptive: "Where is a function that validates user email addresses" > "email"
- Include context: "Find error handling middleware for HTTP requests with logging"
- Specify what you're looking for: "React component that renders a modal dialog"

### When to use

`jbcontext search` is a **code-discovery** tool. Reach for it only when a task requires finding or understanding code whose location you don't already know.

Skip it — go straight to the right tool — when:
- the task names the exact file, class, or symbol (keyword grep is faster);
- the relevant file is already open or identified;
- the task doesn't involve locating code at all — git operations (rebase, merge, commit), running tests or builds, shell/statusline/config setup, or reviewing a diff you already have.

### How to use it
- Start with `jbcontext search` before planning, editing, or exact search in unfamiliar code when you do not yet know the right file, subsystem, implementation, or related test.
- Use one focused natural-language query per search.
- Do not start with grep, ripgrep, or find when the search problem is still semantic or exploratory.
- Inspect the first relevant file or directory before issuing another broad semantic search.
- Use another broad `jbcontext search` only if the local path stops being productive.
- Once you know the relevant file, symbol, or directory, switch to direct file reads or exact search for local inspection.
- If you search again after finding a relevant area, narrow with `-p <path>`.

<!-- jbcontext-instructions-end -->
# AGENTS.md — Deskling

Guide for coding agents (Claude Code reads it through `CLAUDE.md`). Humans start at `README.md`.

**What it is:** an MIT Swift package plus .NET packages with the generic parts of a menu bar / tray app:
pure scheduling (`DesklingCore` / `Deskling.Core`), permission-free system signals, app-shell plumbing, a
StoreKit store, and the layer a desk companion app is built on (`DesklingCompanion` / `Deskling.Companion`, its UI
and `tools/`). Used by mybackhurts, sunnysays and Mister Pagliacci. The Swift code is the reference; the C# port replays its
conformance vectors.

## Layout

| Path | Contents |
|---|---|
| `Package.swift`, `Sources/Deskling{Core,System,Shell,Companion,CompanionUI,Store,Testing}/` | The Swift products (one folder each). `DesklingCore` imports Foundation only: `Scheduling/`, `Random/`, `Input/`, `Localization/`, `Settings/`. `DesklingCompanion` imports Foundation and Observation only: `Phrases/`, `Cadence/`, `Rules/`, `Settings/`, `Coordination/`, `Desktop/`, `Figures/`, `Skins/`; it and `DesklingTesting` (`Fixtures/`, and `Conformance/`: the suite, the generators, the standard case lists and `CompanionVectors` apps reuse) build on Linux. `DesklingShell` has `Speech/` (speaker, sequencer, voice library, chime) and the status item; `DesklingCompanionUI` (macOS) puts the companion on screen: signals, glyph, character view, desktop presence, delivery, notifier, `Theme/` (palette, spacing, speech bubble, status pill, the window chrome), `Shell/` (the app model apps subclass, its delegate, the menus, the copy structs), `Settings/` (rows and editors), `Support/` (the Support window) and `Screenshots/` (the screenshot script). A view or model here takes its words from the app: a `Text` per label or a copy struct, never a default. |
| `Tests/<Product>Tests/` | XCTest, one test target per product. `DesklingCoreTests/Conformance/` generates and replays `conformance/scheduler`, `DesklingCompanionTests/Conformance/` `conformance/companion`. |
| `Tests/Fixtures/characters/` | Art sources for the fixture skins (`fixture-vector`: SVGs; `fixture-raster`: layer PNGs), compiled by `scripts/fixture-art.sh` into `DesklingTesting/Fixtures/Fixture*Art.swift` and `conformance/companion/characters/`. |
| `conformance/` | Generated vectors (`scheduler/`, `companion/`). Never hand-edit; `scripts/conformance.sh`. Formats in `conformance/README.md`. |
| `dotnet/Deskling.Core/` | Pure C# mirror of `DesklingCore`, file for file, plus `Store/` (mirrors `DesklingStore`; its `MockStoreBackend` mirrors `DesklingTesting`'s, shipped here because apps use it in preview modes). `IsAotCompatible`. |
| `dotnet/Deskling.Companion/` | Pure C# mirror of `DesklingCompanion`, folder for folder, plus the loaders `Phrases/PhraseBankJson.cs` and `Figures/CharacterArtJson.cs`. `IsAotCompatible`. |
| `dotnet/Deskling.Companion.Shell/` | The platform-free half of a companion app's shell (0.8, `plans/0.8/`): the C# twin of `DesklingCompanionUI`'s and `DesklingShell`'s logic, mirrored **by role, not file for file** (*The map* in `plans/0.8/README.md`), written against small interfaces (`ISpeaking`, `IChimePlaying`, `ITimers`, the signal sources) so its tests run on any OS. `Speech/` (sequencer, voice library), `Performance/` (`DeliveryPerformer`, phrase localization, pending ids), `Signals/` (`SignalHub` and its source interfaces), `Desktop/` (layout and placement in y-down screen coordinates), `App/` (`CompanionAppModel<TWindow, TSettings>` over an `ICompanionHost<TWindow>`, the copy records, `CompanionMenus` building `MenuItem` data), `Screenshots/` (`SampleEnvironment`). `IsAotCompatible`. |
| `dotnet/Deskling.Testing/` | Mirrors `DesklingTesting` for .NET apps' test projects (no xunit): mocks, `Conformance/` (root, support, `Companion/*Replay.cs` bodies and `CompanionReplay`, one call per file kind), `Fixtures/` loading `conformance/companion/`, and `Shell/` (`ManualTimers`, `FakeSpeaker`, `FakeChime`, `FakeNotifier`, `FakeHost<TWindow>` with its status item, windows, desktop window and hotkey) for the shell's tests. |
| `dotnet/Deskling.Windows/` | Win32 / Windows App SDK services (`net10.0-windows`, no XAML). Restores on Linux and macOS with `EnableWindowsTargeting`, but its Windows App SDK PRI step needs Windows: build it through `dotnet.yml`'s windows job. `System/` and `Shell/` (with `Shell/Localization/`). |
| `dotnet/Deskling.Windows.Companion/` | The Windows half of the companion shell (0.8, `plans/0.8/`, in progress), over `Deskling.Companion.Shell` and `Deskling.Windows` (`net10.0-windows`, code only: no XAML). `Speech/` (`Speaker`: recorded clips and the speech synthesizer through `MediaPlayer`, kept out of the system media controls; `ChimePlayer`), `Shell/` (`WindowsCompanionHost<TWindow>` with the app's `CompanionViews`, `WindowManager<TWindow>` over `WindowSpec`s, `TrayStatusItem` with its glyph and panel, `GlyphRenderer`, `DispatcherTimers`, `HotKey`, `CompanionNotifier`, `MenuConversion`, `ReduceMotion`), `Signals/` (the system's mute, lock, power and appearance as the shell's signal sources). Builds and tests on Windows only, like `Deskling.Windows`. |
| `dotnet/Deskling.Core.Tests/`, `dotnet/Deskling.Companion.Tests/` | xUnit; `Conformance/` replays every vector (`scheduler/`, `companion/`). |
| `dotnet/Deskling.Companion.Shell.Tests/` | xUnit, any OS: the Swift shell tests ported case for case (`SpeechSequencerTests`, `VoiceLibraryTests`, `DeliveryPerformerTests`, `DesktopLayoutTests`, `FloatingPlacementTests`, `CompanionAppModelTests` with the launch and host cases Swift can't run), over the Swift tests' own `Resources/Voice` and sunflower. |
| `dotnet/Deskling.Windows.Tests/` | xUnit for `Deskling.Windows`; runs only in `dotnet.yml`'s windows job. |
| `dotnet/Deskling.Windows.Companion.Tests/` | xUnit for `Deskling.Windows.Companion`, an unpackaged host; runs only in `dotnet.yml`'s windows job. Speech tests play generated WAVs at no volume: real playback where there is an audio device, the failure path where there isn't (a CI runner), and the line ends either way. The shell tests cover what runs without a WinUI app (the glyph's drawing and timing, the menu conversion, the placement next to the tray, the signal adapters); windows, the panel and notifications are checked by running the fixture app. |
| `tools/` | Tools for companion apps, which vendor this repo as a `deskling/` submodule (`tools/README.md`): every one reads the app's `companion.json` (`companion.py`, `companion.schema.json`). `l10n.py`, `phrases.py`, `voice.py`, `character.py`, `synth.py`, `fonts.py`, the shell scripts (`verify.sh`, `setup-linux.sh`, `screenshots.sh`…), `hooks/hook.py`, `store/` (the listing's checks, App Store Connect push, Partner Center import), `landing/` (`@deskling/landing`, published to npm) and `skills/` (the apps symlink them). Shared by every app, so no file names one. Tests in `tools/tests/` over `fixture-app/`. |
| `scripts/` | `verify.sh` (checks for what changed), `conformance.sh`, `fixture-art.sh` (compiles the fixture skins; `--check`), `agent-stop-check.sh` (`verify.sh --fast` as an agent Stop hook). |
| `.agents/skills/` | Agent skills (see *Skills*); `.claude/skills`, `.junie/skills` and `.github/skills` are symlinks to it. `.codex/` holds Codex config and rules. |
| `plans/` | Design and sequence briefs for releases in progress: `plans/0.5/README.md` is the companion-layer design, `1-…`–`6-…` the sessions that build it; `plans/0.7/README.md` the companion shell (0.7.0: what the apps still repeat on top of 0.6 moves here), `1-…`–`6-…` its sessions; `plans/0.8/README.md` the Windows companion shell (0.8.0: `Deskling.Companion.Shell` and `Deskling.Windows.Companion`), `1-…`–`5-…` its sessions. |
| `.github/workflows/` | `swift.yml` (macos-26), `dotnet.yml` (ubuntu + windows), `release.yml` (tags → NuGet + GitHub release), `tools.yml` (the Python tools' tests), `regenerate-vectors.yml`, `copilot-setup-steps.yml` (Copilot's cloud agent). |

## Commands

```sh
swift build && swift test                      # needs macOS (DesklingSystem/Shell/Store import AppKit, CoreAudio, StoreKit)
DESKLING_WRITE_CONFORMANCE=1 swift test --filter 'ConformanceTests/testWriteVectors'   # or scripts/conformance.sh
xcrun swift-format lint -r --strict --configuration .swift-format Sources Tests
cd dotnet && dotnet test Deskling.Core.Tests   # anywhere with the .NET 10 SDK (Deskling.Windows builds on Windows only)
cd dotnet && dotnet test Deskling.Companion.Tests   # the companion vectors, the same anywhere
cd dotnet && dotnet test Deskling.Companion.Shell.Tests   # the companion shell's logic, the same anywhere
cd dotnet && dotnet test Deskling.Windows.Companion.Tests   # Windows only: the speaker, the chime and the Windows shell's services
dotnet format whitespace dotnet --folder       # C# style (dotnet/.editorconfig)
python3 tools/tests/run.py                     # the tools' tests
scripts/verify.sh                              # runs what applies to your changes; --all for everything
```

In a Linux container only the .NET side runs. Verify Swift through `swift.yml` (push a branch, dispatch the workflow,
read the failed job's log). Vectors need the Swift code to regenerate: without a Mac, dispatch `regenerate-vectors.yml`
on your branch and pull the commit it pushes.

## Finding code

The layout above is the map: every Swift file has a C# twin at the same relative path, so `grep -rn` on a type name
finds both sides. If `jbcontext` is installed (JetBrains Context; optional, not every environment has it),
`jbcontext search "<what the code does>"` finds code by meaning and `jbcontext search -p <dir> "<query>"` narrows it.
Use it when you don't know where something lives; grep once you have a name. The `context-search` skill and `context-explorer` agent are installed per user, not in this repository.

## Skills

Step-by-step procedures for the multi-step changes live in `.agents/skills/` (symlinked as `.claude/skills/`, `.junie/skills/` and `.github/skills/`). Read the matching `SKILL.md` before starting:

- `add-scheduler-behavior`: new or changed `ReminderScheduler` behavior, from config field to C# port.
- `port-to-csharp`: mirroring any Swift change in `dotnet/`.
- `release`: cutting `vX.Y.Z`.

## Conventions

- **Public API is deliberate.** Every public type gets a doc comment saying what it does, what it needs and what it
  never does (for example "never prompts for a permission"). Keep type names that mybackhurts already uses
  (`ReminderScheduler`, `SchedulerConfig`, `Clock`, `BusyStateProvider`…) so adopting the package is an import swap.
- **Swift 6 language mode** (strict concurrency; public value types are `Sendable`), macOS 14+ (desktop only: no iOS), no dependencies. `DesklingCore` stays Foundation-only and `DesklingCompanion`
  Foundation and Observation only (both build on Linux); anything that imports AppKit, SwiftUI, CoreAudio, AVFoundation,
  IOKit or Carbon goes in `DesklingSystem`, `DesklingShell` or `DesklingCompanionUI`.
- **Package code never names an app.** No app's skins, phrase bank, product ids, palette or voices, and no app name in
  a doc comment or a fixture: apps inject their bank, defaults, skins, palette and voices. The same goes for `tools/`,
  which reads everything app-specific from the app's `companion.json`.
- **A view or model in the package takes its words from the app.** `DesklingCompanionUI`'s shell, Settings rows,
  Support window and menus get every label as a `Text`, a string or a copy struct (`StatusCopy`, `MenuCopy`,
  `SupportCopy`…), never a default; the app's String Catalog and its `l10n.py` workflow are untouched. The look (the
  app's buttons, chips, titles, cards) comes in as styles or views; only chrome that is the same in every app lives
  here.
- **No permission prompts, no network.** System signals read device and window state. If an API would prompt the user
  or touch the network, it doesn't belong here.
- **Behavior changes are additive and vector-tested.** New scheduler behavior gets a config field whose default keeps
  every existing vector byte-identical, a unit test, a vector case, and the C# port in the same change
  (`cd dotnet && dotnet test` tells you what to port). New fields in a companion vector are optional and omitted at
  their default, so the apps' committed vectors stay byte-identical; the C# loaders read an absent field as the default.
- **`conformance/companion` pins mechanism, over fixtures.** The picker, cadence, quiet rules, coordinator, desktop
  presence and rig are pinned here over `DesklingTesting`'s fixture bank, defaults and skins; each app pins its own
  content (bank, defaults, picks, poses) with the same generators and replays.
- **Rasters never shear.** A character's transforms are rotation, translation and uniform scale only, so an image part
  draws under the same `affine` as a vector one. Pose vectors pin each layer's name, the six affine numbers and the
  content kind, never pixels; the menu bar glyph stays a vector line drawing for every skin.
- **C# mirrors Swift** folder for folder and file for file (`Sources/DesklingCore/Scheduling/ReminderScheduler.swift` ↔
  `dotnet/Deskling.Core/Scheduling/ReminderScheduler.cs`). Core folders: `Scheduling/`, `Random/`, `Input/`, `Localization/`, `Settings/`; Companion folders:
  `Phrases/`, `Cadence/`, `Rules/`, `Settings/`, `Coordination/`, `Desktop/`, `Figures/`, `Skins/`. `DesklingSystem`
  and `DesklingShell` map onto `Deskling.Windows/System/` and `Deskling.Windows/Shell/`. The exception is the companion shell
  (`Deskling.Companion.Shell`, `Deskling.Windows.Companion`): the Swift files mix logic and AppKit, so the C# side splits
  them and mirrors by role, with the same type and member names; `plans/0.8/README.md` maps every Swift file. Tests mirror the same folders, with
  `Conformance/` in each Core and Companion test project.
  No reflection-based JSON (source-generated contexts only): the apps publish with Native AOT and warnings are errors.
- **Strings:** the package ships none. Titles for notification actions and the like are passed in already localized.
- **Versioning:** SemVer, one `vX.Y.Z` tag for both sides; `CHANGELOG.md` gets a line per user-visible change under
  *Unreleased*. Bump the version in the same change as the tag, in all four places: `Deskling.version`
  (`Sources/DesklingCore/Deskling.swift`), `DesklingInfo.Version` (`dotnet/Deskling.Core/DesklingInfo.cs`),
  the `DesklingVersion` default in `dotnet/Directory.Build.props`, and `site/index.html` (the `vX.Y.Z` label in the
  header and both `exact:` install snippets). Move *Unreleased* to the new section. `scripts/verify.sh` fails when
  these disagree, so run it before committing.
- Formatting is enforced in CI: `.swift-format` (4 spaces, 140 columns) and `dotnet/.editorconfig`.

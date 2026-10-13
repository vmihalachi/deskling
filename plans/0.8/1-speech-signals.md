# 1: The pure package: speech, signals, delivery, desktop layout

> Sequence 1 of `README.md` (read *Decisions*, *Packages after 0.8.0*, *The map*). Needs nothing. One session, any OS
> with the .NET 10 SDK (the maintainer's Mac is fine).

## Read only these

- `README.md` here; `AGENTS.md`; `dotnet/Directory.Build.props`, `dotnet/Deskling.sln`, `dotnet/Deskling.Companion/Deskling.Companion.csproj`
  and `dotnet/Deskling.Companion.Tests/Deskling.Companion.Tests.csproj` (the shapes to copy); `dotnet/Deskling.Testing/Mocks.cs`.
- Swift: `Sources/DesklingShell/Speech/*.swift`, `FloatingPlacement.swift`; `Sources/DesklingCompanionUI/{DeliveryPerformer,SignalHub,DesktopLayout,Phrase+Localized,VoiceLibrary+Bank}.swift`;
  `Sources/DesklingSystem/{OutputRoute,PowerSource,ScreenLockWatcher,AppearanceWatcher}.swift` (only the members `SignalHub` reads).
- Tests to port: `Tests/DesklingShellTests/{SpeechSequencerTests,VoiceLibraryTests,FloatingPlacementTests}.swift` (and its `Resources/` the voice tests read),
  `Tests/DesklingCompanionUITests/{DeliveryPerformerTests,DesktopLayoutTests}.swift`, `Support/Fakes.swift`.

## Steps

| Step | Commit | What |
|---|---|---|
| 1.1 | `Companion.Shell: the package` | `dotnet/Deskling.Companion.Shell/` (`net10.0`, `IsAotCompatible`, `IsPackable`, references Core and Companion) and `Deskling.Companion.Shell.Tests` (xUnit, `net10.0`), both in `Deskling.sln`. `Deskling.Testing` references the new package. |
| 1.2 | `Companion.Shell: Speech` | `ISpeaking` (with `ClipTime`, and the static `ClipRate`/`SpeechRate` mappings from `Speaker.swift`), `IChimePlaying`, `SpeechPart`, `SpeechSequencer` over `ITimers` (define `ITimers` here: `IDisposable After(double seconds, Action work)`, `IDisposable Every(double seconds, Action work)`), `VoiceId`, `VoiceLibrary` (manifest through a source-generated `JsonSerializerContext`; `Hash` is the same SHA-256 hex as Swift). Ported tests; `ManualTimers` (advances a `MockClock` and fires what is due, in order) and `FakeSpeaker`/`FakeChime` in `Deskling.Testing`. |
| 1.3 | `Companion.Shell: Performance and signals` | `DeliveryPerformer` line for line, `PhraseLocalization` (`LocalizedParts`, `LocalizedText`), `VoiceLibrary`'s bank extension (`PendingIds`), `SignalHub` over `IIdleTimeProvider`, `IBusyStateProvider` and new `IAudioOutputState`, `IScreenLock`, `IPowerSource` (with `PowerState`), `IAppearance` (each with an `event Action` where Swift has `onChange`). Ported `DeliveryPerformerTests`; new `SignalHubTests` for the busy watch (a call mid-line stops speech once; `watchForDesktop` polls; power transitions fire `unplugged`, `charging`, `batteryLow` once). |
| 1.4 | `Companion.Shell: Desktop layout` | `DesktopLayout`, `FloatingPlacement` (using `Deskling.Companion.Figures`' `Point`/`Size`/`Rect`), `DesktopHide`. Ported tests. |
| 1.5 | `docs: Companion.Shell` | `AGENTS.md` layout rows and the *C# mirrors Swift* convention's exception for the shell (it mirrors by role; *The map* in `plans/0.8/README.md`), CHANGELOG *Unreleased*. `dotnet.yml`'s ubuntu job builds and tests the new project. |

## Finish

`cd dotnet && dotnet test Deskling.Companion.Shell.Tests` green on macOS; `dotnet format whitespace dotnet --folder`;
`scripts/verify.sh` ok. Hand to 2: the interfaces as built and any test that couldn't be ported, with why.

## Outcome (2026-10-13)

Done in one session on macOS; 47 tests green (`dotnet test Deskling.Companion.Shell.Tests`), Core and Companion
suites unchanged (210, 209). Differences from the brief, all deliberate:

- **Folders and namespaces:** `Speech/`, `Performance/` (not `Delivery/`: a namespace `…Delivery` would shadow the
  coordinator's `Delivery` record), `Signals/`, `Desktop/`; namespaces `Deskling.Companion.Shell.<Folder>`. 2's model
  goes in `App/` (`Deskling.Companion.Shell.App`, not `.Shell.Shell`).
- **Clips are file paths** (`string?`), not URLs. `VoiceLibrary` takes the folder and `allowTesting` explicitly (a NuGet
  package is always built Release, so there is no `isDebugBuild`); `VoiceLibrary.None(language)` is the no-voice library.
- **Only `ClipRate` is shared** (`SpeechRates.ClipRate`); the synthesizer's rate maps onto Windows' own range in 3.
- **The sequencer and performer don't cancel late timers**, as in Swift: a generation number ignores them, so the
  ported cases that run a cancelled beat still pass unchanged. `ManualTimers.RunNext` is the Swift tests' scripted
  `schedule` (oldest first, whatever the delay); `Advance` drives the repeating busy watch.
- **`FloatingPlacement` and `DesktopLayout` are in y-down screen coordinates** (Windows' virtual screen and the stored
  `DesktopPosition`); the Swift `topLeftOrigin` conversions have no twin. The ported placement cases flip each Swift
  rectangle with a helper, so the numbers read as in Swift.
- **New:** `SignalHubTests` (the Swift hub is only covered through the model), `BubbleMetrics` (Swift's `BubbleShape`
  statics, which `DesktopLayout.BubbleTailY` needs here).
- **Not ported:** the Swift `VoiceLibraryTests` cases about AVFoundation (`Speaker` with a broken clip, `ChimePlayer`
  with a missing sound) belong to 3's Windows `Speaker` and `ChimePlayer`.
- `release.yml` packs `Deskling.Companion.Shell` already, since `Deskling.Testing` depends on it now; the first push of a
  new package id goes through the same Trusted Publishing policy (check nuget.org accepts it at 0.8.0).

# Deskling

Building blocks for little desktop apps that live in the menu bar or the tray: the parts every such app needs
and nobody wants to write twice. Swift for macOS, .NET for Windows. MIT.

Deskling grew out of [mybackhurts](https://mybackhurts.app) and powers sunnysays; both apps ship it. It is
`0.x` until the second app has settled the API, so minor versions may change public types.

**No support.** Deskling is shared as is, for anyone to use, but it is built for these two apps: issues and pull
requests aren't monitored, and releases follow what the apps need.

## What's inside

| Swift product | .NET package | What it does |
|---|---|---|
| `DesklingCore` | `Deskling.Core` | Pure logic. `ReminderScheduler`: an interval timer with active hours and weekdays, quiet hours, an idle reset ("you were away, the timer restarts"), a hold while the user is busy with a grace period after, snooze, "after my next call" and pause. `Clock`, `IdleTimeProvider` and `BusyStateProvider` protocols so it runs on an injected clock. A seeded `SplitMix64` random source for deterministic jitter. `KeyShortcut`. No OS or UI dependencies, so it builds anywhere Swift or .NET runs. |
| `DesklingCompanion` | `Deskling.Companion` | The pure core of a desk companion: a phrase bank and picker (lines, jokes told in beats, tunes; scoped by skin, flavor and language), chatter cadence presets, quiet rules and mood, write-through settings an app subclasses, a coordinator that replays a whole day from a seed, desktop presence, skins, and a rig that animates vector or raster character art. The app injects its lines, defaults, skins and palette; the .NET side loads them from the JSON the Swift side writes (`PhraseBank.Parse`, `CharacterArt.Parse`). |
| `DesklingSystem` | `Deskling.Windows` | The real providers. Idle time from the last input event. **Call detection without permissions:** whether another app is using the camera or the microphone, read from device state (CoreAudio's process list on macOS, the capability consent store on Windows), never by opening a device. Full-screen detection. Power source (battery, charging, level). Output muted and route. Screen locked. Appearance changes. |
| `DesklingShell` | `Deskling.Windows` | Menu bar / tray app plumbing. A status item with a popover panel, a right-click menu and an animated glyph the app draws. Speech: a speaker (the system voice or a recorded clip), a sequencer that tells a joke in beats, a library of recorded voices that knows which clips are current, a chime player. A window manager that shows the Dock icon only while a window is open, a login item toggle, "was I launched at login?", a notification poster with categories and actions, a global hotkey that needs no Accessibility permission (Carbon on macOS, `RegisterHotKey` on Windows), a floating desktop window (macOS), a raw `Shell_NotifyIcon` tray icon host with a native menu, single-instance startup, a `.resw` lookup, drawing helpers (SVG path sampling, keyframes, a hand-drawn wobble). |
| `DesklingCompanionUI` | — | macOS: the companion on screen, over `DesklingCompanion`. `SignalHub` (the snapshot and the events from the system signals), the menu bar glyph, `CharacterView` (vector and image parts, the wake spring), `DesktopCompanion` (the character and its speech bubble on the desktop), `DeliveryPerformer` (bubble, blip, speech in beats, the notification), `CompanionNotifier`, a `CompanionPalette` the app sets, the speech bubble and status pill, and `ScreenshotCapture` for screenshot runs. The Windows twins come with the apps' ports. |
| `DesklingStore` | `Deskling.Core` (`Store/`) + `Deskling.Windows` (`StoreContextBackend`) | A purchase store for one-time unlockables and tips (StoreKit 2 on the Mac, the Microsoft Store on Windows), with the backend behind a protocol so tests use a mock. |
| `DesklingTesting` | `Deskling.Testing` | Fakes for every protocol, fixtures (a phrase bank, defaults, a vector and a raster skin) and the shared conformance vectors' helpers for the apps' own test targets: Swift generates an app's companion vectors, .NET replays them (each check returns its problems, so neither side needs a particular test framework). |

Everything in `DesklingSystem` reads state and never prompts. None of it talks to the network.

## Install

**Swift** (macOS 14+), in `Package.swift` or Xcode's package list, pinned to a tag:

```swift
.package(url: "https://github.com/vmihalachi/deskling", exact: "0.1.0")
```

**.NET** (10), from NuGet.org:

```sh
dotnet add package Deskling.Core
dotnet add package Deskling.Companion  # the desk companion layer
dotnet add package Deskling.Windows   # net10.0-windows; the tray, hotkey, idle and busy services
dotnet add package Deskling.Testing   # in test projects: fakes, fixtures, vector replays
```

## Using it

The package ships no strings, product ids or window lists: the app passes them in.

- **Scheduling.** `ReminderScheduler(config:clock:idle:busy:calendar:random:)`; call `tick()` every 20 s or so and
  act on `.remind`. `SchedulerConfig.intervalJitter` (seconds, default 0) randomizes each interval from the injected
  `RandomSource`; pass `SplitMix64(seed:)` to replay a run.
- **Signals (macOS).** `SystemBusyStateProvider(cameraCheck:)`: `.avFoundation` (default), `.coreMediaIO` for a sandboxed
  app without the camera entitlement, or `.off` (the microphone check alone catches nearly every call).
  `SystemPowerSource`, `SystemAudioOutput`, `AppearanceWatcher` and `ScreenLockWatcher` expose a current value and an
  `onChange` callback.
- **Shell (macOS).** `WindowManager<MyWindow>` takes an enum conforming to `WindowID` (title, size, placement, exclusivity)
  and a content provider; `NotificationPoster(categories:)` takes action ids with already localized titles and reports
  `onAction(categoryID, actionID)`; `GlobalHotKey(signature:)` registers `KeyShortcut`s by id; `LoginItem` and
  `LaunchContext.launchedAsLoginItem()` cover start-at-login. `FloatingWindow(content:size:anchor:)` floats SwiftUI
  content above other windows on every Space (not full-screen ones) without activating the app: click, drag, right-click
  menu and pinch callbacks, fades, a window riding along beside it (a speech bubble), and `FloatingPlacement` for the
  math (kept on a screen when displays change, a side facing the middle, a platform-neutral top-left origin).
- **Store.** `PurchaseStore(catalog: ProductCatalog(unlockables:tips:))`; `owned`, `isOwned(_:)`, `buy`, `tip`,
  `restore`; `error: PurchaseError?` is for the app to put into words. Tests use `MockStoreBackend`. On Windows the
  same store (`Deskling.Core.Store`) runs over `StoreContextBackend(dispatcher, windowHandle)`, which matches add-ons by
  their Partner Center Product ID and reports consumables fulfilled so tips can be bought again; `MockStoreBackend`
  ships in `Deskling.Core` there, for tests and an app's preview modes.
- **Windows.** The same shapes in `Deskling.Windows`: `TrayIconHost`, `GlobalHotKey` (`KeyShortcut` keeps the Mac
  shape; Carbon masks map onto `MOD_*`), `AppNotifier<TAction>`, `StartupService(taskId)`, `LocalSettingsStore`,
  `SingleInstance.Claim(key)`, `WavPlayer`, and the signals `PowerSource`, `OutputMute`, `AppearanceWatcher`,
  `SessionLockWatcher`. String lookup is `Loc` (`Loc.Configure("Resources")`, `Loc.UseLanguage(...)` at launch) and
  `Formats.Configure(new FormatKeys(...))` names the six catalog keys the formatters need.

## The two implementations stay identical

The Swift code is the reference. `conformance/` holds JSON vectors generated from it, and `dotnet/Deskling.Core.Tests`
replays every one, so a change in behavior on one side fails the other side's tests. See `conformance/README.md`.

## Develop

```sh
swift build && swift test                       # macOS: the package and its tests
scripts/conformance.sh                          # regenerate conformance/ from the Swift code
cd dotnet && dotnet test Deskling.Core.Tests    # .NET: Core builds and tests anywhere; Deskling.Windows builds on Windows (its PRI tooling)
scripts/verify.sh                               # the checks that apply to what you changed
```

Formatting: `xcrun swift-format format -i --configuration .swift-format <files>` and `dotnet format whitespace dotnet --folder`.
Coding agents: read `AGENTS.md`.

## Releases

One tag versions both sides. `git tag v0.1.0 && git push --tags` (or Actions → Release → Run workflow with the version, which
creates the tag on master) runs `release.yml`: it packs the NuGet packages, pushes them to NuGet.org through
[Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) (no stored key: a policy on nuget.org for
repository `vmihalachi/deskling` and workflow `release.yml`, plus the `NUGET_USER` repository variable holding the nuget.org
username; without them it says so and attaches the packages to the release instead) and drafts a GitHub release from
`CHANGELOG.md`.

## License

[MIT](LICENSE). © 2026 Vlad Mihalachi.

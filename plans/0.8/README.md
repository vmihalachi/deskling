# Deskling 0.8: the Windows companion shell

> The design for the 0.8.0 release, written 2026-10-13, before sunnysays' WinUI app (its M3.2) and Pagliacci's (its M6)
> are built on it. The sequences in this folder are self-contained briefs, one per working session: `1` and `2` (the
> pure package, any OS with the .NET 10 SDK) come first, in order; `3` (the Windows services) needs `2`'s interfaces
> and a Windows machine to run; `4` (the views) needs `3`; `5` releases. Then sunnysays builds its app on it (M3.2), and
> Pagliacci its own (M6). Start a sequence from its brief, not from this file, and read only the files the brief lists.

## Why

0.7 moved the Mac companion shell into `DesklingCompanionUI` (about 4,400 lines: the app model, the delivery
performer, the signal hub, the menus, the Settings rows, the Support window, the desktop character, the screenshot
script) and `DesklingShell` (about 1,800: speech, the status item, the window manager, the floating window). On
Windows there is no counterpart: `Deskling.Windows` has the raw services (tray icon, hotkey, idle, busy, power,
lock, appearance, mute, notifications, startup task, settings store, `.resw` lookup, store), and nothing above them.

Two apps need the layer above: sunnysays' M3.2 and Pagliacci's M6, which is planned as "after sunnysays' M3" so it
can copy. Copying is what 0.7 had to undo on the Mac (61% of the app layer repeated, and drifting). So the Windows
shell is written once, here, before either app: not extracted from two apps as 0.7 was, but ported from the Swift
shell, which is the reference and already has its tests.

## Decisions

| Topic | Decision |
|---|---|
| Packages | **Two new NuGet packages; `Deskling.Windows` is unchanged.** `Deskling.Companion.Shell` (`net10.0`, pure, `IsAotCompatible`): the half of the shell that needs no OS, written against small interfaces, so its tests run on macOS and Linux. `Deskling.Windows.Companion` (`net10.0-windows`, Windows App SDK): the Windows implementations of those interfaces and the on-screen parts. `Deskling.Windows` stays the generic tray-app kit (mybackhurts uses it and needs no companion code). Chosen by the owner 2026-10-13 over folding the pure half into `Deskling.Companion` (it would stop being `DesklingCompanion`'s twin) and over one Windows package (no shell test would run off Windows). |
| Mirroring | **By role, not file for file.** The Swift shell mixes logic and AppKit in one file; the C# one splits them. Every Swift type in `DesklingShell` and `DesklingCompanionUI` has a row in *The map* below saying where its C# twin lives, or why it has none. Type and member names stay the same, PascalCased (`CompanionAppModel`, `DeliveryPerformer`, `SignalHub`, `SpeechSequencer`, `VoiceLibrary`, `MenuCopy`…). |
| Behavior | **The Swift tests are the spec.** `CompanionAppModelTests`, `DeliveryPerformerTests`, the speech and voice library tests and `DesktopLayoutTests` are ported case for case into `Deskling.Companion.Shell.Tests`, over the same fixtures (`Deskling.Testing`'s fixture bank and defaults). A case that can't be ported is listed in the sequence's outcome with why. |
| Seams | Everything the Mac model reaches through AppKit or Foundation timers goes through an interface in the pure package: `ITimers` (one-shot and repeating, on the UI thread; tests drive a `ManualTimers` from `MockClock`), `IStatusItem`, `IWindowHost<TWindow>`, `IDesktopCompanion`, `IHotKey`, `ISpeaking`, `IChimePlaying`, `ICompanionNotifying`, `ISystemPreferences` (reduce motion, dark), and the signal sources (`IAudioOutputState`, `IScreenLock`, `IPowerSource`, `IAppearance`, next to `Deskling.Core`'s `IIdleTimeProvider` and `IBusyStateProvider`). The model never sees a WinUI type. |
| Views | **WinUI, code only: no `.xaml` in a package.** Controls are C# classes building their visual tree (as mybackhurts' `Controls/*.cs` do), so the package needs no XAML compiler, no PRI merge into the app, and compiles on macOS with `EnableWindowsTargeting` (only the Windows App SDK's PRI step needs Windows). The apps' own windows stay `.xaml`. |
| Copy | **Stays in the apps,** as on the Mac: the package ships no strings. The same copy structs (`StatusCopy`, `DesktopCopy`, `MenuCopy`, `SupportCopy`, `CompanionAppConfiguration`) carry the app's words, read from its `.resw` through `Loc.Get` with the Mac's catalog keys. |
| The look | **Stays in the apps:** palette resources, fonts, buttons, chips, cards, the window scene, Home, Onboarding, the panel's layout. The package draws only what is the same in every companion: the character canvas, the speech bubble, the status pill, the window chrome, the Settings rows' controls, the Support window's structure, in colors from a `CompanionPalette` the app passes (the Mac's `CompanionPalette`, as brushes). |
| Menus | `CompanionMenus` builds data (`MenuItem` records: command, label, separator, submenu, checked), not a native menu. `Deskling.Windows.Companion` turns it into `TrayMenuItem`s for `TrayIconHost`; the desktop character's right-click uses the same. |
| Desktop character | A layered, topmost, no-activate tool window hosting XAML (`DesktopWindow`, the twin of `FloatingWindow`). **Spike first** (sequence 3.1): per-pixel transparency with a WinUI island is the riskiest part of the release. If it can't be done cleanly, the fallback is a `DesktopAcrylicBackdrop`-free `Window` with a transparent `SystemBackdrop` and a region cut from the character's bounds; the spike's outcome says which. |
| Testing on Windows | `Deskling.Windows.Companion.Tests` (xUnit, unpackaged host, `dotnet.yml`'s windows job, like `Deskling.Windows.Tests`): the menu conversion, the glyph renderer's sizes, the palette brushes, `LayoutCheck` over each control in each size. Everything else is tested in the pure package. |
| Compatibility | **Additive.** Nothing in `Deskling.Core`, `Deskling.Companion`, `Deskling.Windows` or the Swift products changes meaning. `Deskling.Testing` gains the fakes. |
| Version | 0.8.0. The Swift side ships unchanged under the same tag (one tag for both sides). |

## Packages after 0.8.0

| Package | Depends on | Holds |
|---|---|---|
| `Deskling.Core` | — | unchanged |
| `Deskling.Companion` | Core | unchanged |
| `Deskling.Companion.Shell` (new, `net10.0`) | Core, Companion | `Speech/` (`ISpeaking`, `IChimePlaying`, `SpeechPart`, `SpeechSequencer`, `VoiceId`, `VoiceLibrary`), `Signals/` (`SignalHub` and its source interfaces), `Performance/` (`DeliveryPerformer`, `PhraseLocalization`: the twin of `Phrase+Localized`, `VoiceLibrary+Bank`), `Desktop/` (`DesktopLayout`, `FloatingPlacement`, `IDesktopCompanion`, `DesktopHide`), `App/` (`CompanionAppModel<TWindow, TSettings>`, `WindowRoles`, the copy structs, `CompanionSounds`, `CompanionAppConfiguration`, `CompanionMenus` + `MenuItem`, `ICompanionNotifying`, `IStatusItem`, `IWindowHost<TWindow>`, `IHotKey`, `ITimers`, `ISystemPreferences`), `Screenshots/` (`ScreenshotScript`'s shot list and sample environment, `SilentNotifier`). |
| `Deskling.Windows` | Core | unchanged |
| `Deskling.Windows.Companion` (new, `net10.0-windows`) | Companion.Shell, Windows | `Speech/` (`Speaker`: recorded clips and `Windows.Media.SpeechSynthesis` through one `MediaPlayer`; `ChimePlayer` over `WavPlayer`), `Signals/` (adapters from `Deskling.Windows.System` to the source interfaces), `Shell/` (`DispatcherTimers`, `TrayStatusItem` over `TrayIconHost` with the flyout, `GlyphRenderer` (GDI+ as mybackhurts' `TrayGlyph`), `WindowManager<TWindow>`, `HotKey` over `GlobalHotKey`, `CompanionNotifier` over `AppNotifier`, `CompanionApp`: single instance, activation, startup launch, the `Program` skeleton), `Desktop/` (`DesktopWindow`, `DesktopCompanion`), `Views/` (`CharacterCanvas`, `SpeechBubble`, `StatusPill`, `CompanionPalette`, the chrome: `WindowInset`, `ActionBar`, `PanelRow`, `Pointer`), `Settings/` (`ActiveHoursEditor`, `QuietHoursEditor`, `ShortcutRecorder`, `LaunchAtStartupToggle`, `LanguageRow`, `NotificationPermissionNote`, `SettingsFooter`), `Support/` (`SupportWindow`, `SupportCopy`), `Screenshots/` (`ScreenshotCapture`: renders each window to PNG, light and dark, with `LayoutCheck`). |
| `Deskling.Testing` | Core, Companion, Companion.Shell | gains `FakeSpeaker`, `FakeChime`, `FakeNotifier`, `ManualTimers`, `FakeStatusItem`, `FakeWindowHost<TWindow>`, `FakeDesktopCompanion`, `FakeHotKey` (the Swift tests' `Support/Fakes.swift`). |

Test projects: `Deskling.Companion.Shell.Tests` (xUnit, `net10.0`, runs everywhere; `dotnet.yml`'s ubuntu job and the
release job), `Deskling.Windows.Companion.Tests` (windows job only).

## The map

Swift (`Sources/`) → C#. `CS` = `dotnet/Deskling.Companion.Shell/`, `WC` = `dotnet/Deskling.Windows.Companion/`.

| Swift | C# | Note |
|---|---|---|
| `DesklingShell/Speech/Speaker.swift` | `CS/Speech/ISpeaking.cs`, `WC/Speech/Speaker.cs` | The protocol is pure; `clipRate` is `SpeechRates.ClipRate`, tested anywhere. `speechRate` maps onto the platform's synthesizer, so it stays in `WC/Speech/Speaker.cs`. |
| `…/Speech/ChimePlayer.swift` | `CS/Speech/IChimePlaying.cs`, `WC/Speech/ChimePlayer.cs` | Volume through `MediaPlayer` (`WavPlayer` has none). |
| `…/Speech/SpeechSequencer.swift` | `CS/Speech/SpeechSequencer.cs` | `Schedule` becomes `ITimers`. |
| `…/Speech/VoiceLibrary.swift` | `CS/Speech/VoiceLibrary.cs` | Manifest JSON through a source-generated context; the text hash is SHA-256 as in Swift, pinned by a test vector shared with the Swift test. |
| `…/Drawing/*` | — (only if 4 needs them) | The rig's paths are already `PathCommand`s in `Deskling.Companion`; the canvas draws those. |
| `…/FloatingPlacement.swift` | `CS/Desktop/FloatingPlacement.cs` | Pure geometry. |
| `…/FloatingWindow.swift` | `WC/Desktop/DesktopWindow.cs` | The spike. |
| `…/WindowManager.swift` | `CS/App/IWindowHost.cs`, `WC/Shell/WindowManager.cs` | `WindowID`'s placement, exclusivity and min size become a `WindowSpec` the app returns per window. |
| `…/StatusItemController.swift` | `CS/App/IStatusItem.cs`, `WC/Shell/TrayStatusItem.cs` | Left click opens the flyout (the panel), right click the native menu. Animation at the same 8 fps. |
| `…/GlobalHotKey.swift`, `ShortcutCapture.swift` | `CS/App/IHotKey.cs`, `WC/Shell/HotKey.cs`, `WC/Settings/ShortcutRecorder.cs` | Over `Deskling.Windows.GlobalHotKey`. |
| `…/NotificationPoster.swift` | `WC/Shell/CompanionNotifier.cs` | Over `AppNotifier<string>`. |
| `…/LaunchContext.swift`, `LoginItem.swift` | `WC/Shell/CompanionApp.cs` | `StartupService.IsStartupActivation`; the startup task. |
| `…/LayoutCheck.swift`, `FontRegistration.swift` | — | `Deskling.Windows.Shell.LayoutCheck` exists; fonts are package content in the app. |
| `DesklingCompanionUI/DeliveryPerformer.swift` | `CS/Performance/DeliveryPerformer.cs` | Line for line. |
| `…/SignalHub.swift` | `CS/Signals/SignalHub.cs` | The 1 s busy watch through `ITimers`. |
| `…/Phrase+Localized.swift`, `VoiceLibrary+Bank.swift` | `CS/Performance/` | |
| `…/DesktopLayout.swift` | `CS/Desktop/DesktopLayout.cs` | Pure; `DesktopLayoutTests` ported. |
| `…/DesktopCompanion.swift` | `CS/Desktop/IDesktopCompanion.cs`, `WC/Desktop/DesktopCompanion.cs` | |
| `…/CharacterView.swift`, `GlyphRenderer.swift`, `StatusItemController+Glyph.swift` | `WC/Views/CharacterCanvas.cs`, `WC/Shell/GlyphRenderer.cs` | Vector parts as `Path`s, raster parts as `Image`s under the same affine (Pagliacci's layers); mouth level from the sequencer. |
| `…/CompanionNotifier.swift`, `Shell/CompanionNotifying.swift` | `CS/App/ICompanionNotifying.cs`, `WC/Shell/CompanionNotifier.cs` | |
| `…/Shell/CompanionAppModel.swift` | `CS/App/CompanionAppModel.cs` | `@Observable` becomes `INotifyPropertyChanged` (hand-written, no MVVM toolkit in the package); the `open` surface stays `virtual` / `abstract` with the same names. View hooks (`windowContent`, `panelContent`, `desktopCharacter`) move to the Windows side: see 2. |
| `…/Shell/CompanionAppDelegate.swift` | `WC/Shell/CompanionApp.cs` | |
| `…/Shell/CompanionMenus.swift` | `CS/App/CompanionMenus.cs` (data), `WC/Shell/MenuConversion.cs` | |
| `…/Shell/Copy.swift` | `CS/App/Copy.cs` | `paper`, `palette` and `displayFont` move to the Windows side's `CompanionLook`. |
| `…/Settings/*` | `WC/Settings/*` | Bindings on `CompanionSettings` become two-way properties the controls take. |
| `…/Support/*` | `WC/Support/*` | Over `Deskling.Core.Store.PurchaseStore`. |
| `…/Screenshots/ScreenshotScript.swift`, `ScreenshotCapture.swift` | `CS/Screenshots/ScreenshotScript.cs`, `WC/Screenshots/ScreenshotCapture.cs` | |
| `…/Theme/*` | `WC/Views/*` | `Space` as constants. |

## Sequences

| # | Brief | Where | Gate |
|---|---|---|---|
| 1 | `1-speech-signals.md`: the package, `Speech/`, `Signals/`, `Delivery/`, `Desktop/` pure parts, the fakes | any OS | Swift's speech, voice library, performer and layout tests ported and green on macOS and Linux |
| 2 | `2-model.md`: `Shell/` (the model, copy, menus, interfaces), `Screenshots/` pure part | any OS | every `CompanionAppModelTests` case ported and green |
| 3 | `3-windows-services.md`: the desktop window spike, then `Deskling.Windows.Companion`'s services (speaker, chime, timers, tray status item and glyph, window manager, hotkey, notifier, signal adapters, `CompanionApp`) and a fixture app (`dotnet/Fixture.App`, unpackaged, over the fixture bank) that runs the model | Windows | the fixture app speaks a line, shushes, wakes, hushes on a Teams call, shows its tray glyph light and dark |
| 4 | `4-windows-views.md`: the character canvas, bubble, pill, chrome, desktop character, Settings rows, Support window, screenshot capture | Windows | the fixture app's screenshots in light, dark and a contrast theme; `LayoutCheck` clean |
| 5 | `5-release.md`: CI jobs, `release.yml` packs both packages, docs, CHANGELOG, tag | any OS + the owner | `dotnet.yml` green; packages on NuGet.org |

## What stays in the apps, on purpose

The words (`.resw`, through copy structs), the look (`Theme/*.xaml`, the window scene, buttons, cards), Home,
Onboarding, the flyout's layout, the app's own settings and windows, `Program.cs`'s three lines, the MSIX manifest.

## The 0.8.0 changelog, in advance

- **Added** `Deskling.Companion.Shell`: the platform-free half of a companion app's shell (the app model, the delivery
  performer, the signal hub, speech sequencing, the voice library, the menus as data, the desktop layout, the
  screenshot script), the C# twin of `DesklingCompanionUI`'s logic, tested on any OS.
- **Added** `Deskling.Windows.Companion`: the Windows implementations (speaker, chime, tray status item and glyph,
  window manager, hotkey, notifier, the desktop character's window) and the WinUI controls every companion draws the
  same (character canvas, speech bubble, status pill, Settings rows, Support window, the chrome).
- **Added** to `Deskling.Testing`: the fakes the shell's tests use.

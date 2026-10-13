# 2: The app model, copy and menus

> Sequence 2 of `README.md` (read *Decisions*, *The map*'s `Shell/` rows: their C# home is `CS/App/`, namespace `Deskling.Companion.Shell.App`). Needs 1. One session, any OS.

## Read only these

- `README.md` here; 1's outcome (below its steps); `dotnet/Deskling.Companion.Shell/` as 1 left it.
- Swift: `Sources/DesklingCompanionUI/Shell/*.swift`, `Screenshots/ScreenshotScript.swift` (the shot list and
  `SampleEnvironment` only), `Sources/DesklingShell/WindowManager.swift` (`WindowID`, `WindowPlacement`), the C#
  `Deskling.Core/Store/PurchaseStore.cs`.
- Tests to port: `Tests/DesklingCompanionUITests/Shell/CompanionAppModelTests.swift`, `Screenshots/ScreenshotScriptTests.swift`
  (the parts that don't render), `Tests/DesklingShellTests/WindowIDTests.swift`.

## Steps

| Step | Commit | What |
|---|---|---|
| 2.1 | `Companion.Shell: host interfaces and copy` (in `App/`) | `IStatusItem` (glyph drawer, animating, wave, redraw, panel open/close/toggle), `IWindowHost<TWindow>` (show, close, closeAll, isOpen, `Closed` event) with `WindowSpec` (placement, exclusive, min size, topmost), `IDesktopCompanion` (the Swift `DesktopCompanion`'s members and callbacks), `IHotKey`, `ICompanionNotifying`, `ISystemPreferences` (`ReduceMotion`, `IsDark`, `Changed`). `Copy.cs`: `WindowRoles`, `StatusCopy`, `DesktopCopy`, `MenuCopy`, `CompanionSounds`, `CompanionAppConfiguration` (as Swift's, minus `paper`, `palette`, `displayFont`: those are the Windows side's `CompanionLook` in 4). |
| 2.2 | `Companion.Shell: CompanionAppModel` | The Swift model in C#: `abstract class CompanionAppModel<TWindow, TSettings> : INotifyPropertyChanged where TSettings : CompanionSettings`, the same state, actions and `virtual`/`abstract` surface. The host is injected (`CompanionHost`: status item, window host, desktop companion factory, hotkey, timers, preferences) so the model creates nothing native; the view hooks (`WindowContent`, `PanelContent`, `DesktopCharacter`) are not here, the Windows side asks the app for them. `StatusLine(...)` stays a static. Settings observation: `CompanionSettings` raises `PropertyChanged` (check; if it doesn't, add it in `Deskling.Companion` as an additive change and say so in the CHANGELOG), debounced 300 ms through `ITimers`. |
| 2.3 | `Companion.Shell: menus as data` | `MenuItem` records and `CompanionMenus.MenuBar(model, copy)` / `.Desktop(model, copy)` producing the same items, order and checks as Swift's. Tests compare titles and order with a fixture copy. |
| 2.4 | `Companion.Shell: model tests` | Every `CompanionAppModelTests` case, over a `FixtureModel` subclass, the fixture bank, `ManualTimers`, the fakes (`FakeStatusItem`, `FakeWindowHost`, `FakeDesktopCompanion`, `FakeHotKey`, `FakeNotifier` in `Deskling.Testing`). `ScreenshotScript`'s sample environment and shot list (the capture is 4's). |
| 2.5 | `docs: the model` | `AGENTS.md`, CHANGELOG, the doc comment on `CompanionAppModel` with a C# subclass skeleton. |

## Finish

`dotnet test Deskling.Companion.Shell.Tests` green on macOS; format; `scripts/verify.sh` ok. Hand to 3: the exact
`virtual`/`abstract` surface and `CompanionHost`'s members.

# 3: The Windows services and a fixture app

> Sequence 3 of `README.md` (read *Decisions*, *The map*). Needs 2. One or two sessions **on Windows 11** (the code
> compiles on macOS, but nothing here can be judged without running it).

## Read only these

- `README.md` here; 1's and 2's outcomes; `dotnet/Deskling.Companion.Shell/{Speech,App,Signals,Desktop}/` interfaces.
- `dotnet/Deskling.Windows/` (all of `Shell/` and `System/`: what to build on), `Deskling.Windows.Tests.csproj` (the
  unpackaged test host's settings).
- Swift, for behavior: `Sources/DesklingShell/{Speech/Speaker,Speech/ChimePlayer,StatusItemController,WindowManager,FloatingWindow,NotificationPoster}.swift`,
  `Sources/DesklingCompanionUI/{Shell/CompanionAppDelegate,CompanionNotifier,GlyphRenderer,StatusItemController+Glyph}.swift`.
- For WinUI patterns: mybackhurts' `windows/MyBackHurts.App/Shell/{TrayGlyph,WindowManager,ZineWindow.xaml.cs}`, `App.xaml.cs`, `Program.cs`.

## Steps

| Step | Commit | What |
|---|---|---|
| 3.1 | `Windows.Companion: desktop window spike` | A layered, topmost, no-activate, no-taskbar tool window whose WinUI content has per-pixel transparency (a character with a soft edge over any wallpaper), click-through outside the drawn pixels, draggable, right-click menu. Try in order: a `Window` with a transparent `SystemBackdrop` and `WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`; a `DesktopWindowXamlSource` in a raw layered `HWND`. Write the result here, in *Outcome*, before 3.2. |
| 3.2 | `Windows.Companion: the package and speech` | `dotnet/Deskling.Windows.Companion/` (`net10.0-windows10.0.19041.0`, `EnableWindowsTargeting`, `IsAotCompatible`, code only), `.Tests`. `Speaker` (`MediaPlayer` for clips and for `SpeechSynthesizer` streams; `ClipTime` from the playback session; the voice by id or the language's default), `ChimePlayer` (WAVs from the app's `Sounds/` through `MediaPlayer` with volume). |
| 3.3 | `Windows.Companion: shell services` | `WindowsCompanionHost<TWindow> : ICompanionHost<TWindow>` (2's outcome lists its members; it takes the app's view factories: window content, panel, desktop character), `DispatcherTimers : ITimers`, `TrayStatusItem : IStatusItem` (over `TrayIconHost`: glyph frames at 8 fps, the flyout as a borderless `Window` near the icon via `TryGetIconRect`, closing on deactivation), `GlyphRenderer` (the rig's glyph pose to an `HICON` at `SmallIconPixels`, white or black ink by `AppearanceWatcher.IsTaskbarLight`), `WindowManager<TWindow> : IWindowHost<TWindow>` (content from the app, a `WindowSpec` per id defined here: title, size, placement, exclusive, min size; `AppWindow` sizing per DPI), `HotKey : IHotKey`, `CompanionNotifier : ICompanionNotifying`, the signal adapters, `ReduceMotion` from `UISettings.AnimationsEnabled`, `MenuConversion` (`MenuItem` → `TrayMenuItem`). |
| 3.4 | `Windows.Companion: CompanionApp` | The `CompanionAppDelegate` twin: single instance (`SingleInstance.Claim`, redirect to `Reopen`), startup activation (no Home at login), `DispatcherShutdownMode.OnExplicitShutdown`, screenshot mode hook, tests detection; the three-line `Program` skeleton in its doc comment. |
| 3.5 | `Windows.Companion: fixture app` | `dotnet/Fixture.App` (unpackaged WinUI, not packed): the fixture bank, plain windows, the tray glyph, speech through the system voice. Run it: a poke speaks, Shh for an hour and wake, a Teams call hushes and stops a line, a full-screen video holds lines, light and dark taskbar glyphs, a toast with its buttons. |

## Finish

`dotnet.yml`'s windows job green (build both packages, run both Windows test projects). The fixture app checks above,
done by hand and listed in *Outcome*. Hand to 4: the desktop window approach and anything the views need.

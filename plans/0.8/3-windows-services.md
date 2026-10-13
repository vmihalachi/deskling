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

## Outcome

### 3.1, the desktop window spike (2026-10-13)

A scratch WinUI app (unpackaged, code only, Windows App SDK 2.5.1) on Windows 11 ARM64 at 200 %, run as x64 JIT under
emulation and as Native AOT ARM64, over a patterned window, with the owner doing the hand checks. **The first approach
works: a WinUI `Window`.** The island in a raw `HWND` wasn't needed and wasn't tried. The recipe, each ingredient
checked by leaving it out:

| Ingredient | Without it |
|---|---|
| `OverlappedPresenter.CreateForContextMenu()` with `SetBorderAndTitleBar(false, false)`, not resizable, minimizable or maximizable, `IsAlwaysOnTop`: style `WS_POPUP` and nothing else | `Create()` and `CreateForToolWindow()` keep `WS_DLGFRAME \| WS_SYSMENU`, a 1 px light frame around the character; the presenter puts the styles back after every `SetWindowLongPtr` and on `Show`, so they can't be stripped |
| A `SystemBackdrop` subclass (`partial`, for AOT) whose `OnTargetConnected` sets the target's `SystemBackdrop` to a fully transparent `Windows.UI.Composition` color brush, and back to null on disconnect | Black behind the content |
| `WS_EX_LAYERED` | Black behind the content. With it, the DirectComposition content keeps its per-pixel alpha (no `SetLayeredWindowAttributes`, no DWM calls: `DwmExtendFrameIntoClientArea` plus blur-behind also makes it transparent, but click-through needs layered anyway) |
| `WS_EX_TOOLWINDOW \| WS_EX_NOACTIVATE`, `AppWindow.IsShownInSwitchers = false`, `AppWindow.Show(activateWindow: false)` (never `Activate()`) | No taskbar button (checked) and none in Alt+Tab (by construction); the other app kept the foreground through every click and drag on the character |

The compositor needs a `Windows.System.DispatcherQueue`, and WinUI's UI thread has none (only the `Microsoft.UI` one):
`CreateDispatcherQueueController` (CoreMessaging, current thread, STA) once, before the first
`new Windows.UI.Composition.Compositor()`.

**Click-through:** Windows hit-tests the window's child `HWND`s (`DesktopChildSiteBridge`, `InputSiteWindowClass`) by
rectangle, never by pixel, and `HTTRANSPARENT` only passes a click to windows of the same thread. So the window is
`WS_EX_TRANSPARENT` (clicks go to whatever is below) except while the cursor is over drawn content:

- A `System.Threading.Timer` every 33 ms reads `GetCursorPos` and `GetWindowRect` off the UI thread. Only while the
  cursor is inside the window does it post a hit test to the UI thread (`VisualTreeHelper.FindElementsInHostCoordinates`
  at the cursor in DIPs, true when it finds a drawn element), which sets or clears `WS_EX_TRANSPARENT`; outside, it sets
  it once. While a press is held the flag stays off.
- Measured with the cursor moved by script: the flag flips within 60 ms both ways, also after a hide and show.
  `WindowFromPoint` from another process lands on the window below when it's set, on the character when it's not.
- It follows the live geometry, so a swaying or talking pose needs nothing updated. XAML hit-tests an `Image` by its
  bounds, so for raster parts `CharacterCanvas` (4.1) should offer a hit test that reads the layer's alpha under the
  inverse affine; `DesktopWindow` takes it as a `Func<Point, bool>`.
- Cost: the AOT ARM64 build idles at one 15.6 ms scheduler tick of CPU in 20 s with the poll running, next to a
  character that redraws at up to 30 fps. A `DispatcherQueueTimer` doing the same wakes the UI thread 30 times a second
  (2.7 % of a core in the x64 JIT build): keep the poll off the UI thread.
- Not chosen: `SetWindowRgn` from the silhouette (clips the sway and the soft edges unless updated every pose), a
  low-level mouse hook (a global hook for a desk character is more than a poll).

**Drag, click, menu** (XAML pointer events on the content, as `FloatingEventView`): a press records `GetCursorPos` and
the window's rectangle; moving 3 DIPs turns it into a drag that calls `AppWindow.Move`; the release ends the drag (the
anchor is the bottom middle again) or is a click. `RightTapped` opens the native menu through
`TrayIconHost.ShowMenu(cursor)`, and its commands run after it closes. Two traps found:

- `ReleasePointerCapture` raises `PointerCaptureLost` synchronously, before the release handler's own checks: read the
  drag state first, or every drag ends as a click (and the anchor never moves).
- The menu's `SetForegroundWindow` leaves the app in the foreground after the menu closes (the next right-click found the
  character's window there). `DesktopWindow` remembers `GetForegroundWindow()` before the menu and gives it back after,
  unless the chosen command opened a window.

**Also checked:** `AppWindow.MoveAndResize` around the anchor keeps the bottom middle put and the content crisp at every
size; fading the content's `Opacity` (0.2 s out, 0.4 s in) around `AppWindow.Hide()` and `Show(false)` keeps every
style; Native AOT publishes with no trim or AOT warnings; working set 67 MB.

**Not checked** (one display, no Windows 10 machine): moving between displays with different scales (the window should
keep its DIP size; 4.3 clamps it with `FloatingPlacement`), Windows 10, a full-screen app (the model hides the character
then).

**Hand to 3.3 and 4.3:** `WC/Desktop/DesktopWindow.cs` is this recipe: the window, the transparent backdrop, the
click-through poll, the pointer handling with the two fixes above and the menu through the app's `TrayIconHost`. The
bubble's window rides along as a second `DesktopWindow` without drag. 3.3's host can return a stub from `MakeDesktop`
until 4.3 builds `DesktopCompanion` on it.

```csharp
var window = new Window { Content = root, SystemBackdrop = new TransparentBackdrop() };
var presenter = OverlappedPresenter.CreateForContextMenu();
presenter.SetBorderAndTitleBar(false, false);
presenter.IsResizable = presenter.IsMaximizable = presenter.IsMinimizable = false;
presenter.IsAlwaysOnTop = true;
window.AppWindow.SetPresenter(presenter);
window.AppWindow.IsShownInSwitchers = false;
var hwnd = Win32Interop.GetWindowFromWindowId(window.AppWindow.Id);
var ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE) | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
SetWindowLongPtr(hwnd, GWL_EXSTYLE, ex);
window.AppWindow.MoveAndResize(frame); // pixels
window.AppWindow.Show(activateWindow: false);

sealed partial class TransparentBackdrop : SystemBackdrop
{
    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot root)
    {
        base.OnTargetConnected(target, root);
        // After CreateDispatcherQueueController, once per thread.
        target.SystemBackdrop = compositor.CreateColorBrush(Color.FromArgb(0, 0, 0, 0));
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
    {
        target.SystemBackdrop = null;
        base.OnTargetDisconnected(target);
    }
}
```

### 3.2, the package and speech (2026-10-13)

`dotnet/Deskling.Windows.Companion/` (`Speech/`: `Speaker`, `ChimePlayer`) and `Deskling.Windows.Companion.Tests` (14
tests), in the solution and in `dotnet.yml`'s windows job. `release.yml` doesn't pack it yet (5 does): a patch release
cut before 0.8.0 shouldn't publish a half-built package. Differences from the brief, and what was measured:

- **One `MediaPlayer` per line, not one for the speaker's life** (and one per sound in the chime). A player's
  `MediaEnded` can't say which source ended, so a late event from a cut line could end the next one; with a player per
  line and a generation number it's ignored, as Swift's per-clip `AVAudioPlayer` is. Every player is kept out of the
  system media controls (`CommandManager.IsEnabled = false`): no "now playing" card in the volume flyout, no media keys.
- **The clip's rate keeps its pitch.** `PlaybackSession.PlaybackRate`, set on `MediaOpened` (opening a source resets it).
  On a real clip the clip time runs at 0.86× and 1.18× of the wall clock at the slider's ends (0.85× and 1.2× asked).
  Speech at another tempo sounded higher to the owner, so a 440 Hz tone settled it: the same note at all three rates.
  `ClipTime` is `PlaybackSession.Position`, the clip's own seconds whatever the rate.
- **The synthesizer**: a new `SpeechSynthesizer` per line, synthesizing off the UI thread; a stop or a newer line drops
  its result. `SpeakingRate` runs 0.5×–2× around 1× (`Speaker.SpeechRate`); Windows' 6× is past understanding. A line
  that can't be synthesized (no voice installed) ends, as a broken clip's does.
- **Voices**: `Speaker.Voices(language)` for the Settings picker lists the region's voices first, then Windows' default
  voice (Windows has no quality to sort by, unlike the Mac), then by name; `Speaker.Voice(id, language)` falls back to
  the first.
- **The UI thread is a `SynchronizationContext`** (the current one by default), not a `DispatcherQueue`, so the speech
  tests pump their own and need no Windows App SDK runtime.
- **Tests** play generated WAVs at no volume: real playback here (a 0.4 s clip ends after 0.4 s); a runner without an
  audio device should fail the player instead, and the line ends either way. The windows job is the first run without
  one: check it once this is pushed (checked: all 14 pass there, in 22 s). The Swift cases 1 left over (a broken clip, a
  missing sound) are here.

### 3.3, shell services (2026-10-13)

`Shell/` and `Signals/` in the package, 31 more tests (45 in all). A scratch probe app (WinUI, unpackaged, run under JIT
and as Native AOT on ARM64, no AOT warnings) drove each service from a command file; what needs a hand (clicks, keys,
the taskbar's light mode) is on 3.5's fixture checklist. Differences from the brief, and what was found:

- **The host takes the views as one record**, `CompanionViews<TWindow>(WindowSpec, WindowContent, Panel) { Desktop }`,
  plus the tray icon's GUID and tooltip (the model makes the status item without arguments). Without a `Desktop` view,
  `MakeDesktop` returns one that never shows: 4.3 supplies the real one. `Quit` runs the app's `OnQuit` (3.4's
  `CompanionApp`), else `Application.Exit`.
- **`WindowSpec`**: title, content size in DIPs, `Center` (on the pointer's display) or `NearTray` (the work area's corner
  by the taskbar's end), exclusive, minimum size, always on top. The Mac's frame autosave and "bigger" aren't here (no
  companion uses them). Content reaches under the title bar (`ExtendsContentIntoTitleBar`, the Mac's transparent title
  bar), so 4.2's `WindowInset` must leave room for the caption buttons, which follow the theme through
  `TitleBar.PreferredTheme`. Not maximizable.
- **Trap: `AppWindow.ResizeClient` adds a caption's height** (58 px at 200 %) both for a window without a title bar (the
  panel) and for content under the title bar (every window): both came out 29 DIPs too tall.
  `NativeMethods.SetClientSize` resizes by the window's measured frame instead: a 520×600 window is now 1040×1200 client
  pixels.
- **The panel** is a window with a border and no title bar (Windows 11 rounds and shadows it), always on top, out of
  Alt+Tab and the taskbar, made on first open and hidden after (Alt+F4 too), never closed. Its content sits in a
  `Canvas`, so it keeps the size it asks for and the window follows it: two more lines moved it up, still above the
  taskbar. It sits on the icon's display, centered on the icon along the taskbar (`TrayPlacement.NextTo`), or by the
  click or pointer when the icon's rect can't be read. It closes when it loses focus (opening a window closed it) and on
  Esc; a click on the icon within 500 ms of that doesn't reopen it.
- **The menu is rebuilt each time it opens**: `Deskling.Windows` gains `TrayIconHost.MenuOpening` and
  `TrayMenuItem.Command.IsChecked` (both additive). A command that isn't enabled becomes a dimmed `Label`. Opening the
  menu closes the panel, as on the Mac.
- **The glyph**: `GlyphAnimation` holds Swift's frame logic apart from the icon, tested with `ManualTimers`.
  `GlyphRenderer` draws through GDI+ (`System.Drawing.Common`), caching bitmaps and making a fresh `HICON` for each
  `SetGlyph` (the tray host frees it): black ink on a light taskbar, white on dark.
- **Notifications** register and post from an unpackaged app: permission reads `Authorized`, and posts land in
  Notification Center (the banner itself doesn't show in a screen capture). A click on the banner raises nothing (a
  reserved `banner` action), as on the Mac; `Activated(args)` takes the click Windows launched the app with, for 3.4.
  Windows never asks, so `RequestAuthorization` reports whether notifications are on.
- **Signal adapters** bring `Deskling.Windows`' background events to the UI thread through a `SynchronizationContext`.
  `SystemAppearance` reports only a real light and dark swap (Windows also reports accent changes). Mute reads `null`
  without an output device.
- **`DispatcherTimers`** over `DispatcherQueueTimer`: a cancelled one-shot never ran, a 0.5 s one fired at 500–516 ms.
  No unit test (the test host has no Windows App SDK runtime).
- **Not checked yet**, for 3.5's fixture app by hand: a left click toggling the panel and the reopen guard, Esc, the
  right-click menu's checks and rebuild, the hotkey's press (it registers), a notification button reaching the app, and
  the glyph's frames and light-taskbar ink on screen.

### 3.4, CompanionApp (2026-10-13)

`Shell/CompanionApp.cs`, `ICompanionLaunching` in Companion.Shell, 5 more tests (50 in all). A scratch app whose
`App.xaml` is rooted on `CompanionApp` (unpackaged, with `XamlControlsResources`, run under JIT and as Native AOT on ARM64,
no AOT warnings) was driven by script. Differences from the brief, and what was found:

- **A base class, as on the Mac.** The app's `App` derives from `CompanionApp` and `App.xaml`'s root is
  `<shell:CompanionApp>`: XAML takes an abstract base from a package (checked). `CompanionApp.Start(instanceKey, makeApp)`
  is all of `Main`: the COM wrappers, `SingleInstance.Claim`, then `Application.Start` with the UI thread's
  `SynchronizationContext`. A null key skips the claim, so a screenshot run works beside the running app. `OnLaunched`
  is sealed; the app supplies `MakeCompanion()` (called on the UI thread after the App's constructor) and may override
  `RunScreenshots()`.
- **No tests check.** No Windows test host starts the app's `Application` (xUnit runs no WinUI app, and a WinUI test app
  brings its own), so the Mac's `isRunningTests` has no twin.
- **What a launch means** (`CompanionActivation.IsOpening`, tested): Home, unless the startup task (sign-in) or a
  notification click started the app. A launch redirected from a second copy reopens (the open windows, or Home), except
  for those two kinds. The Mac shows Home when a notification click launches the app; here the button acts without Home.
- **The click that started the app belongs to the notifier.** `CompanionNotifier.Configure` (the model calls it at launch)
  reads the launch activation and posts its action once the launch is done. It also takes a click redirected from a
  second copy. 3.3's `Activated(args)` is gone.
- **Quit.** `WindowsCompanionHost.Quit` now always runs the app's `OnQuit`, then disposes the host and exits. Without
  the dispose, the tray icon would linger until hovered; checked gone after quit with `Shell_NotifyIconGetRect`. The app's
  `OnQuit` disposes its notifier, which unregisters, so a later click on a notification starts the app again.
- **Checked by script** (JIT and AOT): the launch shows Home. A second launch exits in about 250 ms with code 0, and the
  first reopens. With every window closed the app runs on, and a launch brings Home back. Quit ends the process and
  removes the tray icon. A copy with a null key runs beside the first. `RunScreenshots` takes over before any model is
  made.
- **For the fixture app and sunnysays:** `Application.Resources` throws `E_UNEXPECTED` in the App's constructor, even
  after `InitializeComponent`; read it from `MakeCompanion` on. The scratch app set `EnableMsixTooling`, as the
  unpackaged template does (not tried without it).
- **Not checked yet**, for 3.5 by hand: a notification button clicked while the app isn't running (it should act without
  Home). The startup task's quiet launch needs package identity, so it waits for sunnysays' package.

# 4: The WinUI views every companion draws

> Sequence 4 of `README.md` (read *Decisions*: *Views*, *Copy*, *The look*). Needs 3. One or two sessions on Windows 11.

## Read only these

- `README.md` here; 3's outcome; `dotnet/Deskling.Windows.Companion/` as 3 left it; `Deskling.Companion/Figures/`.
- Swift, for behavior and measurements: `Sources/DesklingCompanionUI/{CharacterView,DesktopCompanion}.swift`, `Theme/*`,
  `Settings/*`, `Support/*`, `ScreenshotCapture.swift`, `Screenshots/ScreenshotScript.swift`.
- For the look the apps will put around them: sunnysays' `DESIGN.md` (*Components*, *Motion*) and Pagliacci's, only
  to check the controls take every color and font from outside.

## Steps

| Step | Commit | What |
|---|---|---|
| 4.1 | `Windows.Companion: CharacterCanvas` | The rig posed by `RiggedCharacter` drawn as `Path`s (vector parts) and `Image`s (raster parts) under each layer's affine; paints from a `PaintRole → Brush` map the app passes (`CompanionPalette`); the mouth from `DeliveryPerformer.MouthLevel` each frame, else its own timer; idle sway and blink; still under reduce motion and in screenshot runs. Tests: the layer count and transforms for the fixture rigs match `conformance/companion/characters`. |
| 4.2 | `Windows.Companion: bubble, pill, chrome` | `SpeechBubble` (tail edge, countdown ring, max width), `StatusPill`, `WindowInset`, `ActionBar`, `PanelRow`, `Pointer`, `Space`; `CompanionLook` (paper, palette brushes, display font family). |
| 4.3 | `Windows.Companion: DesktopCompanion` | `IDesktopCompanion` over 3.1's window: stand, bubble beside it toward the screen's middle, drag to move, Ctrl+wheel and pinch to resize, right-click menu, fade in and out, hidden during calls and full screen (the model already decides). |
| 4.4 | `Windows.Companion: Settings and Support` | The Settings rows and editors (words and bindings from the app), `SupportWindow` over `PurchaseStore` with `SupportCopy`. |
| 4.5 | `Windows.Companion: screenshots` | `ScreenshotCapture`: each window rendered with `RenderTargetBitmap` to PNG in light, dark and contrast, `LayoutCheck.TextProblems` on each; the fixture app's run. |

## Finish

The fixture app's screenshots in every theme look right (the owner's look); `LayoutCheck` clean; `dotnet.yml` green.
Hand to 5: anything the apps must know (the resources they must define, the brushes the palette needs).

## Outcome

### 4.1, CharacterCanvas (2026-10-13)

`Views/` in the package (`CharacterCanvas`, `CompanionPalette` with `ThemedColor`, `CharacterMotion`, `CharacterScene`,
`CharacterImages` with `AlphaMask`, `FrameClock`) and `Screenshots/ScreenshotCapture.IsEnabled`; 19 more tests (69 in
all). The fixture app's Home draws the vector fixture, bound to the model, and the raster fixture from its PNGs.
Differences from the brief, and what was found:

- **Parts are made once, and a frame only moves them.** Each part is a canvas the size of the view box holding its
  `Path`s or its `Image`; a frame sets the rig's transform on the part's composition visual
  (`ElementCompositionPreview`), and changes the stage's children only when the eyes, mouth or overlays change. Measured
  on Home with both characters moving (ARM64 Release, 15 s; held still, 0):

  | How the parts move | CPU (one core) |
  |---|---|
  | `RenderTransform`, on `CompositionTarget.Rendering` | 15 % |
  | `RenderTransform`, on a 30 fps `DispatcherQueueTimer` | 10 % |
  | the visual's `TransformMatrix`, on that timer | 6–7 % |
  | the same, skipping a frame that moves nothing by half a screen pixel | 5.2 % (5.3 % Native AOT) |

  The rest is WinUI's own per-frame work, split about evenly between the UI thread and its in-process compositor.
  `CompositionTarget.Rendering` comes every frame the display draws and keeps the UI thread rendering at its rate, so
  it isn't used. One `FrameClock` serves every character, so Home, the panel and the desktop character move in the same
  frames. 4.3 measures the desktop character alone.
- **The hit test is the canvas's own.** XAML's hit test doesn't see a visual's transform, so `IsDrawnAt(point)` works
  from the pose on screen (`CharacterScene.IsDrawnAt`). A point counts when it is inside a fill (by its fill rule),
  within half a stroke's width plus half a point of a stroke, or on an image pixel with alpha 0x20 or more under the
  inverse affine. It is pure and tested, and 4.3's `DesktopWindow` takes it as its `Func<Point, bool>`.
- **Raster parts** are `<skin>.<part>.scale-<n>.png` in `ImageFolder` (`Assets\Characters` by default): Windows'
  scaled-image names, with `scale-200` meaning two pixels per view-box unit. The canvas draws the smallest file at least
  as sharp as the screen needs (the fit times the display's scale), and picks again on a resize or a display change.
  Files load by path, the same packaged and unpackaged. The fixture app links the raster fixture's `head@2x.png` layers
  under those names (an MSBuild rename). The fixture's PNGs are opaque squares, so the alpha tests draw their own.
- **The palette is colors, per theme:** `ThemedColor(light, dark)` for each UI role and each paint. The controls pick
  by `ActualTheme` and pick again on `ActualThemeChanged`.
  - `ThemedColor.Resource(key)` reads a brush from the app's `Light` and `Dark` theme dictionaries, so sunnysays can
    build its palette from `Palette.xaml`.
  - Under a contrast theme, `ForCurrentContrast()` gives Windows' contrast colors for the UI roles (4.2's controls use
    them); the character keeps its paints.
  - `CompanionPalette.System` is the default, with every paint in the text color.
- **Plain properties, not dependency properties** (`Character`, `Mood`, `Talking`, `Line`…): `x:Bind` sets them one
  way, and a dependency property of a non-WinRT type is a needless risk under AOT. `MouthLevel` is a `Func<int?>` the
  view sets (`() => Model.Performer.MouthLevel`).
- **Single precision:** the compositor's matrices are floats, so the scene's tests compare at 1e-4. Against the vectors,
  the vector parts land on the poses file's shapes, and the raster parts take its transforms in its order.
- **Narrator** sees one image named by the line, else the label (checked with UI Automation).
- **Checked on screen** (JIT and Native AOT, no warnings): asleep, awake, talking (the mouth differs between frames
  300 ms apart), and shushed's droop, for both fixtures. The canvas stops while its window is hidden
  (`XamlRoot.IsHostVisible`); measured in 4.2, with the panel hidden and Home closed the app uses no CPU.
- **Not checked yet:**
  - switching to the light theme on screen (UI Automation can't select WinUI's `RadioButtons`; 4.5's run draws light,
    dark and contrast);
  - the 0.6 s wake spring by eye;
  - the raster file choice on a 100 % or 150 % display;
  - the hit test on screen (4.3).

### 4.2, bubble, pill, chrome (2026-10-13)

`Views/` gains `SpeechBubble` (with `BubbleShape` and `BubbleTail`), `StatusPill`, `ActionBar`, `PanelRow`, `Pointer`,
`Space`, `WindowInset` and `CompanionLook`; 9 more tests (78 in all). The windows and the tray panel get the look's
paper behind their content. The fixture app uses them all: the pill, the bubble beside the character and a pointer on
Home; the pill, a small character with its bubble and panel rows in the panel; an action bar in onboarding. Home also
gets "Open the panel", for checking the panel while the icon sits in the overflow. Differences from the brief, and what
was found:

- **`CompanionLook.Current`** stands in for SwiftUI's environment: the palette, the display face (`FontFamily`, null
  keeps the control's) and its weight (medium). The app sets it once in `MakeCompanion`.
  - The controls repaint when their theme changes, when the look changes and when Windows' colors change (a contrast
    theme turning on or off, a new accent; `UISettings.ColorValuesChanged`, brought to the UI thread). This is
    `LookBinding`, the code-side `{ThemeResource}`.
  - `CharacterCanvas.Palette` now defaults to the look's.
- **`PanelRow` is a `Button` restyled through its own theme dictionaries** (WinUI's lightweight styling: `Light`, `Dark`
  and `Default` filled from the palette when the row is made). Its pointer-over and pressed fills therefore follow the
  theme with no template.
  - It has no `HighContrast` dictionary, so a contrast theme gets Windows' own button colors.
  - Checked on screen: a full-width rounded `accentSoft` fill under the pointer.
- **The bubble** is the Mac's `BubbleShape`, ported as pure geometry (corners as cubic quarter circles; tested by tail
  side and on a short card), under the words.
  - Its shadow is a composition `DropShadow` masked by the outline (`GetAlphaMask`), blur 4, 2 down.
  - The countdown dial is a composition shape whose `TrimStart` the compositor animates. It drains with no UI-thread
    work, from whatever time is left when the countdown is set.
  - New words fade in over 0.15 s, unless animation effects are off.
  - `FontSize` is the control's own, 16 by default.
- **x:Bind gives a null string as ""**, so the bubble shows "…" for empty text too (seen on screen: an empty bubble).
- **A capsule needs its radius set to half its height**: `CornerRadius(999)` draws an ellipse in WinUI.
- **`WindowInset`** adds the title bar's 32-point band to the Mac's 24 at the top, since the windows' content reaches
  under the caption buttons.
  - `{x:Bind}` can't call `WindowInset.Window(24)`: a function with a number argument crashes the XAML compiler
    (WMC9999).
  - So the frames are static properties: `Content`, `ContentOverActionBar` (16 under the content, as the Mac's Support
    window) and `PanelPadding`.
- **`ActionBar` is a horizontal `StackPanel`** with its top border as the hairline. Put a `Grid` in it to push buttons
  to the end.
- **`Pointer` points down and right by default**, toward where the notification area usually is. `PointsUp` gives the
  Mac's direction, for a taskbar at the top.
- **Checked on screen** (JIT and Native AOT, no warnings): Home, the panel (pill, character, bubble, rows, hover) and
  onboarding's action bar, dark.
- **Cost:** with the panel open over Home, both characters moving cost 5.8 % of a core (AOT). With no window showing it
  is 0.
- **For 4.3:** the panel's one 64-point character alone costs 3.8 %, so most of a frame's cost doesn't depend on what
  moves. The desktop character is always on screen, so 4.3 starts by measuring and cutting that (a lower rate for the
  idle sway, or letting the compositor run it).
- **Not checked yet:**
  - the light theme and a contrast theme (4.5's run draws them);
  - the display face, which the fixture doesn't set;
  - the bubble's trailing, top and bottom tails on screen (only tested as geometry);
  - Narrator over the bubble and the pill.

### 4.3, DesktopCompanion (2026-10-13)

`Desktop/` in the package: `DesktopWindow` (Swift's `FloatingWindow`), `DesktopCompanion` (`IDesktopCompanion`) with its
stand, and `DesktopNative`; 4 more tests (82 in all). `CompanionViews.Desktop`, 3.3's placeholder that took a whole
`IDesktopCompanion`, became `DesktopCharacter`: the app gives only the character view, and the host stands it in a
`DesktopCompanion` the first time the model shows it. The fixture app gives a `CharacterCanvas` that follows the model,
and Home gets "On the desktop, or off". Differences from the brief, and what was found:

- **`DesktopWindow` is 3.1's recipe:** the context-menu presenter, the transparent backdrop, `WS_EX_LAYERED`, tool
  window, no-activate, no rounded corners or border. It adds the following:
  - **Positions are screen pixels:** the anchor (the bottom middle) with y down, as `FloatingPlacement` takes it. Sizes
    are DIPs at the scale of the display under the anchor. `DesktopPosition` saves the anchor in pixels.
  - **The work areas** come from `DisplayArea.FindAll()`, read by index: enumerating the projected list throws
    `InvalidCastException` (seen at the first try).
  - **Display changes** come from `DisplayAreaWatcher`, so no window subclass is needed. A scale change re-places the
    window through `XamlRoot.Changed`.
  - **The root is transparent but hit-testable,** so every press reaches the window's handlers. The canvas's own hit
    test decides where clicks land, through the click-through poll.
  - **The poll** checks every 33 ms while the pointer is within 160 pixels of the window, and every 100 ms when it's
    farther. It is one-shot and re-armed each time, so no poll runs after a hide.
  - **The bubble rides along as an owned window** (`GWLP_HWNDPARENT`), which Windows keeps above the character. It
    follows drags step by step (`OnDragging`).
  - **Ctrl+wheel** (which a precision touchpad's pinch sends) steps the size once per gesture; a gesture ends after
    0.4 s without the wheel. A touch pinch does the same through `ManipulationDelta`.
- **The menu's owner is a second `TrayIconHost` with no glyph,** so no icon shows (as the spike did). After the menu
  closes, the foreground goes back to the app that had it, once the chosen command has run, unless that command opened
  a window.
- **The stand** is the character over a radial-gradient ellipse in the palette's shade (the Mac blurs a filled one).
  The shadow isn't hit-testable. Narrator sees one button named like the character, with the say action's name as its
  help text, and its Invoke says something.
- **A bug the tests caught:** the bubble's vertical place was the Mac's y-up formula. It only looked right on screen
  because, for a one-line bubble, both formulas give the same answer. `DesktopCompanion.BubbleFrame` is now pure and
  tested (side, the tail level with the head, scale 2 against 1, kept on the display), and on screen a nine-line
  bubble's tail meets the head.
- **Checked by script** (JIT and Native AOT, no warnings):
  - It shows by the taskbar's corner.
  - The pointer over the head or stem reaches the window. Over the window's corner, beside the stem or on the shadow,
    it reaches the app below (`WindowFromPoint`).
  - A click says a line into the bubble, on the side toward the screen's middle with its tail toward the character. A
    click on the bubble puts it away.
  - A drag moves the character, the bubble follows, and the model keeps the spot (off and on again, it comes back
    there). Near the screen's edge a tall bubble is held inside the work area.
  - The right-click menu shows the model's desktop menu. Esc closes it, and Notepad gets the foreground back.
  - Ctrl+wheel steps the size up and back around the anchor; a plain wheel does nothing.
  - With Notepad in front, a click on the character leaves Notepad in front. With the fixture's own window in front,
    a click does make the character's window the foreground, within the same app.
- **Cost:** the desktop character alone uses 2.5–3.1 % of a core (Native AOT 2.7–3.1 %), noisy by about ±1 %. That
  is:
  - 0.6 % for the two timers;
  - 0.8 % for computing the pose;
  - the rest for about 15 composition commits a second, roughly 1.3 ms of CPU each across the UI thread and the
    in-process compositor.

  The idle sway now asks the shared `FrameClock` for 15 fps; talking and the wake spring ask for 30. Side by side in
  one session, the two rates measured the same (3.4 against 3.6 %), because the half-pixel skip already kept commits
  near 15 a second. 15 is kept for the halved pose work.
- **For the owner:** going lower means fewer commits. Either a coarser skip (1 pixel instead of half: about half the
  cost, the sway in visible steps), or moving the sway into a compositor animation (no UI-thread work, but the
  compositor then draws at the display's rate). Neither was tried.
- **Not checked yet:**
  - a touch pinch;
  - two displays, or a scale change while dragging;
  - the hide during a call or full screen (the model's, checked by 3.5's hand list);
  - the fades by eye;
  - the light theme;
  - Windows 10.

### 4.4, Settings and Support (2026-10-13)

`Settings/` (`ActiveHoursEditor`, `QuietHoursEditor`, `ShortcutRecorder`, `LaunchAtStartupToggle`, `LanguageRow`,
`NotificationPermissionNote`, `SettingsFooter`, the copy records, `SettingsBindings`) and `Support/` (`SupportWindow`,
`SupportCopy`, `SupportProducts`, `SupportLook`, `SupportStage`); 7 more tests (89 in all). The fixture app's Settings
uses every row, and its Support window is `SupportWindow` over the mock store, 460 × 580 as the Mac's test.
Differences from the brief, and what was found:

- **Words and styles from the app, bindings from the settings.** Each control takes the app's words as a copy record
  (`ActiveHoursCopy`, `ShortcutCopy`…) and its styles as `Style` properties (`ChipStyle`, `ButtonStyle`). It takes the
  `CompanionSettings` itself, and watches it only while loaded (`SettingsWatch`), since settings outlive their windows.
  Swift's `CompanionSettings+Bindings` is `SettingsBindings`: `IsOn` for a channel or a flavor, `SetFlavor`,
  `MutedCount`.
- **The active hours' pickers stack,** each with its label ("From", "To"). Side by side, two WinUI time pickers cut
  their minutes off at 420 points (seen on screen), so `ActiveHoursCopy` has no word for between them. The weekday
  chips follow the culture's first day (Monday first, in Italian, on this machine).
- **Quiet hours' labels** follow the culture's short time pattern without its minutes ("3 PM", "15", "09"). The
  designator goes first where the pattern puts it first.
- **The language row is a list in the app,** as in mybackhurts: Windows has no per-app language page for every app,
  where the Mac has one. The list has Windows' language first, then each of the app's in its own words, through the
  Windows App SDK's `ApplicationLanguages.PrimaryLanguageOverride` (packaged or not). A restart note and button show
  once it changes; `OnRestart` is `AppInstance.Restart` unless the app sets it. An unknown tag shows as itself
  (`predefinedOnly`).
- **Startup** is the package's startup task (`StartupService`). Without package identity (the fixture) the toggle reads
  off and can't be turned on. When Windows refuses, a line in the accent text says where to turn it back on.
- **The shortcut recorder** is mybackhurts' over `SayShortcut`, with Swift's `OnCaptureStarted` and `OnCaptureEnded`
  (the model's `SuspendHotKey` and `RegisterHotKey`), and `Describe` ("Ctrl+Alt+F", the layout's names).
- **`SupportWindow`** follows Swift: one ask at a time, only thanks right after a purchase, a spring into the
  celebration unless animation effects are off, and Esc as Done. Its stage is the same pure function, tested with the
  Swift cases. The model acknowledges a tip when the window closes, so a reopened window shows the tips again.
  - Found on the way: a button whose content is two texts has no name for Narrator, so a tip is named "title, price".
  - `ActionBar` became a `Grid` (4.2's was a horizontal `StackPanel`, in which a grid can't push Done to the end, as
    its doc wrongly said).
- **`x:Name="Language"`** clashes with `FrameworkElement.Language` in an app's view (CS0108, and the XAML compiler's
  WMC9999 with it): name the row something else.
- **Checked by script** (JIT and Native AOT, no warnings):
  - Settings draws every row (Italian culture, dark); a quiet hour turned on comes back on when Settings reopens.
  - Support's pitch, then the purchase's thanks, then on reopening the supporter's thanks and the tip, the tip's
    thanks, and the tips again.
- **Not checked yet:**
  - recording a shortcut by keyboard;
  - the startup task in a package (sunnysays');
  - a language change and the restart;
  - the notification note (notifications are on here);
  - the light theme;
  - Narrator over the editors.

### 4.5, screenshots (2026-10-13)

`Screenshots/ScreenshotCapture` (grown from 4.1's flag) and `ScreenshotScript<TWindow, TSettings>`; 6 more tests (95 in
all). The fixture app overrides `RunScreenshots` and shoots over a `SampleEnvironment`: a fixed Tuesday at 10:30,
silent channels and sample prices. `scripts/fixture-screenshots.ps1` builds it, runs it and copies the PNGs; it fails
when the run reports problems. Differences from the brief, and what was found:

- **`PrintWindow`, not `RenderTargetBitmap`.** `RenderTargetBitmap` draws XAML only, so it would miss the character's
  parts (moved on their composition visuals), the bubble's shadow and the dial. `PrintWindow` with full content,
  cropped to the visible frame, captures what the screen shows, caption buttons included, as the Mac's shots show the
  title bar (mybackhurts does the same). Real windows keep Windows 11's thin border. A view shot at its fitting size
  (the panel, the desktop sheet) uses the context-menu presenter, so no frame edges it (seen, then fixed).
- **The contrast theme can't be shot from inside the app.** An app can't turn a contrast theme on for itself, and
  turning on the system's would be changing the user's settings. So the run shoots light and dark, or the contrast
  theme alone while Windows is in one (`ScreenshotCapture.Appearances`): run it again with a contrast theme on. Not
  done yet. The package's controls take Windows' contrast colors then (4.1's `ForCurrentContrast`).
- **The run is Swift's:** the shots are a window, a view at a size, a view at its fitting size, the supporter sequence,
  and the app's own, each named `<name>-<appearance>.png` and filtered by `DESKLING_SCREENSHOT_MODE` prefixes.
  - The model is never launched: no tray icon, hotkey or desktop character. Its status comes from the fixed clock.
  - While a run goes, `ReduceMotion.IsOn` is true and the character holds its still pose, so nothing animates mid-shot.
  - A run claims no single instance (the fixture's `Program`), so it works beside a copy in use.
- **`LayoutCheck.TextProblems`** runs on every window shot. The fixture's 16 shots report none.
- **The desktop character's window is transparent,** so it isn't shot alone. The fixture shoots a sheet instead:
  `DesktopCompanion.BubblePreview` beside `DesktopCompanion.Stand`, as the Mac's sheet.
- **The status pill's minutes differ from run to run** (67, then 62). The fixed clock doesn't fix the scheduler's
  randomness. A seeded random for screenshot runs would make the shots stable. Not done: the model takes no random from
  outside yet.
- **Checked** (JIT, and Native AOT with no warnings): all 16 shots of the fixture in light and dark (Home, Settings,
  onboarding, Support asked, thanked and as a supporter, the panel, the desktop sheet). Each was looked at: the paper,
  the cards and the pill follow the theme, and the bubble's shadow, the dial and the character all show.

## Finish

Done on this machine (2026-10-13), sequence 4's steps all committed:

- The fixture's screenshots look right in light and dark. The contrast theme waits for a run with one on.
- `LayoutCheck` is clean.
- `dotnet.yml` is green on 4.x, its windows job included (run 38422091815).

### Hand to 5 (and to sunnysays' M3.2)

What an app on `Deskling.Windows.Companion` must give or know:

- **The look:** `CompanionLook.Current = new CompanionLook(palette) { DisplayFont = … }`, set first thing in
  `MakeCompanion` (and in `RunScreenshots`).
  - `CompanionPalette` takes a `ThemedColor` per role (`Paper`, `Card`, `Ink`, `InkSoft`, `Line`, `Outline`, `Shade`,
    `Accent`, `AccentText`, `AccentSoft`, `Secondary…`, `OnInk`, `OnAccent`) and `Paints` for the art's paint roles.
    `ThemedColor.Resource("PaperBrush")` reads one from the app's `Light` and `Dark` theme dictionaries, so the app's
    `Palette.xaml` stays the source.
  - The package defines no resources and needs none of the app's by name.
  - Contrast themes map to Windows' colors by themselves.
- **The views:** `CompanionViews.DesktopCharacter` is a `CharacterCanvas` bound to the model. `WindowInset.Content`,
  `ContentOverActionBar` and `PanelPadding` are static properties for `{x:Bind}`; a function call with a number
  crashes the XAML compiler. Don't name an element `Language`.
- **Raster characters** keep their PNGs as `Assets\Characters\<skin>.<part>.scale-200.png` and so on, next to the exe
  (`CharacterCanvas.ImageFolder` otherwise).
- **Settings:** each row takes a copy record of the app's words and its `Style`s. `LaunchAtStartupToggle.TaskId` is
  the manifest's `uap5:StartupTask` id; `LanguageRow.Languages` are the app's tags.
- **Support:** `SupportWindow` takes the `PurchaseStore`, the products, a `SupportCopy`, a `SupportLook` and Done.
  The model acknowledges a tip when the window closes.
- **Screenshots:**
  - The app overrides `RunScreenshots`, builds its model over a `SampleEnvironment` and runs a `ScreenshotScript`.
  - Its `Program` passes a null instance key while `ScreenshotCapture.IsEnabled`.
  - A runner like `scripts/fixture-screenshots.ps1` belongs in `tools/` for every app: for 5 or M3.2, with
    `companion.json`'s Windows section.
- **Cost:** a character on screen costs about 2.5–3 % of a core on this ARM64 machine (4.3). The owner decides
  whether that's fine, or whether the sway moves to the compositor or a coarser skip.
- **Still to do by hand,** besides 3.5's list:
  - the light theme on screen;
  - two displays;
  - a touch pinch;
  - shortcut recording;
  - the startup task in a package;
  - a language change.

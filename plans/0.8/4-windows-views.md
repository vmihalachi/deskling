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
  300 ms apart), and shushed's droop, for both fixtures. The canvas stops when its window hides (`XamlRoot.IsHostVisible`).
- **Not checked yet:**
  - switching to the light theme on screen (UI Automation can't select WinUI's `RadioButtons`; 4.5's run draws light,
    dark and contrast);
  - the 0.6 s wake spring by eye;
  - the raster file choice on a 100 % or 150 % display;
  - the hit test on screen (4.3).

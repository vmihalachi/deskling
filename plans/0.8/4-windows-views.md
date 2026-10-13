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

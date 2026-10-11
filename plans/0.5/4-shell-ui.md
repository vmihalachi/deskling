# 4: Shell (status item, speech, chime) and CompanionUI

> Sequence 4 of `README.md` (read *Shell and CompanionUI*, *Two-part jokes*). Needs 2; can run while 3 runs. One session, macOS.

## Read only these

- `README.md` here; `Sources/DesklingShell/{FloatingWindow,FloatingPlacement,NotificationPoster,WindowManager}.swift`; `Package.swift`.
- From sunnysays `macos/SunnySays/`: `Shell/{StatusItemController,SunnyGlyph,DesktopSunny}.swift`, `Channels/{Speaker,VoiceLibrary,ChimePlayer,Notifier}.swift`,
  `Signals/SignalHub.swift`, `Character/CharacterView.swift`, `App/AppModel.swift` lines 150–220 and 513–671 only, `App/ScreenshotMode.swift`,
  `Theme/{Spacing,Styles}.swift` (the bubble and pill types), `Views/DesktopView.swift`; tests `macos/SunnySaysTests/{VoiceLibraryTests,CharacterViewTests,AppModelTests}.swift`.

## Steps

| Step | Commit | What |
|---|---|---|
| 4.1 | `Shell: StatusItemController, Speech, ChimePlayer` | `Sources/DesklingShell/StatusItemController.swift` (Mood-free; the glyph comes from the injected `image(animating:frame:)`; tooltip and accessibility label injected; the popover, right-click menu trick, 8 fps frames, wave and Reduce Motion as today); `Speech/Speaker.swift` (`Speaking`, `Speaker`, `appLanguage(bundle:)`), `Speech/SpeechSequencer.swift`, `Speech/VoiceLibrary.swift` (string `VoiceID`s, `fallbackToSystemVoice`, no bank knowledge), `Speech/ChimePlayer.swift` (`ChimePlaying`, `init(bundle:)`). Tests in `Tests/DesklingShellTests`: `SpeechSequencerTests` with a scripted `Speaking` fake (beat order, `isActive` through the beat, `stop()` cancels the pending part, `isVoicing` false during the beat); `VoiceLibraryTests` moved from sunnysays with a manifest fixture under `Tests/DesklingShellTests/Resources/Voice/` including `.1`/`.2` clip ids and a stale `textHash`. |
| 4.2 | `CompanionUI: SignalHub, glyph, CharacterView, DesktopCompanion` | New product `DesklingCompanionUI` (depends on Companion, Core, System, Shell; `#if os(macOS)`), `DesklingCompanionUITests`. `SignalHub.swift` (public, `lowBatteryPercent` injected), `GlyphRenderer.swift` + `StatusItemController+Glyph.swift`, `CharacterView.swift` (image parts via `drawLayer` + `draw(Image(asset, bundle: artBundle), in:)` under the transform; paints from the environment palette; wake spring as today), `DesktopLayout.swift` (pure: `init(art:)`; `headHeightRatio`, `bubbleTuck`), `DesktopCompanion.swift` (over `FloatingWindow`; callbacks as `DesktopSunny`). Tests: `DesktopLayoutTests` (sunnysays' `sunflower.json` as a resource → 44/132 and 0.12), `CharacterViewTests` (wake math), an image-part render test (a 2-layer fixture drawn into a bitmap: the head layer's pixel moves with `headAngle`). |
| 4.3 | `CompanionUI: DeliveryPerformer, notifier, palette, bubble, pill, Space, ScreenshotCapture` | `DeliveryPerformer.swift` (line, tune, sequence with and without speech, notify fallback, `partDelay`, `unavailableIDs(voice:)`, `preview`, `stop`), `VoiceLibrary+Bank.swift`, `CompanionNotifier.swift` (over `NotificationPoster`), `Theme/CompanionPalette.swift` (+ `EnvironmentValues.companionPalette`), `Theme/Space.swift`, `Theme/SpeechBubble.swift` (`SpeechBubble`, `BubbleShape`, `BubbleCountdown`), `Theme/StatusPill.swift`, `ScreenshotCapture.swift` (env `DESKLING_SCREENSHOT_MODE`/`_DIR`, the capture loop, make-key retry, throwaway defaults suite, fixed clock, `SilentSpeaker`, `SilentChime`, `FixedPriceBackend`). Tests: `DeliveryPerformerTests` with the silent fakes (bubble parts in order, `isSpeaking` across the beat, notification body with two lines, stop mid-beat). |
| 4.4 | `docs: Shell and CompanionUI` | README rows, `AGENTS.md` layout (the new product, `Speech/`), CHANGELOG *Unreleased* "Added" lines. |

## Finish

`swift build && swift test` on macOS; `xcrun swift-format lint`; `scripts/verify.sh --all` ok; `swift.yml` green.
Hand to sequence 6: the list of `#if os(macOS)` files for the iOS step (every file in System, Shell, Store, CompanionUI).

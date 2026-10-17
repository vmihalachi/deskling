# 1: The app model, the delegate, the menus

> Sequence 1 of `README.md` (read *Decisions*, *Products after 0.7.0*, *Shell/*). Needs nothing; 2, 3 and 4 can run
> alongside. One session, macOS.

## Read only these

- `README.md` here; `AGENTS.md`; `Package.swift`; `Sources/LachingCompanionUI/{DeliveryPerformer,DesktopCompanion,SignalHub,CompanionNotifier,StatusItemController+Glyph}.swift`;
  `Sources/LachingShell/{WindowManager,StatusItemController,GlobalHotKey,LaunchContext}.swift`, `Speech/SpeechSequencer.swift`;
  `Sources/LachingCompanion/Settings/CompanionSettings.swift` (the subclassing pattern and its doc comment).
- From sunnysays `../sunnysays/macos/SunnySays/`: `App/{AppModel,AppDelegate,SunnySaysApp}.swift`, `Shell/{SunnyMenu,SunnyWindow}.swift`,
  `Channels/Notifier.swift`; tests `../sunnysays/macos/SunnySaysTests/AppModelTests.swift`.
- From Pagliacci `../pagliacci/macos/Pagliacci/`: `App/AppModel.swift` (what the subclass must be able to do: the
  two-part bubble, the gel, the skin, `faceMood`, `isVoicing`), `Channels/VoicingSpeaker.swift`, `Shell/PagliacciMenu.swift`.
- Never the apps' String Catalogs, generated files or anything else.

## Steps

| Step | Commit | What |
|---|---|---|
| 1.1 | (spike, no commit if it works) | A generic `@Observable open class CompanionAppModel<Window: WindowID>` in `LachingCompanionUI`, subclassed from a test target in another module with an `@Observable final class` adding a property and overriding an `open` method, `@MainActor` on both. If the macro, the generic and the cross-module subclass don't get along, the fallback is a non-generic base that talks to windows through `WindowRoles` of type-erased `AnyWindowID` and a `protocol CompanionWindows` the subclass implements over its `WindowManager`; write the fallback down in the commit message of 1.2. |
| 1.2 | `CompanionUI: CompanionAppModel` | `Shell/Copy.swift` (`StatusCopy`, `DesktopCopy`, `CompanionAppConfiguration`, `WindowRoles`), `Shell/CompanionNotifying.swift`, `Shell/CompanionAppModel.swift`: `APP/App/AppModel.swift` made generic, every `String(localized:)` replaced by a copy closure, every Sunny-specific line turned into an `open` hook (the list in the README). `statusLine(status:snapshot:now:calendar:endOfToday:copy:)` stays a static function so it is unit-testable as today. `DeliveryPerformer.onVoicingChanged` driven by `SpeechSequencer` (true when a part's audio starts; false when it ends or the beat starts); `isVoicing` on the model. `refreshVoice()` uses `performer.unavailableIDs(voice:)` (Pagliacci's way) and logs pending ids in Debug. No app name anywhere, no product id, no sound name as a default. |
| 1.3 | `CompanionUI: CompanionAppDelegate, CompanionMenus` | `Shell/CompanionAppDelegate.swift` (`APP/App/AppDelegate.swift`: `isRunningTests`, the login-item launch context, `reopen`, the optional screenshot mode hooks; `makeModel()` required); `Shell/CompanionMenus.swift` (`APP/Shell/SunnyMenu.swift` over `MenuCopy`, with the desktop sizes labelled by a closure; `ActionItem` public; the Debug quit title is the app's: `MenuCopy.quit` is just a string). |
| 1.4 | `CompanionUI: CompanionAppModel tests` | `Tests/LachingCompanionUITests/Shell/CompanionAppModelTests.swift`: a fixture subclass (`FixtureModel`) over `PhraseBank.fixture` and `CompanionDefaults.fixture`, scripted `FakeSpeaker`, `FakeChime`, `FakeNotifier` in `Tests/LachingCompanionUITests/Support/` (from sunnysays' `AppModelTests.swift` lines 374–433), and the generic tests moved from sunnysays' file: poke speaks when voice is on; bubble only when every channel is off; blip precedes the line; a tick on a call delivers nothing; ambient lines arrive when calm; turning the notification channel on asks once; onboarding asks only when notifications were picked; Shh for an hour sets the status line and no speech; holding shushes only when awake; rest of today and until I say have their own status lines; wake speaks the shush-end line; a call mid-line stops speech; Shh cuts the current line; reset ends Shh without a line; a channel tile previews once; previews keep their manners; mute with undo; notification actions route back; the desktop is off until turned on; a call takes the character off the desktop until ten seconds after; a delivered line fills the desktop bubble; a call clears it; hide for an hour comes back; show now ends a hide; hide until shown turns the setting off; a pinch steps the size; reset puts it back in the corner. Plus: `onVoicingChanged` is false during a sequence's beat (from Pagliacci's two-part test, over the fixture sequence). |
| 1.5 | `docs: Shell/` | `AGENTS.md` layout row for `LachingCompanionUI` (`Shell/`), CHANGELOG *Unreleased* "Added" lines, a doc comment on `CompanionAppModel` showing the subclass skeleton (as `CompanionSettings` does). |

## Finish

`swift build && swift test` on macOS; `xcrun swift-format lint -r --strict --configuration .swift-format Sources Tests`;
`scripts/verify.sh --all` ok; `swift.yml` green. Hand to 5: the exact `open` surface (so the migration briefs can be
checked against it) and whether 1.1 needed the fallback.

## Outcome (2026-10-12)

Done in one session. **1.1 needed no fallback**: `@MainActor @Observable open class CompanionAppModel<Window: WindowID,
Settings: CompanionSettings>` is subclassed by an `@Observable final class` in the test module with a property of its
own. Differences from the README's API, all deliberate:

- **A second generic parameter, `Settings`**, so `model.settings` keeps the app's own type (`AppSettings` with
  `sunnyVoice` or `skin`) and views don't change.
- **`willSay(_ phrase:)` instead of `didDeliver(_:)`**: one hook before deliveries *and* previews (Pagliacci's
  `prepare(phrase)` needs both). Previews go through `say(_:)` (no guards, clears `bubblePhraseID`, stops first:
  Pagliacci's way; sunnysays' `testVoice` becomes `say(...)`); `preview(_:)` = `canPreview` + `say`.
- **`desktopBubbleWaits`** (open, false): Pagliacci's `!isSettingUp` in `lingerDesktopBubble`. **`stopSpeaking()` is
  open** (Pagliacci resets `isSettingUp`, then super).
- **`CompanionAppDelegate` isn't generic** (AppKit calls delegates through Objective-C, which a generic class can't
  expose): the app's subclass holds its model and overrides `companion: any CompanionLaunching`; a screenshot run
  overrides `runScreenshots() -> Bool`. `isRunningTests` is a static on it.
- **`CompanionAppConfiguration`** also carries `palette`, `displayFont` (the desktop bubble), `supporterProduct` (the
  menus' thanks; `model.isSupporter`) and `localize` (default `Phrase.localize`, the app's catalog). Copy structs are
  closures, not `@Sendable`, and nothing is `Sendable` (they live on the main actor).
- **`waveFromHome()` is in the base** (both apps had it, over `sounds.hello`). `DesktopHide` is top level.
- **The glyph talks while `isVoicing`** (sunnysays used `isSpeaking`; the same for one-part lines).
- `statusLine(status:snapshot:now:calendar:endOfToday:copy:)` is a static on the base (callable as `AppModel.statusLine`).

The `open` surface: required `windowContent(for:)`, `panelContent()`, `menuCopy`, `desktopCharacter()`, `desktopLayout`,
`glyphCharacter`; optional `willLaunch()`, `menuBarMenu()`, `desktopMenu()`, `glyphMood`, `preferredVoice`,
`willSay(_:)`, `showBubble(_:)`, `desktopBubbleWaits`, `voicingChanged(_:)`, `handleNotificationAction(_:)`,
`settingsDidReset()`, `stopSpeaking()`. Public helpers for subclasses: `observe(_:then:)`, `refreshVoice()`,
`refreshGlyph()`, `refreshStatus(snapshot:)`, `say(_:)`, `performer`, `chime`, `voices`, `statusItem`, `isLaunched`.

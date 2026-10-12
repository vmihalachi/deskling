# Deskling 0.7: the companion shell

> The design for the 0.7.0 release, written 2026-10-12 from a measurement of what sunnysays and Mister Pagliacci
> still repeat on top of 0.6. The sequences in this folder are self-contained briefs, one per working session:
> `1` and `2` (Swift), `3` (testing) and `4` (tools) are independent of each other and can run in any order or in
> parallel; `5` releases; `6` (mybackhurts) is droppable. Then each app migrates: sunnysays' M10
> (`../sunnysays/plans/milestones/10-companion-shell.md`) and Pagliacci's M7 (`../pagliacci/plans/milestones/7-companion-shell.md`).
> Start a sequence from its brief, not from this file, and read only the files the brief lists.

## Why

0.5 moved the mechanism under the character into `DesklingCompanion`: the apps' pure cores are now content only (a
bank, flavors, defaults, keys, skins, generated art). The layer above it was left in the apps on purpose ("the brand
kit stays until 0.6"), and a second app has now been built on it. Measured on 2026-10-12 with app and character names
normalized, non-blank lines, `difflib` matching blocks:

| Layer | sunnysays | Pagliacci | Same lines | Same |
|---|---|---|---|---|
| Pure core (Swift) | 690 | 990 | content only | ~0% |
| Mac app (`macos/<App>/`) | 3,564 | 3,880 | 2,362 | 61% |
| Mac tests and specs | 1,097 | 1,096 | 757 | 69% |
| Windows core and tests | 342 | 263 | 179 | 52% |
| Landing `vite.config.ts` + `og/render.ts` | 561 | 512 | 444 | 79% |

The biggest shared files: `AppModel` (512 lines, 72%), `SettingsView` (434, 80%), `AppModelTests` (267, 62%),
`ScreenshotMode` (189, 55%), the quiet-timeline case list (167, 98%), `SupportView` (143, 80%), `MenuPanel` (128),
the menus (119), `HomeView` (122). mybackhurts holds a third copy of the tooling it never migrated: its `l10n.py`
shares 703 lines with `tools/l10n.py`, its listing checker is 97% sunnysays', its App Store push script 58%, its
Vite config 69%.

It is drifting: adopting 0.6.0 needed the same 25-line edit to Settings and Onboarding in both apps; Pagliacci's
`AppModel` has grown helpers sunnysays lacks (the glyph refresh, status strings as statics, an onboarding opener, a
preview path, the Debug system-voice fallback), and each fix in one app is a manual re-do in the other.

Half of the repetition is the look and should stay duplicated. The other half is mechanism and moves here.

## Decisions

| Topic | Decision |
|---|---|
| Copy | **Stays in the apps.** The package still ships no strings (`AGENTS.md`). Every moved view, menu and the model take their words from the app through a copy struct or a parameter per label, never a default. The apps' String Catalogs and `l10n.py` workflows are untouched: the same `String(localized:comment:)` calls move from one app file to another. |
| The look | **Stays in the apps:** `HomeView`, `OnboardingView`, the panel's layout, `Palette`, the font names, the button and chip styles (`SunButtonStyle`, `LeafChipStyle` / `PrimaryButtonStyle`, `ChipToggleStyle`, `TileToggleStyle`), the title and label views, the sticker card / card surface, `Windowsill` / `GelStage`, the landing's `main.js` and `style.css`. Only chrome that is byte-identical in both apps moves (`WindowInset`, `ActionBar`, `PanelRowStyle`, `Pointer`, the window root modifier). |
| Mechanism | **Moves, and is the same bytes afterwards.** Both apps' content vectors stay byte-identical, their screenshots pixel-identical, their test counts accounted for. A diff is a bug. |
| Compatibility | **Additive.** 0.7.0 adds types and functions; nothing from 0.6 changes meaning or signature. The app migrations are deletions. (`DeliveryPerformer` gains one optional callback.) |
| Where | `DesklingCompanionUI` gains `Shell/`, `Settings/`, `Support/`, `Screenshots/`; `DesklingShell` gains `FontRegistration`; `DesklingTesting` gains the standard cases and `CompanionVectors`; `Deskling.Testing` gains `CompanionReplay`; `tools/` gains `landing/` and `store/`. No new product, no new NuGet package. |
| Landing | `tools/landing/` is an npm package (`@deskling/landing`, TypeScript sources, not published) that each app's `landing/package.json` takes as `file:../deskling/tools/landing` through the submodule. The app's `vite.config.ts` keeps its data and brand hooks. |
| Store | `tools/store/{check,push,import}.py` read a new `store` section of `companion.json`; the apps keep two-line wrappers at today's paths. sunnysays' `push.py` (the review part, `--status`, IAP creation) and mybackhurts' (previews, `test_push.py`) are merged, with both repos' "App Information languages first" fix. |
| mybackhurts | Adopts the tools only (`l10n.py`, the hook, `store/`, `landing`), keeps its own `conformance.sh`, `verify.sh` and screenshots, and stays on Deskling 0.4.0 for code. Droppable (sequence 6). |
| Timing | After Pagliacci's M3 lands and, if the dates allow, after its M5 ships: no session may be editing either app's `AppModel` or `SettingsView` while 5.4 dry-runs the migration. The brief of each migration says the same. |

## Products after 0.7.0

| Product | Gains |
|---|---|
| `DesklingShell` | `FontRegistration` (registers the app's bundled `.ttf` files once; `isAvailable(names:)`). `DeliveryPerformer` is in CompanionUI, see below. |
| `DesklingCompanionUI` | `Shell/`: `CompanionAppModel<Window>` (the 20 s tick, the channels, the status item, the desktop presence, the windows, the hotkey, the store, the bubble, mute and undo, previews, the status line), `CompanionAppDelegate<Window>`, `CompanionMenus` + `ActionItem`, `WindowRoles`, the copy structs (`StatusCopy`, `DesktopCopy`, `MenuCopy`), `CompanionNotifying`. `Settings/`: `ActiveHoursEditor`, `QuietHoursEditor`, `ShortcutRecorder`, `LaunchAtLoginToggle`, `LanguageRow`, `NotificationPermissionNote`, `SettingsFooter`, bindings on `CompanionSettings`. `Support/`: `SupportWindow`, `SupportCopy`, `SupportProducts`. `Screenshots/`: `ScreenshotScript`, `ScreenshotCapture.SampleEnvironment`. `Theme/`: `WindowInset`, `windowPadding`, `ActionBar`, `PanelRowStyle`, `Pointer`, `companionWindowRoot`, `DisplayWeight`. `Phrase+Localized` (`localizedParts`, `localizedText`, `localize`). `DesktopCompanion.stand(...)` and `bubblePreview(...)`, the views it draws, for screenshot sheets. `DeliveryPerformer.onVoicingChanged`. |
| `DesklingTesting` | `QuietTimeline.standardCases(base:dst:)`, `PickerVector.standardCases(...)`, `CompanionVectors`, `ConformanceSuite.companion(_:root:regenerateHint:)`. |
| `Deskling.Testing` | `Conformance/Companion/CompanionReplay.cs`: one call per vector kind over a root, so an app's replay test is theories only. |
| `tools/` | `landing/` (`@deskling/landing`: the locale plugin, legal pages, hreflang and sitemap, the CSP `_headers`, the OG and icon renderer, the rollup inputs), `store/` (`check.py`, `push.py`, `import.py`, tests), `companion.schema.json` `store` section, `character` and `conformance` optional, `verify.sh` runs `store/check.py` for `store-listing/*.md`. |

Test targets: `DesklingCompanionUITests` gains `CompanionAppModelTests` (the generic third of the apps' `AppModelTests`,
over a fixture subclass and the fixture bank), `SupportWindowTests`, `SettingsRowsTests`; `DesklingCompanionTests`
gains `StandardCasesTests` (the standard lists reproduce `conformance/companion/{picker,quiet}` over the fixtures);
`tools/tests/test_store.py` (from mybackhurts' `test_push.py`), `tools/landing/test/` (vitest; `tools.yml` gains a node
job).

## The API

Origins: `APP` = `../sunnysays/macos/SunnySays`, `PAG` = `../pagliacci/macos/Pagliacci`. Where the two differ, the
brief says which one is the reference.

### Shell/ (`CompanionAppModel.swift`, `CompanionAppDelegate.swift`, `CompanionMenus.swift`, `Copy.swift`)

From `APP/App/AppModel.swift` (706 lines; `PAG/App/AppModel.swift` is the same with the two-part bubble, the gel and
the skin on top). The base keeps everything the two have in common; the subclass keeps what is its own.

```swift
/// Which of the app's windows plays which part. The base shows onboarding on the first run, Home otherwise, closes
/// onboarding on Get started, and acknowledges a tip when the Support window closes.
public struct WindowRoles<Window: WindowID>: Sendable { public init(home:onboarding:settings:support:) }

/// What the package can't know. No defaults for the words.
public struct CompanionAppConfiguration: Sendable {
    public var hotKeySignature: OSType              // "SNNY", "PGLC"
    public var statusItemTooltip: String            // "sunnysays Dev" in Debug; the app decides
    public var accessibilityLabel: String           // "Sunny", "Mister Pagliacci"
    public var paper: NSColor                       // the windows' background
    public var sounds: (blip: String, shh: String?, hello: String?)
    public var status: StatusCopy; public var desktop: DesktopCopy; public var tuneBubbleText: @Sendable () -> String
}
public struct StatusCopy: Sendable {   // the status pill, exactly APP/App/AppModel.swift's statusLine(status:snapshot:now:calendar:endOfToday:)
    public var shhUntilYouSay, shhRestOfToday, quietCall, quietFullScreen, offHours, asleep: @Sendable () -> String
    public var shhUntil: @Sendable (_ time: String) -> String; public var shhUntilDay: @Sendable (_ day: String) -> String
    public var nextIn: @Sendable (_ span: String) -> String
}
public struct DesktopCopy: Sendable { public var sayActionName, backTomorrow: @Sendable () -> String; public var backAt: @Sendable (_ time: String) -> String }

/// Every side effect goes through an injected protocol or provider, so tests drive a whole day with a mock clock and
/// fake channels. Subclass it (the same name `AppModel` in the app keeps views and tests as they are).
@MainActor @Observable
open class CompanionAppModel<Window: WindowID> {
    public let settings: CompanionSettings, coordinator: CompanionCoordinator, signals: SignalHub, purchases: PurchaseStore, voices: VoiceLibrary
    public let roles: WindowRoles<Window>, configuration: CompanionAppConfiguration
    @ObservationIgnored public private(set) var windows: WindowManager<Window>!
    public let performer: DeliveryPerformer
    // Read by views, as today: bubble, bubblePhraseID, justMuted, mood, statusLine, isSpeaking, isVoicing, isShushed,
    // notificationPermission, shortcutUnavailable, isOnDesktop, desktopBubble, desktopCountdown, desktopHiddenUntil, desktopBackLine.
    public init(settings:bank:roles:configuration:clock:idle:busy:calendar:speaker:chime:notifier: any CompanionNotifying,
                signals:purchases:voices:seed:)
    // Actions, as today: launch(showHome:), reopen(), tick(), sayNow(), shush(_:), holdToShush(), wake(), muteBubbleLine(), undoMute(),
    // previewChannel(_:line:), preview(_:), canPreview, openHome/openSettings/openSupport/openOnboarding(), closeWindow(_:), quit(),
    // completeOnboarding(), setChannel(_:on:), requestNotificationPermission(), refreshNotificationPermission(), openNotificationSettings(),
    // resetSettings(), registerHotKey(), suspendHotKey(), refreshStatus(snapshot:), refreshVoice(), stopSpeaking(),
    // setShowOnDesktop(_:), hideDesktop(_:), showDesktopNow(), stepDesktopSize(_:), moveDesktop(to:), resetDesktopPosition(), dismissDesktopBubble().
    public func observe(_ read: @escaping @MainActor () -> Void, then react: @escaping @MainActor () -> Void)  // re-arming watcher, for the subclass's own keys
    // Open, for the app:
    open func willLaunch()                                  // fonts, anything before the status item exists
    open func windowContent(for id: Window) -> AnyView      // required (fatalError in the base)
    open func panelContent() -> AnyView                     // required
    open func menuBarMenu() -> NSMenu                       // default CompanionMenus.menuBar(self, copy: menuCopy, …): override menuCopy instead
    open func desktopMenu() -> NSMenu
    open var menuCopy: MenuCopy { get }                     // required
    open func desktopCharacter() -> AnyView                 // required
    open var desktopLayout: DesktopLayout { get }           // required
    open var glyphCharacter: RiggedCharacter { get }        // required (sunnysays: .sunflower; Pagliacci: the worn skin)
    open var glyphMood: Mood { mood }                       // Pagliacci: awake during the beat
    open var preferredVoice: VoiceID? { nil }               // sunnysays: settings.sunnyVoice; Pagliacci: "clown"
    open func didDeliver(_ delivery: Delivery)              // after bubblePhraseID is set, before the performer runs
    open func showBubble(_ text: String)                    // default: bubble = text; showDesktopBubble(text). Pagliacci: setup/punchline/gel, then super
    open func voicingChanged(_ on: Bool)                    // default: isVoicing = on; the glyph refreshes
    open func handleNotificationAction(_ id: String)        // the raw action id
    open func settingsDidReset()                            // after resetToDefaults, before the scheduler resumes
}

/// The raw action ids of the app's notification, so the package needs no enum of the app's.
@MainActor public protocol CompanionNotifying: AnyObject { var onAction: ((String) -> Void)? { get set }; func configure(); func requestAuthorization(completion:); func refreshAuthorization(completion:); func post(line: String) }
```

`CompanionAppDelegate<Window>` is `APP/App/AppDelegate.swift` with `makeModel() -> CompanionAppModel<Window>`
(required override) and an optional `screenshotMode: (isEnabled: () -> Bool, makeModel: () -> CompanionAppModel<Window>, run: (CompanionAppModel<Window>) -> Void)?`.
`CompanionMenus` is `APP/Shell/SunnyMenu.swift` over a `MenuCopy` (say, wakeUp, shh, forAnHour, forTheRestOfToday,
untilISay, desktopSubmenu, show, showNow, hideForAnHour, hideForTheRestOfToday, size, hide, untilIShowItAgain, settings,
support, thanksForSupporting, quit; sizes come from the app's `DesktopSize` labels closure); `ActionItem` becomes public.
`DeliveryPerformer.onVoicingChanged: ((Bool) -> Void)?` fires from the sequencer (true when a part's audio starts,
false when it ends or the beat begins), which retires `PAG/Channels/VoicingSpeaker.swift`.

### Settings/ (from `APP/Views/SettingsView.swift` lines 329–515; `PAG` has the same)

Rows and editors, each taking its labels; the sections, headers and toggles stay in the app (their copy differs by
design: "Comment on the battery" is "Joke about the battery" there).

```swift
public struct ActiveHoursEditor<Chip: ToggleStyle>: View { public init(settings: CompanionSettings, activeHours: Text, from: Text, to: Text, days: Text, chipStyle: Chip) }
public struct QuietHoursEditor<Chip: ToggleStyle>: View { public init(settings: CompanionSettings, title: Text, chipStyle: (_ compact: Bool) -> Chip); public static func label(_ hour: Int) -> String }
public struct ShortcutRecorder<Style: ButtonStyle>: View { public init(shortcut: Binding<KeyShortcut?>, pressKeys: Text, record: Text, clear: Text, hint: Text, buttonStyle: Style, onCaptureStarted: () -> Void, onCaptureEnded: () -> Void) }
public struct LaunchAtLoginToggle: View { public init(label: Text, errorText: @escaping (String) -> String, errorColor: Color) }
public struct LanguageRow<Style: ButtonStyle>: View { public init(label: Text, change: Text, buttonStyle: Style) }   // current language name + System Settings
public struct NotificationPermissionNote<Style: ButtonStyle>: View { public init(permission: NotificationPermission, text: Text, open: Text, buttonStyle: Style, action: () -> Void) }
public struct SettingsFooter: View { public init(_ text: Text) }
extension CompanionSettings { public func binding(for channel: Channels, set: @escaping (Channels, Bool) -> Void) -> Binding<Bool>; public func binding(for flavor: FlavorID) -> Binding<Bool>; public func mutedCount(in bank: PhraseBank) -> Int }
```

### Support/ (from `APP/Views/SupportView.swift`; `PAG` identical in structure)

```swift
public struct SupportProducts: Sendable { public init(supporter: String, tips: [String]) }
public struct SupportCopy {   // every Text the window shows; no defaults
    title, pitch, supporterNote, supporterThanks, tipsTitle, tipsNote, askToBuy, pricesFailed, tryAgain, restore, done: Text
    becomeSupporter: (_ price: String?) -> Text; tipTitle: (_ product: String) -> String
    welcomeLine: String; welcomeNote: Text?; tipThanksLine: String; error: (PurchaseError) -> String
}
public struct SupportWindow<Primary: ButtonStyle, Outline: ButtonStyle>: View {
    public init(purchases: PurchaseStore, products: SupportProducts, copy: SupportCopy, character: @escaping (_ size: CGFloat) -> AnyView,
                characterSizes: (aside: CGSize, celebrating: CGSize), bubbleSize: CGFloat, primary: Primary, outline: Outline, onDone: () -> Void)
}
```

The Settings Support card stays in the app (its surface is the brand).

### Theme/ and the rest of CompanionUI

- `WindowInset` (side 32, top 24, bottom 32, panel 16: the same numbers in both apps) and `View.windowPadding(bottom:)`;
  `ActionBar`; `PanelRowStyle` (hover fill = the palette's `accentSoft`); `Pointer(color:lineWidth:headLength:)` (sunnysays strokes 2 pt with an 11 pt head, Pagliacci 1.5 pt with 10);
  `View.companionWindowRoot(palette:displayFont:tint:bodySize:)` (what `SunnyWindowRoot` / `PagliacciWindowRoot` do);
  `DisplayWeight`.
- `FontRegistration` (`DesklingShell`): `register(bundle:subdirectory:)` once, `isAvailable(_ names: [String], size:)`. The
  apps keep their font names and `Font.display`.
- `Phrase+Localized.swift`: `localizedParts(bundle:)` (`PAG/App/JokeText.swift`'s `parts`, the superset), `localizedText`,
  `static localize(_:_:bundle:)`.
- `DesktopCompanion.stand(character: AnyView, layout: DesktopLayout, shade: Color) -> some View` (what `DesktopView` draws)
  and `bubblePreview(text:tail:countdown:layout:)`; `DesktopCompanion` itself draws through the same views.
- `Screenshots/ScreenshotScript.swift`: `ScreenshotCapture.SampleEnvironment` (in-memory settings store, `FixedClock`
  at `sampleNow`, `ConstantIdle`, `NeverBusy`, a silent `SignalHub`, `FixedPriceBackend` + throwaway defaults) and
  `ScreenshotScript<Window>` with `Shot` cases `.window(Window)`, `.view(name:_:size:)`, `.fitting(name:_:)`,
  `.supporterSequence(window:product:shots:)` (bought while open → thanks; opened again → the tips), `.custom(name:_:)`,
  run in light and dark by `run(model:shots:root:paper:)`. The app's `ScreenshotMode` becomes the shot list plus its
  sheets.

### Testing

- `QuietTimeline.standardCases(base: QuietTimeline.Settings, dst: (name: String, timeZone: String)) -> [Case]`: the 17
  timelines of `../sunnysays/macos/SunnySaysCore/Tests/SunnySaysCoreTests/Conformance/QuietVectors.swift`, names, starts,
  seeds and steps verbatim, over the app's `base` (its defaults with the frequency it chose) and the app's DST city
  (`evening-bucharest-dst-end`, `evening-rome-dst-end`). `Tests/DesklingCompanionTests/Conformance/CompanionCases.swift`
  uses it too (plus its own `sequence-delivered`, `italian-afternoon`).
- `PickerVector.standardCases(bank:skin:language:single: (name: String, flavors: Set<FlavorID>), pair: (name:flavors:)) -> [Case]`:
  the cases both apps share (dayparts, recents, events, excluded, the two flavor cases by the app's names); the app
  appends its own (Pagliacci's Italian ones).
- `CompanionVectors` (a protocol with static `bank`, `flavors`, `defaults`, `characters`, `settings: JSONValue`,
  `pickerCases`, `quietCases`) and `ConformanceSuite.companion(_ vectors: some CompanionVectors.Type, root: URL, regenerateHint: String)`,
  which builds today's `generate` and `replayers`. The app's `ConformanceTests` keeps its four test methods over it.
- C# `CompanionReplay`: `Phrases(root, bank, flavors)`, `Settings(root, defaults, compareShortcut)`, `Picker(path, bank)`,
  `Quiet(path, bank, defaults)`, `Character(path, characters)`, each returning the problems list; `Files(folder)` as
  `ConformanceRoot.Files`.

### `tools/landing` (`@deskling/landing`)

From `../sunnysays/landing/vite.config.ts` (512 lines; Pagliacci's and mybackhurts' are the same file with other
data). `companionLanding(options)` returns `{ plugins, build }` for `defineConfig`:

```ts
export type Language = { code: string; path: string; label: string; nativeName: string; ogLocale: string };
export function companionLanding(options: {
  root: string; siteUrl: string; appStoreUrl: string | null; contactEmail: string;
  languages: readonly Language[]; defaultLanguageCode: string; locales: Record<string, Locale>;   // same keys enforced
  legalPages?: readonly string[];                                   // ["privacy", "terms"]
  templatePath?: string; notFoundPath?: string;
  placeholders: (key: string, language: Language) => string | undefined;   // the app's own: line.*, runtimeJson, voiceSwitch, jokes…
  jsonLd: (language: Language) => object;                           // name, category, description…
  languageSwitcher?: "menu" | "inline" | ((current: Language, label: string) => string);
  storeButton?: (locale: Locale, big: boolean) => string;           // default: the Apple button / "coming soon"
  og: { dir: string; iconPath: string; fontFamily: string; copy: (language: Language) => Record<string, string>; escapeF?: boolean };
  csp?: { mediaSrc?: string };                                       // "'self'" where recorded voices play
  extraPlugins?: Plugin[];                                           // sunnysays' voice plugin
}): { plugins: Plugin[]; build: { rollupOptions: { input: Record<string, string> } } };
```

Inside: `locales.ts` (flattenKeys, readPath, assertSameLocaleKeys), `html.ts` (escapes, the switcher, the redirect
script, hreflang, the store button, `renderTemplate`), `legal.ts`, `sitemap.ts`, `hosting.ts` (the CSP hashes and
`_headers`), `og.ts` (`socialAssetsPlugin` with the font family and the resvg `f` workaround as options), `inputs.ts`.
`package.json`: `"name": "@deskling/landing"`, `"type": "module"`, `"exports": { ".": "./src/index.ts" }`,
`peerDependencies`: `vite-plus`, `@resvg/resvg-js`. Tests with vitest over a two-language fixture site.

### `tools/store`

`companion.json` gains:

```json
"store": {
  "dir": "store-listing",
  "apple": { "appId": "6820142690", "platform": "MAC_OS", "copyright": "2026 Vlad Mihalachi",
             "locales": {"en": "en-US", "de": "de-DE", "es": "es-ES", "fr": "fr-FR", "it": "it", "pt-BR": "pt-BR", "ro": "ro"},
             "extraLocales": {"en": ["en-GB", "en-CA", "en-AU"], "es": ["es-MX"], "fr": ["fr-CA"]},
             "baseTerritory": "ITA", "screenshots": "apple/screenshots", "previews": "apple/previews",
             "iaps": { "app.sunnysays.supporter": {"type": "NON_CONSUMABLE", "familySharing": true, "price": 1.99, "fields": "apple.iap.supporter", "reviewNote": "…", "reviewShot": "support"} },
             "review": { "notes": "app-review.md", "shots": "screenshots/macos", "size": [1440, 900], "background": "FEF8E9" } },
  "microsoft": { "screenshots": "microsoft/screenshots", "art": "microsoft/art" }
}
```

`check.py` keeps its limits table and reads `languages.all`; `push.py` is the union of the two scripts with
`test_push.py` as `tools/tests/test_store.py`; `import.py` is mybackhurts'. The schema makes `character` and
`conformance` optional (mybackhurts has neither); `companion.py` returns `None` paths for absent sections and
`l10n.py`'s scoped keys are empty without a `phrases.json`.

## What stays in the apps, on purpose

`HomeView`, `OnboardingView`, the panel's body (it keeps `PanelRowStyle` rows and the package's `StatusPill`),
`Palette`, the font names and `TypeSize`, the button, chip and tile styles, the title and label views, the cards,
`Windowsill` / `GelStage` / `Gel`, the Settings sections and their headers, the Support card, `AppSettings`,
`NotificationAction` and `Notifier` (over `CompanionNotifier`, now `CompanionNotifying`), the window enum, the sheets
(`CharacterSheet`, `DesktopSheet`), the flavor titles and samples, Pagliacci's two-part bubble, gel and skin logic,
sunnysays' voice picker and `waveFromHome`, the test fakes (`FakeSpeaker`, `FakeChime`, `FakeNotifier`: `DesklingTesting`
can't depend on `DesklingShell`), the workflows, the landing's `main.js`, `style.css`, `voice.ts`, `jokes.json`.

## The 0.7.0 changelog, in advance

**Added**: `DesklingCompanionUI` `Shell/` (`CompanionAppModel`, `CompanionAppDelegate`, `CompanionMenus`, `WindowRoles`,
the copy structs), `Settings/` rows and editors, `SupportWindow`, `ScreenshotScript`, the chrome, `Phrase+Localized`,
`DesktopCompanion.stand`; `DeliveryPerformer.onVoicingChanged`; `DesklingShell.FontRegistration`; `DesklingTesting`
standard cases and `CompanionVectors`; `Deskling.Testing.CompanionReplay`; `tools/landing`, `tools/store`,
`companion.json` `store`. **Changed**: `companion.json` no longer requires `character` or `conformance`.
**Migration**: none required; an app on 0.6 builds unchanged. To shed its copies, follow sunnysays' M10 brief.

# Deskling 0.5: the companion layer

> The design for the 0.5.0 release, agreed with the owner on 2026-10-11. The six sequences in this folder (`1-…` to
> `6-…`) are self-contained briefs, one per working session, in order; `3` and `4` can run in parallel with `2`.
> Start a sequence from its brief, not from this file, and read only the files the brief lists.

## Why

sunnysays (`../sunnysays`) built a desk companion on Deskling: a character that says lines on a cadence, knows when
to be quiet, stands on the desktop, speaks with recorded voices, and is mirrored on Windows through vectors. A second
app, Mister Pagliacci (`../pagliacci`: a realistic clown that tells jokes, with a Supporter purchase that unlocks skins),
needs the same skeleton. Everything under sunnysays' character is generic, so it moves here: the phrase bank and
picker, cadence presets, quiet rules and mood, the settings pattern, the coordinator, the desktop presence, the rig
that animates a character (vector or painted layers), the speaker and voice library, the status item, and the
tooling. sunnysays then migrates onto 0.5.0 (its milestone 9, with its vectors and screenshots as the safety net)
and Pagliacci is bootstrapped on it.

## Principles

- **Swift is the reference; every pure type mirrors to .NET file for file.** macOS-only products (Shell, CompanionUI)
  get Windows twins with sunnysays' M3, as `FloatingWindow` did.
- **New vector fields are optional and omitted at their default**, so sunnysays' committed vectors stay byte-identical
  through the migration. The C# loaders treat an absent field as the default.
- **Package code never names an app.** No `Skin.sunflower`, no `PhraseBank.standard`, no "Sunny" in a doc comment, no
  product ids, no strings. Apps inject their bank, defaults, skins, palette and voices.
- **Mechanism vectors live here, content vectors in each app.** Deskling pins the picker, cadence, rules, coordinator,
  desktop presence and rig over fixture data; each app pins its own bank, defaults, picks and poses.
- **Breaking is allowed** (0.x) with a CHANGELOG entry per change and the migration written down.

## Products after 0.5.0

| Swift product (platforms) | Depends on | .NET package | Contents |
|---|---|---|---|
| `DesklingCore` (macOS 14, iOS 17) | – | `Deskling.Core` | Today's, plus `Settings/SettingsStore.swift` (the typed protocol, `InMemorySettingsStore`, `UserDefaultsSettingsStore`). .NET: `ISettingsStore` becomes the typed interface; `LocalSettingsStore` implements it (`double` added). |
| `DesklingCompanion` (new; macOS 14, iOS 17) | Core | `Deskling.Companion` (new, net10.0, AOT) | `Phrases/`, `Cadence/`, `Rules/`, `Settings/`, `Skins/`, `Coordination/`, `Desktop/`, `Figures/`. Foundation + Observation only. |
| `DesklingSystem` | Core | `Deskling.Windows` | Unchanged. |
| `DesklingShell` | Core | `Deskling.Windows` | Plus `StatusItemController` (Mood-free), `Speech/` (`Speaker`, `SpeechSequencer`, `VoiceLibrary`, `ChimePlayer`). Shell keeps depending only on Core (mybackhurts uses it). |
| `DesklingCompanionUI` (new, macOS) | Companion, Core, System, Shell | – (with sunnysays' M3) | `SignalHub`, `GlyphRenderer`, `CharacterView` (vector and image parts), `DesktopCompanion`, `DeliveryPerformer`, `CompanionNotifier`, `VoiceLibrary+Bank`, `Theme/` (`CompanionPalette`, `Space`, `SpeechBubble`, `StatusPill`), `ScreenshotCapture`. The brand kit (buttons, chips, cards, Windowsill, fonts, the hours and shortcut editors) stays in the apps until 0.6. |
| `DesklingStore` | Core (new) | `Deskling.Core/Store` | `PurchaseStore` caches owned ids through a `SettingsStore` (`UserDefaults` convenience kept). |
| `DesklingTesting` | Core, Store, Companion | `Deskling.Testing` (new) | Mocks as today, plus `Fixtures/` (a fixture bank, vector and raster fixture rigs, fixture defaults), `Conformance/` (`ConformanceRoot`, `ConformanceSuite`, codecs, the generators parameterized by bank/defaults/character/cases, `BankAudit`). .NET: mocks, conformance helpers, replay bodies, fixture loaders. |

Test targets: `DesklingCompanionTests` (runs on Linux; its `Conformance/` generates `conformance/companion/`),
`DesklingCompanionUITests` (macOS), `dotnet/Deskling.Companion.Tests`. `swift.yml` gains an ubuntu job (Core +
Companion + Testing) and an iOS Simulator build of `DesklingCompanion` (sequence 6, droppable).

Two renames before the API goes public: the `Character` protocol becomes `DrawableCharacter` (it shadows
`Swift.Character` for every importer) and `Shape` becomes `PathShape` (SwiftUI's `Shape` is in scope in every view).

## The Companion API

Origins: `SS` = `../sunnysays/macos/SunnySaysCore/Sources/SunnySaysCore`, `APP` = `../sunnysays/macos/SunnySays`,
`WIN` = `../sunnysays/windows/SunnySays.Core`. Raw values and settings keys never change: they are in sunnysays'
vectors and in its users' defaults.

### Phrases/ (`Tag.swift`, `Phrase.swift`, `PhraseBank.swift`, `PhraseContext.swift`, `PhrasePicker.swift`)

`Tag` is split out of `SS/Phrases/Phrase.swift` and stays a closed enum (same raw values, `events`, `dayparts`,
`weekdays`, `daypart(hour:)`, `weekdayTag(weekday:)`): the moments the sensing can detect are the same for every app.

```swift
public struct FlavorID: RawRepresentable, Hashable, Codable, Sendable, ExpressibleByStringLiteral { … }   // SkinID, PaintRole, VoiceID alike
public struct Phrase: Equatable, Sendable, Identifiable {
    public enum Kind: Equatable, Sendable {
        case line(source: String)            // one catalog key, phrase.<id>
        case tune(sound: String)             // a sound file, chime channel only
        case sequence(source: [String])      // a joke in beats, ≥ 2 parts: phrase.<id>.1, phrase.<id>.2, …
    }
    public let id: String, kind: Kind, tags: Set<Tag>, weight: Int /* 1…5 */, flavor: FlavorID?, skins: Set<SkinID>
    /// BCP-47 base codes ("it"); empty = every language. A joke that only works in Italian lives in the Italian bank only.
    public let languages: Set<String>
    public init(_ id: String, _ kind: Kind, tags: Set<Tag>, weight: Int = 3, flavor: FlavorID? = nil, skins: Set<SkinID> = [], languages: Set<String> = [])
    public var partCount: Int; public var catalogKey: String; public var catalogKeys: [String]; public var clipIDs: [String]  // "<id>" or "<id>.1", "<id>.2"
    public var isTune: Bool; public var hasEventTag: Bool
    public func fits(_ skin: SkinID) -> Bool; public func fits(language: String) -> Bool     // base language: "pt-BR" → "pt"
}
public struct PhraseContext: Equatable, Sendable {   // skin and language have no defaults: that was the coupling to the app's SettingsDefaults
    hour, weekday, event: Tag?, chimeEnabled, recentIDs, flavors: Set<FlavorID>, excludedIDs, skin: SkinID, language: String
}
public enum PhrasePicker { recentWindow = 12; pick(from:context:unit:); candidates(in:context:) }  // candidates drop phrases not fitting `language` first, then as today
public enum PhraseTiming {
    public static let partBeat: TimeInterval = 1.2                                   // silence after a spoken setup
    public static func partDelay(spoken: Bool, previousPart: String) -> TimeInterval  // the beat when spoken, else DesktopPresence.bubbleSeconds(previousPart)
}
```
`source` replaces `english` in the Swift and C# API (an Italian-only joke's source text is Italian); the `phrases.json`
field keeps the name `english` for lines, so sunnysays' file does not move.

### Cadence/, Rules/, Desktop/

Moved verbatim from `SS/Cadence`, `SS/Rules`, `SS/Desktop`: `Frequency`, `ChatterCadence` (+ `Shush`;
`config(frequency:activeStartMinute:activeEndMinute:activeWeekdays:quietHours:hushInFullScreen:)` and
`config(for: CompanionSettings)`), `Snapshot`, `Channels`, `Trigger`, `ChannelPlan`, `SkipReason`, `Outcome`,
`Manners`, `QuietRules`, `Mood`, `CompanionState`, `DesktopPresence`. Doc comments lose the leaf and the pot.

### Settings/ (`SettingsKeys.swift`, `CompanionDefaults.swift`, `CompanionSettings.swift`, `SettingChoices.swift`)

```swift
public enum SettingsKeys { /* the 23 generic key constants of SS/Settings/SettingsKeys.swift, same strings */ }
public struct CompanionDefaults: Equatable, Sendable {  // today's numbers as package defaults; the three product choices are required
    public init(skin: SkinID, phraseFlavors: Set<FlavorID>, sayShortcut: KeyShortcut?)   // then adjust the vars
}
@Observable open class CompanionSettings {
    public let store: any SettingsStore; public let defaults: CompanionDefaults
    public init(store: any SettingsStore, defaults: CompanionDefaults)
    /* the 23 write-through properties of SS/Settings/AppSettings.swift minus sunnyVoice, same didSet pattern */
    public var manners: Manners
    open func resetToDefaults()           // subclasses override and call super
}
// SettingChoices.swift: AppearanceSetting, DesktopSize, DesktopPosition (from SS/Settings/Skin.swift minus Skin)
```
An app subclasses: `@Observable final class AppSettings: CompanionSettings` adds its own keys (sunnysays: `sunnyVoice`).
Sequence 1 spiked cross-module `@Observable` subclassing first: it passed (macOS and Linux, 2026-10-11), so the
class design above is what shipped and the `CompanionSettingsProviding` fallback was not needed.
Sequence 2 (2026-10-11) shipped `Figures/`, `Skins/`, the generators and `conformance/companion/`; the moved
generators rewrote sunnysays' picker, quiet, cadence, desktop, phrases and settings vectors byte for byte.
Sequence 3 (2026-10-11) shipped `Deskling.Companion`, `Deskling.Testing` and `Deskling.Companion.Tests`; every
companion vector replays on .NET.
Sequence 4 (2026-10-11) shipped the Shell status item and speech and `DesklingCompanionUI`.

### Skins/ (`SkinCatalog.swift`)

```swift
public struct SkinEntry: Sendable { public let id: SkinID; public let art: CharacterArt; public let unlockedBy: String? /* product id; nil = free */ }
public struct SkinCatalog: Sendable {
    public init(_ skins: [SkinEntry])     // skins[0] is the default and must be free
    public var defaultSkin: SkinID; public subscript(id: SkinID) -> SkinEntry?
    public func character(for id: SkinID) -> RiggedCharacter                       // falls back to the default
    public func isUnlocked(_ id: SkinID, owned: Set<String>) -> Bool
    public func effectiveSkin(selected: SkinID, owned: Set<String>) -> SkinID      // selected if unlocked, else default (a refund drops you back)
}
```

### Coordination/ (`Delivery.swift`, `CompanionCoordinator.swift`, from `SS/Coordination/AppCoordinator.swift`)

```swift
public struct Delivery: Equatable, Sendable { phrase, trigger, plan }
public final class CompanionCoordinator {
    public static let welcomeBackCooldown: TimeInterval = 30 * 60; public static let recentCapacity = 24
    public init(settings: CompanionSettings, bank: PhraseBank, language: String, clock: Clock, idle: IdleTimeProvider,
                busy: BusyStateProvider, calendar: Calendar, seed: UInt64 = UInt64.random(in: 0...UInt64.max))
    public var language: String; public var unavailableIDs: Set<String>
    // tick / poke / event / shush / wake / settingsChanged / mood / isShushed / recentIDs / lastDelivery unchanged
}
```
Purchase gating is not the coordinator's business: the app keeps `settings.skin` valid through
`SkinCatalog.effectiveSkin` when entitlements change.

### Figures/ (vector and raster)

```swift
public struct Rect: Equatable, Sendable, Codable { origin: Point; size: Size }
public enum PartContent: Equatable, Sendable { case shapes([PathShape]); case image(asset: String, frame: Rect) }   // frame in view-box units
public struct PathShape: Equatable, Sendable { path: [PathCommand]; fill: PaintRole?; stroke: PaintRole?; strokeWidth; opacity; evenOdd }
public struct BubbleAnchor: Equatable, Sendable { pivot: String; tuck: Double }     // where a speech bubble aims and how far it tucks in (fraction of width)
public struct CharacterArt: Sendable { skin: SkinID; viewBox; pivots: [String: Point]; parts: [String: PartContent]; rig: Rig; glyphSize; glyphFrames: [String: [PathShape]]
    public var bubblePoint: Point /* pivots[rig.bubble.pivot] */ }
public struct Rig: Sendable { period, stillTime, always: [String], head, stem, pulse: RigGroup, blink: Blink, moods: [Mood: MoodRig], glyph: [Mood: [String]], bubble: BubbleAnchor }
public struct MoodRig: Sendable { eyes, mouth: [String], mouthRate, headExtras: [String] /* ride on the head, e.g. ["cheeks"] */, overlays: [String] /* drawn last, untransformed, e.g. ["zz"] */, blink, droop, sink, sway, swayPeriod, pulse }
public struct Pose: Equatable, Sendable { mood, headAngle, sink, pulse, eyes, mouth, headExtras: [String], overlays: [String], blinking; func blended(to:progress:) }
public struct PlacedPart: Equatable, Sendable { name: String; transform: Transform; content: PartContent }
public protocol DrawableCharacter: Sendable {
    var skin: SkinID { get }; var viewBox: Size { get }; var glyphSize: Double { get }
    func pose(mood: Mood, time: Double) -> Pose; func stillPose(mood: Mood) -> Pose
    func layers(for pose: Pose) -> [PlacedPart]   // drawing order: `always` parts under pulse/head/stem/identity, headExtras + eyes + mouth under head, overlays last
    func shapes(for pose: Pose) -> [PathShape]    // vector convenience: path parts flattened through their transform; image parts skipped
    func glyph(mood: Mood, frame: Int) -> [PathShape]; func glyphFrameCount(mood: Mood) -> Int
}
public struct RiggedCharacter: DrawableCharacter { public init(art: CharacterArt) }
public enum CharacterArtJSON { static func encode(_:) throws -> Data; static func decode(_:) throws -> CharacterArt }   // the characters/<skin>.json format
```
`Transform` gains `affine: (a, b, c, d, tx, ty)`; transforms are rotation, translation and uniform scale, so a raster
never shears. In the JSON a vector part is an array of shapes (today's format) and an image part is
`{"image": "<skin>.<part>", "frame": [x, y, w, h]}`; the rig JSON accepts `cheeks: true` / `zz: true` as sugar for
`headExtras: ["cheeks"]` / `overlays: ["zz"]` and requires `bubble`. Pose vectors pin `name` + the six affine numbers
+ the content kind, never pixels. The menu bar glyph stays a vector line drawing for every skin.

## Shell and CompanionUI (macOS)

```swift
// DesklingShell/StatusItemController.swift (from APP/Shell/StatusItemController.swift; Mood-free)
@MainActor public final class StatusItemController: NSObject {
    public var menu: (() -> NSMenu)?
    public init(panel: AnyView, tooltip: String, accessibilityLabel: String, image: @escaping @MainActor (_ animating: Bool, _ frame: Int) -> NSImage)
    public func setAnimating(_ on: Bool); public func redraw(); public func wave(for duration: TimeInterval = 1.2)
    public func togglePanel(); openPanel(); closePanel()
}
// DesklingShell/Speech/Speaker.swift: `Speaking` + `Speaker` from APP/Channels/Speaker.swift (AVSpeechSynthesizer or an AVAudioPlayer clip; rate maps)
// DesklingShell/Speech/SpeechSequencer.swift (new)
public struct SpeechPart: Sendable { text: String; clip: URL? }
@MainActor public final class SpeechSequencer {
    public init(speaker: Speaking)
    public private(set) var isActive: Bool   // from the first part until the last finished, beats included (what Snapshot.isSpeaking reads)
    public var isVoicing: Bool               // audio playing right now (the mouth)
    public var onPartStarted: ((Int) -> Void)?; public var onFinished: (() -> Void)?
    public func speak(_ parts: [SpeechPart], beat: TimeInterval, voiceIdentifier: String?, rate: Double, volume: Double)
    public func stop()                       // cancels a pending beat too
}
// DesklingShell/Speech/VoiceLibrary.swift (from APP/Channels/VoiceLibrary.swift; the manifest format is unchanged, format 1)
@MainActor public final class VoiceLibrary {
    public init(folder: URL?, language: String, allowTesting: Bool, fallbackToSystemVoice: Bool = false)
    public var availableVoices: [VoiceID]; public var isVoiced: Bool; public let mouthFPS: Double
    public func voice(preferred: VoiceID?) -> VoiceID?
    public func clip(for id: String, text: String, voice: VoiceID) -> URL?      // valid iff textHash == sha256(text) and the file exists
    public func mouth(for id: String, voice: VoiceID) -> [Int]
    public func pendingClipIDs(_ clips: [(id: String, text: String)], voice: VoiceID) -> Set<String>
    public static func hash(_ text: String) -> String
}
// DesklingShell/Speech/ChimePlayer.swift: `ChimePlaying` + `ChimePlayer(bundle:)`

// DesklingCompanionUI
@MainActor public final class SignalHub { /* APP/Signals/SignalHub.swift; lowBatteryPercent injected */ }
@MainActor public enum GlyphRenderer { static func image(character:mood:frame:size: 18, accessibilityDescription:) -> NSImage }   // APP/Shell/SunnyGlyph.swift
extension StatusItemController { public func show(glyph character: RiggedCharacter, mood: Mood, talking: Bool, accessibilityDescription: String) }
public struct CharacterView: View { public init(character: RiggedCharacter, mood: Mood, talking: Bool, line: String?, animated: Bool = true, artBundle: Bundle = .main) }
public struct DesktopLayout: Sendable { headHeightRatio, bubbleTuck, shadowHeight, bubbleMaxWidth, bubbleMargin; public init(art: CharacterArt, …) }  // 44/132 and 0.12 for the sunflower
@MainActor public final class DesktopCompanion { /* APP/Shell/DesktopSunny.swift over FloatingWindow; the bubble placed from DesktopLayout */ }
@MainActor public final class DeliveryPerformer {   // the perform/speak/refreshVoice third of APP/App/AppModel.swift (lines 160–215)
    public init(bank: PhraseBank, speaker: Speaking, chime: ChimePlaying, voices: VoiceLibrary, post: ((String) -> Void)?)
    public var localize: (_ catalogKey: String, _ source: String) -> String; public var tuneBubbleText: () -> String; public var blipSound: String?
    public var onBubble: ((String) -> Void)?; public var onSpeechStarted: (() -> Void)?; public var onSpeechFinished: (() -> Void)?
    public var isSpeaking: Bool; public var isVoicing: Bool
    public func perform(_ delivery: Delivery, settings: CompanionSettings, voice: VoiceID?)
    public func preview(_ phrase: Phrase, settings: CompanionSettings, voice: VoiceID?)
    public func unavailableIDs(voice: VoiceID?) -> Set<String>     // pending phrases → coordinator.unavailableIDs
    public func stop()
}
@MainActor public final class CompanionNotifier<Action: RawRepresentable<String>> { public init(categoryID: String, actions: [(Action, String)], lineID: String) }
public struct CompanionPalette: Sendable { paper, paper2, card, ink, inkSoft, line, outline, shade, accent, accentText, accentSoft, secondary, secondaryText, secondarySoft, onInk, onAccent: Color; paints: @Sendable (PaintRole) -> Color }
extension EnvironmentValues { public var companionPalette: CompanionPalette }
public enum Space { … }   // APP/Theme/Spacing.swift
public struct SpeechBubble: View; public struct BubbleShape: SwiftUI.Shape; public struct BubbleCountdown: View; public struct StatusPill: View   // APP/Theme/Styles.swift
public enum ScreenshotCapture { … }   // the generic half of APP/App/ScreenshotMode.swift: DESKLING_SCREENSHOT_MODE / _DIR, in-process capture, make-key retry, fakes
```

## Two-part jokes end to end

- **Core:** `Kind.sequence(source:)`, keys `phrase.<id>.1`, `.2`; a sequence has a flavor like a line. `phrases.json`
  entries carry `kind: "sequence"`, `keys`, `parts` (omitted for lines and tunes). Recents, muting and the quiet
  timelines stay keyed on the phrase id ("Don't say that again" mutes the whole joke).
- **Delivery:** `DeliveryPerformer.perform` plays the chime blip if planned, then `SpeechSequencer.speak(parts, beat:
  PhraseTiming.partBeat)`; `onPartStarted(i)` → the bubble shows part *i*; `Snapshot.isSpeaking` is `sequencer.isActive`,
  so no other line starts during the beat and a call or Shh cuts the whole joke through `stop()`; the character's
  mouth follows `isVoicing`, closed during the beat. Without speech the performer times the parts with `partDelay`.
  The notification is one banner when the punchline lands, both parts on two lines. After delivery the panel bubble
  shows both parts; the desktop bubble shows the current part.
- **Voice:** two clips per joke, ids `<id>.1` and `<id>.2`, two manifest entries with their own `textHash`, `duration`
  and `mouth`; files `Voice/<lang>/<voice>/<id>.1.m4a`. A joke is pending in a voice when any part is. Two clips keep
  the bubble and mouth in step per part, keep the beat an app-side constant, and re-render one part on a rewrite.
- **Pagliacci Debug builds** use `VoiceLibrary(fallbackToSystemVoice: true)`: pending parts fall through to the system
  voice instead of being withheld.

## Language-scoped phrases

`Phrase.languages` lists the languages a phrase exists in (empty = all); `PhraseContext.language` is the app language;
the picker drops phrases that don't fit it before anything else. `phrases.json` writes `languages` only when non-empty.
`tools/l10n.py` reads `phrases.json` and requires a key `phrase.<id>` (or `.n`) only in that phrase's languages,
marking the entry "do not translate" elsewhere; `tools/voice.py` renders a clip only in the phrase's languages.
sunnysays' phrases all have `languages = []`, so nothing changes for it.

## Vectors: mechanism here, content in the apps

`conformance/companion/`, generated by `Tests/DesklingCompanionTests/Conformance` (flag `DESKLING_WRITE_CONFORMANCE`)
and replayed by `dotnet/Deskling.Companion.Tests`, over the fixtures in `DesklingTesting`:

| File | Over | Content |
|---|---|---|
| `phrases.json` | the fixture bank | sunnysays' format plus `sequence` entries and a language-scoped phrase |
| `settings.json` | fixture defaults | the 23 generic keys, defaults, `keyOrder`, the frequencies table, names, constants (`idleThresholdSeconds`, `busyGraceSeconds`, `welcomeBackCooldownSeconds`, `recentCapacity`, `recentWindow`, `partBeatSeconds`, `reappearDelaySeconds`, `hourHideSeconds`) |
| `picker/*.json` | the fixture bank | sunnysays' 19 cases re-expressed over fixture ids, plus `sequence-picked`, `skin-restricted-dropped`, `language-scoped-dropped` |
| `cadence/*.json` | – | sunnysays' 5 cases, moved byte for byte (no app data in them) |
| `quiet/*.json` | fixture bank + defaults | sunnysays' 17 timelines over fixture data, plus `sequence-delivered` |
| `desktop.json` | – | sunnysays' file, moved, plus `partDelays[]` |
| `characters/fixture-vector.json`, `fixture-vector-poses.json` | `FixtureArt.vector` | written by `CharacterArtJSON.encode` (the same format `tools/character.py` writes) and the pose vector in sunnysays' format |
| `characters/fixture-raster.json`, `fixture-raster-poses.json` | `FixtureArt.raster` | image parts; the poses pin `layers[]` (name, the six affine numbers, content kind) |

Each app keeps its content vectors (`phrases.json`, `settings.json`, `picker/`, `quiet/`, `characters/<skin>.json` and
`-poses.json`, `plurals.json`), generated by its own tests through `DesklingTesting`'s generators (case lists) and
`ConformanceSuite`, replayed through `Deskling.Testing`. sunnysays' `cadence/` and `desktop.json` retire in its M9.

The fixture bank (`Sources/DesklingTesting/Fixtures/FixtureBank.swift`, ~32 phrases with placeholder texts such as
"Fixture line, general, flavor a"): `general-a1…a3` (weights 1, 3, 5), `general-b1`, `general-c1`, one line per daypart
and weekday tag, one per event tag plus a second `welcomeBack`, `tune-1`, `skin-raster-only` (skins `["fixture-raster"]`),
`it-only` (languages `["it"]`), `joke-1` (sequence, buttonPress + general), `joke-it` (sequence, `["it"]`). Flavors
`a`/`b`/`c`; skins `fixture-vector` (default) and `fixture-raster`; `CompanionDefaults.fixture`.

## `tools/`

`tools/{l10n,phrases,voice,character,synth,fonts,companion}.py`, `tools/{screenshots,store-screenshots,install-dev,conformance,verify,setup-linux}.sh`,
`tools/store-screenshots.py`, `tools/dev-icon.swift`, `tools/hooks/hook.py`, `tools/companion.schema.json`, `tools/tests/`
(each script's tests over a fixture app), `tools/skills/{localization,add-phrase,add-setting,conformance,voice,windows-port,release-mac,review-invariants}/SKILL.md`.
Every script reads `companion.json` at the app root (found by walking up from the current directory, or
`$COMPANION_ROOT`; `python3 deskling/tools/companion.py get mac.scheme`). Each app vendors this repo as a git submodule
at `deskling/` pinned to the release tag it also pins in SwiftPM and NuGet (the submodule is tools and skills only; the
code still comes from the package registries), keeps two-line wrappers so its documented commands keep working, and
symlinks the generic skills. `verify.sh` fails when the submodule commit, the SwiftPM pin and the NuGet pins disagree.

`companion.json` sections: `name`, `displayName`, `character`, `bundleId`, `devBundleId`, `domain`; `deskling.{tag,path}`;
`mac.{dir,project,projectSpec,scheme,app,core,coreModule,tests,devAppName,hotkeySignature,screenshotEnv,derivedData}`;
`windows.{dir,core,coreTests,app,sln,strings}`; `languages.{source,all}`; `l10n.{catalog,plist,landing,glossaryKeys,keepNames,rulesFile,pluralCategories,linuxPo}`;
`conformance.{dir,writeFlag,corePackagePath}`; `characters.{design,swiftOut,swiftModule,assets,windowsAssets,moods,paletteFile,mode}`;
`voice.{dir,repo,config,direction,voices,defaultVoice,keychainService,reviewTitle,siteCopyright,extraClips,required}`;
`sounds.{out,recipes}`; `fonts` (nullable); `screenshots.{out,shots,store}`; `landing` (nullable); `hooks.{guardedExtra,macUI,windowsUI}`.
The schema (`tools/companion.schema.json`) is the source of truth from sequence 5 on; `../pagliacci/companion.json` and
`../sunnysays/companion.json` (written in its M9) are the two instances.

## The 0.5.0 changelog, in advance

Breaking: `SettingsStore` (typed) in `DesklingCore` and `ISettingsStore` typed in `Deskling.Core` (mybackhurts'
Windows port adapts `AppSettings` when it bumps; it stays on 0.4.0 until then); `PurchaseStore` caches through a
`SettingsStore` (the `UserDefaults` convenience stays); `DesklingStore` depends on `DesklingCore`; `Character` →
`DrawableCharacter`, `Shape` → `PathShape`. Added: `DesklingCompanion` / `Deskling.Companion`, `DesklingCompanionUI`,
`Deskling.Testing`, the Shell speech and status item, `tools/`, iOS 17 in `platforms` (if sequence 6 keeps it).

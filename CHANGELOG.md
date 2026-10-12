# Changelog

All notable changes to Deskling. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project uses [Semantic Versioning](https://semver.org/). One tag (`vX.Y.Z`) versions the Swift
package and the NuGet packages together.

## Unreleased

## 0.7.1 - 2026-10-12

0.7.0 as it was meant to ship: its tag exists (SwiftPM sees it), but its release stopped at packing
`@deskling/landing`, before NuGet, npm and the GitHub release. Use 0.7.1 everywhere 0.7.0 is named; nothing else
changes.

- **Release**: the landing job creates the folder it packs into, and the Tools workflow packs the same way on every
  push, so a release can't be the first to run it.

## 0.7.0 - 2026-10-12

The companion shell: what sunnysays and Mister Pagliacci still repeated on top of 0.6 (the app model, its delegate
and menus, the Settings rows, the Support window, the window chrome, the screenshot run, the vector case lists, the
landing page's plumbing and the store tools) is Deskling's now. Every word and the look stay the apps'.

### Migration
- **One name collides.** An app that defines its own `View.windowPadding(bottom:)` (both companion apps do) deletes
  it: the package's is the same, and the two are ambiguous. Nothing else needs changing to build on 0.7.0. To shed the
  rest of an app's copies, follow sunnysays' M10 brief (`plans/milestones/10-companion-shell.md` there).

### Changed
- **`companion.json` no longer requires `character` or `conformance`**: an app without a phrase bank uses the
  tools too; the bank's tools say the section is missing.
- **Tools: a landing page kept in its own repository.** When `landing.dir` is a submodule, `verify.sh` lists the files
  changed inside it (since the commit the app records, so its commits not yet recorded count too) instead of only the
  submodule's path, so the landing checks run; and `setup-linux.sh` fetches it.

### Fixed
- **The desktop bubble's words stay inside it.** A new line used to animate its card and words from the last line's
  shape and place while the window took its new size at once, so for a moment (and in a running app sometimes until
  the bubble went away) the words hung out of the top of the card. `DesktopCompanion` now lays each line out at exactly
  the measured size from the window's top left, as a view of its own (no animation from the line before), and
  `FloatingWindow.setFrame(_:)` lays the content out at the new size before it shows.

### Added
- **The companion shell (`DesklingCompanionUI/Shell/`)**: what every companion app's model, delegate and menus repeated.
  `CompanionAppModel<Window, Settings>` is an `@Observable open class` with the 20 s tick, the channels, the status
  item, the desktop presence and bubble, the windows by role (`WindowRoles`), the hotkey, the store's start, mute with
  Undo, previews (`say(_:)` clears `bubblePhraseID`) and the status pill's words; the app subclasses it and supplies its
  views, character and words through open hooks (`windowContent`, `panelContent`, `menuCopy`, `desktopCharacter`,
  `desktopLayout`, `glyphCharacter`; `glyphMood`, `preferredVoice`, `willSay`, `showBubble`, `desktopBubbleWaits`,
  `voicingChanged`, `handleNotificationAction`, `settingsDidReset`, `willLaunch`, `stopSpeaking`) and
  `CompanionAppConfiguration` (`StatusCopy`, `DesktopCopy`, `CompanionSounds`, the palette and face, the supporter
  product). `observe(_:then:)` watches the app's own preferences. `CompanionNotifying` is the notification it drives
  (actions by raw id). `CompanionAppDelegate` (subclassed, hands over `companion`; `runScreenshots()`),
  `CompanionMenus` over `MenuCopy`, a public `ActionItem`, `DesktopHide`.
- **Settings rows and editors (`DesklingCompanionUI/Settings/`)**: `ActiveHoursEditor`, `QuietHoursEditor`,
  `ShortcutRecorder`, `LaunchAtLoginToggle`, `LanguageRow`, `NotificationPermissionNote`, `SettingsFooter`, each taking
  every label as a `Text` and its chip or button style as a generic parameter; `CompanionSettings` channel and flavor
  bindings and `mutedCount(in:)`.
- **`SupportWindow` (`DesklingCompanionUI/Support/`)**: the supporter purchase, the tips and the thank-yous over
  `PurchaseStore`, with every word in `SupportCopy`, the products in `SupportProducts` and the app's look in
  `SupportLook`; `SupportWindow.stage(...)` is its state machine.
- **The chrome (`Theme/`)**: `WindowInset` and `windowPadding(bottom:)`, `ActionBar`, `PanelRowStyle`, `Pointer`,
  `companionWindowRoot(palette:displayFont:tint:bodySize:)`, `DisplayWeight`; **`FontRegistration`** (DesklingShell)
  registers an app's bundled fonts once.
- **`DesktopCompanion.stand(character:layout:)`** (`CompanionStand`, what the desktop window draws) and
  **`bubblePreview(text:tail:countdown:layout:)`**, for screenshot sheets; **`DesktopLayout.shadowWidthRatio`** (0.42 by
  default).
- **`ScreenshotScript` (`DesklingCompanionUI/Screenshots/`)**: an app's screenshot run as a list of shots in light and
  dark over its model; `ScreenshotCapture.SampleEnvironment` (what the run's model is built on) and `SilentNotifier`.
- **Standard case lists (`DesklingTesting`)**: `QuietTimeline.standardCases(base:dst:)` (the 17 timelines every app
  pins, the DST one named and zoned by the app) and `PickerVector.standardCases(bank:skin:language:single:pair:
  leftEmpty:excluded:recent:)` (the 15 shared picker cases); **`CompanionVectors`** and
  **`ConformanceSuite.companion(_:root:regenerateHint:)`**, an app's whole suite from its content. sunnysays' and
  Pagliacci's committed vectors come out of them byte for byte.
- **`CompanionReplay` (Deskling.Testing)**: one call per vector kind over an app's vectors folder, so its replay test is
  theories and one assertion each.
- **`@deskling/landing` (`tools/landing`)**: a landing page's localized pages, legal pages, sitemap, CSP `_headers` and
  social cards as Vite plugins, from `companionLanding(options)`; the app keeps its data, placeholders, JSON-LD, brand
  and page. An npm package (the landing pages are their own repositories): `release.yml` stages it on npm at the tag's
  version through trusted publishing (no stored token), and an owner approves it with 2FA. sunnysays' and Pagliacci's
  landing builds come out the same.
- **The store tools (`tools/store`)**: `check.py` (each listing field against the stores' limits), `push.py` (App Store
  Connect: text, in-app purchases with their creation, screenshots, previews, review notes, `--status`) and
  `import.py` (a Partner Center import folder), over `companion.json`'s new `store` section; `verify.sh` runs
  `check.py` when a listing file changes. **`l10n.py`** checks `%#@name@` plural substitutions in every category.
- **`DeliveryPerformer.onVoicingChanged`** and **`SpeechSequencer.onVoicingChanged`**: audio started or stopped, false
  in the beat between a joke's parts, so the mouth moves only while words are heard (an app's own voicing wrapper can
  go).
- **`Phrase.localizedParts(bundle:)`, `localizedText`, `Phrase.localize(_:_:bundle:)`**: a phrase in the user's
  language from the app's String Catalog.
- **`FloatingWindow.idealSize(of:maxWidth:)`** measures content before it goes into the window.

## 0.6.5 - 2026-10-12

### Fixed
- **`LayoutCheck.TextProblems` (Deskling.Windows) measures the real text**: it flagged hundreds of false problems in
  French. A detached probe rounds each line to whole DIPs (so a line wrapped at its own width), copied letter spacing
  that a button's label reads back but never applies, and counted a label's `MinWidth` as text. Lines are now measured
  on the real TextBlock with its width limits lifted for the measure (restored after), and the word probe takes the
  spacing that reproduces the box's own width. It lays the tree out again when it's done, so run it after the
  screenshot, not before.

## 0.6.4 - 2026-10-12

### Added
- **`LayoutCheck` (DesklingShell)** finds SwiftUI content taller than its window: it measures the content's ideal
  height at the window's width and skips windows that scroll. `overflow(in:)` for a window, `overflow(of:in:)` for a
  view on its own, `contentRoom(forWindowOfSize:)` for the room below a `WindowManager` window's title bar.
- **`ScreenshotCapture` checks every shot**: a window that couldn't be drawn, a PNG that wasn't written, or content
  taller than its window is a problem (`problems`, `report(_:)` for the app's own), and `finish()` writes them to
  `problems.txt` next to the shots.
- **`tools/screenshots.sh` fails on `problems.txt`** (after copying the shots), and **`tools/check-layout.sh`** renders
  the windows in every language (or the ones named) into `<mac.dir>/build/layout-check/` and names the languages that
  don't fit. Apps add a two-line wrapper like `screenshots.sh`'s.
- **`LayoutCheck` (Deskling.Windows)** finds WinUI text that doesn't fit its box: `TextProblems(root)` lists every
  visible TextBlock cut off with an ellipsis, wider than its room, breaking a word across lines or cut off at the
  bottom. **`XamlCast.As<T>`** reads an untyped XAML object as a type under Native AOT, where a cast throws and
  `as`/`is` quietly fail.

## 0.6.3 - 2026-10-12

### Fixed
- **`voice.py publish` pushes to the voices repository's default branch** (`master`, `main`…), read from `origin`;
  it always pushed to `main`, which created a stray branch in repositories whose default is `master`.

## 0.6.2 - 2026-10-12

### Added
- **A Voiced filter on the voice review pages**, next to Waiting for a voice: only the lines with a current clip.

## 0.6.1 - 2026-10-12

### Added
- **`voice.reviewColors` in `companion.json`** gives the voice review pages and the friends' site the app's own colors
  (per scheme, a token → `#RRGGBB` map: `paper`, `card`, `ink`, `ink-soft`, `line`, `accent`, `accent-soft`, `on-accent`,
  `done`, `done-soft`, `flag`, `flag-soft`, `link`). Apps that set none keep today's colors.

## 0.6.0 - 2026-10-12

### Changed
- **`Frequency` has seven presets**, from every 15 minutes to every 3 hours: `every45Minutes`, `every90Minutes`,
  `every2Hours` and `every3Hours` join `chatty` (15 min), `sometimes` (30 min) and `rarely` (1 h), whose raw values
  stay. The cases are declared from the most frequent to the least (`allCases` and C#'s `Frequencies.All` list them in
  that order, for a picker), so an app's `switch` over `Frequency` needs the new cases.
- **The default frequency is every hour** (`rarely`, was `sometimes`): a character also speaks on its occasions, so
  twice an hour on top was a lot. Settings a user chose stay; the app's `settings.json` vector changes.

### Migration
- Give the four new cases a label wherever the app switches over `Frequency` (its frequency picker), then regenerate
  the app's vectors: `settings.json` gets the new default and presets, and any timeline written for the old default
  that should keep its cadence sets `frequency: "sometimes"`.

## 0.5.2 - 2026-10-12

### Fixed
- `Formats.WeekdayMonthDay` and `Formats.Hour` (Windows) threw in apps built with Native AOT (`PublishAot`, Debug runs
  included): the language list was a collection expression the projection couldn't marshal, so a window that formatted
  a date never opened. `Deskling.Windows.Tests` now builds with `PublishAot` too, so its tests catch this kind of interop.

## 0.5.1 - 2026-10-12

### Fixed
- `tools/dev-icon.swift` makes `AppIcon-Dev.appiconset` (starting from `AppIcon`'s `Contents.json`) when an app doesn't
  have one yet, instead of failing with "Can't write".
- `tools/voice.py site`: the README says "The recorded lines of <character>" without ", in <app>" when the app is
  named after its character, and the LICENSE names `voice.copyrightHolder` (new, optional; default `displayName`).

## 0.5.0 - 2026-10-11

The companion layer: everything under sunnysays' character, made generic so a second companion app can be built on
it. A typed settings store, `DesklingCompanion` / `Deskling.Companion` (phrases, cadence, quiet rules, settings,
coordinator, desktop presence, vector and raster character rigs, skins), `DesklingCompanionUI`, speech and a status
item in `DesklingShell`, `Deskling.Testing`, and `tools/`. Desktop only: macOS 14 and Windows, as before.

### Migration
- **Apps on 0.4 without the companion layer** (mybackhurts): only the settings store is breaking. Swift: drop an
  app-local `SettingsStore` protocol for `DesklingCore`'s; `PurchaseStore(…defaults:)` keeps working. .NET: move
  `ISettingsStore` callers to the typed getters and setters (below). Staying on 0.4.0 until then is fine.
- **Moving a companion app onto it** (sunnysays): import `DesklingCompanion` in place of the app's own copies. The raw
  values, settings keys and vector formats are unchanged, so committed vectors stay byte-identical. Renames: `Character`
  → `DrawableCharacter`, `Shape` → `PathShape`, `PaletteRole` → `PaintRole`, `Phrase.Kind.line(english:)` →
  `line(source:)`, a mood's `cheeks`/`zz` flags → `headExtras`/`overlays` (the rig JSON still accepts the flags), and
  in C# `PartContent.Vector` for Swift's `.shapes`. The app now injects what the package no longer names: its
  `PhraseBank`, `CompanionDefaults(skin:phraseFlavors:sayShortcut:)`, `SkinCatalog`, `CompanionPalette` and voices.
  App-only preferences go in a `CompanionSettings` subclass. Content vectors are generated with `DesklingTesting`'s
  generators and replayed with `Deskling.Testing`; the app's `cadence/` and `desktop.json` vectors retire (they are
  pinned here now). `tools/README.md` has the steps for the `deskling/` submodule and `companion.json`.

### Added
- Swift: `DesklingCompanion`, the pure core of a desk companion (Foundation and Observation only; builds and tests
  on Linux, now a job in `swift.yml`). `Phrases/`: `Tag`, `FlavorID` and `SkinID` (app-named string ids), `Phrase`
  (a line, a tune, or a `sequence` of two or more parts with keys `phrase.<id>.1`, `.2`…; `languages` scopes a
  phrase to the languages it exists in), `PhraseBank` (a container: the app injects its lines), `PhraseContext`
  (skin and language required) and `PhrasePicker` (drops phrases outside the context's language first).
  `Cadence/`: `Frequency`, `ChatterCadence`. `Rules/`: `Snapshot`, `Channels`, `Trigger`, `ChannelPlan`,
  `SkipReason`, `Outcome`, `Manners`, `QuietRules`, `Mood`, `CompanionState`. `Settings/`: `SettingsKeys` (the 23
  generic keys, same strings), `CompanionDefaults` (the package's numbers; skin, flavors and shortcut required),
  `CompanionSettings` (an `@Observable open class` with the 23 write-through properties and `manners`, which an app
  subclasses to add its own keys), `AppearanceSetting`, `DesktopSize`, `DesktopPosition`. `Coordination/`:
  `Delivery`, `CompanionCoordinator` (bank and language injected). `Desktop/`: `DesktopPresence`. `PhraseTiming`
  spaces the parts of a sequence. Moved from sunnysays with the same raw values and keys;
  `Phrase.Kind.line(english:)` is now `line(source:)`.
- Swift: `DesklingTesting` depends on `DesklingCompanion` and has `Fixtures/`: `PhraseBank.fixture`, `FixtureIDs`
  (flavors a/b/c, skins `fixture-vector` and `fixture-raster`) and `CompanionDefaults.fixture`.
- `tools/character.py`: compiles a skin's art and rig into the `CharacterArt` JSON and Swift source, from
  arguments (`--design --skin --out --swift-out --module --mode vector|raster --assets --windows-assets --check`)
  instead of sunnysays' constants, or, without `--design`, every skin from the app's `companion.json`. Rigs take `headExtras`/`overlays` (`cheeks: true`/`zz: true` stay as sugar) and
  require `bubble: {pivot, tuck}`. Raster skins draw parts from `layers/<part>@2x.png` (+ `@3x`), sized frame ×
  scale, and get an imageset per layer and Windows `scale-200`/`scale-300` PNGs. Tests in `tools/tests/`, run by
  the new `tools.yml`; fixture skins in `Tests/Fixtures/characters/`.
- Swift: `DesklingCompanion` `Figures/`: `Point`, `Size`, `Rect`, `PaintRole` (an app-named string; `ink` for
  glyphs), `PathCommand`, `PathShape`, `PartContent` (shapes, or an image asset placed in a frame), `Transform` (with
  `affine`), `Pose` (`headExtras`, `overlays`), `CharacterArt` (`bubblePoint`), `Rig`, `RigGroup`, `Blink`,
  `BubbleAnchor`, `MoodRig`, the `DrawableCharacter` protocol with `PlacedPart`, `RiggedCharacter` (`layers(for:)`;
  `shapes(for:)` skips image parts) and `CharacterArtJSON` (the compiler's exact bytes, and a decoder that refuses a
  rig naming missing parts). `Skins/`: `SkinEntry`, `SkinCatalog` (a locked, refunded or unknown skin draws the
  default). Moved from sunnysays: `Shape` is now `PathShape`, `PaletteRole` is now `PaintRole`, `Character` is now
  `DrawableCharacter`, and a mood's `cheeks`/`zz` flags are `headExtras`/`overlays` lists.
- Swift: `DesklingTesting` `FixtureArt.vector`/`.raster` (compiled by `scripts/fixture-art.sh`) and
  `SkinCatalog.fixture`.
- Swift: `DesklingTesting` `Conformance/`: `ConformanceRoot.find(markerFile:from:)`, `ConformanceSuite` (`checkUpToDate`,
  `replayCommitted`, `checkDeterministic`, `writeIfAsked(flag:)`, each returning its problems so the library never
  links XCTest), `JSONValue`, `SnapshotData`, and the companion generators parameterized by bank, defaults, character
  and cases: `PhrasesVector`, `PickerVector`, `CadenceVector`, `QuietTimeline`, `CharacterPoseVector`,
  `DesktopVector`, `SettingsVector.base(defaults:)` (a tree the app adds its keys to) and
  `BankAudit.floors(bank:languages:skins:minimumPerTag:)`. Apps generate their content vectors with these.
  `ConformanceSupport` moved into the folder; the Core `ConformanceTests` runs on `ConformanceSuite`.
- .NET: `Deskling.Companion` (net10.0, AOT-compatible), the C# mirror of `DesklingCompanion` file for file: the same
  types with `FlavorId`, `SkinId` and `PaintRole` as string record structs, `PhraseKind.Sequence`, `Phrase.Languages`,
  `PhraseTiming`, `CompanionDefaults` (a record; skin, flavors and shortcut required), `CompanionSettings`
  (`INotifyPropertyChanged`, non-sealed, `virtual ResetToDefaults()` and protected `SetProperty`/`SetOrRemove` for an
  app's own keys), `CompanionCoordinator` (bank, language and a `TimeZoneInfo` injected), `SkinCatalog`,
  `PartContent.Vector`/`.Image`, `IDrawableCharacter.Layers`. Loaders: `PhraseBank.Parse(Stream)` (phrases.json,
  strict) and `CharacterArt.Parse(Stream)` (`characters/<skin>.json`, refusing a rig that names missing parts).
- .NET: `Deskling.Testing`: `MockClock`, `MockIdle`, `MockBusy`, `ScriptedRandom`, `CountingRandom`;
  `ConformanceRoot.Find(markerFile)`/`Files(folder)`, `ConformanceSupport`, `VectorSupport`; the companion replays
  `PhrasesReplay`, `SettingsReplay` (generic keys only, so an app's own keys and `skins` sit beside them),
  `PickerReplay`, `CadenceReplay`, `QuietReplay`, `CharacterReplay` and `DesktopReplay`, each
  `Verify(Stream, file, …)` returning its problems; fixtures `FixtureIds`, `FixtureDefaults.Value`,
  `FixtureBank.Load(root)`, `FixtureArt.Load(root, skin)` and `FixtureArt.Catalog(root)`. `Deskling.Core.Tests` uses
  it in place of its own copies. `release.yml` packs both packages beside `Deskling.Core`; `dotnet.yml` tests the
  Core and Companion vectors on Linux and Windows.
- Swift: `DesklingShell` `StatusItemController` (a status item with a popover panel, a right-click menu and a glyph the
  app draws through `image(animating:frame:)`; 8 fps frames, a wave, Reduce Motion) and `Speech/`: `Speaking` and
  `Speaker` (system voice or a recorded clip; `appLanguage(bundle:)`), `SpeechPart` and `SpeechSequencer` (parts with
  a beat between; `isActive` through the beat, `isVoicing`, `stop()` cancels the pending part), `VoiceID` and
  `VoiceLibrary` (manifest format 1; `pendingClipIDs`, `fallbackToSystemVoice`), `ChimePlaying` and `ChimePlayer(bundle:)`.
- Swift: `DesklingCompanionUI` (macOS; depends on Companion, Core, System, Shell and Store): `SignalHub`
  (`lowBatteryPercent` injected), `GlyphRenderer` and `StatusItemController.show(glyph:mood:talking:accessibilityDescription:)`,
  `CharacterView` (vector parts in the palette's paints, image parts from `artBundle` under the rig's transform),
  `DesktopLayout`, `DesktopCompanion`, `DeliveryPerformer` (jokes in beats, one two-line banner at the punchline,
  `unavailableIDs(voice:)`), `VoiceLibrary.pendingIDs(in:voice:text:)`, `CompanionNotifier`, `NotificationPermission`,
  `Theme/`: `CompanionPalette` (`EnvironmentValues.companionPalette`, `.companionDisplayFont`), `Space`, `SpeechBubble`,
  `BubbleShape`, `BubbleCountdown`, `StatusPill`; and `ScreenshotCapture` (`DESKLING_SCREENSHOT_MODE`/`_DIR`) with
  `FixedClock`, `ConstantIdle`, `SilentSpeaker`, `SilentChime` and `FixedPriceBackend`. During a capture run
  `CharacterView` holds the still pose, so the same build shoots the same pixels every time.
- `tools/` for companion apps (vendored as a `deskling/` submodule pinned to the release): `companion.py` and
  `companion.schema.json` (each app's `companion.json`: names, paths, languages, tool settings; `env`, `pins`),
  `l10n.py` and `phrases.py` (language-scoped phrases; a joke's parts as `phrase.<id>.n` keys), `voice.py` and
  `voice-review.html` (a clip per joke part, any voice names), `synth.py`, `fonts.py`, `screenshots.sh`,
  `store-screenshots.sh`/`.py`, `install-dev.sh`, `dev-icon.swift`, `conformance.sh`, `verify.sh` (with a check that
  the submodule, SwiftPM and NuGet pins agree), `setup-linux.sh`, `hooks/hook.py` and eight skills, all moved from
  sunnysays and parameterized; tests over a fixture app in `tools/tests/` (`run.py`).
- Vectors: `conformance/companion/` over the fixtures (`phrases.json`, `settings.json`, `picker/`, `cadence/`,
  `quiet/`, `desktop.json` with `partDelays`, `characters/` for both fixture skins), generated by
  `Tests/DesklingCompanionTests/Conformance` (`scripts/conformance.sh` regenerates both folders).

### Changed (breaking)
- Swift: `SettingsStore` (typed: `bool`/`integer`/`double`/`string`/`strings`/`integers(forKey:)`, the matching
  `set(…:forKey:)` and `remove(forKey:)`), `InMemorySettingsStore` and `UserDefaultsSettingsStore` are in
  `DesklingCore`. `DesklingStore` now depends on `DesklingCore`, and `PurchaseStore` caches owned ids through a
  `SettingsStore`: `PurchaseStore(catalog:backend:settings:cacheKey:logger:)`, `UserDefaultsSettingsStore()` by
  default. `PurchaseStore(catalog:backend:defaults:…)` stays as a convenience and reads the same cache key. Migration:
  none unless you passed `defaults:` without a value; apps that had their own `SettingsStore` protocol can drop it.
- .NET: `ISettingsStore` is typed (`GetBool`/`GetInteger`/`GetDouble`/`GetString`/`GetStrings`/`GetIntegers`, the
  matching setters, `Remove`); `object? Get(key)` and `Set(key, object)` are gone, and so is `DateTimeOffset`.
  `LocalSettingsStore` implements it (scalars native, lists in the same JSON composite as before, `double` added),
  so stored values stay readable. Migration: `Get(key) as string[]` → `GetStrings(key)`, `Get(key) as int?` →
  `GetInteger(key)`, `Set(key, value)` → the typed setter; a stored `DateTimeOffset` becomes a `double` of Unix
  seconds (`SetDouble(key, date.ToUnixTimeMilliseconds() / 1000.0)`).

## 0.4.0 - 2026-10-10

### Added
- Swift (macOS): `FloatingWindow` in `DesklingShell`, a small borderless window that floats above other apps' windows
  on every Space but full-screen ones, for a desktop pet or a sticky note. It never becomes key and never activates
  the app (no Dock icon for an `LSUIElement` app; it isn't one of `WindowManager`'s windows). It stands on an anchor
  (the middle of its bottom edge) so a size change grows it upward in place, stays wholly on a screen when displays
  change, reports clicks, drags, a right or Control click menu and trackpad pinch steps, fades in and out, and can
  carry a second window beside it (`attach(to:)`, for a speech bubble). No Windows twin yet; it comes with the first
  app that needs one.
- Swift (macOS): `FloatingPlacement`, the pure rectangle math behind it: anchor ↔ frame, the default bottom-right
  spot, clamping onto the nearest screen, the side of a window with more room, and a platform-neutral top-left origin
  for storing a position.

## 0.3.1 - 2026-10-10

### Added
- .NET: `PluralRules` knows Czech, Danish, Dutch, Japanese, Korean, Norwegian (Bokmål), Polish, Swedish, Turkish and
  Chinese. Polish counts take one/few/many, Czech one/few/other, and Japanese, Korean and Chinese always take
  "other". Before, these languages fell back to the English rule.

## 0.3.0 - 2026-10-09

### Added
- .NET: the purchase store, mirroring `DesklingStore`. `Deskling.Core.Store` has `ProductCatalog`, `PurchaseStore`
  (owned set cached in an `ISettingsStore`, load state, prices, purchasing/restoring/pending, tip thanks, errors,
  one `Changed` event), `IStoreBackend` and `MockStoreBackend`. The mock ships in `Deskling.Core` (the Swift one is in
  `DesklingTesting`) because apps use it in Debug screenshot and preview modes as well as tests.
- .NET: `Deskling.Windows.Shell.StoreContextBackend`, the Microsoft Store backend over `Windows.Services.Store`.
  Matches add-ons by Partner Center Product ID (`InAppOfferToken`), parents the purchase dialog to a window you name,
  reports consumables fulfilled right after purchase so they can be bought again, and turns `OfflineLicensesChanged`
  into a transaction update on the UI thread. Needs no manifest capability.

## 0.2.1 - 2026-10-08

### Fixed
- `AppNotifier<TAction>.Dispose()` (.NET) unregisters only when `Register()` did, and never throws. Disposing a
  notifier that was never registered (an app's screenshot or test mode, say) threw `COMException` 0x80070490
  "Not Registered for App Notifications!" whenever Windows had no registration left for the package.
  `Register()` is idempotent and leaves nothing subscribed when Windows refuses the registration.

## 0.2.0 - 2026-10-08

### Changed
- The Swift package builds in the Swift 6 language mode (`swift-tools-version: 6.0`), with strict concurrency checking.
  Apps in either language mode can use it; Swift 6 apps no longer need `@preconcurrency import` for its types.
- `NotificationActionSpec`, `NotificationCategorySpec`, `SystemClock`, `NeverBusy`, `SystemRandom`,
  `SystemIdleTimeProvider`, `SystemBusyStateProvider` and the `DesklingTesting` helpers `ScriptedRandom`,
  `MockStoreBackend.Failure` and `ConformanceSupport.VectorError` are `Sendable`.
- `NotificationPoster` answers `willPresent` through the async delegate method; behavior is unchanged.
- **Breaking:** `ScreenLockWatcher` is `@MainActor` (its callback always ran on the main queue). Create and read it
  from the main actor; `lockedName` and `unlockedName` stay usable anywhere.

## 0.1.1 - 2026-10-07

### Changed
- The release workflow publishes to NuGet.org through Trusted Publishing (OIDC) instead of a stored API key, and
  creates the tag itself when run by hand. No code changes; 0.1.0's packages were never pushed to NuGet.org.

## 0.1.0 - 2026-10-07

The first release: the generic parts of mybackhurts, extracted and generalized, for mybackhurts and sunnysays.

### Added
- `DesklingCore`: `ReminderScheduler`, `SchedulerConfig`, `Clock`, `IdleTimeProvider`, `BusyStateProvider` and the
  `BusyReason`s, moved from mybackhurts with the same names and behavior (its 37 scheduler vectors replay byte for
  byte), plus one addition: `SchedulerConfig.intervalJitter`, a whole-second random offset on every interval drawn
  from an injected `RandomSource`; zero by default.
- `DesklingCore`: `RandomSource`, `SystemRandom` and `SplitMix64` (specified bit for bit for the .NET port),
  `KeyShortcut`, `LocalizedKey`.
- `DesklingTesting`: `MockClock`, `MockIdle`, `MockBusy`, `ScriptedRandom`, `CountingRandom`, `ConformanceSupport`.
- `DesklingSystem`: `SystemIdleTimeProvider` and `SystemBusyStateProvider` from mybackhurts, the latter with
  `init(cameraCheck:)` (`.avFoundation`, `.coreMediaIO` for sandboxed apps without the camera entitlement, `.off`);
  new permission-free signals `PowerStateProvider` / `SystemPowerSource` (battery, charging, level, change callback),
  `AudioOutputStateProvider` / `SystemAudioOutput` (muted, route), `AppearanceWatcher` and `ScreenLockWatcher`.
- `DesklingShell`: `WindowManager<ID: WindowID>` (activation-policy dance, exclusive windows, placement, "Bigger"),
  `GlobalHotKey` with several ids per signature, `ShortcutCapture`, `NotificationPoster` with injected categories and
  titles, `LoginItem`, `LaunchContext`, and the drawing helpers `SVGPath` (now also relative commands, `H`/`V`, `T` and
  arcs), `Keyframes`, `Wobble`, `Polyline.circle`, `CGAffineTransform.rotation(degrees:about:)`.
- `DesklingStore`: `ProductCatalog`, `PurchaseStore` (`owned`, `isOwned`, `error: PurchaseError?`, injected cache key
  and logger), `StoreBackend`, `StoreKitBackend`.
- `DesklingTesting`: `MockStoreBackend`.
- `Deskling.Core` (.NET): the file-for-file mirror of `DesklingCore` plus `Localization/PluralRules`, `ReswName` and
  `Settings/ISettingsStore` from mybackhurts; `Deskling.Core.Tests` replays every scheduler vector.
- `Deskling.Windows` (.NET): `SystemIdleTimeProvider`, `SystemBusyStateProvider`, `TrayIconHost`, `TrayMenuItem`,
  `GlobalHotKey` (Carbon masks mapped onto `MOD_*`), `StartupService`, `LocalSettingsStore`, `WavPlayer`,
  `AppNotifier<TAction>`, `SingleInstance`, `Loc` / `LocExtension` / `Formats` with injected `FormatKeys`, a shared
  `MessageWindow`, and the new signals `PowerSource`, `OutputMute`, `AppearanceWatcher`, `SessionLockWatcher`.
- Repository: Swift package layout (`DesklingCore`, `DesklingSystem`, `DesklingShell`, `DesklingStore`,
  `DesklingTesting`), .NET solution (`Deskling.Core`, `Deskling.Windows`, `Deskling.Core.Tests`), conformance
  vectors, CI (`swift.yml` on macOS, `dotnet.yml` on Ubuntu and Windows, `regenerate-vectors.yml`) and the
  release workflow.

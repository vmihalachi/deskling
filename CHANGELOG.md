# Changelog

All notable changes to Deskling. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project uses [Semantic Versioning](https://semver.org/). One tag (`vX.Y.Z`) versions the Swift
package and the NuGet packages together.

## Unreleased

The 0.5.0 companion layer is being built in six sequences (`plans/0.5/`): a typed settings store, `DesklingCompanion` /
`Deskling.Companion` (phrases, cadence, rules, settings, coordinator, desktop presence, vector and raster character rigs,
skins), `DesklingCompanionUI`, Shell speech and status item, `Deskling.Testing`, `tools/`. Breaking changes land here as
each sequence commits; the migration list is in `plans/0.5/README.md`.

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
  instead of sunnysays' constants. Rigs take `headExtras`/`overlays` (`cheeks: true`/`zz: true` stay as sugar) and
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

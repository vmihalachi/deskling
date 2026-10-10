# Changelog

All notable changes to Deskling. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project uses [Semantic Versioning](https://semver.org/). One tag (`vX.Y.Z`) versions the Swift
package and the NuGet packages together.

## Unreleased

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

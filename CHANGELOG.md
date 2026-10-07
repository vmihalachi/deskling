# Changelog

All notable changes to Deskling. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project uses [Semantic Versioning](https://semver.org/). One tag (`vX.Y.Z`) versions the Swift
package and the NuGet packages together.

## Unreleased

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

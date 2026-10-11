# 1: Typed settings store, the Companion pure core

> Sequence 1 of `README.md` (read its *Principles*, *Products*, *The Companion API* and *Language-scoped phrases*
> sections). Nothing to wait for. One session; commits in the order below; `scripts/verify.sh --all` green after each.

## Read only these

- This folder's `README.md`; `AGENTS.md` (conventions); `Package.swift`; `Sources/DesklingCore/Scheduling/ReminderScheduler.swift`
  (doc-comment style); `Sources/DesklingTesting/Mocks.swift`; `Sources/DesklingStore/PurchaseStore.swift`;
  `dotnet/Deskling.Core/Settings/ISettingsStore.cs`, `dotnet/Deskling.Core/Store/PurchaseStore.cs`, `dotnet/Deskling.Windows/Shell/LocalSettingsStore.cs`.
- From sunnysays (`../sunnysays`), the sources that move: `macos/SunnySaysCore/Sources/SunnySaysCore/{Phrases,Cadence,Rules,Settings,Coordination,Desktop}/*.swift`,
  `macos/SunnySays/App/UserDefaultsSettingsStore.swift`, and their tests under `macos/SunnySaysCore/Tests/SunnySaysCoreTests/{Phrases,Cadence,Rules,Settings,Coordination,Desktop}/`.
  `windows/SunnySays.Core/Settings/SettingsStore.cs` for the typed C# interface. Never `conformance/*` there except one `picker/*.json` as a sample.

## Steps

| Step | Commit | What |
|---|---|---|
| 1.0 | (none) | **Spike, 30 min:** in a scratch test target, `@Observable open class Base` in `DesklingCompanion` subclassed by `@Observable final class Sub` in the test target, Swift 6 mode; a tracked property on each side observed through `withObservationTracking`. Also `import Observation` on the Linux toolchain `swift.yml` uses. Pass → the class design below; fail → the fallback (a `CompanionSettingsProviding` protocol with the 23 properties + `manners`, consumed by `ChatterCadence` and `CompanionCoordinator`, plus `SettingsCodec` static read/write helpers; each app keeps its own `@Observable final class`). Record the outcome in this table. **Outcome (2026-10-11): pass.** Xcode's Swift 6.4 (macOS) and `swift:6.1` (Linux, Docker): a property of the `open` base and one of the subclass each notify through `withObservationTracking`, a computed property over a base property too, `didSet` write-through runs on both sides, `override init` / `override func` chain to `super`, and `import Observation` builds on Linux. One thing to know: Swift 6.2+ skips the notification when an `Equatable` property is set to the value it already has. → the class design. |
| 1.1 | `Core: typed SettingsStore; Store caches through it` | Swift: `Sources/DesklingCore/Settings/SettingsStore.swift` (sunnysays' protocol and `InMemorySettingsStore` verbatim, public) and `UserDefaultsSettingsStore.swift`; `DesklingStore` gains a `DesklingCore` dependency in `Package.swift`; `PurchaseStore.init(catalog:backend:settings: any SettingsStore = UserDefaultsSettingsStore(), cacheKey:logger:)` storing `owned` as strings; the old `defaults:` init stays as a convenience. .NET: `ISettingsStore` becomes the typed interface from `windows/SunnySays.Core/Settings/SettingsStore.cs` plus `InMemorySettingsStore`; `PurchaseStore.cs` uses `GetStrings/SetStrings`; `LocalSettingsStore.cs` implements it (scalars native, arrays as the existing JSON composite, `double` added). Tests: `PurchaseStoreTests` on both sides over the in-memory store; a `LocalSettingsStore` round-trip test is Windows-only (`dotnet.yml`). CHANGELOG *Unreleased*: the two breaking lines with the mybackhurts migration (`Get(key) as string[]` → `GetStrings(key)`). |
| 1.2 | `Companion: Phrases, Cadence, Rules` | New product `DesklingCompanion` (Foundation + Observation, depends on Core) in `Package.swift`, with `DesklingCompanionTests`. `Phrases/Tag.swift`, `Phrase.swift` (`FlavorID`, `SkinID`, `Kind.sequence`, `languages`, `partCount`, `catalogKeys`, `clipIDs`, `fits(language:)`, precondition on ≥ 2 parts), `PhraseBank.swift` (container; `phrases(in language:)`), `PhraseContext.swift` (`language`, no `skin` default), `PhrasePicker.swift` (language filter first), `PhraseTiming.swift`; `Cadence/`, `Rules/` verbatim (doc comments without leaf and pot). `Sources/DesklingTesting/Fixtures/FixtureBank.swift` (the bank in the README) and `FixtureIDs` (flavors a/b/c, skins). Tests copied from sunnysays and re-pointed at fixture ids: `PhraseBankTests` (minus content floors), `PhrasePickerTests` (+ language scoping, sequence candidate), `ChatterCadenceTests`, `QuietRulesTests`, `CompanionStateTests`; new `PhraseTests` for keys and clip ids. Add the ubuntu job to `swift.yml` (`swift build --product DesklingCompanion && swift test --filter DesklingCompanionTests`). |
| 1.3 | `Companion: Settings, Coordination, Desktop` | `Settings/SettingsKeys.swift` (23 constants), `CompanionDefaults.swift`, `CompanionSettings.swift` (per 1.0), `SettingChoices.swift`; `Coordination/Delivery.swift`, `CompanionCoordinator.swift` (`language`, bank injected, no `.standard`); `Desktop/DesktopPresence.swift`; `Fixtures/FixtureDefaults.swift` (`CompanionDefaults.fixture`). Tests: `CompanionSettingsTests` (from `AppSettingsTests` minus `sunnyVoice`), `CompanionCoordinatorTests`, `DesktopPresenceTests`. Doc comment on `CompanionSettings`: how an app subclasses and adds keys. |

## Finish

`scripts/verify.sh --all` ok on a Mac (swift-format, build, tests, vectors still current: nothing here changes
`scheduler/`); `swift.yml` green including the ubuntu job; `cd dotnet && dotnet test` green. CHANGELOG *Unreleased*
lists 1.1's breaking changes and "Added: `DesklingCompanion`". Hand to sequence 2 the outcome of the spike.

# Conventions, in full

The complete rules (moved from `AGENTS.md`, which keeps the short form).

- **Public API is deliberate.** Every public type gets a doc comment saying what it does, what it needs and what it
  never does (for example "never prompts for a permission"). Keep type names that mybackhurts already uses
  (`ReminderScheduler`, `SchedulerConfig`, `Clock`, `BusyStateProvider`…) so adopting the package is an import swap.
- **Swift 6 language mode** (strict concurrency; public value types are `Sendable`), macOS 14+ (desktop only: no iOS), no dependencies. `DesklingCore` stays Foundation-only and `DesklingCompanion`
  Foundation and Observation only (both build on Linux); anything that imports AppKit, SwiftUI, CoreAudio, AVFoundation,
  IOKit or Carbon goes in `DesklingSystem`, `DesklingShell` or `DesklingCompanionUI`.
- **Package code never names an app.** No app's skins, phrase bank, product ids, palette or voices, and no app name in
  a doc comment or a fixture: apps inject their bank, defaults, skins, palette and voices. The same goes for `tools/`,
  which reads everything app-specific from the app's `companion.json`.
- **A view or model in the package takes its words from the app.** `DesklingCompanionUI`'s shell, Settings rows,
  Support window and menus get every label as a `Text`, a string or a copy struct (`StatusCopy`, `MenuCopy`,
  `SupportCopy`…), never a default; the app's String Catalog and its `l10n.py` workflow are untouched. The look (the
  app's buttons, chips, titles, cards) comes in as styles or views; only chrome that is the same in every app lives
  here.
- **No permission prompts, no network.** System signals read device and window state. If an API would prompt the user
  or touch the network, it doesn't belong here.
- **Behavior changes are additive and vector-tested.** New scheduler behavior gets a config field whose default keeps
  every existing vector byte-identical, a unit test, a vector case, and the C# port in the same change
  (`cd dotnet && dotnet test` tells you what to port). New fields in a companion vector are optional and omitted at
  their default, so the apps' committed vectors stay byte-identical; the C# loaders read an absent field as the default.
- **`conformance/companion` pins mechanism, over fixtures.** The picker, cadence, quiet rules, coordinator, desktop
  presence and rig are pinned here over `DesklingTesting`'s fixture bank, defaults and skins; each app pins its own
  content (bank, defaults, picks, poses) with the same generators and replays.
- **Rasters never shear.** A character's transforms are rotation, translation and uniform scale only, so an image part
  draws under the same `affine` as a vector one. Pose vectors pin each layer's name, the six affine numbers and the
  content kind, never pixels; the menu bar glyph stays a vector line drawing for every skin.
- **C# mirrors Swift** folder for folder and file for file (`Sources/DesklingCore/Scheduling/ReminderScheduler.swift` ↔
  `dotnet/Deskling.Core/Scheduling/ReminderScheduler.cs`). Core folders: `Scheduling/`, `Random/`, `Localization/`, `Settings/`; Companion folders:
  `Phrases/`, `Cadence/`, `Rules/`, `Settings/`, `Coordination/`, `Desktop/`, `Figures/`, `Skins/`. `DesklingSystem`
  and `DesklingShell` map onto `Deskling.Windows/System/` and `Deskling.Windows/Shell/`. The exception is the companion shell
  (`Deskling.Companion.Shell`, `Deskling.Windows.Companion`): the Swift files mix logic and AppKit, so the C# side splits
  them and mirrors by role, with the same type and member names; `plans/0.8/README.md` maps every Swift file. Tests mirror the same folders, with
  `Conformance/` in each Core and Companion test project.
  No reflection-based JSON (source-generated contexts only): the apps publish with Native AOT and warnings are errors.
- **Strings:** the package ships none. Titles for notification actions and the like are passed in already localized.
- **Versioning:** SemVer, one `vX.Y.Z` tag for both sides; `CHANGELOG.md` gets a line per user-visible change under
  *Unreleased*. Bump the version in the same change as the tag, in all four places: `Deskling.version`
  (`Sources/DesklingCore/Deskling.swift`), `DesklingInfo.Version` (`dotnet/Deskling.Core/DesklingInfo.cs`),
  the `DesklingVersion` default in `dotnet/Directory.Build.props`, and `site/index.html` (the `vX.Y.Z` label in the
  header and both `exact:` install snippets). Move *Unreleased* to the new section. `scripts/verify.sh` fails when
  these disagree, so run it before committing.
- Formatting is enforced in CI: `.swift-format` (4 spaces, 140 columns) and `dotnet/.editorconfig`.

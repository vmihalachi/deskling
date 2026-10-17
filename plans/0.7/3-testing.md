# 3: Standard cases, `CompanionVectors`, `CompanionReplay`

> Sequence 3 of `README.md` (read *Testing*). Needs nothing. One session; Swift on macOS or Linux, the C# anywhere
> with the .NET 10 SDK.

## Read only these

- `README.md` here; `AGENTS.md`; `Sources/LachingTesting/Conformance/{ConformanceSuite,QuietTimeline,PickerVector,PhrasesVector,SettingsVector,CharacterPoseVector}.swift`;
  `Tests/LachingCompanionTests/Conformance/{CompanionCases,ConformanceTests}.swift`; `dotnet/Laching.Testing/Conformance/{ConformanceRoot,VectorSupport}.cs`
  and `Conformance/Companion/*.cs`; `dotnet/Laching.Companion.Tests/Conformance/` (one file as a sample).
- From sunnysays: `macos/SunnySaysCore/Tests/SunnySaysCoreTests/Conformance/{QuietVectors,PickerVectors,ConformanceTests,SunnyVectors}.swift`,
  `windows/SunnySays.Core.Tests/Conformance/SunnyVectorsTests.cs`.
- From Pagliacci: `macos/PagliacciCore/Tests/PagliacciCoreTests/Conformance/{QuietVectors,PickerVectors}.swift` (the
  differences: the DST city, the flavor names, the Italian cases).

## Steps

| Step | Commit | What |
|---|---|---|
| 3.1 | `Testing: standard quiet and picker cases` | `QuietTimeline.standardCases(base:dst:)`: sunnysays' 17 cases verbatim (names, starts, seeds, steps), the DST one named and zoned by the caller. `PickerVector.standardCases(bank:skin:language:single:pair:)`: the shared cases, the two flavor cases named by the caller. `CompanionCases.swift` uses both (its extra cases appended) and `scripts/conformance.sh` leaves `conformance/companion` **byte-identical** (`git status --short conformance/` empty). `StandardCasesTests`: the lists have the expected names in order. |
| 3.2 | `Testing: CompanionVectors and ConformanceSuite.companion` | `Conformance/CompanionVectors.swift` (the protocol) and `ConformanceSuite.companion(_:root:regenerateHint:)` building the same `generate` and `replayers` as sunnysays' `ConformanceTests.swift`; `CompanionCases` conforms and Laching's own `ConformanceTests` uses the factory (still byte-identical). |
| 3.3 | `.NET: CompanionReplay` | `dotnet/Laching.Testing/Conformance/Companion/CompanionReplay.cs`: `Phrases`, `Settings`, `Picker`, `Quiet`, `Character` over a root (each opening the file and returning the problems), `Files(folder)`; `Laching.Companion.Tests` uses it. `cd dotnet && dotnet test Laching.Companion.Tests` green; `dotnet format whitespace dotnet --folder --verify-no-changes`. |
| 3.4 | `docs: Testing` | `conformance/README.md` (a paragraph: an app's case lists are the standard ones plus its own), `AGENTS.md` (the `LachingTesting` row), CHANGELOG *Unreleased*. |

## Finish

`swift test` (`LachingCompanionTests` on macOS and the ubuntu job), `dotnet test`, `scripts/conformance.sh` leaves
the tree clean, `scripts/verify.sh --all` ok. Hand to 5: confirmation that the standard lists reproduce sunnysays'
and Pagliacci's committed `picker/` and `quiet/` byte for byte (run their generators against `path: ../laching` in a
scratch worktree, or leave it to 5.4).

## Outcome (2026-10-12)

Done in one session. **Confirmed byte for byte**: a scratch package building sunnysays' and Pagliacci's cores against
this checkout regenerated their `picker/` (19 and 21 files) and `quiet/` (17 each) from the standard lists plus each
app's own cases, identical to what they have committed. `conformance/companion` is unchanged too.

- `PickerVector.standardCases` takes more than the README said, because the three lists differ in counts:
  `leftEmpty` (the flavor with no battery line: sunnysays `.wonder`, Pagliacci `.silly`), `excluded` (Monday morning's
  first N candidates: sunnysays 10, Pagliacci 3, the fixture 4) and `recent` (default `PhrasePicker.recentWindow`; the
  fixture 4). Its 15 cases are the dayparts but Monday, `recent-skipped`, five events, the two flavor cases,
  `event-flavor-left-empty-falls-back`, `excluded-ids` and the two fallbacks. Each app keeps its own Monday-morning,
  button and `recent-older-than-window-count-again` cases (sunnysays: `monday-morning-chime`, `-no-chime`,
  `event-button-press`, `recent-older-…` with `recentWindow + 4`; Pagliacci: `monday-morning`, `-italian`, the three
  `event-button-press-*`, `supporter-skin`).
- `ConformanceSuite.companion` owns `picker/` and `quiet/` only (`characters/` also holds the compiled art). Laching's
  own suite is that plus `cadence/`, `characters/` and `desktop.json`.
- `CompanionReplay` also has `Desktop(root)` and `Cadence(path)` (Laching's own test uses them); problem messages name
  the file by its name only.

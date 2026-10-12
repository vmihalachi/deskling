# 3: Standard cases, `CompanionVectors`, `CompanionReplay`

> Sequence 3 of `README.md` (read *Testing*). Needs nothing. One session; Swift on macOS or Linux, the C# anywhere
> with the .NET 10 SDK.

## Read only these

- `README.md` here; `AGENTS.md`; `Sources/DesklingTesting/Conformance/{ConformanceSuite,QuietTimeline,PickerVector,PhrasesVector,SettingsVector,CharacterPoseVector}.swift`;
  `Tests/DesklingCompanionTests/Conformance/{CompanionCases,ConformanceTests}.swift`; `dotnet/Deskling.Testing/Conformance/{ConformanceRoot,VectorSupport}.cs`
  and `Conformance/Companion/*.cs`; `dotnet/Deskling.Companion.Tests/Conformance/` (one file as a sample).
- From sunnysays: `macos/SunnySaysCore/Tests/SunnySaysCoreTests/Conformance/{QuietVectors,PickerVectors,ConformanceTests,SunnyVectors}.swift`,
  `windows/SunnySays.Core.Tests/Conformance/SunnyVectorsTests.cs`.
- From Pagliacci: `macos/PagliacciCore/Tests/PagliacciCoreTests/Conformance/{QuietVectors,PickerVectors}.swift` (the
  differences: the DST city, the flavor names, the Italian cases).

## Steps

| Step | Commit | What |
|---|---|---|
| 3.1 | `Testing: standard quiet and picker cases` | `QuietTimeline.standardCases(base:dst:)`: sunnysays' 17 cases verbatim (names, starts, seeds, steps), the DST one named and zoned by the caller. `PickerVector.standardCases(bank:skin:language:single:pair:)`: the shared cases, the two flavor cases named by the caller. `CompanionCases.swift` uses both (its extra cases appended) and `scripts/conformance.sh` leaves `conformance/companion` **byte-identical** (`git status --short conformance/` empty). `StandardCasesTests`: the lists have the expected names in order. |
| 3.2 | `Testing: CompanionVectors and ConformanceSuite.companion` | `Conformance/CompanionVectors.swift` (the protocol) and `ConformanceSuite.companion(_:root:regenerateHint:)` building the same `generate` and `replayers` as sunnysays' `ConformanceTests.swift`; `CompanionCases` conforms and Deskling's own `ConformanceTests` uses the factory (still byte-identical). |
| 3.3 | `.NET: CompanionReplay` | `dotnet/Deskling.Testing/Conformance/Companion/CompanionReplay.cs`: `Phrases`, `Settings`, `Picker`, `Quiet`, `Character` over a root (each opening the file and returning the problems), `Files(folder)`; `Deskling.Companion.Tests` uses it. `cd dotnet && dotnet test Deskling.Companion.Tests` green; `dotnet format whitespace dotnet --folder --verify-no-changes`. |
| 3.4 | `docs: Testing` | `conformance/README.md` (a paragraph: an app's case lists are the standard ones plus its own), `AGENTS.md` (the `DesklingTesting` row), CHANGELOG *Unreleased*. |

## Finish

`swift test` (`DesklingCompanionTests` on macOS and the ubuntu job), `dotnet test`, `scripts/conformance.sh` leaves
the tree clean, `scripts/verify.sh --all` ok. Hand to 5: confirmation that the standard lists reproduce sunnysays'
and Pagliacci's committed `picker/` and `quiet/` byte for byte (run their generators against `path: ../deskling` in a
scratch worktree, or leave it to 5.4).

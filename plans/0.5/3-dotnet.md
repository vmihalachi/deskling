# 3: .NET: Laching.Companion, Laching.Testing, Companion.Tests

> Sequence 3 of `README.md` (read *Products*, *The Companion API*, *Vectors*). Needs 2 (the vectors); can run while 4 runs.
> One session. Use the `port-to-csharp` skill.
>
> From sequence 2 (done 2026-10-11): `conformance/companion/` exists (formats in `conformance/README.md`), generated
> by `Tests/LachingCompanionTests/Conformance` over `PhraseBank.fixture`, `CompanionDefaults.fixture` and
> `FixtureArt.vector`/`.raster`; the C# replay needs the same fixtures (port `Sources/LachingTesting/Fixtures/`,
> loading the art from `companion/characters/fixture-*.json` with a `CharacterArtJSON` decoder). Shipped names that
> differ from the README's sketch: `PaintRole` is string-backed; `SkinEntry(art:unlockedBy:)` takes its id from the
> art; `PartContent.kind` is "shapes"/"image"; generators are `PhrasesVector`, `PickerVector`, `CadenceVector`,
> `QuietTimeline`, `CharacterPoseVector`, `DesktopVector` (`partDelays` optional), `SettingsVector.base(defaults:)`
> (a `JSONValue` tree the app adds `skins` and its own keys to) and `BankAudit`. `ConformanceSuite` checks return
> problem lists rather than asserting. `tools/character.py` takes `--design --skin --out --swift-out --module --mode
> --assets --windows-assets --palette --moods --check`; `scripts/fixture-art.sh` runs it for the fixtures.

## Read only these

- `README.md` here; `AGENTS.md`; `.agents/skills/port-to-csharp/SKILL.md`; `dotnet/Laching.sln`, `dotnet/Directory.Build.props`,
  `dotnet/Laching.Core/Laching.Core.csproj`, `dotnet/Laching.Core.Tests/{Mocks.cs,Conformance/ConformanceRoot.cs,Conformance/ConformanceSupport.cs}`;
  `.github/workflows/{dotnet,release}.yml`.
- The Swift sources of `Sources/LachingCompanion/**` and `Sources/LachingTesting/{Fixtures,Conformance}/**` (the reference).
- From sunnysays, the C# mirror to move: `windows/SunnySays.Core/{Phrases,Cadence,Rules,Settings,Coordination,Desktop,Figures}/*.cs`
  and `windows/SunnySays.Core.Tests/**` (the replays and the copied helpers). Never the `.resw` files.

## Steps

| Step | Commit | What |
|---|---|---|
| 3.1 | `.NET: Laching.Companion` | `dotnet/Laching.Companion/` (net10.0, `IsAotCompatible`, PackageId `Laching.Companion`, ProjectReference Core): the folders of `Sources/LachingCompanion` file for file, starting from sunnysays' C# files and applying the API changes (`FlavorId`/`SkinId`/`PaintRole`/`VoiceId` as `readonly record struct`s over `string`; `PhraseKind.Sequence(IReadOnlyList<string> Source)`; `Languages`; `PhraseTiming`; `CompanionSettings` non-sealed with `virtual ResetToDefaults()`; `PartContent`, `PlacedPart`, `IDrawableCharacter.Layers`; `SkinCatalog`). The two loaders every port needs move in from sunnysays: `Phrases/PhraseBankJson.cs` (`PhraseBank.Parse(Stream)`, strict, absent `languages` = empty) and `Figures/CharacterArtJson.cs` (`CharacterArt.Parse(Stream)`, array = shapes, object = image). Source-generated `JsonSerializerContext`s only. |
| 3.2 | `.NET: Laching.Testing` | `dotnet/Laching.Testing/` (net10.0, refs Core + Companion, no xunit; PackageId `Laching.Testing`): `Mocks.cs`, `ScriptedRandom.cs` (from `Laching.Core.Tests`), `Conformance/ConformanceRoot.cs` (`Find(markerFile)`, `Files(folder)`), `ConformanceSupport.cs`, `VectorSupport.cs` (busy names, `SnapshotData`), the replay bodies `Conformance/Companion/{Phrases,Settings,Picker,Cadence,Quiet,Character,Desktop}Replay.cs` (`static IReadOnlyList<string> Verify(Stream json, …)`), `Fixtures/` (`FixtureBank.Load(root)`, `FixtureDefaults`, `FixtureArt.Load(root, name)` reading `conformance/companion/*`). `Laching.Core.Tests` references it and deletes its copies. `MockStoreBackend` stays in Core (apps use it in preview modes). |
| 3.3 | `.NET: Laching.Companion.Tests` | xUnit: the unit mirrors of `Tests/LachingCompanionTests` plus `Conformance/` theories over `ConformanceRoot.Files("companion/<folder>")` replaying every vector through the `Laching.Testing` bodies. `Laching.sln`, `dotnet.yml` (test the new project on ubuntu and windows), `release.yml` (pack `Laching.Companion` and `Laching.Testing` beside `Laching.Core` in the Linux job). |
| 3.4 | `docs: .NET packages` | README "What's inside" rows for the two packages; `PRODUCT.md` capabilities line; CHANGELOG *Unreleased* "Added" lines. |

## Finish

`cd dotnet && dotnet format whitespace dotnet --folder --verify-no-changes && dotnet test` green (every companion
vector replays); `dotnet.yml` green on both runners; `scripts/verify.sh --all` ok. Owner confirmed (2026-10-11)
that the NuGet Trusted Publishing policy and the `Laching.` prefix reservation cover the two new package ids.

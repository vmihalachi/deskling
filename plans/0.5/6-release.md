# 6: iOS platform, docs, site, changelog, the M9 dry run, `v0.5.0`

> Sequence 6 of `README.md`. Needs 1–5. One session, macOS. Use the `release` skill for the version mechanics.
>
> From sequence 3 (done 2026-10-11, branch `0.5-dotnet`): `Deskling.Companion` and `Deskling.Testing` exist, pack, and
> replay every `conformance/companion` vector (`dotnet/Deskling.Companion.Tests`); `release.yml` packs both beside
> `Deskling.Core`. C# names that differ from the Swift: `FlavorId`/`SkinId`, `PartContent.Vector` (Swift `.shapes`),
> `IDrawableCharacter`, `Transform.Then` (`concatenating`), `PhraseTiming.PartBeatSeconds`/`PartDelaySeconds`, a
> `TimeZoneInfo` where Swift takes a `Calendar`; no `VoiceId` yet (Swift's `VoiceID` arrives in Shell with sequence 4).
> Still open for the owner before tagging: the NuGet Trusted Publishing policy and the `Deskling.` prefix reservation
> must cover `Deskling.Companion` and `Deskling.Testing`.

## Read only these

- `README.md` here (the changelog list at the end); `.agents/skills/release/SKILL.md`; `CHANGELOG.md`; `README.md` (root); `AGENTS.md`;
  `PRODUCT.md`; `site/index.html` (the version label and install snippets only: grep `exact:` and `class="version"`); `scripts/verify.sh`;
  `.github/workflows/swift.yml`; `Package.swift`.
- From sunnysays: `plans/milestones/9-deskling-companion.md` (steps 2–5 are the dry run).

## Steps

| Step | Commit | What |
|---|---|---|
| 6.1 | `iOS: Core and Companion build for iOS 17` | `platforms: [.macOS(.v14), .iOS(.v17)]`; every file in `DesklingSystem`, `DesklingShell`, `DesklingStore`, `DesklingCompanionUI` and their tests wrapped in `#if os(macOS)` (sequence 4's list); `swift.yml` step `xcodebuild -scheme DesklingCompanion -destination 'generic/platform=iOS Simulator' build`. **Droppable:** if the wrapping drags past an hour, revert this step, note it in the CHANGELOG as 0.6 work, and go on. |
| 6.2 | `docs: 0.5 in README, AGENTS, PRODUCT, site` | Root README "What's inside" rows (Companion, CompanionUI, Testing, tools) and the *Using it* notes (profile, skins, sequences, languages); `AGENTS.md` layout table (`Sources/DesklingCompanion`, `DesklingCompanionUI`, `dotnet/Deskling.Companion`, `Deskling.Testing`, `tools/`, `plans/`), conventions ("package code never names an app", the raster rule, `conformance/companion`); `PRODUCT.md` (the store is on both platforms since 0.3.0; the companion layer); `site/index.html` sections and the "Used by" line. |
| 6.3 | `0.5.0: the companion layer` | CHANGELOG: move *Unreleased* under `## 0.5.0 - <date>` with the breaking list and migrations from the README; bump the four version places; `scripts/verify.sh --all`. Do **not** tag yet. |
| 6.4 | (sunnysays branch, no Deskling commit) | **M9 dry run:** on a sunnysays branch `m9-dry-run`, point `macos/SunnySaysCore/Package.swift` and `macos/project.yml` at `path: ../deskling` and the csproj at a local feed (`dotnet pack dotnet -o /tmp/deskling-feed` + `nuget.config`), then do M9's steps 2–5 (core, Windows core, Mac app, tools) far enough to run Gate A (vectors byte-identical) and Gate C (screenshots identical). Any API that bites gets fixed here in Deskling (amend 6.3's commit or add a fix commit). Leave the branch for the real M9 to reuse. |
| 6.5 | tag | Owner tags `v0.5.0` (or dispatches `release.yml` with the version). Confirm NuGet shows `Deskling.Core`, `Deskling.Companion`, `Deskling.Testing`, `Deskling.Windows` 0.5.0 and that `.package(url:…, exact: "0.5.0")` resolves in a scratch package. |

## Finish

`scripts/verify.sh --all` ok; `swift.yml`, `dotnet.yml`, `release.yml` green; the dry run's gates passed (say which
API changes it forced). sunnysays' M9 and Pagliacci's M0 can start.

# 2: Figures (vector and raster), Skins, the companion vectors

> Sequence 2 of `README.md` (read *Figures/*, *Skins/*, *Vectors* and *Two-part jokes*). Needs 1. One session.

## Read only these

- `README.md` here; `Sources/DesklingTesting/ConformanceSupport.swift`; `Tests/DesklingCoreTests/Conformance/{ConformanceTests,ConformanceRoot}.swift`;
  `conformance/README.md`.
- From sunnysays: `macos/SunnySaysCore/Sources/SunnySaysCore/Figures/*.swift` (not `Art/SunflowerArt.swift`: generated, 100+ KB; read its first 20 lines only),
  `macos/scripts/character.py` and `test_character.py`, `design/characters/sunflower/sunflower.rig.json`, `design/characters/palette.json`,
  `macos/SunnySaysCore/Tests/SunnySaysCoreTests/Conformance/*.swift` (the generators and `VectorSupport`), `macos/SunnySaysCore/Tests/SunnySaysCoreTests/Figures/*.swift`,
  `conformance/README.md`, and one sample each of `conformance/picker/*.json`, `quiet/*.json`, `cadence/*.json`, `desktop.json`,
  `characters/sunflower-poses.json` (head -60), `phrases.json` (head -40), `settings.json`.

## Steps

| Step | Commit | What |
|---|---|---|
| 2.1 | `tools: character.py, generalized` | `tools/character.py` + `tools/tests/test_character.py` from sunnysays' script: CLI arguments instead of constants (`--design DIR --skin NAME --out JSON --swift-out FILE --module NAME --mode vector|raster [--assets DIR --windows-assets DIR] [--check]`; sequence 5 wires them to `companion.json`); rig JSON accepts `cheeks: true`/`zz: true` as sugar for `headExtras`/`overlays` and requires `bubble: {pivot, tuck}`; **raster mode**: `layers/<part>@2x.png` (+ `@3x`) with `layers: {<part>: {frame: [x, y, w, h]}}` in the rig, pixel size verified = frame × scale, parts emitted as `{"image": "<skin>.<part>", "frame": […]}`, an imageset per layer under `--assets` and `scale-200/300` PNGs under `--windows-assets`, `glyph.svg` as today, `--check` hashing the PNGs. Fixture art sources under `Tests/Fixtures/characters/{fixture-vector,fixture-raster}/`. **Safety net:** run it on `../sunnysays/design/characters/sunflower` into a scratch folder and diff the JSON against `../sunnysays/conformance/characters/sunflower.json`: identical except the added `bubble` and the `headExtras`/`overlays` spelling. |
| 2.2 | `Companion: Figures, Skins` | `Figures/Geometry.swift` (`Point`, `Size`, `Rect`, string-backed `PaintRole`, `PathCommand`, `PathShape`, `PartContent`), `Transform.swift` (+ `affine`), `Pose.swift` (`headExtras`, `overlays`), `CharacterArt.swift` (`PartContent` parts, `BubbleAnchor`, `bubblePoint`), `DrawableCharacter.swift`, `RiggedCharacter.swift` (`layers(for:)`, `shapes(for:)`), `CharacterArtJSON.swift` (encode/decode of the JSON 2.1 writes); `Skins/SkinCatalog.swift`. Tests: `RiggedCharacterTests` from sunnysays over `FixtureArt.vector` (compiled into `Sources/DesklingTesting/Fixtures/FixtureArt.swift` by 2.1's `--swift-out`), raster tests (`layers` transforms, image parts skipped by `shapes`), `CharacterArtJSONTests` (round trip; decoding a copy of sunnysays' `sunflower.json` as a test resource), `SkinCatalogTests` (locked, owned, revoked, unknown → default). |
| 2.3 | `Testing: fixtures, ConformanceSuite, generators` | `Sources/DesklingTesting/Conformance/`: `ConformanceRoot` (public `find(markerFile:from:)`), `ConformanceSuite` (`checkUpToDate`, `replayCommitted`, `checkDeterministic`, `writeIfAsked(flag:)`), `JSONValue`, `SnapshotData`, and the generators from sunnysays' `Conformance/*Vectors.swift` parameterized by bank/defaults/character/cases (`PhrasesVector`, `PickerVector`, `CadenceVector`, `QuietTimeline`, `CharacterPoseVector`, `DesktopVector`, `SettingsVector.base(defaults:)` returning a `JSONValue` tree), `BankAudit.floors(bank:languages:skins:minimumPerTag:)`. New fields written only when non-default (`languages`, `keys`, `parts`, `layers`, `language` when not `"en"`). |
| 2.4 | `conformance/companion` | `Tests/DesklingCompanionTests/Conformance/` (cases: the README's picker list, the 5 cadence cases copied from sunnysays, the 17 quiet timelines over fixture ids + `sequence-delivered`, desktop + `partDelays`, character poses for both fixtures); generate with `DESKLING_WRITE_CONFORMANCE=1`; `ConformanceTests` gains `companion/*` folders. **Safety net, in the PR only:** a test that loads a copy of sunnysays' bank (`PhraseBank.sunny` pasted as a test resource with the `source:` spelling), runs the moved generators with sunnysays' defaults and `cmp`s the output with `../sunnysays/conformance/{picker,quiet,cadence}/*.json`, `desktop.json`, `settings.json`, `phrases.json`: byte-identical. Delete the test and the resource before merging. |
| 2.5 | `docs: conformance/README for companion` | The companion section of `conformance/README.md` (formats copied from sunnysays' README rows: phrases, picker, cadence, quiet, desktop, characters, plus the new fields and the raster poses); `scripts/conformance.sh` unchanged. |

## Finish

`scripts/verify.sh --all` ok; `swift.yml` green (macOS and ubuntu); 2.4's byte-identity test passed before removal
(say so in the commit message of 2.4). Hand to sequence 3: `conformance/companion/*` exists and the C# mirror can replay it.

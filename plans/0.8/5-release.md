# 5: Release 0.8.0

> Sequence 5 of `README.md`. Needs 1–4. One session, any OS, plus the owner for the tag.

## Read only these

- `README.md` here and every sequence's outcome; `AGENTS.md` (*Versioning*); `.github/workflows/{dotnet,release}.yml`;
  the `release` skill.

## Steps

| Step | Commit | What |
|---|---|---|
| 5.1 | `CI: the companion shell packages` | `dotnet.yml`: ubuntu builds and tests `Laching.Companion.Shell`; windows builds `Laching.Windows.Companion` and the fixture app and runs its tests. `release.yml`: packs `Laching.Companion.Shell` with the pure packages and `Laching.Windows.Companion` in the windows job; both under Trusted Publishing. |
| 5.2 | `docs: 0.8` | `README.md` (the two packages, a Windows companion skeleton), `AGENTS.md` layout and commands, `tools/skills/windows-port/SKILL.md` (the packages, the app's remaining files), `site/index.html`. |
| 5.3 | `0.8.0` | The version in the four places, CHANGELOG, `scripts/verify.sh` ok; the owner tags `v0.8.0`. |
| 5.4 | (no commit) | Each app's pin bump is its own change: sunnysays M3.2 starts with it; Pagliacci's M6 too. |

## Finish

NuGet.org lists `Laching.Companion.Shell` and `Laching.Windows.Companion` 0.8.0; the GitHub release exists.

## Outcome

### Before 5

0.7.7 (another session's `fonts.py` release, 2026-10-13) was cut from a `master` that already held sequences 1–3, and
`release.yml` already packed `Laching.Companion.Shell`. So NuGet.org has had `Laching.Companion.Shell` since 0.7.7,
and `Laching.Testing` 0.7.7 depends on it. `Laching.Windows.Companion` was not packed.

0.7.7 had taken the whole *Unreleased* section, so the `Windows.Companion` and fixture app entries went back under
*Unreleased* (`f017001`). The v0.7.7 GitHub release's notes still list them.

### 5.1

- `dotnet.yml`'s ubuntu job already built and tested `Laching.Companion.Shell`, and its windows job already built
  `Laching.Windows.Companion` and ran its tests. The windows job now builds the fixture app too, in Release with no
  platform named (the runner's own, win-x64).
- `release.yml`'s windows job tests `Laching.Windows` and `Laching.Windows.Companion`, then packs both. The core job
  packed `Laching.Companion.Shell` already. Trusted Publishing needs nothing new: its policy is per repository and
  workflow, and a new package id is pushed under the same account, as `Laching.Companion.Shell` was in 0.7.7.
- `Laching.Windows.Companion`'s package description names what it holds now.
- Checked here before anything was published:
  - The six packages were packed at `0.8.0-local`. `Laching.Windows.Companion` holds its dll and pri, and depends on
    `Laching.Companion.Shell` and `Laching.Windows` at the same version, the Windows App SDK 2.5.1 and
    `System.Drawing.Common`.
  - A copy of the fixture app was built on those packages instead of the projects. Its screenshot run gave 16 shots
    with no problems.
  - Its Native AOT publish had no warnings, and that build's home and desktop shots had no problems either.
  - The XAML compiler can't read a package past 260 characters of path, so the copy restored into the user's package
    cache, not a deep scratch folder.

### 5.2

- `README.md`: the two packages in *What's inside* and *Install*, the companion shell's fakes under `Laching.Testing`,
  *A companion app on Windows* (`Program`, the `App`'s `MakeCompanion` and `RunScreenshots`, and what the app keeps),
  and the shell's tests, the fixture app and its screenshots under *Develop*.
- `AGENTS.md`: `Laching.Windows.Companion`'s row lists every folder and is no longer "in progress".
- `tools/skills/windows-port/SKILL.md`: *The companion shell (0.8)*, what each package holds, what stays in the app,
  and the XAML traps from 4 (`WindowInset`'s static properties, no element named `Language`).
- `site/index.html`: six NuGet packages, the two in the companion row and the install card. Checked in a headless
  render: the long package names break at their dots, so they don't run into the next column.
- The `release` skill says every NuGet package, not both.

### 5.3

- 0.8.0 in the four places. *Unreleased* became `## 0.8.0 - 2026-10-13`, with a lead paragraph for the release notes
  and the `windows-port` skill's line. Since v0.7.7 the Swift side changed only its version.
- `verify.sh --fast` is ok (versions agree, C# formatting). Every .NET test project passes in Release here: Core 210,
  Companion 209, Companion.Shell 90, Windows 4, Windows.Companion 95.
- The tag is the owner's: `v0.8.0` on this commit once `dotnet.yml` is green on it.

### Released

- 5.1 and 5.2 were pushed first; `.NET` (fixture app build included), `Tools`, `Pages` and CodeQL were green on them.
  `c84b749` went up with the tag at the owner's word, so the site showed 0.8.0 only once it existed.
- `release.yml` run 38428336861 succeeded in every job. It pushed the six packages to NuGet.org, staged
  `@laching/landing` 0.8.0 on npm for the owner's 2FA approval, and published the GitHub release
  (https://github.com/vmihalachi/laching/releases/tag/v0.8.0) with the packages and the landing tarball.
- 5.4 is the apps': sunnysays M3.2 and Pagliacci's M6 each start with their pin bump.
- Left for later: the screenshot runner in `tools/` (done in 0.8.1, with Pagliacci's M6), the contrast-theme shots, the hand checks in 3.5 and
  4, and the desktop character's CPU (4.3).

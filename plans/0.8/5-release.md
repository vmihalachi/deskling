# 5: Release 0.8.0

> Sequence 5 of `README.md`. Needs 1–4. One session, any OS, plus the owner for the tag.

## Read only these

- `README.md` here and every sequence's outcome; `AGENTS.md` (*Versioning*); `.github/workflows/{dotnet,release}.yml`;
  the `release` skill.

## Steps

| Step | Commit | What |
|---|---|---|
| 5.1 | `CI: the companion shell packages` | `dotnet.yml`: ubuntu builds and tests `Deskling.Companion.Shell`; windows builds `Deskling.Windows.Companion` and the fixture app and runs its tests. `release.yml`: packs `Deskling.Companion.Shell` with the pure packages and `Deskling.Windows.Companion` in the windows job; both under Trusted Publishing. |
| 5.2 | `docs: 0.8` | `README.md` (the two packages, a Windows companion skeleton), `AGENTS.md` layout and commands, `tools/skills/windows-port/SKILL.md` (the packages, the app's remaining files), `site/index.html`. |
| 5.3 | `0.8.0` | The version in the four places, CHANGELOG, `scripts/verify.sh` ok; the owner tags `v0.8.0`. |
| 5.4 | (no commit) | Each app's pin bump is its own change: sunnysays M3.2 starts with it; Pagliacci's M6 too. |

## Finish

NuGet.org lists `Deskling.Companion.Shell` and `Deskling.Windows.Companion` 0.8.0; the GitHub release exists.

## Outcome

### Before 5

0.7.7 (another session's `fonts.py` release, 2026-10-13) was cut from a `master` that already held sequences 1–3, and
`release.yml` already packed `Deskling.Companion.Shell`. So NuGet.org has had `Deskling.Companion.Shell` since 0.7.7,
and `Deskling.Testing` 0.7.7 depends on it. `Deskling.Windows.Companion` was not packed.

0.7.7 had taken the whole *Unreleased* section, so the `Windows.Companion` and fixture app entries went back under
*Unreleased* (`f017001`). The v0.7.7 GitHub release's notes still list them.

### 5.1

- `dotnet.yml`'s ubuntu job already built and tested `Deskling.Companion.Shell`, and its windows job already built
  `Deskling.Windows.Companion` and ran its tests. The windows job now builds the fixture app too, in Release with no
  platform named (the runner's own, win-x64).
- `release.yml`'s windows job tests `Deskling.Windows` and `Deskling.Windows.Companion`, then packs both. The core job
  packed `Deskling.Companion.Shell` already. Trusted Publishing needs nothing new: its policy is per repository and
  workflow, and a new package id is pushed under the same account, as `Deskling.Companion.Shell` was in 0.7.7.
- `Deskling.Windows.Companion`'s package description names what it holds now.
- Checked here before anything was published:
  - The six packages were packed at `0.8.0-local`. `Deskling.Windows.Companion` holds its dll and pri, and depends on
    `Deskling.Companion.Shell` and `Deskling.Windows` at the same version, the Windows App SDK 2.5.1 and
    `System.Drawing.Common`.
  - A copy of the fixture app was built on those packages instead of the projects. Its screenshot run gave 16 shots
    with no problems.
  - Its Native AOT publish had no warnings, and that build's home and desktop shots had no problems either.
  - The XAML compiler can't read a package past 260 characters of path, so the copy restored into the user's package
    cache, not a deep scratch folder.

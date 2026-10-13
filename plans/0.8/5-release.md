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

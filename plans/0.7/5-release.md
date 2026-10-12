# 5: Docs, changelog, the two dry runs, `v0.7.0`

> Sequence 5 of `README.md`. Needs 1–4. One session, macOS. Use the `release` skill for the version mechanics.
> Read the *Outcome* notes the earlier sequences left at the end of their briefs before starting.

## Read only these

- `README.md` here (the changelog list at the end); `.agents/skills/release/SKILL.md`; `CHANGELOG.md`; `README.md` (root); `AGENTS.md`;
  `site/index.html` (grep `exact:` and `class="version"` only); `scripts/verify.sh`; `Package.swift`.
- From sunnysays: `plans/milestones/10-companion-shell.md`. From Pagliacci: `plans/milestones/7-companion-shell.md`.

## Steps

| Step | Commit | What |
|---|---|---|
| 5.1 | `docs: 0.7 in README, AGENTS, site` | Root README "What's inside" rows (the companion shell, the settings rows, the Support window, the screenshot script, the standard cases, the landing and store tools); `AGENTS.md` layout and conventions (a view in the package takes its words as parameters; the `plans/` row points at `plans/0.7/`); `site/index.html` sections. |
| 5.2 | `0.7.0: the companion shell` | CHANGELOG: move *Unreleased* under `## 0.7.0 - <date>` with the list from the README; bump the four version places; `scripts/verify.sh --all`. Do **not** tag yet. |
| 5.3 | (sunnysays worktree, no Deskling commit) | **M10 dry run.** Never `git checkout` in `../sunnysays` (shared checkout): `git -C ../sunnysays worktree add ../sunnysays-m10 -b m10-dry-run`, point `macos/SunnySaysCore/Package.swift` and `macos/project.yml` there at `path: ../deskling` and the csproj at a local feed (`dotnet pack dotnet -o /tmp/deskling-feed` + `nuget.config`), set `landing/package.json`'s dependency to `file:../../deskling/tools/landing`, then do M10's steps 2–5 far enough to run Gate A (vectors byte-identical), Gate C (screenshots pixel-identical) and Gate D (the landing's `dist/` identical, `push.py --status` the same plan). Any API that bites gets fixed here (amend 5.2 or a fix commit). Leave the worktree for the real M10. |
| 5.4 | (Pagliacci worktree, no Deskling commit) | **M7 dry run**, the same way (`../pagliacci-m7`, branch `m7-dry-run`), to Gate A and Gate C. Pagliacci exercises what sunnysays doesn't: the two-part bubble and gel over `showBubble`/`voicingChanged`, the skin over `glyphCharacter`, the inline language nav over `languageSwitcher`. Skip it only if Pagliacci's M3 is still uncommitted in the main checkout (its `AppModel` is then in flux): say so in the outcome and let M7 be the first run. |
| 5.5 | tag | Owner tags `v0.7.0` (or dispatches `release.yml`). Confirm NuGet shows `Deskling.Core`, `Deskling.Companion`, `Deskling.Testing`, `Deskling.Windows` 0.7.0 and that `exact: "0.7.0"` resolves in a scratch package. |

## Finish

`scripts/verify.sh --all` ok; `swift.yml`, `dotnet.yml`, `tools.yml`, `release.yml` green; the dry runs' gates passed
(say which API changes they forced, and the `open` surface as it ended up). sunnysays' M10, Pagliacci's M7 and
sequence 6 can start.

# 6: mybackhurts onto the shared tools (droppable)

> Sequence 6 of `README.md`. Needs `v0.7.0`. One session, run in `../mybackhurts` (its `AGENTS.md` rules apply there:
> never load its String Catalogs or `.resw` files, never `git checkout` in the shared checkout). Droppable: if the
> tools fight mybackhurts' shape for more than an hour, stop, note what bit in the outcome here, and leave it.
>
> The landing pages moved to public repositories of their own on 2026-10-12 (submodules at `landing/`): read the
> note under *`tools/landing`* in `README.md` before touching `@deskling/landing` or a landing `package.json`.

mybackhurts keeps a fork of the tooling (its `macos/scripts/l10n.py` shares 703 of ~860 lines with `tools/l10n.py`;
`store-listing/check.py` is 97% sunnysays'; `apple/push.py` 58%; `landing/vite.config.ts` 69%). It is not a companion
app: no bank, no character, no `conformance/` of Deskling's shape, and it stays on Deskling 0.4.0 for code. The
submodule it adds is for tools only.

## Read only these

- `README.md` here (*`tools/store`*, *mybackhurts*); `tools/README.md`; `tools/companion.schema.json`.
- In mybackhurts: `AGENTS.md`, `macos/scripts/l10n.py` (its constants at the top and `main()` only), `.claude/hooks/hook.py`, `.claude/settings.json`,
  `store-listing/README.md`, `store-listing/apple/push.py` (constants only), `landing/vite.config.ts`, `landing/package.json`, `scripts/verify.sh`.

## Steps

| Step | Commit (in mybackhurts) | What |
|---|---|---|
| 6.1 | `Deskling tools: the submodule and companion.json` | `git submodule add https://github.com/vmihalachi/deskling deskling && git -C deskling checkout v0.7.0`; `companion.json` with `name`, `displayName`, `bundleId`, `devBundleId`, `deskling` (`tag` v0.7.0, `path`), `mac`, `windows`, `languages` (its 17), `l10n` (catalog, plist, landing, glossaryKeys, keepNames, rulesFile, pluralCategories, linuxPo), `store`, `landing`, `hooks`; no `character`, `conformance`, `voice`, `characters`, `sounds`. `python3 deskling/tools/companion.py validate` ok. Its own `scripts/verify.sh` stays (it does not run the pin check; the code pins 0.4.0 on purpose, noted in `AGENTS.md`). |
| 6.2 | `l10n.py from Deskling` | `macos/scripts/l10n.py` → a two-line wrapper; `python3 macos/scripts/l10n.py status -v` and `check` give the same report as before (compare the output of the fork and the tool before deleting the fork); `macos/scripts/test_l10n.py` deleted (the tests live in Deskling); the `localization` skill → a symlink into `deskling/tools/skills/` with mybackhurts' specifics in `.agents/notes/localization.md`. |
| 6.3 | `Store tools from Deskling` | `store-listing/check.py`, `apple/push.py`, `microsoft/import.py` → wrappers; `apple/test_push.py` deleted; `python3 store-listing/apple/push.py --version <current> --status` prints the same plan as before. |
| 6.4 | `Landing on @deskling/landing` | `landing/package.json` + `vite.config.ts` on `companionLanding` (its `app-sync.ts` assertion and `og/poster.ts` stay as `extraPlugins`/placeholders); `vp check && vp build`; `dist/` identical to a build from before the change, file for file. `landing.yml` checks out submodules. |
| 6.5 | `Hook from Deskling` | `.claude/hooks/hook.py` → wrapper, `test_hook.py` deleted, `.claude/settings.json` unchanged (it calls the wrapper); the guards and stop checks it needs expressed in `companion.json`'s `hooks`. |
| 6.6 | docs | mybackhurts' `AGENTS.md` (the submodule, the wrappers, the 0.4.0 code pin), `README.md` if it names the scripts. |

## Finish

In mybackhurts: `scripts/verify.sh --all` ok, `l10n.py check` ok, the landing builds identical, the push script's
dry run unchanged, `macos.yml` and `landing.yml` green. Here: nothing to commit unless a tool needed a fix (then a
`0.7.1` with a CHANGELOG line).

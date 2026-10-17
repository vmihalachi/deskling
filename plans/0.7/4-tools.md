# 4: `tools/landing`, `tools/store`, the schema

> Sequence 4 of `README.md` (read *`tools/landing`*, *`tools/store`*). Needs nothing. One session, anywhere with
> python3 and node 24.
>
> The landing pages moved to public repositories of their own on 2026-10-12 (submodules at `landing/`): read the
> note under *`tools/landing`* in `README.md` before touching `@laching/landing` or a landing `package.json`.

## Read only these

- `README.md` here; `tools/README.md`; `tools/companion.py`; `tools/companion.schema.json`; `tools/verify.sh`; `tools/tests/{run,support,test_companion}.py`
  and `tools/tests/fixture-app/companion.json`; `.github/workflows/tools.yml`.
- From sunnysays: `landing/vite.config.ts`, `landing/og/render.ts`, `landing/package.json`, `landing/src/i18n/languages.ts`, `landing/voice.ts` (stays
  sunnysays'; only its plugin hook matters), `landing/wrangler.json`; `store-listing/check.py`, `store-listing/apple/push.py`
  (the working copy: it has the "App Information languages first" fix), `store-listing/README.md`, `companion.json`.
- From Pagliacci: `landing/vite.config.ts` (its `jokes`, `portrait`, inline language nav and `renderJoke` are app
  placeholders), `companion.json`.
- From mybackhurts: `landing/vite.config.ts` (its `app-sync.ts` and `og/poster.ts` stay its own), `store-listing/apple/{push,test_push}.py`,
  `store-listing/microsoft/import.py`.
- Never the apps' `landing/src/i18n/*.json` whole files.

## Steps

| Step | Commit | What |
|---|---|---|
| 4.1 | `tools: @laching/landing` | `tools/landing/` (`package.json`, `src/index.ts` exporting `companionLanding` with the README's options, `src/{locales,html,legal,sitemap,hosting,og,inputs}.ts`, `README.md`), a `test/` with vitest over `test/fixture-site/` (two languages, one legal page, one inline script): the rendered pages carry the right canonical, hreflang and `og:locale` tags, the redirect script only on the default page, the sitemap lists every page, `_headers` has one hash per inline script and `connect-src 'none'`, the OG PNG renders in the given font. `tools.yml` gains a `landing` job (`npm ci && npx vp test` in `tools/landing`). `setup-linux.sh` installs its dependencies when the app's `landing/package.json` references it. |
| 4.2 | `tools: store/check.py, push.py, import.py` | `tools/store/check.py` (sunnysays', `LANGS` from `languages.all`, `DIR` from `store.dir`), `tools/store/push.py` (the union: sunnysays' `review` part, `--status`, IAP creation with `baseTerritory`; mybackhurts' `previews`; `APP_ID`, `COPYRIGHT`, `LOCALE`, `EXTRA_LOCALES`, `IAPS`, the review screenshot settings from `store.apple`), `tools/store/import.py` (mybackhurts'), `tools/tests/test_store.py` (mybackhurts' `test_push.py` over the fixture app, plus `check.py` over a fixture listing). `companion.schema.json`: the `store` section as the README shows; `character` and `conformance` optional, and `companion.py`'s `phrases_file`, `characters` and friends `None` when absent (`l10n.py`: no scoped keys without a `phrases.json`; `phrases.py`, `character.py`, `conformance.sh`, `verify.sh`'s vector checks: a clear "this app has no conformance section" error). `verify.sh`: `store-listing/*.md` touched → `store/check.py`. |
| 4.3 | `tools: substitution checks in l10n.py` | From mybackhurts' `macos/scripts/l10n.py`: `english_substitutions` and `substitution_issues` (`%#@name@` plural substitutions must exist in every category of every language), wired into `check` and `status`; a test over a fixture catalog entry with a substitution. Generic, so it lands here before mybackhurts adopts the tool. |
| 4.4 | `docs: tools 0.7` | `tools/README.md` (the two new tools, the `store` section, the landing package and its `file:` dependency, what an app without `character`/`conformance` can run), `AGENTS.md` (`tools/` row), CHANGELOG *Unreleased*. |

## Finish

`python3 tools/tests/run.py` green; `npx vp test` in `tools/landing` green; `tools.yml` green; both apps' and the
fixture app's `companion.json` validate (sunnysays' and Pagliacci's gain `store` in their migrations, so validate
them as they are). Hand to 5: the exact `store` keys `push.py` reads, so the migration briefs fill them right.

## Outcome (2026-10-12)

Done in one session after 1–3. **The landing route is npm** (the owner's choice, since the landing pages became their
own public repositories): `@laching/landing` is TypeScript compiled to `dist/` (Node won't strip types inside
`node_modules`), `release.yml`'s `landing` job tests and packs it at the tag's version and `publish` pushes it to npm
when the repository variable `NPM_PUBLISH` is `true`. **Before 0.7.0: the owner creates the `@laching` npm scope**
(an organization on npmjs.com) and adds an `NPM_TOKEN` secret for the first publish; after it, a trusted publisher on
npmjs.com (repository `vmihalachi/laching`, workflow `release.yml`) replaces the token. Without them the tarball is
still on the GitHub release. **Superseded before 0.7.0 (2026-10-12):** no token at all. An `npm` job stages the
package through trusted publishing and an owner approves it with 2FA (npm's staged publishing); the one-time setup is
in `tools/landing/README.md`.

- **Gate D, checked early**: sunnysays' and Pagliacci's landing configs, converted onto a packed tarball, built
  `dist/` byte for byte the same as before and pass `vp check`. sunnysays' `vite.config.ts` keeps its lines, voice
  switch, runtime JSON and JSON-LD (521 → about 180 lines); Pagliacci's keeps its jokes, portrait, inline switcher
  (`languageSwitcher: "inline"`) and its own store button (`storeButton` with the exported `appleIcon`). In the
  landing repos: `"@laching/landing": "0.7.0"` in `devDependencies`, `import { companionLanding, … } from
  "@laching/landing"`, then `defineConfig({ ...landing, fmt, lint })`. `og/render.ts` goes (the package's
  `og: { dir, iconPath, fontFamily, iconBackground, breakFLigatures, copy }`: sunnysays `"Fredoka"`, `#FFF8E7`,
  `breakFLigatures: true`; Pagliacci `"Bodoni Moda"`, `#B8321D`).
- The options beyond the README: `localeProblems` (Pagliacci's jokes check), `languageSwitcherLabel`, `headers` (extra
  `_headers` rules: mybackhurts' posters), `Language.aliases` and `notFor` (mybackhurts' redirect script, which only
  takes its `skip` form when a language has `notFor`). mybackhurts' `notFoundJson` and other keys are its own
  `placeholders`.
- **The store keys `push.py` reads** (`store.apple`): `appId`, `platform` (MAC_OS), `copyright`, `locales`,
  `extraLocales`, `baseTerritory` (USA by default; sunnysays ITA), `screenshots`, `previews`, `screenshotType`,
  `previewType`, `iaps` (product id → `reference`, `type`, `familySharing`, `price` as a string, `name` and
  `description` field ids, `reviewNote`, `reviewShot`), `review` (`notes`, `shots` relative to the root, `size`,
  `background`). `store.siteUrl` defaults to `https://<domain>`. `store.microsoft`: `screenshots`, `art`,
  `screenshotCount`, `publisher`; its presence makes `check.py` require the `ms.*` fields. sunnysays' constants as a
  `store` section made the shared `check.py` print exactly what its own does; `push.py --status` needs the owner's
  App Store Connect key, so M10 compares it there.
- `l10n.py` gained the substitution **checks** (status and check); mybackhurts' export and apply of substitutions are
  still its fork's: sequence 6 ports them if its `status -v` / `check` comparison needs it.
- `setup-linux.sh` needs nothing new: a landing repository installs `@laching/landing` from npm like any dependency.

# @deskling/landing

A desk companion's landing page as Vite plugins: every home page rendered from one template per language, the
hand-translated legal pages given canonical and hreflang links, `sitemap.xml`, the Cloudflare `_headers` with a CSP
that hashes the inline scripts, and the social cards and icons rendered from SVG with resvg. The page itself (its
template, script, styles, data and brand) stays the app's; this is the plumbing every Deskling app's page repeated.

```ts
import { companionLanding, escapeHtml, scriptJson } from "@deskling/landing";
import { defineConfig } from "vite-plus";

const landing = companionLanding({
  root: __dirname,
  siteUrl: "https://example.app",
  appStoreUrl: null, // "coming soon" until the listing is live
  contactEmail: "hello@example.app",
  languages, // { code, path, label, nativeName, ogLocale, aliases?, notFor? }[]
  defaultLanguageCode: "en",
  locales, // code → src/i18n/<code>.json
  placeholders: (key, language, locale) =>
    key === "runtimeJson" ? scriptJson(locale.runtime) : undefined,
  jsonLd: (language, locale) => ({
    "@context": "https://schema.org",
    "@type": "SoftwareApplication" /* … */,
  }),
  og: {
    dir: `${__dirname}/og`,
    iconPath: `${__dirname}/public/favicon.svg`,
    fontFamily: "Fredoka",
    iconBackground: "#FFF8E7",
  },
});

export default defineConfig({ ...landing, fmt: {}, lint: {} });
```

The template's `{{key}}` placeholders are the app's own (`placeholders`, asked first), then the generated ones:
`langRedirectScript`, `storeButton`, `storeButtonBig`, `contactEmail`, `legal<Page>Url`, `jsonLd`,
`languageSwitcher` (`"menu"`, `"inline"` or a function), `hreflangLinks`, `ogLocale`, `ogLocaleAlternates`, then the
locale's dotted keys. Locale files must share the default's keys. `csp.mediaSrc` allows recorded audio, `headers` adds
`_headers` rules, `extraPlugins` runs the app's own plugins between the hosting and social-asset ones.

Published to npm from Deskling's `release.yml` on each `vX.Y.Z` tag, at that version (the `version` here is a
placeholder). Peer dependencies: `vite-plus` and `@resvg/resvg-js`. Develop with `npm ci`, `npx vp test run`,
`npx vp check` and `npx tsc --noEmit`.

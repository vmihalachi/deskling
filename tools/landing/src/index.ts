import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import type { Plugin } from "vite-plus";
import { hostingConfigPlugin } from "./hosting.js";
import {
  inlineLanguageSwitcher,
  langRedirectScript,
  macStoreButton,
  menuLanguageSwitcher,
  renderTemplate,
  scriptJson,
} from "./html.js";
import { alternates, legalPageForPath, legalPath } from "./legal.js";
import { assertSameLocaleKeys } from "./locales.js";
import { type OgCopy, socialAssetsPlugin } from "./og.js";
import { renderSitemap } from "./sitemap.js";
import type { Language, Locale } from "./types.js";

export type { JsonValue, Language, Locale } from "./types.js";
export { flattenKeys, readPath, assertSameLocaleKeys } from "./locales.js";
export {
  appleIcon,
  escapeAttr,
  escapeHtml,
  inlineLanguageSwitcher,
  langRedirectScript,
  macStoreButton,
  menuLanguageSwitcher,
  renderTemplate,
  scriptJson,
} from "./html.js";
export { alternates, legalPageForPath, legalPath } from "./legal.js";
export { renderSitemap } from "./sitemap.js";
export { headersFile, hostingConfigPlugin, inlineScriptHashes } from "./hosting.js";
export {
  createRenderer,
  socialAssets,
  socialAssetsPlugin,
  type OgCopy,
  type OgOptions,
} from "./og.js";

export interface CompanionLandingOptions<L extends Language = Language> {
  /** The landing's folder (`__dirname` in its `vite.config.ts`). */
  root: string;
  /** "https://example.app", no trailing slash. */
  siteUrl: string;
  /** The Mac App Store listing; null shows "coming soon" (`locale.store.soon`). */
  appStoreUrl: string | null;
  /** `{{contactEmail}}`. */
  contactEmail?: string;
  languages: readonly L[];
  defaultLanguageCode: L["code"];
  /** Every language's locale file, by code; they must share the default's keys. */
  locales: Record<string, Locale>;
  /** Hand-translated pages at `<language path><page>/index.html`; `{{legal<Page>Url}}` links each. */
  legalPages?: readonly string[];
  /** The home page template the other languages render from (`<root>/index.html`). */
  templatePath?: string;
  /** The app's own placeholders, asked first: a string fills `{{key}}`, undefined falls through. */
  placeholders?: (key: string, language: L, locale: Locale) => string | undefined;
  /** The schema.org object for `{{jsonLd}}`. */
  jsonLd?: (language: L, locale: Locale) => object;
  /** `{{languageSwitcher}}`: a globe dropdown, the codes in a row, or the app's own. */
  languageSwitcher?: "menu" | "inline" | ((current: L, label: string) => string);
  /** Where the switcher finds its label in a locale. */
  languageSwitcherLabel?: (locale: Locale) => string;
  /** `{{storeButton}}` and `{{storeButtonBig}}`; `macStoreButton` by default. */
  storeButton?: (locale: Locale, big: boolean) => string;
  /** More problems with a language's files, checked with the locale keys (a missing data file, say). */
  localeProblems?: (language: L) => string | undefined;
  /**
   * The social cards and icons. `copy` gives each language's card text (its locale's `og` by default). Leave it out
   * when the app renders its own (a plugin in `extraPlugins`).
   */
  og?: {
    dir: string;
    iconPath: string;
    fontFamily: string;
    iconBackground: string;
    breakFLigatures?: boolean;
    copy?: (language: L, locale: Locale) => OgCopy;
  };
  /** `media-src` for recorded voices ("'self'"); none by default. */
  csp?: { mediaSrc?: string };
  /** More `_headers` rules, after the global ones and `/assets/*`. */
  headers?: [path: string, headers: string[]][];
  /** The app's own plugins (a voice plugin), between the hosting and social-asset ones. */
  extraPlugins?: Plugin[];
}

/**
 * A desk companion's landing page as Vite plugins and build inputs: every home page rendered from one template per
 * language (`{{key}}` from the locale files and generated values), the hand-translated legal pages given canonical and
 * hreflang links, `sitemap.xml`, the Cloudflare `_headers` with a CSP that hashes the inline scripts, and the social
 * cards and icons rendered from SVG. The app keeps its data, its page and its brand; spread the result into
 * `defineConfig`.
 */
export function companionLanding<L extends Language>(
  options: CompanionLandingOptions<L>,
): { plugins: Plugin[]; build: { rollupOptions: { input: Record<string, string> } } } {
  const { root, siteUrl, languages, locales } = options;
  const legalPages = options.legalPages ?? ["privacy", "terms"];
  const defaultLanguage = languages.find(({ code }) => code === options.defaultLanguageCode);
  if (!defaultLanguage)
    throw new Error(`No language ${options.defaultLanguageCode} among the languages`);

  function languageForPath(pathname: string): L {
    return (
      languages.find(({ path }) => {
        const prefix: string = path;
        return prefix !== "/" && (pathname === prefix || pathname.endsWith(`${prefix}index.html`));
      }) ?? defaultLanguage!
    );
  }

  const switcher = options.languageSwitcher ?? "menu";
  const storeButton =
    options.storeButton ??
    ((locale: Locale, big: boolean) => macStoreButton(options.appStoreUrl, locale, big));
  const switcherLabel =
    options.languageSwitcherLabel ?? ((locale: Locale) => String(locale.nav.languageLabel));

  function generated(key: string, language: L): string | undefined {
    const locale = locales[language.code];
    const own = options.placeholders?.(key, language, locale);
    if (own !== undefined) return own;
    for (const page of legalPages) {
      if (key === `legal${page[0].toUpperCase()}${page.slice(1)}Url`)
        return legalPath(language, page);
    }
    switch (key) {
      case "langRedirectScript":
        return langRedirectScript(languages, defaultLanguage!, language);
      case "storeButton":
        return storeButton(locale, false);
      case "storeButtonBig":
        return storeButton(locale, true);
      case "contactEmail":
        return options.contactEmail;
      case "jsonLd":
        return options.jsonLd
          ? `<script type="application/ld+json">${scriptJson(options.jsonLd(language, locale))}</script>`
          : undefined;
      case "languageSwitcher": {
        const label = switcherLabel(locale);
        if (switcher === "menu") return menuLanguageSwitcher(languages, language, label);
        if (switcher === "inline") return inlineLanguageSwitcher(languages, language, label);
        return switcher(language, label);
      }
      case "hreflangLinks":
        return alternates(siteUrl, languages, defaultLanguage!, ({ path }) => path)
          .map(
            ([hreflang, href]) => `<link rel="alternate" hreflang="${hreflang}" href="${href}" />`,
          )
          .join("\n    ");
      case "ogLocale":
        return language.ogLocale;
      case "ogLocaleAlternates":
        return languages
          .filter(({ code }) => code !== language.code)
          .map(({ ogLocale }) => `<meta property="og:locale:alternate" content="${ogLocale}" />`)
          .join("\n    ");
      default:
        return undefined;
    }
  }

  function localizedHtmlPlugin(): Plugin {
    assertSameLocaleKeys(locales, languages, defaultLanguage!, (language) =>
      options.localeProblems?.(language as L),
    );
    const templatePath = options.templatePath ?? resolve(root, "index.html");

    return {
      name: "laching-landing-html",
      transformIndexHtml: {
        order: "pre",
        handler(html, ctx) {
          // Legal pages are hand-written per language; give them the canonical and hreflang links the home page
          // template has, so search engines treat them as translations, not duplicates.
          const legal = legalPageForPath(languages, legalPages, ctx.path);
          if (legal) {
            const links: Record<string, string>[] = [
              { rel: "canonical", href: `${siteUrl}${legalPath(legal.language, legal.page)}` },
              ...alternates(siteUrl, languages, defaultLanguage!, (language) =>
                legalPath(language, legal.page),
              ).map(([hreflang, href]) => ({ rel: "alternate", hreflang, href })),
            ];
            return {
              html,
              tags: links.map((attrs) => ({ tag: "link", attrs, injectTo: "head" as const })),
            };
          }

          const language = languageForPath(ctx.path);
          // 404.html is its own template; every home page renders from index.html.
          const isNotFound = ctx.path.endsWith("404.html");
          const template =
            language === defaultLanguage || isNotFound ? html : readFileSync(templatePath, "utf8");
          return renderTemplate(template, language.code, locales[language.code], (key) =>
            generated(key, language),
          );
        },
      },
      generateBundle() {
        this.emitFile({
          type: "asset",
          fileName: "sitemap.xml",
          source: renderSitemap(siteUrl, languages, defaultLanguage!, legalPages),
        });
      },
    };
  }

  const og = options.og;
  const ogCopy = og?.copy ?? ((_language: L, locale: Locale) => locale.og as OgCopy);
  const plugins: Plugin[] = [
    localizedHtmlPlugin(),
    hostingConfigPlugin({ mediaSrc: options.csp?.mediaSrc, rules: options.headers }),
    ...(options.extraPlugins ?? []),
    ...(og
      ? [
          socialAssetsPlugin({
            ogDir: og.dir,
            iconPath: og.iconPath,
            fontFamily: og.fontFamily,
            iconBackground: og.iconBackground,
            breakFLigatures: og.breakFLigatures,
            copy: Object.fromEntries(
              languages.map((language) => [
                language.code,
                ogCopy(language, locales[language.code]),
              ]),
            ),
          }),
        ]
      : []),
  ];

  const input = {
    ...Object.fromEntries(
      languages.map(({ code, path }) => [
        code === defaultLanguage.code ? "main" : code,
        resolve(root, `.${path}index.html`),
      ]),
    ),
    ...Object.fromEntries(
      languages.flatMap((language) =>
        legalPages.map((page) => [
          language === defaultLanguage ? page : `${language.code}-${page}`,
          resolve(root, `.${legalPath(language, page)}index.html`),
        ]),
      ),
    ),
    notFound: resolve(root, "404.html"),
  };

  return { plugins, build: { rollupOptions: { input } } };
}

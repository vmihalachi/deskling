import type { Language } from "./types.js";

/** A legal page's path in a language: `/de/privacy/`. */
export function legalPath(language: Language, page: string) {
  return `${language.path}${page}/`;
}

/** The legal page a built HTML file is (`/de/privacy/index.html` → German privacy), if any. */
export function legalPageForPath(
  languages: readonly Language[],
  pages: readonly string[],
  pathname: string,
) {
  for (const language of languages) {
    for (const page of pages) {
      const path = legalPath(language, page);
      if (pathname === path || pathname === `${path}index.html`) return { language, page };
    }
  }
  return undefined;
}

/** hreflang → absolute URL of one page in every language, plus x-default. */
export function alternates(
  siteUrl: string,
  languages: readonly Language[],
  defaultLanguage: Language,
  pathFor: (language: Language) => string,
): [string, string][] {
  return [
    ...languages.map((language): [string, string] => [
      language.code,
      `${siteUrl}${pathFor(language)}`,
    ]),
    ["x-default", `${siteUrl}${pathFor(defaultLanguage)}`],
  ];
}

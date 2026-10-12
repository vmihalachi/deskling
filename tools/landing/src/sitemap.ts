import { alternates, legalPath } from "./legal.js";
import type { Language } from "./types.js";

/** Every page (home and legal) in every language, each with its alternates. */
export function renderSitemap(
  siteUrl: string,
  languages: readonly Language[],
  defaultLanguage: Language,
  legalPages: readonly string[],
) {
  const pages: ((language: Language) => string)[] = [
    ({ path }) => path,
    ...legalPages.map((page) => (language: Language) => legalPath(language, page)),
  ];
  const urls = pages
    .flatMap((pathFor) => {
      const links = alternates(siteUrl, languages, defaultLanguage, pathFor)
        .map(
          ([hreflang, href]) =>
            `<xhtml:link rel="alternate" hreflang="${hreflang}" href="${href}"/>`,
        )
        .join("");
      return languages.map(
        (language) => `<url><loc>${siteUrl}${pathFor(language)}</loc>${links}</url>`,
      );
    })
    .join("\n  ");
  return `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">
  ${urls}
</urlset>
`;
}

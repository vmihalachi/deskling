import type { JsonValue, Language, Locale } from "./types.js";

/** Every leaf key of a locale, dotted ("hero.title"). */
export function flattenKeys(value: JsonValue, prefix = ""): string[] {
  if (!value || typeof value !== "object" || Array.isArray(value)) return [prefix];

  return Object.entries(value).flatMap(([key, child]) =>
    flattenKeys(child, prefix ? `${prefix}.${key}` : key),
  );
}

/** The value at a dotted key, undefined when a step is missing. */
export function readPath(source: JsonValue, key: string): JsonValue | undefined {
  return key.split(".").reduce<JsonValue | undefined>((current, part) => {
    if (!current || typeof current !== "object" || Array.isArray(current)) return undefined;
    return current[part];
  }, source);
}

/**
 * Throws unless every locale has exactly the default locale's keys; `extra` adds a language's own problems (a
 * missing data file, say).
 */
export function assertSameLocaleKeys(
  locales: Record<string, Locale>,
  languages: readonly Language[],
  defaultLanguage: Language,
  extra: (language: Language) => string | undefined = () => undefined,
) {
  const baseKeys = new Set(flattenKeys(locales[defaultLanguage.code]));
  const problems = languages
    .filter((language) => language !== defaultLanguage)
    .flatMap((language) => {
      const keys = new Set(flattenKeys(locales[language.code]));
      const missing = [...baseKeys].filter((key) => !keys.has(key));
      const surplus = [...keys].filter((key) => !baseKeys.has(key));
      return [
        missing.length ? `Missing in ${language.code}.json: ${missing.join(", ")}` : "",
        surplus.length ? `Missing in ${defaultLanguage.code}.json: ${surplus.join(", ")}` : "",
        extra(language) ?? "",
      ].filter(Boolean);
    });
  if (problems.length) {
    throw new Error(["Locale files must share the same keys.", ...problems].join("\n"));
  }
}

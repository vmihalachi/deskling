/** One language the page is published in. `path` is where it's served ("/" for the default, "/de/" otherwise). */
export interface Language {
  readonly code: string;
  readonly path: string;
  /** The switcher's short label ("DE"). */
  readonly label: string;
  /** The language's name in itself ("Deutsch"). */
  readonly nativeName: string;
  /** Open Graph's locale ("de_DE"). */
  readonly ogLocale: string;
  /** More browser language tags sent here on a first visit (lower-case). */
  readonly aliases?: readonly string[];
  /** Browser language tags (prefixes) never sent anywhere on a first visit, though their base matches. */
  readonly notFor?: readonly string[];
}

/** A locale file (`src/i18n/<code>.json`): nested objects of strings. */
export type Locale = Record<string, any>;

export type JsonValue =
  | string
  | number
  | boolean
  | null
  | JsonValue[]
  | { [key: string]: JsonValue };

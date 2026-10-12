import { readFileSync, readdirSync } from "node:fs";
import { resolve } from "node:path";
import { Resvg } from "@resvg/resvg-js";
import type { Plugin } from "vite-plus";

export type OgCopy = Record<string, string>;

export interface OgOptions {
  /** Holds `og-image.svg` (the 1200×630 card, with `{{og.<key>}}` placeholders) and `fonts/*.ttf`. */
  ogDir: string;
  /** The square icon's SVG (`public/favicon.svg`). */
  iconPath: string;
  /** The card's default font family (it must be in `fonts/`). */
  fontFamily: string;
  /** Behind the padded icons (iOS and Android masks): `#RRGGBB`. */
  iconBackground: string;
  /**
   * resvg's shaper turns some faces' "fl" into a wrong glyph (Fredoka's "flower" comes out "fbwer"; HarfBuzz and
   * browsers are fine). True puts a zero-width non-joiner after every f followed by a letter.
   */
  breakFLigatures?: boolean;
  /** Each language's card text: `og-<code>.png`. */
  copy: Record<string, OgCopy>;
}

export function createRenderer(options: Omit<OgOptions, "iconPath" | "copy">) {
  const escapeXml = (value: string) => {
    const escaped = value.replace(/[<>&"']/g, (char) => `&#${char.charCodeAt(0)};`);
    return options.breakFLigatures ? escaped.replace(/f(?=[a-z])/g, "f&#8204;") : escaped;
  };
  const fontFiles = readdirSync(resolve(options.ogDir, "fonts"))
    .filter((file) => file.endsWith(".ttf"))
    .map((file) => resolve(options.ogDir, "fonts", file));

  function toPng(svg: string, width?: number) {
    return new Resvg(svg, {
      font: { fontFiles, loadSystemFonts: false, defaultFontFamily: options.fontFamily },
      fitTo: width ? { mode: "width", value: width } : { mode: "original" },
    })
      .render()
      .asPng();
  }

  /** Renders the 1200×630 social card, filling {{og.*}} placeholders from `og`. */
  function renderOgImage(og: OgCopy) {
    const template = readFileSync(resolve(options.ogDir, "og-image.svg"), "utf8");
    const svg = template.replace(/{{\s*og\.(\w+)\s*}}/g, (_placeholder, key: string) => {
      if (typeof og[key] !== "string") {
        throw new Error(`Missing og.${key} for the social preview image`);
      }
      return escapeXml(og[key]);
    });
    return toPng(svg);
  }

  /** Renders the square icon at `size`. `padded` insets it on `iconBackground` for iOS/Android masks. */
  function renderIcon(iconSvg: string, size: number, { padded = false } = {}) {
    if (!padded) return toPng(iconSvg, size);
    // Everything up to the root tag's `>` (the XML declaration with it) goes, found by index: a `[^>]*` regex over
    // the file is quadratic on a run of unclosed `<svg`.
    const open = iconSvg.indexOf("<svg");
    const close = open < 0 ? -1 : iconSvg.indexOf(">", open);
    if (close < 0) throw new Error("The icon has no <svg> tag");
    const inner =
      '<svg x="8" y="8" width="48" height="48" viewBox="0 0 64 64">' + iconSvg.slice(close + 1);
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64"><rect width="64" height="64" fill="${options.iconBackground}"/>${inner}</svg>`;
    return toPng(svg, size);
  }

  return { renderOgImage, renderIcon };
}

/** The link-preview cards and PNG icons, by file name. */
export function socialAssets(options: OgOptions) {
  const { renderOgImage, renderIcon } = createRenderer(options);
  const icon = readFileSync(options.iconPath, "utf8");
  const files = new Map<string, Buffer>([
    ["favicon-32.png", renderIcon(icon, 32)],
    ["apple-touch-icon.png", renderIcon(icon, 180, { padded: true })],
    ["icon-192.png", renderIcon(icon, 192, { padded: true })],
    ["icon-512.png", renderIcon(icon, 512, { padded: true })],
  ]);
  for (const [code, og] of Object.entries(options.copy)) {
    files.set(`og-${code}.png`, renderOgImage(og));
  }
  return files;
}

/**
 * Renders link-preview images and PNG icons from SVG sources. They are emitted into the build output and served from
 * memory by the dev server, so nothing generated is committed.
 */
export function socialAssetsPlugin(options: OgOptions): Plugin {
  let assets: Map<string, Buffer> | undefined;
  return {
    name: "deskling-landing-social-assets",
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const name = req.url?.split("?")[0]?.replace(/^\//, "");
        if (!name?.endsWith(".png")) return next();
        assets ??= socialAssets(options);
        const file = assets.get(name);
        if (!file) return next();
        res.setHeader("Content-Type", "image/png");
        res.end(file);
      });
    },
    generateBundle() {
      for (const [fileName, source] of socialAssets(options)) {
        this.emitFile({ type: "asset", fileName, source });
      }
    },
  };
}

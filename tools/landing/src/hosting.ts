import { createHash } from "node:crypto";
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";
import type { Plugin } from "vite-plus";

function htmlFiles(dir: string): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) return htmlFiles(path);
    return entry.name.endsWith(".html") ? [path] : [];
  });
}

/** CSP hashes for every inline script that runs (JSON and JSON-LD data blocks don't need one). */
export function inlineScriptHashes(outDir: string) {
  const hashes = new Set<string>();
  for (const file of htmlFiles(outDir)) {
    const html = readFileSync(file, "utf8");
    for (const [, tag, body] of html.matchAll(/<script\b([^>]*)>([\s\S]*?)<\/script>/g)) {
      if (/\bsrc=/.test(tag) || /type="application\/(ld\+)?json"/.test(tag)) continue;
      hashes.add(`'sha256-${createHash("sha256").update(body).digest("base64")}'`);
    }
  }
  return [...hashes].sort();
}

/** The `_headers` file's text for the built site in `outDir`. */
export function headersFile(
  outDir: string,
  options: { mediaSrc?: string; rules?: [path: string, headers: string[]][] } = {},
) {
  const csp = [
    "default-src 'none'",
    `script-src 'self' ${inlineScriptHashes(outDir).join(" ")}`,
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data:",
    ...(options.mediaSrc ? [`media-src ${options.mediaSrc}`] : []),
    "font-src 'self'",
    "manifest-src 'self'",
    "connect-src 'none'",
    "base-uri 'none'",
    "form-action 'none'",
    "frame-ancestors 'none'",
  ].join("; ");
  // Cloudflare merges every matching rule and joins repeated headers with commas, so a path rule first detaches
  // (`! Name`) the global header it replaces.
  const rules: [path: string, headers: string[]][] = [
    [
      "/*",
      [
        `Content-Security-Policy: ${csp}`,
        "Strict-Transport-Security: max-age=10886400; includeSubDomains; preload",
        "X-Content-Type-Options: nosniff",
        "Referrer-Policy: no-referrer",
        "Permissions-Policy: camera=(), microphone=(), geolocation=(), browsing-topics=()",
        // Deploys replace every hashed asset, so HTML must always be revalidated.
        "Cache-Control: no-cache",
      ],
    ],
    ["/assets/*", ["! Cache-Control", "Cache-Control: public, max-age=31536000, immutable"]],
    ...(options.rules ?? []),
  ];
  return (
    rules
      .map(([path, headers]) => [path, ...headers.map((h) => `  ${h}`)].join("\n"))
      .join("\n\n") + "\n"
  );
}

/**
 * Writes the Cloudflare `_headers` file after the build, so the CSP can hash the final inline scripts.
 * `connect-src 'none'` backs up the page's own claim: it fetches nothing but its own files.
 */
export function hostingConfigPlugin(
  options: { mediaSrc?: string; rules?: [path: string, headers: string[]][] } = {},
): Plugin {
  let outDir = "dist";
  return {
    name: "deskling-landing-hosting",
    apply: "build",
    configResolved(config) {
      outDir = resolve(config.root, config.build.outDir);
    },
    writeBundle() {
      writeFileSync(join(outDir, "_headers"), headersFile(outDir, options));
    },
  };
}

---
name: release
description: Use when cutting a Deskling release (vX.Y.Z) or bumping the version. Lists the five places the version lives, the CHANGELOG move, the checks, and how the tag triggers release.yml.
---

# Release vX.Y.Z

One tag versions the Swift package and every NuGet package. Pick the number by SemVer from *Unreleased*
(while `0.x`: breaking public API → minor, otherwise patch).

## 1. Bump the version everywhere (one commit)

| File | What |
|---|---|
| `Sources/DesklingCore/Deskling.swift` | `public static let version = "X.Y.Z"` |
| `dotnet/Deskling.Core/DesklingInfo.cs` | `public const string Version = "X.Y.Z"` |
| `dotnet/Directory.Build.props` | the `<DesklingVersion Condition=…>` default |
| `site/index.html` | the `vX.Y.Z` label in the header and **both** `exact: "X.Y.Z"` install snippets |
| `README.md` | the `exact: "X.Y.Z"` install pin and the `git tag vX.Y.Z` example under *Releases* |

## 2. CHANGELOG.md

Rename the `## Unreleased` content into `## X.Y.Z - YYYY-MM-DD` (today's date) and leave an empty `## Unreleased`
above it. `release.yml` requires a `## X.Y.Z ` heading and uses that section as the release notes.

## 3. Check

```sh
scripts/verify.sh --all   # fails if any of the eight version strings disagree
```

Commit as `X.Y.Z: <headline change>` (the history's style), then merge to `master`.

## 4. Tag

Ask the user before tagging: it publishes to NuGet, stages `@deskling/landing` on npm if it changed, and tags are
protected (they never move).

- `git tag -a vX.Y.Z -m "Deskling X.Y.Z" && git push origin vX.Y.Z`, or
- without tag-push rights: dispatch `release.yml` with `version: X.Y.Z`, which tags `master` itself.

`release.yml` tests, packs the six NuGet packages, pushes them through Trusted Publishing, stages `@deskling/landing` on
npm and drafts the GitHub release. Publishing the draft, and approving the staged npm version with 2FA (npmjs.com, or
`npm stage approve <id>`), are the user's.

The NuGet packages ship at every tag, changed or not: an app pins one Deskling version everywhere (`companion.py
pins`). `@deskling/landing` ships only when `tools/landing` changed since the version on npm (the run's summary says
which), so its versions skip the releases that leave it alone; nothing waits for approval on those. A breaking change
to it still counts toward the release number (a minor bump while `0.x`).

## 5. Move the apps

Once NuGet lists the packages (`release.yml`'s run is green; nuget.org indexes them a few minutes later), from this
checkout:

```sh
tools/bump-apps.sh X.Y.Z   # companion.py bump in every app next to this checkout, then its verify.sh --fast
```

It commits nothing: read each app's diff, build what the release touched, and commit there (the landing page's
`@deskling/landing` change inside its own repository first). A change that spans Deskling and the apps is tried before
the tag with `companion.py link` in each app (`tools/README.md`, *Moving to a release, or trying one first*), so one
release covers the batch.

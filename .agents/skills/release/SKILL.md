---
name: release
description: Use when cutting a Deskling release (vX.Y.Z) or bumping the version. Lists the four places the version lives, the CHANGELOG move, the checks, and how the tag triggers release.yml.
---

# Release vX.Y.Z

One tag versions the Swift package and both NuGet packages. Pick the number by SemVer from *Unreleased*
(while `0.x`: breaking public API → minor, otherwise patch).

## 1. Bump the version everywhere (one commit)

| File | What |
|---|---|
| `Sources/DesklingCore/Deskling.swift` | `public static let version = "X.Y.Z"` |
| `dotnet/Deskling.Core/DesklingInfo.cs` | `public const string Version = "X.Y.Z"` |
| `dotnet/Directory.Build.props` | the `<DesklingVersion Condition=…>` default |
| `site/index.html` | the `vX.Y.Z` label in the header and **both** `exact: "X.Y.Z"` install snippets |

## 2. CHANGELOG.md

Rename the `## Unreleased` content into `## X.Y.Z - YYYY-MM-DD` (today's date) and leave an empty `## Unreleased`
above it. `release.yml` requires a `## X.Y.Z ` heading and uses that section as the release notes.

## 3. Check

```sh
scripts/verify.sh --all   # fails if any of the six version strings disagree
```

Commit as `X.Y.Z: <headline change>` (the history's style), then merge to `master`.

## 4. Tag

Ask the user before tagging: it publishes to NuGet and tags are protected (they never move).

- `git tag -a vX.Y.Z -m "Deskling X.Y.Z" && git push origin vX.Y.Z`, or
- without tag-push rights: dispatch `release.yml` with `version: X.Y.Z`, which tags `master` itself.

`release.yml` tests, packs both NuGet packages, pushes them through Trusted Publishing and drafts the GitHub release.
Publishing the draft is the user's call.

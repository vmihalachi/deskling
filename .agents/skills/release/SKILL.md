---
name: release
description: Use when a Deskling release (vX.Y.Z) is wanted sooner or differently than release.yml makes it on its own, when bumping the version by hand, or when moving the apps to a release. Releases are automatic: every push to master with CHANGELOG.md entries ships. Lists what scripts/release.sh does, the checks, and the manual ways.
---

# Releases

One tag versions the Swift package and every NuGet package, and **`release.yml` makes it**: every push to `master` that
touches `CHANGELOG.md` or the version runs `scripts/release.sh plan`, which picks one of

- **tag X.Y.Z**: the version strings name a version with no tag yet (a change bumped it by hand): it tags the commit
  that bumped it;
- **bump X.Y.Z**: entries under `## Unreleased`: the next patch, or the next minor while `0.x` (major after) when an
  entry says `**Breaking.**`. `scripts/release.sh bump X.Y.Z` moves the entries under `## X.Y.Z - <today>` and puts the
  number in the ten version strings; the run commits that to `master` as `X.Y.Z: release` and tags it;
- **none**.

So a change only writes its CHANGELOG entry under *Unreleased* (marking a breaking one `**Breaking.**`) and merges; it
never bumps the version or tags. Merges that land while a release runs go out together in the next one (one run at a
time, each on `master` as it is when it starts). The run then tests, packs the six NuGet packages, pushes them through
Trusted Publishing, stages `@deskling/landing` on npm when it changed, makes the GitHub release, and redeploys the site
after a bump. Approving the staged npm version with 2FA (npmjs.com, or `npm stage approve <id>`) is the user's.

`scripts/verify.sh --version X.Y.Z` gates every release before anything is tagged or packed: the ten version strings
must be `X.Y.Z` and `CHANGELOG.md` must have its `## X.Y.Z` section. Tags are protected and never move: a tag that fails
is spent, so fix the commit and release the next number.

## The version strings

| File | What |
|---|---|
| `Sources/DesklingCore/Deskling.swift` | `public static let version = "X.Y.Z"` |
| `dotnet/Deskling.Core/DesklingInfo.cs` | `public const string Version = "X.Y.Z"` |
| `dotnet/Directory.Build.props` | the `<DesklingVersion Condition=…>` default |
| `site/index.html` | the `vX.Y.Z` label in the header and **both** `exact: "X.Y.Z"` install snippets |
| `README.md` | the `exact: "X.Y.Z"` install pin and the `git tag vX.Y.Z` example under *Releases* |

## By hand

Only when the automatic number is wrong (a minor without a breaking entry, say) or a release must not wait:

- Bump in the change itself: `scripts/release.sh bump X.Y.Z`, `scripts/verify.sh --version X.Y.Z`, commit as
  `X.Y.Z: <headline change>`. When it reaches `master` the run tags that commit (*tag X.Y.Z* above).
- Or tag a commit whose strings and CHANGELOG already say `X.Y.Z`: `git tag -a vX.Y.Z -m "Deskling X.Y.Z" && git push
  origin vX.Y.Z`, or dispatch `release.yml` with `version: X.Y.Z` (it tags the commit it runs on). Dispatching it with
  no version does what a push does.

The NuGet packages ship at every release, changed or not: an app pins one Deskling version everywhere (`companion.py
pins`). `@deskling/landing` ships only when `tools/landing` changed since the version on npm (the run's summary says
which), so its versions skip the releases that leave it alone. A breaking change to it still counts toward the release
number (a minor bump while `0.x`).

## Move the apps

Once NuGet lists the packages (`release.yml`'s run is green; nuget.org indexes them a few minutes later), from this
checkout:

```sh
tools/bump-apps.sh X.Y.Z   # companion.py bump in every app next to this checkout, then its verify.sh --fast
```

It commits nothing: read each app's diff, build what the release touched, and commit there (the landing page's
`@deskling/landing` change inside its own repository first). A change that spans Deskling and the apps is tried before
the release with `companion.py link` in each app (`tools/README.md`, *Moving to a release, or trying one first*).

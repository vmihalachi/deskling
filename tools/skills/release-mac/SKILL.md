---
name: release-mac
description: Ship a new build of a Laching companion's Mac app to App Store Connect (bump CFBundleVersion, verify, archive, upload, attach to the version, tag). Use when asked to publish, release, upload or submit a new macOS version or build, including a new build for a version already in review.
---

# Release the Mac app (App Store)

Names come from `companion.json` (`mac.dir`, `mac.project`, `mac.scheme`, `mac.projectSpec`); the team ID, product
ids, the App Store Connect record and where release notes are kept are in the app's `.agents/notes/release-mac.md`.

## 1. Decide the version

- **New build of the same version** (update while in review or rejected): keep `CFBundleShortVersionString`, raise
  `CFBundleVersion` by 1.
- **New version** (after approval): bump `CFBundleShortVersionString` too; `CFBundleVersion` only goes up.
- Both live in the XcodeGen spec. Never hand-edit `project.pbxproj`.

## 2. Preflight (don't upload a build that fails these)

```sh
scripts/verify.sh --all          # includes the Laching pin check (submodule = SwiftPM = XcodeGen = NuGet)
laching/tools/conformance.sh && git diff --exit-code <conformance.dir>   # vectors current
python3 laching/tools/l10n.py check
python3 laching/tools/voice.py check
```

Also confirm: every user-visible Mac change is ported to Windows (or says why not), README, landing and store-listing
claims match what ships, and purchase UI is only where the app's rules allow.

## 3. Bump, commit, archive, upload

1. Edit the version in the XcodeGen spec, run `xcodegen generate` in `mac.dir`, commit the spec and the regenerated
   project with the work (message like `Release 1.0 (4)`).
2. Archive (Release signs automatically with the team in the notes; Debug stays ad-hoc):
   ```sh
   cd <mac.dir>
   xcodebuild archive -project <mac.project> -scheme <mac.scheme> -configuration Release \
     -destination 'generic/platform=macOS' -archivePath build/release.noindex/<mac.scheme>.xcarchive -allowProvisioningUpdates
   ```
3. Upload with an export-options plist (method `app-store-connect`, destination `upload`, the team ID, signingStyle
   `automatic`):
   ```sh
   xcodebuild -exportArchive -archivePath build/release.noindex/<mac.scheme>.xcarchive \
     -exportOptionsPlist build/release.noindex/ExportOptions.plist -exportPath build/release.noindex/out -allowProvisioningUpdates
   ```
   This needs the owner's Apple ID signed in to Xcode. If it fails on auth, stop and ask; never handle credentials.

## 4. App Store Connect (the owner clicks; guide them)

Wait for the build to process, then: Version page → **Build** → select it; update **What's New** and review notes; the
in-app purchases must be in the submission and Ready to Submit; release stays **Manual**; submit. Smoke-test the
processed build in TestFlight, especially purchases and prices.

## 5. Wrap up

- Tag `mac-<version>-<build>` on the release commit; push commits and tags only when the owner says so.
- Record the build number and review status where the notes say.

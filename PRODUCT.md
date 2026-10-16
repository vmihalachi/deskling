# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Stack

The project site: hand-written static HTML/CSS in `site/`, deployed to GitHub Pages by a `pages.yml` workflow. No build
tools and no dependencies, matching the package's own no-dependency rule.

## Users

Indie developers building a small desktop app that lives in the macOS menu bar or the Windows tray, in Swift or .NET,
evaluating whether to adopt Deskling instead of writing the same plumbing again, and those building a desk companion
(a character that talks on a cadence and knows when to be quiet). Secondary: the maintainers of mybackhurts,
sunnysays and Mister Pagliacci, who use it as a reference.

## Product Purpose

Deskling is an MIT Swift package plus .NET packages with the generic parts every menu bar / tray app needs: a pure
reminder scheduler, permission-free system signals, app-shell plumbing, a purchase store (StoreKit 2 and the Microsoft
Store) and, since 0.5.0, the companion layer: phrases, cadence, quiet rules, settings, skins, character rigs, speech
and the tooling a companion app needs. Success for the site: a
visitor understands what is inside, trusts the permission-free and cross-platform promises, and copies the install line.

## Positioning

- Call detection (camera or microphone in use by another app) read from device state, never by opening a device, so it
  never prompts for a permission. Nothing in it touches the network.
- One behavior on two platforms: the Swift code is the reference, JSON conformance vectors are generated from it and the
  C# port replays every one.
- Extracted from two real apps (mybackhurts, which ships it, and sunnysays, in development), not designed in the
  abstract. The companion layer is sunnysays' character skeleton made generic so a second companion app, Mister
  Pagliacci (in design), can be built on it.

## Capabilities and Constraints

Products: `DesklingCore` / `Deskling.Core` (scheduler, clock/idle/busy protocols, SplitMix64),
`DesklingCompanion` / `Deskling.Companion` (a desk companion's phrases, cadence, quiet rules, settings, coordinator,
desktop presence, skins and vector or raster character rigs), `DesklingSystem` / `Deskling.Windows` (idle, busy, full-screen, power, audio output, screen lock, appearance),
`DesklingShell` / `Deskling.Windows` (window manager, login item, notifications, tray icon, single
instance, drawing helpers; on the Mac also the status item, speech and chimes), `DesklingCompanionUI` (macOS: the
companion's views, desktop presence and delivery), `DesklingStore` / `Deskling.Core` `Store/` + `Deskling.Windows` (one-time unlockables and tips: StoreKit 2 on the Mac,
the Microsoft Store on Windows, both since 0.3.0), `DesklingTesting` / `Deskling.Testing` (fakes, fixtures, conformance generators and replays), and `tools/` (Python
tools, scripts and agent skills a companion app vendors as a submodule).
macOS 14+ and Windows 11 (.NET 10); desktop only. Version 0.x: minor versions may change public types. The package ships no strings.

## Brand Commitments

Name: Deskling. Voice: plain, precise, dry, first-hand ("the parts every such app needs and nobody wants to write
twice"). No logo exists yet.

## Evidence on Hand

README, CHANGELOG (every release since 0.1.0), the scheduler conformance vectors in `conformance/scheduler/` and the
companion vectors in `conformance/companion/`, mybackhurts.app, sunnysays.app, pagliacci.app.
No testimonials, download counts, benchmarks or third-party adopters: never invent them.

## Product Principles

- Never prompt, never phone home.
- Behavior is specified once and proven on both platforms.
- The app owns its words; the package owns the plumbing.

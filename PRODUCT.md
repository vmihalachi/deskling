# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Stack

The project site: hand-written static HTML/CSS in `site/`, deployed to GitHub Pages by a `pages.yml` workflow. No build
tools and no dependencies, matching the package's own no-dependency rule.

## Users

Indie developers building a small desktop app that lives in the macOS menu bar or the Windows tray, in Swift or .NET,
evaluating whether to adopt Deskling instead of writing the same plumbing again. Secondary: the maintainers of
mybackhurts and sunnysays, who use it as a reference.

## Product Purpose

Deskling is an MIT Swift package plus .NET packages with the generic parts every menu bar / tray app needs: a pure
reminder scheduler, permission-free system signals, app-shell plumbing and a StoreKit store. Success for the site: a
visitor understands what is inside, trusts the permission-free and cross-platform promises, and copies the install line.

## Positioning

- Call detection (camera or microphone in use by another app) read from device state, never by opening a device, so it
  never prompts for a permission. Nothing in it touches the network.
- One behavior on two platforms: the Swift code is the reference, JSON conformance vectors are generated from it and the
  C# port replays every one.
- Extracted from two shipping apps (mybackhurts, sunnysays), not designed in the abstract.

## Capabilities and Constraints

Products: `DesklingCore` / `Deskling.Core` (scheduler, clock/idle/busy protocols, SplitMix64, KeyShortcut),
`DesklingSystem` / `Deskling.Windows` (idle, busy, full-screen, power, audio output, screen lock, appearance),
`DesklingShell` / `Deskling.Windows` (window manager, login item, notifications, global hotkey, tray icon, single
instance, drawing helpers), `DesklingStore` (StoreKit 2, macOS only), `DesklingTesting` (fakes, conformance helpers).
macOS 14+, .NET 10. Version 0.x: minor versions may change public types. The package ships no strings.

## Brand Commitments

Name: Deskling. Voice: plain, precise, dry, first-hand ("the parts every such app needs and nobody wants to write
twice"). No logo exists yet.

## Evidence on Hand

README, CHANGELOG (0.1.0, 0.1.1), the 37 scheduler conformance vectors in `conformance/scheduler/`, mybackhurts.app.
No testimonials, download counts, benchmarks or third-party adopters: never invent them.

## Product Principles

- Never prompt, never phone home.
- Behavior is specified once and proven on both platforms.
- The app owns its words; the package owns the plumbing.

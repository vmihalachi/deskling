// swift-tools-version: 6.0
import PackageDescription

// Deskling: building blocks for little desktop apps that live in the menu bar.
//
//   DesklingCore     Foundation only. Interval scheduler with active hours and weekdays, quiet hours, idle reset and
//                    busy hold; the Clock / IdleTimeProvider / BusyStateProvider protocols; a seeded random source;
//                    LocalizedKey; the typed SettingsStore. Builds and tests anywhere Swift runs.
//   DesklingSystem   The macOS implementations of those providers and the other system signals (camera or microphone
//                    in use, full screen, power source, output mute and route, screen lock, appearance), read from
//                    device state through CoreAudio, CoreMediaIO, CoreGraphics and IOKit, all without permission prompts.
//   DesklingShell    Menu bar app plumbing: window manager with the activation-policy dance, the floating desktop
//                    window and its placement, login item, launch context, notification poster, font registration,
//                    a layout check, drawing helpers, the status item, and speech (system voice or recorded clips,
//                    jokes in beats) and chimes.
//   DesklingCompanion  Foundation and Observation only. The pure core of a desk companion: the phrase bank and
//                    picker, chatter cadence, quiet rules and mood, settings, the coordinator, desktop presence,
//                    skins and the character rig. Builds and tests anywhere Swift runs.
//   DesklingCompanionUI  macOS. The companion's views and shell: signals, the menu bar glyph, the character view
//                    (vector and image parts), the desktop presence, delivery through speech, chime and notification,
//                    the speech bubble and status pill over an app palette, screenshot capture, and the app shell an
//                    app subclasses (app model, menus, Settings rows, the Support window).
//   DesklingStore    StoreKit 2 purchase store for unlockables and tips, caching ownership in a SettingsStore.
//   DesklingTesting  Fakes for every protocol, fixture data and conformance-vector helpers, for the apps' test targets.
let package = Package(
    name: "Deskling",
    platforms: [.macOS(.v14)],
    products: [
        .library(name: "DesklingCore", targets: ["DesklingCore"]),
        .library(name: "DesklingSystem", targets: ["DesklingSystem"]),
        .library(name: "DesklingShell", targets: ["DesklingShell"]),
        .library(name: "DesklingCompanion", targets: ["DesklingCompanion"]),
        .library(name: "DesklingCompanionUI", targets: ["DesklingCompanionUI"]),
        .library(name: "DesklingStore", targets: ["DesklingStore"]),
        .library(name: "DesklingTesting", targets: ["DesklingTesting"]),
    ],
    targets: [
        .target(name: "DesklingCore"),
        .target(name: "DesklingSystem", dependencies: ["DesklingCore"]),
        .target(name: "DesklingShell", dependencies: ["DesklingCore"]),
        .target(name: "DesklingCompanion", dependencies: ["DesklingCore"]),
        .target(
            name: "DesklingCompanionUI",
            dependencies: ["DesklingCompanion", "DesklingCore", "DesklingSystem", "DesklingShell", "DesklingStore"]),
        .target(name: "DesklingStore", dependencies: ["DesklingCore"]),
        .target(name: "DesklingTesting", dependencies: ["DesklingCore", "DesklingStore", "DesklingCompanion"]),
        .testTarget(name: "DesklingCoreTests", dependencies: ["DesklingCore", "DesklingTesting"]),
        .testTarget(
            name: "DesklingCompanionTests", dependencies: ["DesklingCompanion", "DesklingCore", "DesklingTesting"],
            resources: [.copy("Resources")]),
        .testTarget(
            name: "DesklingTestingTests", dependencies: ["DesklingTesting", "DesklingCore", "DesklingCompanion", "DesklingStore"]),
        // The ones below are macOS-only (every file is wrapped in `#if os(macOS)`), so they build empty on Linux.
        .testTarget(name: "DesklingSystemTests", dependencies: ["DesklingSystem", "DesklingCore"]),
        .testTarget(name: "DesklingShellTests", dependencies: ["DesklingShell", "DesklingCore"], resources: [.copy("Resources")]),
        .testTarget(name: "DesklingStoreTests", dependencies: ["DesklingStore", "DesklingTesting"]),
        .testTarget(
            name: "DesklingCompanionUITests",
            dependencies: ["DesklingCompanionUI", "DesklingCompanion", "DesklingShell", "DesklingCore", "DesklingTesting"],
            // Processed, so the PNGs sit at the bundle's top level where `Image(_:bundle:)` finds them.
            resources: [.process("Resources")]),
    ]
)

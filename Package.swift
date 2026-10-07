// swift-tools-version: 5.10
import PackageDescription

// Deskling: building blocks for little desktop apps that live in the menu bar.
//
//   DesklingCore     Foundation only. Interval scheduler with active hours, quiet hours, idle reset and
//                    busy hold; the Clock / IdleTimeProvider / BusyStateProvider protocols; a seeded
//                    random source; KeyShortcut. Builds and tests anywhere Swift runs.
//   DesklingSystem   The macOS implementations of those providers (CoreAudio, AVFoundation,
//                    CoreGraphics, IOKit), all without permission prompts.
//   DesklingShell    Menu bar app plumbing: window manager with the activation-policy dance, login item,
//                    launch context, notification poster, global hotkey, drawing helpers.
//   DesklingStore    StoreKit 2 purchase store for unlockables and tips.
//   DesklingTesting  Fakes for every protocol plus conformance-vector helpers, for the apps' test targets.
let package = Package(
    name: "Deskling",
    platforms: [.macOS(.v14)],
    products: [
        .library(name: "DesklingCore", targets: ["DesklingCore"]),
        .library(name: "DesklingSystem", targets: ["DesklingSystem"]),
        .library(name: "DesklingShell", targets: ["DesklingShell"]),
        .library(name: "DesklingStore", targets: ["DesklingStore"]),
        .library(name: "DesklingTesting", targets: ["DesklingTesting"]),
    ],
    targets: [
        .target(name: "DesklingCore"),
        .target(name: "DesklingSystem", dependencies: ["DesklingCore"]),
        .target(name: "DesklingShell", dependencies: ["DesklingCore"]),
        .target(name: "DesklingStore"),
        .target(name: "DesklingTesting", dependencies: ["DesklingCore", "DesklingStore"]),
        .testTarget(name: "DesklingCoreTests", dependencies: ["DesklingCore", "DesklingTesting"]),
    ]
)

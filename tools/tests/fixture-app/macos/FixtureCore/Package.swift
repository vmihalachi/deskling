// swift-tools-version: 6.0
import PackageDescription

// The tools only read the `exact:` pin. No targets: there are no sources here, and CodeQL's Swift autobuild runs
// `swift build` on every Package.swift in the repo, which fails on a target without sources.
let package = Package(
    name: "FixtureCore",
    platforms: [.macOS(.v14)],
    dependencies: [
        .package(url: "https://github.com/vmihalachi/deskling", exact: "0.5.0")
    ]
)

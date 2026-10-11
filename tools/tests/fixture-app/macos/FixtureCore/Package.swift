// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "FixtureCore",
    platforms: [.macOS(.v14)],
    products: [.library(name: "FixtureCore", targets: ["FixtureCore"])],
    dependencies: [
        .package(url: "https://github.com/vmihalachi/deskling", exact: "0.5.0")
    ],
    targets: [.target(name: "FixtureCore", dependencies: [.product(name: "DesklingCompanion", package: "deskling")])]
)

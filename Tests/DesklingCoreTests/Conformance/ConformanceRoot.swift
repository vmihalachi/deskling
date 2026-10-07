import Foundation

/// The repo's `conformance/` folder, found by walking up from this source file. SwiftPM tests aren't
/// sandboxed, so the generator writes there directly (`DESKLING_WRITE_CONFORMANCE=1 swift test --filter Conformance`).
enum ConformanceRoot {
    static let url: URL = {
        var folder = URL(fileURLWithPath: #filePath).deletingLastPathComponent()
        // The marker is conformance/README.md, not the folder: on a case-insensitive file system the test
        // target's own Conformance/ folder would otherwise match.
        while folder.path != "/" {
            let candidate = folder.appendingPathComponent("conformance", isDirectory: true)
            if FileManager.default.fileExists(atPath: candidate.appendingPathComponent("README.md").path) {
                return candidate
            }
            folder.deleteLastPathComponent()
        }
        preconditionFailure("conformance/ not found above \(#filePath)")
    }()
}

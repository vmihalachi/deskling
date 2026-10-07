import Foundation

/// The repo's `conformance/` folder, found by walking up from this source file. SwiftPM tests aren't
/// sandboxed, so the generator writes there directly (`DESKLING_WRITE_CONFORMANCE=1 swift test --filter Conformance`).
enum ConformanceRoot {
    static let url: URL = {
        var folder = URL(fileURLWithPath: #filePath).deletingLastPathComponent()
        while folder.path != "/" {
            let candidate = folder.appendingPathComponent("conformance", isDirectory: true)
            if FileManager.default.fileExists(atPath: candidate.path) {
                return candidate
            }
            folder.deleteLastPathComponent()
        }
        preconditionFailure("conformance/ not found above \(#filePath)")
    }()
}

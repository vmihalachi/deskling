import Foundation

/// Finds a repo's vectors folder by walking up from a source file. SwiftPM tests aren't sandboxed, so generators
/// read and write there directly.
public enum ConformanceRoot {
    /// The folder holding `markerFile` (a path such as "conformance/README.md"), searched for from the folder of
    /// `filePath` (pass `#filePath`) up to the root; nil when no ancestor has it. A marker file rather than a folder
    /// name, because on a case-insensitive file system a test target's own `Conformance/` folder would match.
    public static func find(markerFile: String, from filePath: String) -> URL? {
        var folder = URL(fileURLWithPath: filePath).deletingLastPathComponent()
        while folder.path != "/" {
            let marker = folder.appendingPathComponent(markerFile)
            if FileManager.default.fileExists(atPath: marker.path) { return marker.deletingLastPathComponent() }
            folder.deleteLastPathComponent()
        }
        return nil
    }
}

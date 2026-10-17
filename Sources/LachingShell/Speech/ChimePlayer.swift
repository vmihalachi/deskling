#if os(macOS)
    import AVFoundation
    import Foundation

    /// What an app needs from its chime, so tests can play nothing.
    @MainActor
    public protocol ChimePlaying: AnyObject {
        /// Plays the sound `<sound>.wav`; `completion` runs once it ended (or right away if it can't play), so a blip
        /// can precede a spoken line.
        func play(_ sound: String, volume: Double, completion: (() -> Void)?)
        func stop()
    }

    /// Plays the app's short sounds (`<sound>.wav` in `bundle`), several at once if asked. Asks for no permission.
    @MainActor
    public final class ChimePlayer: NSObject, ChimePlaying, AVAudioPlayerDelegate {
        private struct Pending {
            let player: AVAudioPlayer
            let completion: (() -> Void)?
        }

        private let bundle: Bundle
        private var pending: [ObjectIdentifier: Pending] = [:]

        public init(bundle: Bundle = .main) {
            self.bundle = bundle
            super.init()
        }

        public func play(_ sound: String, volume: Double, completion: (() -> Void)?) {
            guard let url = bundle.url(forResource: sound, withExtension: "wav"),
                let player = try? AVAudioPlayer(contentsOf: url)
            else {
                completion?()
                return
            }
            player.volume = Float(min(max(volume, 0), 1))
            player.delegate = self
            let id = ObjectIdentifier(player)
            pending[id] = Pending(player: player, completion: completion)
            if !player.play() {
                pending[id] = nil
                completion?()
            }
        }

        public func stop() {
            for entry in pending.values { entry.player.stop() }
            pending = [:]
        }

        public nonisolated func audioPlayerDidFinishPlaying(_ player: AVAudioPlayer, successfully flag: Bool) {
            let id = ObjectIdentifier(player)
            DispatchQueue.main.async {
                MainActor.assumeIsolated { self.finished(id) }
            }
        }

        private func finished(_ id: ObjectIdentifier) {
            guard let entry = pending.removeValue(forKey: id) else { return }
            entry.completion?()
        }
    }
#endif

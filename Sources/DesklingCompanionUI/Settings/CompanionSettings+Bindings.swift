#if os(macOS)
    import DesklingCompanion
    import SwiftUI

    @MainActor
    extension CompanionSettings {
        /// Whether `channel` is on; turning it on or off goes through `set` (the model's `setChannel`, which asks for
        /// the notification permission when that channel comes on).
        public func binding(for channel: Channels, set: @escaping (Channels, Bool) -> Void) -> Binding<Bool> {
            Binding(get: { self.channels.contains(channel) }, set: { on in set(channel, on) })
        }

        /// Whether lines of `flavor` are picked.
        public func binding(for flavor: FlavorID) -> Binding<Bool> {
            Binding(
                get: { self.phraseFlavors.contains(flavor) },
                set: { on in
                    if on { self.phraseFlavors.insert(flavor) } else { self.phraseFlavors.remove(flavor) }
                })
        }

        /// The muted lines still in `bank` (a retired line no longer counts).
        public func mutedCount(in bank: PhraseBank) -> Int {
            mutedPhraseIDs.filter { bank[$0] != nil }.count
        }
    }
#endif

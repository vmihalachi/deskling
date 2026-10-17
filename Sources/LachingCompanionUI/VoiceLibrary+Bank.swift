#if os(macOS)
    import LachingCompanion
    import LachingShell

    extension VoiceLibrary {
        /// The bank's phrases in this language that `voice` can't say yet: a line whose clip is missing or of older
        /// text, a joke with any such part. `text` gives the words the user sees for a catalog key (the clips record the
        /// translation). Tunes are sounds and always fine; none with `fallbackToSystemVoice`.
        public func pendingIDs(
            in bank: PhraseBank, voice: VoiceID, text: (_ catalogKey: String, _ source: String) -> String
        ) -> Set<String> {
            var pending: Set<String> = []
            for phrase in bank.phrases(in: languageCode) {
                let sources: [String]
                switch phrase.kind {
                case .line(let source): sources = [source]
                case .sequence(let parts): sources = parts
                case .tune: continue
                }
                let clips = zip(zip(phrase.clipIDs, phrase.catalogKeys), sources).map { (id: $0.0, text: text($0.1, $1)) }
                if !pendingClipIDs(clips, voice: voice).isEmpty { pending.insert(phrase.id) }
            }
            return pending
        }
    }
#endif

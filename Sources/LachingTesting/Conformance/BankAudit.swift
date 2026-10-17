import Foundation
import LachingCompanion

/// Checks an app's phrase bank has enough lines for the picker in every language and skin, so no moment falls back
/// to silence. Tunes don't count (they need the chime channel).
public enum BankAudit {
    /// One message per (language, skin, tag) with fewer than `minimumPerTag` lines or sequences, e.g.
    /// "it / fixture-raster: monday has 0 (needs 1)"; empty when the bank is deep enough.
    public static func floors(bank: PhraseBank, languages: [String], skins: [SkinID], minimumPerTag: Int) -> [String] {
        var problems: [String] = []
        for language in languages {
            for skin in skins {
                let usable = bank.phrases.filter { !$0.isTune && $0.fits(language: language) && $0.fits(skin) }
                for tag in Tag.allCases {
                    let count = usable.filter { $0.tags.contains(tag) }.count
                    if count < minimumPerTag {
                        problems.append("\(language) / \(skin.rawValue): \(tag.rawValue) has \(count) (needs \(minimumPerTag))")
                    }
                }
            }
        }
        return problems
    }
}

import LachingCompanion
import Foundation
import XCTest

final class PhraseTests: XCTestCase {
    func testALineHasOneKeyAndOneClip() {
        let line = Phrase("hello", .line(source: "Hello"), tags: [.general])
        XCTAssertEqual(line.partCount, 1)
        XCTAssertEqual(line.catalogKey, "phrase.hello")
        XCTAssertEqual(line.catalogKeys, ["phrase.hello"])
        XCTAssertEqual(line.clipIDs, ["hello"])
        XCTAssertFalse(line.isTune)
    }

    // A weight under 1 is a precondition failure, which XCTest cannot catch; the C# twin asserts the throw.
    func testWeightIsNormalByDefaultAndStartsAtOne() {
        XCTAssertEqual(Phrase("hello", .line(source: "Hello"), tags: [.general]).weight, 3)
        XCTAssertEqual(Phrase("rare", .line(source: "Rarely"), tags: [.general], weight: 1).weight, 1)
    }

    func testASequenceHasAKeyAndAClipPerPart() {
        let joke = Phrase("joke", .sequence(source: ["Setup", "Middle", "Punchline"]), tags: [.general])
        XCTAssertEqual(joke.partCount, 3)
        XCTAssertEqual(joke.catalogKey, "phrase.joke")
        XCTAssertEqual(joke.catalogKeys, ["phrase.joke.1", "phrase.joke.2", "phrase.joke.3"])
        XCTAssertEqual(joke.clipIDs, ["joke.1", "joke.2", "joke.3"])
        XCTAssertFalse(joke.isTune)
    }

    func testATuneHasNothingToSay() {
        let tune = Phrase("tune", .tune(sound: "tune-1"), tags: [.general])
        XCTAssertEqual(tune.partCount, 1)
        XCTAssertEqual(tune.catalogKeys, [])
        XCTAssertEqual(tune.clipIDs, [])
        XCTAssertTrue(tune.isTune)
    }

    func testLanguagesCompareByTheirBase() {
        let everywhere = Phrase("any", .line(source: "Any"), tags: [.general])
        let italian = Phrase("it", .line(source: "Ciao"), tags: [.general], languages: ["it"])
        for language in ["en", "it", "pt-BR"] {
            XCTAssertTrue(everywhere.fits(language: language), language)
        }
        XCTAssertTrue(italian.fits(language: "it"))
        XCTAssertTrue(italian.fits(language: "it-CH"))
        XCTAssertTrue(italian.fits(language: "IT_it"))
        XCTAssertFalse(italian.fits(language: "en"))
        XCTAssertFalse(italian.fits(language: "id"))
        XCTAssertEqual(Phrase.baseLanguage("pt-BR"), "pt")
        XCTAssertEqual(Phrase.baseLanguage("zh_Hans_CN"), "zh")
        XCTAssertEqual(Phrase.baseLanguage("en"), "en")
    }

    func testSkins() {
        let mine = Phrase("mine", .line(source: "Mine"), tags: [.general], skins: ["one"])
        XCTAssertTrue(mine.fits("one"))
        XCTAssertFalse(mine.fits("two"))
        XCTAssertTrue(Phrase("any", .line(source: "Any"), tags: [.general]).fits("two"))
    }

    func testIDsAreCodedAsPlainStrings() throws {
        let data = try JSONEncoder().encode([FlavorID("a")])
        XCTAssertEqual(String(decoding: data, as: UTF8.self), #"["a"]"#)
        XCTAssertEqual(try JSONDecoder().decode([SkinID].self, from: Data(#"["fixture-raster"]"#.utf8)), ["fixture-raster"])
    }
}

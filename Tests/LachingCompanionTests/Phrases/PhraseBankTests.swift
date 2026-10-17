import LachingCompanion
import LachingTesting
import XCTest

final class PhraseBankTests: XCTestCase {
    let bank = PhraseBank.fixture

    func testIdsAreUniqueAndStable() {
        let ids = bank.phrases.map(\.id)
        XCTAssertEqual(ids.count, Set(ids).count)
        for id in ids {
            XCTAssertTrue(id.allSatisfy { $0.isLowercase || $0.isNumber || $0 == "-" }, id)
        }
    }

    func testEveryPhraseHasTagsAndASaneWeight() {
        for phrase in bank.phrases {
            XCTAssertFalse(phrase.tags.isEmpty, phrase.id)
            XCTAssertTrue((1...5).contains(phrase.weight), phrase.id)
        }
    }

    func testEveryLineHasAFlavorAndTunesFitEveryone() {
        for phrase in bank.phrases {
            XCTAssertEqual(phrase.flavor == nil, phrase.isTune, phrase.id)
        }
        for tune in bank.phrases where tune.isTune {
            XCTAssertTrue(tune.skins.isEmpty, tune.id)
            XCTAssertTrue(tune.languages.isEmpty, tune.id)
        }
    }

    func testTheFixtureHasEveryMoment() {
        for tag in Tag.allCases {
            XCTAssertFalse(bank.phrases(tagged: tag).isEmpty, tag.rawValue)
        }
        XCTAssertEqual(bank.phrases(tagged: .welcomeBack).count, 2)
    }

    func testCatalogKeys() {
        XCTAssertEqual(bank["general-a1"]?.catalogKey, "phrase.general-a1")
        XCTAssertEqual(bank["joke-1"]?.catalogKeys, ["phrase.joke-1.1", "phrase.joke-1.2"])
        XCTAssertNil(bank["nope"])
    }

    func testSkinAndLanguageViews() {
        XCTAssertFalse(bank.phrases(for: FixtureIDs.vectorSkin).contains { $0.id == "skin-raster-only" })
        XCTAssertTrue(bank.phrases(for: FixtureIDs.rasterSkin).contains { $0.id == "skin-raster-only" })
        XCTAssertEqual(bank.phrases.count - bank.phrases(in: "en").count, 2)
        XCTAssertEqual(bank.phrases(in: "it-IT").count, bank.phrases.count)
        XCTAssertEqual(bank.phrases.filter { !$0.fits(language: "en") }.map(\.id), ["it-only", "joke-it"])
    }

    func testDaypartsAndWeekdays() {
        XCTAssertEqual(Tag.daypart(hour: 5), .morning)
        XCTAssertEqual(Tag.daypart(hour: 11), .morning)
        XCTAssertEqual(Tag.daypart(hour: 12), .afternoon)
        XCTAssertEqual(Tag.daypart(hour: 16), .afternoon)
        XCTAssertEqual(Tag.daypart(hour: 17), .evening)
        XCTAssertEqual(Tag.daypart(hour: 21), .evening)
        XCTAssertEqual(Tag.daypart(hour: 22), .night)
        XCTAssertEqual(Tag.daypart(hour: 4), .night)
        XCTAssertEqual(Tag.weekdayTag(weekday: 2), .monday)
        XCTAssertEqual(Tag.weekdayTag(weekday: 6), .friday)
        XCTAssertEqual(Tag.weekdayTag(weekday: 1), .weekend)
        XCTAssertEqual(Tag.weekdayTag(weekday: 7), .weekend)
        XCTAssertNil(Tag.weekdayTag(weekday: 4))
    }
}

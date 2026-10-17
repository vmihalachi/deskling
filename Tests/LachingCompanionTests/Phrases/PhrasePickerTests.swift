import LachingCompanion
import LachingTesting
import XCTest

final class PhrasePickerTests: XCTestCase {
    let bank = PhraseBank.fixture
    let skin = FixtureIDs.vectorSkin

    /// Monday 10:00 in English with the chime on: general and morning lines, tunes included.
    private var mondayMorning: PhraseContext { PhraseContext(hour: 10, weekday: 2, chimeEnabled: true, skin: skin, language: "en") }

    private func context(hour: Int, weekday: Int, event: Tag? = nil, chime: Bool = false) -> PhraseContext {
        PhraseContext(hour: hour, weekday: weekday, event: event, chimeEnabled: chime, skin: skin, language: "en")
    }

    /// `count` general lines of weight 3, "l0", "l1", …
    private func generalBank(_ count: Int) -> PhraseBank {
        PhraseBank((0..<count).map { Phrase("l\($0)", .line(source: "l\($0)"), tags: [.general]) })
    }

    func testAmbientCandidatesAreGeneralDaypartAndWeekdayLinesOnly() {
        let candidates = PhrasePicker.candidates(in: bank, context: mondayMorning)
        XCTAssertEqual(
            candidates.map(\.id), ["general-a1", "general-a2", "general-a3", "general-b1", "general-c1", "morning-1", "monday-1", "tune-1"])
        for phrase in candidates {
            XCTAssertFalse(phrase.hasEventTag, phrase.id)
            XCTAssertFalse(phrase.tags.isDisjoint(with: [.general, .morning, .monday]), phrase.id)
        }
    }

    func testEventLinesWinWhenThereIsAnEvent() {
        var context = mondayMorning
        context.event = .welcomeBack
        let candidates = PhrasePicker.candidates(in: bank, context: context)
        XCTAssertEqual(candidates.map(\.id), bank.phrases(tagged: .welcomeBack).map(\.id))
        XCTAssertEqual(PhrasePicker.pick(from: bank, context: context, unit: 0.5)?.tags, [.welcomeBack])
    }

    func testASequenceIsOneCandidateLikeALine() {
        var context = mondayMorning
        context.event = .buttonPress
        XCTAssertEqual(PhrasePicker.candidates(in: bank, context: context).map(\.id), ["button-press-1", "joke-1"])
        XCTAssertEqual(PhrasePicker.pick(from: bank, context: context, unit: 0.9)?.id, "joke-1")
        // Its event tag keeps it out of ambient chatter, though it is also tagged general.
        XCTAssertFalse(PhrasePicker.candidates(in: bank, context: mondayMorning).contains { $0.id == "joke-1" })
        // Recents are keyed on the phrase id: a told joke is skipped whole.
        context.recentIDs = ["joke-1"]
        XCTAssertEqual(PhrasePicker.candidates(in: bank, context: context).map(\.id), ["button-press-1"])
    }

    func testPhrasesOutsideTheLanguageAreDroppedFirst() {
        XCTAssertFalse(PhrasePicker.candidates(in: bank, context: mondayMorning).contains { !$0.languages.isEmpty })
        var context = mondayMorning
        context.language = "it-IT"
        let italian = PhrasePicker.candidates(in: bank, context: context).map(\.id)
        XCTAssertTrue(italian.contains("it-only"))
        XCTAssertTrue(italian.contains("joke-it"))
        // An event whose only lines are in another language falls back to ambient chatter.
        let tiny = PhraseBank([
            Phrase("any", .line(source: "any"), tags: [.general]),
            Phrase("ciao", .line(source: "ciao"), tags: [.welcomeBack], languages: ["it"]),
        ])
        context = self.context(hour: 14, weekday: 4, event: .welcomeBack)
        XCTAssertEqual(PhrasePicker.candidates(in: tiny, context: context).map(\.id), ["any"])
        context.language = "it"
        XCTAssertEqual(PhrasePicker.candidates(in: tiny, context: context).map(\.id), ["ciao"])
    }

    func testChimeOffLeavesTunesOut() {
        var context = mondayMorning
        XCTAssertTrue(PhrasePicker.candidates(in: bank, context: context).contains(where: \.isTune))
        context.chimeEnabled = false
        XCTAssertFalse(PhrasePicker.candidates(in: bank, context: context).contains(where: \.isTune))
    }

    func testUnitZeroPicksTheFirstCandidateAndUnitNearOnePicksTheLast() {
        let candidates = PhrasePicker.candidates(in: bank, context: mondayMorning)
        XCTAssertEqual(PhrasePicker.pick(from: bank, context: mondayMorning, unit: 0)?.id, candidates.first?.id)
        XCTAssertEqual(PhrasePicker.pick(from: bank, context: mondayMorning, unit: 0.999_999)?.id, candidates.last?.id)
    }

    func testASkinsOwnLinesPlayOnlyForIt() {
        XCTAssertFalse(PhrasePicker.candidates(in: bank, context: mondayMorning).contains { $0.id == "skin-raster-only" })
        var context = mondayMorning
        context.skin = FixtureIDs.rasterSkin
        XCTAssertTrue(PhrasePicker.candidates(in: bank, context: context).contains { $0.id == "skin-raster-only" })
    }

    /// A character never says another's lines, and an event whose lines all belong to someone else falls back to
    /// ambient chatter.
    func testACharacterNeverSaysAnothersLines() {
        for skin in FixtureIDs.skins {
            for other in FixtureIDs.skins where other != skin {
                let tiny = PhraseBank([
                    Phrase("shared", .line(source: "shared"), tags: [.general]),
                    Phrase("mine", .line(source: "mine"), tags: [.general], skins: [skin]),
                    Phrase("theirs", .line(source: "theirs"), tags: [.general], skins: [other]),
                    Phrase("their-hello", .line(source: "their hello"), tags: [.welcomeBack], skins: [other]),
                ])
                let context = PhraseContext(hour: 14, weekday: 4, event: .welcomeBack, chimeEnabled: false, skin: skin, language: "en")
                XCTAssertEqual(PhrasePicker.candidates(in: tiny, context: context).map(\.id), ["shared", "mine"])
            }
        }
    }

    func testDaypartLinesCountDouble() {
        // A bank where the general line and the morning line both weigh 1: the morning line takes 2 of 3 draws.
        let tiny = PhraseBank([
            Phrase("g", .line(source: "g"), tags: [.general], weight: 1),
            Phrase("m", .line(source: "m"), tags: [.morning], weight: 1),
        ])
        let context = context(hour: 9, weekday: 3)
        XCTAssertEqual(PhrasePicker.pick(from: tiny, context: context, unit: 0.1)?.id, "g")
        XCTAssertEqual(PhrasePicker.pick(from: tiny, context: context, unit: 0.4)?.id, "m")
        XCTAssertEqual(PhrasePicker.pick(from: tiny, context: context, unit: 0.9)?.id, "m")
    }

    func testRecentLinesAreSkipped() {
        let first = PhrasePicker.candidates(in: bank, context: mondayMorning).prefix(3).map(\.id)
        var context = mondayMorning
        context.recentIDs = Array(first)
        let candidates = PhrasePicker.candidates(in: bank, context: context)
        XCTAssertTrue(Set(candidates.map(\.id)).isDisjoint(with: Set(first)))
        XCTAssertFalse(candidates.isEmpty)
    }

    func testOnlyTheLastRecentWindowCounts() {
        let tiny = generalBank(PhrasePicker.recentWindow + 5)
        var context = context(hour: 14, weekday: 4)
        let all = tiny.phrases.map(\.id)
        context.recentIDs = Array(all.prefix(PhrasePicker.recentWindow + 3))
        let candidates = PhrasePicker.candidates(in: tiny, context: context).map(\.id)
        // The three oldest are eligible again, and the two never said.
        XCTAssertEqual(candidates, Array(all.prefix(3)) + Array(all.suffix(2)))
    }

    func testExhaustedPoolFallsBackToEverythingButTheLastLine() {
        let tiny = generalBank(2)
        var context = context(hour: 10, weekday: 3)
        context.recentIDs = ["l0", "l1"]
        XCTAssertEqual(PhrasePicker.candidates(in: tiny, context: context).map(\.id), ["l0"])
        context.recentIDs = ["l1", "l0"]
        XCTAssertEqual(PhrasePicker.candidates(in: tiny, context: context).map(\.id), ["l1"])
    }

    func testFlavorsAndExclusionsActAsIfTheLinesWerentThere() {
        let tiny = PhraseBank([
            Phrase("calm", .line(source: "a"), tags: [.general], flavor: FixtureIDs.flavorA),
            Phrase("silly", .line(source: "b"), tags: [.general], flavor: FixtureIDs.flavorB),
            Phrase("tune", .tune(sound: "tune-1"), tags: [.general]),
            Phrase("back", .line(source: "c"), tags: [.welcomeBack], flavor: FixtureIDs.flavorB),
        ])
        var context = context(hour: 10, weekday: 3, chime: true)
        context.flavors = [FixtureIDs.flavorA]
        XCTAssertEqual(PhrasePicker.candidates(in: tiny, context: context).map(\.id), ["calm", "tune"])
        context.flavors = []
        context.excludedIDs = ["silly", "tune"]
        XCTAssertEqual(PhrasePicker.candidates(in: tiny, context: context).map(\.id), ["calm"])
        // An event whose lines are all filtered out falls back to ambient chatter, as if it had none.
        context.event = .welcomeBack
        XCTAssertEqual(PhrasePicker.candidates(in: tiny, context: context).map(\.id), ["back"])
        context.flavors = [FixtureIDs.flavorA]
        XCTAssertEqual(PhrasePicker.candidates(in: tiny, context: context).map(\.id), ["calm"])
    }

    func testEmptyBankPicksNothing() {
        XCTAssertNil(PhrasePicker.pick(from: PhraseBank([]), context: mondayMorning, unit: 0.5))
    }

    func testPickIsDeterministic() {
        let a = PhrasePicker.pick(from: bank, context: mondayMorning, unit: 0.37)
        let b = PhrasePicker.pick(from: bank, context: mondayMorning, unit: 0.37)
        XCTAssertEqual(a, b)
    }
}

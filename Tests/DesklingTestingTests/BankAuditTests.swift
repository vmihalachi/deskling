import DesklingCompanion
import DesklingTesting
import XCTest

final class BankAuditTests: XCTestCase {
    func testTheFixtureBankHasALineForEveryTag() {
        XCTAssertEqual(BankAudit.floors(bank: .fixture, languages: ["en", "it"], skins: FixtureIDs.skins, minimumPerTag: 1), [])
    }

    func testShortTagsAreNamed() {
        let bank = PhraseBank(PhraseBank.fixture.phrases.filter { !$0.tags.contains(.monday) })
        let problems = BankAudit.floors(bank: bank, languages: ["en"], skins: [FixtureIDs.vectorSkin], minimumPerTag: 1)
        XCTAssertEqual(problems, ["en / fixture-vector: monday has 0 (needs 1)"])
    }

    func testTunesDoNotCount() {
        let tunes = PhraseBank(PhraseBank.fixture.phrases.filter(\.isTune))
        let problems = BankAudit.floors(bank: tunes, languages: ["en"], skins: [FixtureIDs.vectorSkin], minimumPerTag: 1)
        XCTAssertEqual(problems.count, Tag.allCases.count)
    }
}

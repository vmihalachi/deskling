import DesklingCore
import XCTest

final class SplitMix64Tests: XCTestCase {
    // Reference values of the published SplitMix64 algorithm (seed 0 is the sequence most implementations print).
    func testSeedZeroMatchesTheReference() {
        var g = SplitMix64(seed: 0)
        XCTAssertEqual(g.next(), 16294208416658607535)
        XCTAssertEqual(g.next(), 7960286522194355700)
        XCTAssertEqual(g.next(), 487617019471545679)
    }

    func testSeed42MatchesTheReference() {
        var g = SplitMix64(seed: 42)
        XCTAssertEqual(g.next(), 13679457532755275413)
        XCTAssertEqual(g.next(), 2949826092126892291)
        XCTAssertEqual(g.next(), 5139283748462763858)
    }

    func testUnitsAreTheTop53BitsOverTwoToThe53() {
        var g = SplitMix64(seed: 42)
        XCTAssertEqual(g.nextUnit(), 0.7415648787718233)
        XCTAssertEqual(g.nextUnit(), 0.1599103928769201)
        XCTAssertEqual(g.nextUnit(), 0.27860113025513866)
    }

    func testUnitsStayInRange() {
        var g = SplitMix64(seed: 123_456_789)
        for _ in 0..<10_000 {
            let unit = g.nextUnit()
            XCTAssertGreaterThanOrEqual(unit, 0)
            XCTAssertLessThan(unit, 1)
        }
    }

    func testSameSeedSameSequence() {
        var a = SplitMix64(seed: 9)
        var b = SplitMix64(seed: 9)
        XCTAssertEqual((0..<5).map { _ in a.next() }, (0..<5).map { _ in b.next() })
    }
}

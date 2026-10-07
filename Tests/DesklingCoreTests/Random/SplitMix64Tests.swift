import DesklingCore
import XCTest

final class SplitMix64Tests: XCTestCase {
    // Reference values of the published SplitMix64 algorithm (seed 0 is the sequence most implementations print).
    func testSeedZeroMatchesTheReference() {
        var g = SplitMix64(seed: 0)
        XCTAssertEqual(g.next(), 0xE220_A839_7B1D_CDAF)
        XCTAssertEqual(g.next(), 0x6E78_9E6A_A1B9_65F4)
        XCTAssertEqual(g.next(), 0x06C4_5D18_8009_454F)
    }

    func testSeed42MatchesTheReference() {
        var g = SplitMix64(seed: 42)
        XCTAssertEqual(g.next(), 0xBDD7_3226_2FEB_6E95)
        XCTAssertEqual(g.next(), 0x28EF_E333_B266_F103)
        XCTAssertEqual(g.next(), 0x4752_6757_130F_9F52)
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

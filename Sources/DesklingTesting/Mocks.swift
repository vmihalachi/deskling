import DesklingCore
import Foundation

/// A clock tests move by hand.
public final class MockClock: Clock {
    public var now: Date

    public init(_ now: Date) {
        self.now = now
    }

    public func advance(minutes: Double) {
        now = now.addingTimeInterval(minutes * 60)
    }
}

/// An idle-time provider with a settable value.
public final class MockIdle: IdleTimeProvider {
    public var idle: TimeInterval = 0

    public init() {}

    public func secondsSinceLastInput() -> TimeInterval { idle }
}

/// A busy-state provider with settable reasons.
public final class MockBusy: BusyStateProvider {
    public var reasons: Set<BusyReason> = []

    public init() {}

    public func currentBusyReasons() -> Set<BusyReason> { reasons }
}

/// A random source that returns the given unit values in order, then repeats the last one.
public struct ScriptedRandom: RandomSource, Sendable {
    public var values: [Double]
    public private(set) var reads = 0

    public init(_ values: [Double]) {
        self.values = values
    }

    public mutating func nextUnit() -> Double {
        defer { reads += 1 }
        return values[min(reads, values.count - 1)]
    }
}

/// A random source that reports every read, for asserting that code never consults it.
public final class CountingRandom: RandomSource {
    public private(set) var reads = 0
    public var onRead: (() -> Void)?

    public init(onRead: (() -> Void)? = nil) {
        self.onRead = onRead
    }

    public func nextUnit() -> Double {
        reads += 1
        onRead?()
        return 0.5
    }
}

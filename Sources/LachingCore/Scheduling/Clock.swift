import Foundation

/// Where the scheduler reads the time from, so tests and conformance vectors can drive it.
public protocol Clock {
    var now: Date { get }
}

/// The wall clock.
public struct SystemClock: Clock, Sendable {
    public init() {}
    public var now: Date { Date() }
}

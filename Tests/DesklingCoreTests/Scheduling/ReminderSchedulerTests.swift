import DesklingCore
import DesklingTesting
import XCTest

final class ReminderSchedulerTests: XCTestCase {
    private var calendar: Calendar = {
        var c = Calendar(identifier: .gregorian)
        c.timeZone = TimeZone(identifier: "UTC")!
        return c
    }()

    private var clock: MockClock!
    private var idle: MockIdle!
    private var busy: MockBusy!

    /// Monday 2026-09-28 at the given time (UTC).
    private func monday(_ hour: Int, _ minute: Int = 0) -> Date {
        calendar.date(from: DateComponents(year: 2026, month: 9, day: 28, hour: hour, minute: minute))!
    }

    private func makeScheduler(start: Date, config: SchedulerConfig = SchedulerConfig()) -> ReminderScheduler {
        clock = MockClock(start)
        idle = MockIdle()
        busy = MockBusy()
        return ReminderScheduler(config: config, clock: clock, idle: idle, busy: busy, calendar: calendar)
    }

    /// Advances in 20 s ticks (like the app), returning the ticks' decisions.
    private func tick(_ s: ReminderScheduler, seconds: Int) -> [SchedulerDecision] {
        (0..<(seconds / 20)).map { _ in
            clock.now = clock.now.addingTimeInterval(20)
            return s.tick()
        }
    }

    /// Advances minute by minute, returning the minutes (from now) at which reminders fired.
    private func run(_ s: ReminderScheduler, minutes: Int) -> [Int] {
        var fired: [Int] = []
        for m in 1...minutes {
            clock.advance(minutes: 1)
            if s.tick() == .remind { fired.append(m) }
        }
        return fired
    }

    func testMondayFixtureIsAWeekday() {
        XCTAssertEqual(calendar.component(.weekday, from: monday(10)), 2)
    }

    func testRemindsAfterInterval() {
        let s = makeScheduler(start: monday(9, 30))
        XCTAssertEqual(s.nextFireDate, monday(10, 15))
        XCTAssertEqual(run(s, minutes: 100), [45, 90])
    }

    func testCustomInterval() {
        var config = SchedulerConfig()
        config.interval = 20 * 60
        let s = makeScheduler(start: monday(10), config: config)
        XCTAssertEqual(run(s, minutes: 60), [20, 40, 60])
    }

    func testIdleUserIsNotRemindedAndTimerResets() {
        let s = makeScheduler(start: monday(10))
        _ = run(s, minutes: 30)
        idle.idle = 10 * 60  // user walked away
        XCTAssertEqual(run(s, minutes: 30), [])
        XCTAssertEqual(s.status, .away)
        idle.idle = 0  // back at the desk: a fresh full interval starts
        XCTAssertEqual(run(s, minutes: 50), [45])
    }

    func testShortIdleDoesNotCountAsAway() {
        let s = makeScheduler(start: monday(10))
        idle.idle = 2 * 60
        XCTAssertEqual(run(s, minutes: 45), [45])
    }

    func testNoRemindersOutsideActiveHours() {
        let s = makeScheduler(start: monday(17, 30))
        XCTAssertEqual(run(s, minutes: 29), [])
        clock.advance(minutes: 1)
        XCTAssertEqual(s.tick(), .none)  // 18:00 – window closed
        XCTAssertEqual(s.status, .outsideActiveHours)
        XCTAssertNil(s.nextFireDate)
        XCTAssertEqual(run(s, minutes: 300), [])
    }

    func testEnteringActiveHoursStartsFreshInterval() {
        let s = makeScheduler(start: monday(8, 0))
        XCTAssertNil(s.nextFireDate)
        // 9:00 enters the window; first reminder at 9:45.
        XCTAssertEqual(run(s, minutes: 110), [105])
    }

    func testNoRemindersOnWeekend() {
        let saturday = calendar.date(from: DateComponents(year: 2026, month: 9, day: 26, hour: 10))!
        XCTAssertEqual(calendar.component(.weekday, from: saturday), 7)
        let s = makeScheduler(start: saturday)
        XCTAssertEqual(run(s, minutes: 200), [])
    }

    func testOvernightActiveWindow() {
        var config = SchedulerConfig()
        config.activeStartMinute = 22 * 60
        config.activeEndMinute = 6 * 60
        config.activeWeekdays = Set(1...7)
        let s = makeScheduler(start: monday(12), config: config)
        XCTAssertFalse(s.isWithinActiveHours(monday(12)))
        XCTAssertTrue(s.isWithinActiveHours(monday(23)))
        XCTAssertTrue(s.isWithinActiveHours(monday(2)))
    }

    func testSnoozeDelaysByTenMinutes() {
        let s = makeScheduler(start: monday(10))
        XCTAssertEqual(run(s, minutes: 45), [45])
        s.snooze(minutes: 10)
        XCTAssertEqual(run(s, minutes: 15), [10])
    }

    func testSnoozeWithoutReminderNeverBringsItCloser() {
        let s = makeScheduler(start: monday(10))
        XCTAssertEqual(run(s, minutes: 20), [])
        s.snooze(minutes: 10)
        // Still due at 10:45, not 10:30.
        XCTAssertEqual(run(s, minutes: 30), [25])
    }

    func testSnoozeAfterAnsweredReminderNeverBringsItCloser() {
        let s = makeScheduler(start: monday(10))
        XCTAssertEqual(run(s, minutes: 45), [45])
        s.sessionCompleted()
        s.snooze(minutes: 10)
        XCTAssertEqual(run(s, minutes: 45), [45])
    }

    func testSnoozeHeldReminderRemindsInTenMinutes() {
        let s = makeScheduler(start: monday(10))
        busy.reasons = [.call]
        XCTAssertEqual(run(s, minutes: 50), [])
        s.snooze(minutes: 10)
        busy.reasons = []
        XCTAssertEqual(run(s, minutes: 10), [10])
    }

    func testSnoozeLaterThanNextReminderPushesItOut() {
        let s = makeScheduler(start: monday(10))
        XCTAssertEqual(run(s, minutes: 40), [])
        s.snooze(minutes: 10)
        XCTAssertEqual(run(s, minutes: 10), [10])
    }

    func testPauseSuppressesRemindersThenRestartsInterval() {
        let s = makeScheduler(start: monday(10))
        s.pause(for: 3600)
        if case .paused = s.status {} else { XCTFail("expected paused") }
        XCTAssertEqual(run(s, minutes: 60), [])
        // Pause expired at +60; next reminder 45 min after that.
        XCTAssertEqual(run(s, minutes: 50), [45])
    }

    func testResumeEndsPauseEarly() {
        let s = makeScheduler(start: monday(10))
        s.pause(for: 3600)
        _ = run(s, minutes: 10)
        s.resume()
        XCTAssertEqual(run(s, minutes: 45), [45])
    }

    func testPauseUntilDateLastsAcrossDaysThenRestartsInterval() {
        let s = makeScheduler(start: monday(10))
        let wednesday = calendar.date(byAdding: .day, value: 2, to: calendar.startOfDay(for: monday(10)))!
        s.pause(until: wednesday)
        XCTAssertEqual(s.status, .paused(until: wednesday))
        clock.now = calendar.date(byAdding: .hour, value: 30, to: monday(10))!
        XCTAssertEqual(s.tick(), .none)
        clock.now = calendar.date(bySettingHour: 10, minute: 0, second: 0, of: wednesday)!
        XCTAssertEqual(s.tick(), .none)
        XCTAssertNil(s.pausedUntil)
        XCTAssertEqual(run(s, minutes: 50), [45])
    }

    func testEndOfTodayIsNextMidnight() {
        let s = makeScheduler(start: monday(23, 30))
        XCTAssertEqual(s.endOfToday, calendar.date(byAdding: .minute, value: 30, to: monday(23, 30)))
        s.pause(until: s.endOfToday)
        clock.now = monday(23, 59)
        XCTAssertEqual(s.tick(), .none)
        if case .paused = s.status {} else { XCTFail("expected paused") }
    }

    func testSessionCompletedResetsInterval() {
        let s = makeScheduler(start: monday(10))
        _ = run(s, minutes: 40)
        s.sessionCompleted()
        XCTAssertEqual(run(s, minutes: 50), [45])
    }

    func testChangingIntervalReschedules() {
        let s = makeScheduler(start: monday(10))
        _ = run(s, minutes: 10)
        var config = SchedulerConfig()
        config.interval = 15 * 60
        s.update(config: config)
        XCTAssertEqual(run(s, minutes: 16), [15])
    }

    // MARK: Busy

    func testCallPostponesDueReminderAndFiresAfterGrace() {
        let s = makeScheduler(start: monday(10))
        _ = run(s, minutes: 40)
        busy.reasons = [.call]
        XCTAssertEqual(run(s, minutes: 20), [])  // due at 45, held
        XCTAssertEqual(s.status, .postponed(.call))
        XCTAssertEqual(s.tick(), .postpone)
        busy.reasons = []
        // Last busy tick at t; +20 s is inside the 30 s grace, +40 s fires.
        XCTAssertEqual(tick(s, seconds: 40), [.postpone, .remind])
        if case .scheduled = s.status {} else { XCTFail("expected scheduled") }
        // Then a full interval from the release.
        XCTAssertEqual(run(s, minutes: 46), [45])
    }

    func testBusyBeforeDueDoesNotChangeTheSchedule() {
        let s = makeScheduler(start: monday(10))
        busy.reasons = [.fullScreen]
        XCTAssertEqual(run(s, minutes: 30), [])
        if case .scheduled = s.status {} else { XCTFail("expected scheduled") }
        busy.reasons = []
        XCTAssertEqual(run(s, minutes: 15), [15])
    }

    func testReturnsPostponeOnlyWhenAReminderIsHeld() {
        let s = makeScheduler(start: monday(10))
        busy.reasons = [.call]
        clock.advance(minutes: 10)
        XCTAssertEqual(s.tick(), .none)
        clock.advance(minutes: 40)
        XCTAssertEqual(s.tick(), .postpone)
    }

    func testDisabledReasonsAreIgnored() {
        var config = SchedulerConfig()
        config.postponeDuringFullScreen = false
        let s = makeScheduler(start: monday(10), config: config)
        busy.reasons = [.fullScreen]
        XCTAssertEqual(run(s, minutes: 45), [45])
        config.postponeDuringCalls = false
        s.update(config: config)
        busy.reasons = [.call]
        XCTAssertEqual(run(s, minutes: 45), [45])
    }

    func testCallStatusWinsOverFullScreen() {
        let s = makeScheduler(start: monday(10))
        busy.reasons = [.call, .fullScreen]
        _ = run(s, minutes: 46)
        XCTAssertEqual(s.status, .postponed(.call))
    }

    func testIdleDuringCallIsNotABreak() {
        let s = makeScheduler(start: monday(10))
        _ = run(s, minutes: 30)
        busy.reasons = [.call]
        idle.idle = 20 * 60  // listening, hands off the keyboard
        XCTAssertEqual(run(s, minutes: 30), [])
        XCTAssertNotEqual(s.status, .away)
        busy.reasons = []
        idle.idle = 0
        XCTAssertEqual(run(s, minutes: 1), [1])  // held reminder, not a reset
    }

    func testWalkingAwayAfterCallDropsHeldReminder() {
        let s = makeScheduler(start: monday(10))
        busy.reasons = [.call]
        _ = run(s, minutes: 50)
        busy.reasons = []
        idle.idle = 10 * 60
        XCTAssertEqual(run(s, minutes: 5), [])
        XCTAssertEqual(s.status, .away)
        idle.idle = 0
        XCTAssertEqual(run(s, minutes: 46), [45])
    }

    func testHeldReminderIsDroppedWhenActiveHoursEnd() {
        let s = makeScheduler(start: monday(17))
        busy.reasons = [.call]
        _ = run(s, minutes: 65)  // due 17:45, held; window closes 18:00
        XCTAssertEqual(s.status, .outsideActiveHours)
        busy.reasons = []
        XCTAssertEqual(run(s, minutes: 120), [])
        XCTAssertFalse(s.isHolding)
    }

    func testBusyOutsideActiveHoursDoesNothing() {
        let s = makeScheduler(start: monday(19))
        busy.reasons = [.call]
        XCTAssertEqual(s.tick(), .none)
        XCTAssertEqual(s.status, .outsideActiveHours)
    }

    func testPauseWinsOverBusy() {
        let s = makeScheduler(start: monday(10))
        busy.reasons = [.call]
        _ = run(s, minutes: 50)
        s.pause(for: 3600)
        busy.reasons = []
        XCTAssertEqual(run(s, minutes: 59), [])
        if case .paused = s.status {} else { XCTFail("expected paused") }
    }

    // MARK: After this call

    func testAfterCallFiresWhenCallEnds() {
        let s = makeScheduler(start: monday(10))
        _ = run(s, minutes: 45)
        s.snoozeUntilAfterCall()
        XCTAssertTrue(s.isWaitingForCall)
        XCTAssertEqual(run(s, minutes: 2), [])
        busy.reasons = [.call]
        XCTAssertEqual(run(s, minutes: 3), [])
        XCTAssertEqual(s.status, .postponed(.call))
        busy.reasons = []
        XCTAssertEqual(run(s, minutes: 1), [1])
        XCTAssertFalse(s.isWaitingForCall)
    }

    func testAfterCallWaitsForLongCallsPastTheFallback() {
        let s = makeScheduler(start: monday(10))
        s.snoozeUntilAfterCall()
        busy.reasons = [.call]
        XCTAssertEqual(run(s, minutes: 40), [])
        busy.reasons = []
        XCTAssertEqual(run(s, minutes: 1), [1])
    }

    func testAfterCallFallsBackWhenNoCallStarts() {
        let s = makeScheduler(start: monday(10))
        s.snoozeUntilAfterCall()
        XCTAssertEqual(run(s, minutes: 20), [15])
    }

    func testAfterCallIgnoresFullScreen() {
        var config = SchedulerConfig()
        config.postponeDuringFullScreen = false
        let s = makeScheduler(start: monday(10), config: config)
        s.snoozeUntilAfterCall()
        busy.reasons = [.fullScreen]
        XCTAssertEqual(run(s, minutes: 5), [])
        busy.reasons = []
        XCTAssertEqual(run(s, minutes: 5), [])  // still waiting for a call
        XCTAssertTrue(s.isWaitingForCall)
    }

    func testAfterCallWorksEvenWhenCallPostponingIsOff() {
        var config = SchedulerConfig()
        config.postponeDuringCalls = false
        let s = makeScheduler(start: monday(10), config: config)
        s.snoozeUntilAfterCall()
        busy.reasons = [.call]
        XCTAssertEqual(run(s, minutes: 30), [])
        busy.reasons = []
        XCTAssertEqual(run(s, minutes: 1), [1])
        // Back to normal: calls no longer hold reminders.
        busy.reasons = [.call]
        XCTAssertEqual(run(s, minutes: 45), [45])
    }

    func testRegularSnoozeCancelsAfterCall() {
        let s = makeScheduler(start: monday(10))
        s.snoozeUntilAfterCall()
        s.snooze(minutes: 10)
        XCTAssertFalse(s.isWaitingForCall)
        XCTAssertEqual(run(s, minutes: 10), [10])
    }

    func testQuietHourHoldsRemindersAndRestartsAfter() {
        var config = SchedulerConfig()
        config.quietHours = [10]
        let s = makeScheduler(start: monday(9, 30), config: config)
        XCTAssertFalse(s.isWithinActiveHours(monday(10, 15)))
        XCTAssertTrue(s.isWithinActiveHours(monday(11)))
        // Due at 10:15, but 10:00–11:00 is quiet. A fresh 45 min starts at 11:00.
        XCTAssertEqual(run(s, minutes: 30), [])
        XCTAssertEqual(s.status, .outsideActiveHours)
        XCTAssertEqual(run(s, minutes: 105), [105])
    }

    // MARK: Jitter

    func testZeroJitterNeverConsultsTheRandomSource() {
        let random = CountingRandom(onRead: { XCTFail("the random source was read with zero jitter") })
        clock = MockClock(monday(10))
        idle = MockIdle()
        busy = MockBusy()
        let s = ReminderScheduler(
            config: SchedulerConfig(), clock: clock, idle: idle, busy: busy, calendar: calendar, random: random)
        XCTAssertEqual(run(s, minutes: 100), [45, 90])
        s.snooze(minutes: 5)
        s.resume()
        s.sessionCompleted()
        s.pause(for: 60)
        _ = run(s, minutes: 2)
        XCTAssertEqual(random.reads, 0)
    }

    func testJitterOffsetsEveryIntervalFromTheSeed() {
        var config = SchedulerConfig()
        config.interval = 30 * 60
        config.intervalJitter = 10 * 60
        clock = MockClock(monday(10))
        idle = MockIdle()
        busy = MockBusy()
        let s = ReminderScheduler(
            config: config, clock: clock, idle: idle, busy: busy, calendar: calendar, random: SplitMix64(seed: 42))
        // SplitMix64(42) first draws 0.74156…: floor((0.4831… × 600) + 0.5) = 290 s past the 30 minutes.
        XCTAssertEqual(s.nextFireDate, monday(10).addingTimeInterval(30 * 60 + 290))
        // Then −408 s (10:35 + 23:12) and −266 s (10:59 + 25:34), each from the firing tick.
        XCTAssertEqual(run(s, minutes: 100), [35, 59, 85])
    }

    func testJitterBoundsAndRounding() {
        var config = SchedulerConfig()
        config.interval = 30 * 60
        config.intervalJitter = 10 * 60
        clock = MockClock(monday(10))
        idle = MockIdle()
        busy = MockBusy()
        let low = ReminderScheduler(
            config: config, clock: clock, idle: idle, busy: busy, calendar: calendar, random: ScriptedRandom([0]))
        XCTAssertEqual(low.nextFireDate, monday(10, 20))
        let middle = ReminderScheduler(
            config: config, clock: clock, idle: idle, busy: busy, calendar: calendar, random: ScriptedRandom([0.5]))
        XCTAssertEqual(middle.nextFireDate, monday(10, 30))
        let high = ReminderScheduler(
            config: config, clock: clock, idle: idle, busy: busy, calendar: calendar, random: ScriptedRandom([0.999_999]))
        XCTAssertEqual(high.nextFireDate, monday(10, 40))
    }
}

import DesklingCompanion
import DesklingCore
import XCTest

final class QuietRulesTests: XCTestCase {
    let talkative = Manners(channels: [.voice, .chime])

    func testCalmMomentSpeaksAndChimes() {
        XCTAssertEqual(
            QuietRules.decide(trigger: .due, snapshot: .calm, manners: talkative),
            .deliver(ChannelPlan(speak: true, chime: true, notify: false)))
    }

    func testBusyHoldsEverythingExceptTheButton() {
        let busy = Snapshot(busy: [.call])
        XCTAssertEqual(QuietRules.decide(trigger: .due, snapshot: busy, manners: talkative), .skip(.busy))
        XCTAssertEqual(QuietRules.decide(trigger: .welcomeBack, snapshot: busy, manners: talkative), .skip(.busy))
        XCTAssertEqual(QuietRules.decide(trigger: .buttonPress, snapshot: busy, manners: talkative), .deliver(.bubbleOnly))
    }

    func testAwayAndLockedSayNothing() {
        XCTAssertEqual(QuietRules.decide(trigger: .due, snapshot: Snapshot(idleSeconds: 600), manners: talkative), .skip(.away))
        XCTAssertEqual(QuietRules.decide(trigger: .due, snapshot: Snapshot(isScreenLocked: true), manners: talkative), .skip(.locked))
        // Coming back is the one thing worth saying to someone who was away.
        XCTAssertEqual(
            QuietRules.decide(trigger: .welcomeBack, snapshot: Snapshot(idleSeconds: 600), manners: talkative),
            .deliver(ChannelPlan(speak: true, chime: true, notify: false)))
    }

    func testNothingOverlapsSpeech() {
        let speaking = Snapshot(isSpeaking: true)
        XCTAssertEqual(QuietRules.decide(trigger: .due, snapshot: speaking, manners: talkative), .skip(.speaking))
        XCTAssertEqual(QuietRules.decide(trigger: .welcomeBack, snapshot: speaking, manners: talkative), .skip(.speaking))
    }

    func testAButtonPressAnswersMidLineAndTheShellCutsIt() {
        // A notification's Say more comes while its line is still heard: the press is delivered, never queued.
        let speaking = Snapshot(isSpeaking: true)
        XCTAssertEqual(
            QuietRules.decide(trigger: .buttonPress, snapshot: speaking, manners: talkative),
            .deliver(ChannelPlan(speak: true, chime: true, notify: false)))
        XCTAssertEqual(
            QuietRules.decide(trigger: .buttonPress, snapshot: Snapshot(busy: [.call], isSpeaking: true), manners: talkative),
            .deliver(.bubbleOnly))
        XCTAssertEqual(
            QuietRules.decide(trigger: .buttonPress, snapshot: Snapshot(isScreenLocked: true, isSpeaking: true), manners: talkative),
            .skip(.locked))
    }

    func testMutedOutputFallsBackToANotificationWhenAllowed() {
        let muted = Snapshot(isOutputMuted: true, notificationsAuthorized: true)
        XCTAssertEqual(
            QuietRules.decide(trigger: .due, snapshot: muted, manners: talkative),
            .deliver(ChannelPlan(speak: false, chime: false, notify: true)))
        let notAllowed = Snapshot(isOutputMuted: true, notificationsAuthorized: false)
        XCTAssertEqual(QuietRules.decide(trigger: .due, snapshot: notAllowed, manners: talkative), .deliver(.bubbleOnly))
    }

    func testNotificationChannel() {
        let manners = Manners(channels: [.notification])
        XCTAssertEqual(
            QuietRules.decide(trigger: .due, snapshot: .calm, manners: manners),
            .deliver(ChannelPlan(speak: false, chime: false, notify: true)))
        // A button press shows in the panel; no banner on top.
        XCTAssertEqual(QuietRules.decide(trigger: .buttonPress, snapshot: .calm, manners: manners), .deliver(.bubbleOnly))
    }

    func testQuietChannelsStillShowTheBubble() {
        XCTAssertEqual(QuietRules.decide(trigger: .due, snapshot: .calm, manners: Manners(channels: .quiet)), .deliver(.bubbleOnly))
    }

    func testDisabledComments() {
        let manners = Manners(channels: [.voice], greetOnReturn: false, commentOnPower: false, commentOnAppearance: false)
        XCTAssertEqual(QuietRules.decide(trigger: .welcomeBack, snapshot: .calm, manners: manners), .skip(.disabled))
        XCTAssertEqual(QuietRules.decide(trigger: .batteryLow, snapshot: .calm, manners: manners), .skip(.disabled))
        XCTAssertEqual(QuietRules.decide(trigger: .charging, snapshot: .calm, manners: manners), .skip(.disabled))
        XCTAssertEqual(QuietRules.decide(trigger: .wentDark, snapshot: .calm, manners: manners), .skip(.disabled))
        XCTAssertEqual(
            QuietRules.decide(trigger: .due, snapshot: .calm, manners: manners),
            .deliver(ChannelPlan(speak: true, chime: false, notify: false)))
    }

    func testTriggerTags() {
        XCTAssertNil(Trigger.due.tag)
        XCTAssertEqual(Trigger.afterCall.tag, .afterCall)
        XCTAssertEqual(Trigger.buttonPress.tag, .buttonPress)
    }

    func testChannelNamesRoundTrip() {
        XCTAssertEqual(Channels.all.names, ["voice", "chime", "notification"])
        XCTAssertEqual(Channels(names: ["notification", "voice"]), [.voice, .notification])
        XCTAssertEqual(Channels(names: ["jingle"]), .quiet)
        XCTAssertEqual(Channels.quiet.names, [])
    }
}

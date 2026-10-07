#if os(macOS)
    import DesklingCore
    import DesklingSystem
    import XCTest

    /// The real providers can't be asserted on (CI has no camera, battery or lock screen), but each must
    /// construct and answer without crashing or prompting.
    final class ProvidersSmokeTests: XCTestCase {
        func testIdleTimeIsNonNegative() {
            XCTAssertGreaterThanOrEqual(SystemIdleTimeProvider().secondsSinceLastInput(), 0)
        }

        func testBusyProviderAnswersWithoutTheCamera() {
            let reasons = SystemBusyStateProvider(cameraCheck: .off).currentBusyReasons()
            XCTAssertTrue(reasons.isSubset(of: [.call, .fullScreen]))
        }

        func testCoreMediaIOCameraCheckAnswers() {
            _ = SystemBusyStateProvider.isCameraInUse(using: .coreMediaIO)
            XCTAssertFalse(SystemBusyStateProvider.isCameraInUse(using: .off))
        }

        func testDefaultCameraCheckIsAVFoundation() {
            XCTAssertEqual(SystemBusyStateProvider().cameraCheck, .avFoundation)
        }

        func testPowerStateIsAbsentOrPlausible() {
            guard let state = SystemPowerSource().currentPowerState() else { return }
            if let percent = state.percent { XCTAssertTrue((0...100).contains(percent), "\(percent)") }
            XCTAssertFalse(state.isOnBattery && state.isCharging, "Charging means plugged in")
        }

        func testAudioOutputAnswers() {
            let output = SystemAudioOutput()
            _ = output.outputRoute()
            _ = output.isOutputMuted()
        }

        func testScreenStartsUnlocked() {
            let watcher = ScreenLockWatcher()
            XCTAssertFalse(watcher.isLocked)
            XCTAssertEqual(ScreenLockWatcher.lockedName.rawValue, "com.apple.screenIsLocked")
            XCTAssertEqual(ScreenLockWatcher.unlockedName.rawValue, "com.apple.screenIsUnlocked")
        }
    }
#endif

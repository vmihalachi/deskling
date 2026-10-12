# 2: Settings rows, the Support window, the chrome, screenshots

> Sequence 2 of `README.md` (read *Decisions*, *Settings/*, *Support/*, *Theme/ and the rest*). Needs nothing for 2.1–2.3;
> 2.4 needs 1 (the model type the script drives). One session, macOS.

## Read only these

- `README.md` here; `AGENTS.md`; `Sources/DesklingCompanionUI/{ScreenshotCapture,DesktopCompanion,DesktopLayout}.swift`,
  `Theme/{CompanionPalette,Space,SpeechBubble,StatusPill}.swift`; `Sources/DesklingShell/{ShortcutCapture,LoginItem}.swift`;
  `Sources/DesklingStore/PurchaseStore.swift` (the published state the Support window reads).
- From sunnysays `../sunnysays/macos/SunnySays/`: `Views/{SettingsView,SupportView,DesktopView}.swift`, `Theme/{Spacing,Styles,Fonts}.swift`,
  `App/{ScreenshotMode,PhraseText}.swift`, `Store/SupportModel.swift`.
- From Pagliacci `../pagliacci/macos/Pagliacci/`: `Views/{SettingsView,SupportView}.swift` (where its copy differs, so no
  label gets a default), `Theme/Styles.swift` (its `WindowInset`, `ActionBar`, `PanelRowStyle`, `Pointer`, `PagliacciWindowRoot`
  are the same as sunnysays'; its buttons and tiles are not and stay), `App/{JokeText,ScreenshotMode}.swift`.

## Steps

| Step | Commit | What |
|---|---|---|
| 2.1 | `CompanionUI: settings rows and editors` | `Settings/ActiveHoursEditor.swift`, `QuietHoursEditor.swift`, `ShortcutRecorder.swift`, `LaunchAtLoginToggle.swift`, `LanguageRow.swift`, `NotificationPermissionNote.swift`, `SettingsFooter.swift`, `CompanionSettings+Bindings.swift`, with the signatures in the README: every label a `Text` parameter, every style a generic parameter, no `String(localized:)` in the package. Tests in `Tests/DesklingCompanionUITests/Settings/`: the minute ↔ `Date` binding round-trips, the weekday order follows `Calendar.firstWeekday`, `QuietHoursEditor.label` per locale, the channel and flavor bindings write through, `mutedCount` ignores retired ids. |
| 2.2 | `CompanionUI: SupportWindow` | `Support/SupportWindow.swift`, `SupportCopy.swift`, `SupportProducts.swift`: `APP/Views/SupportView.swift`'s state machine (pitch → Supporter; just supported → welcome; supporter → thanks + tips; just tipped → thanks; status: error, pending, prices failed) over `PurchaseStore`, the character and every word injected. Tests with `MockStoreBackend`: the four states render the expected sections (`SupportWindowTests`, hosting the view in an `NSHostingView` and reading the accessibility tree). |
| 2.3 | `CompanionUI: chrome, fonts, localized phrases, the desktop stand` | `Theme/WindowInset.swift` (+ `windowPadding`), `Theme/ActionBar.swift`, `Theme/PanelRowStyle.swift` (hover = `companionPalette.accentSoft`), `Theme/Pointer.swift` (`color`, `lineWidth`, `headLength`: the two apps differ by a point), `Theme/CompanionWindowRoot.swift` (`companionWindowRoot(palette:displayFont:tint:bodySize:)`), `Theme/DisplayWeight.swift`; `DesklingShell/FontRegistration.swift`; `Phrase+Localized.swift`; `DesktopCompanion.stand(character:layout:shade:)` and `bubblePreview(text:tail:countdown:layout:)`, and `DesktopCompanion` draws its own window through the same `stand`. A render test: `stand` at the three sizes yields the `DesktopLayout` window sizes. |
| 2.4 | `CompanionUI: ScreenshotScript` | `Screenshots/ScreenshotScript.swift` and `ScreenshotCapture.SampleEnvironment`: `APP/App/ScreenshotMode.swift` lines 1–108 generalized (the loop over appearances, windows by id, a view at a size, a fitting view, the Supporter sequence, custom shots; `wants` filtering). Test: a script over the fixture model writes the expected file names into a temporary folder in both appearances (`DESKLING_SCREENSHOT_DIR`). |
| 2.5 | `docs: Settings/, Support/, Screenshots/, Theme/` | `AGENTS.md` layout row, CHANGELOG *Unreleased* "Added" lines. |

## Finish

`swift build && swift test`; `swift-format lint`; `scripts/verify.sh --all` ok; `swift.yml` green. Hand to 5: the
list of label parameters per view (the migration briefs map each to the app's existing string, so no catalog key
changes).

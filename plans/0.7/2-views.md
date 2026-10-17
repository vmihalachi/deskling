# 2: Settings rows, the Support window, the chrome, screenshots

> Sequence 2 of `README.md` (read *Decisions*, *Settings/*, *Support/*, *Theme/ and the rest*). Needs nothing for 2.1–2.3;
> 2.4 needs 1 (the model type the script drives). One session, macOS.

## Read only these

- `README.md` here; `AGENTS.md`; `Sources/LachingCompanionUI/{ScreenshotCapture,DesktopCompanion,DesktopLayout}.swift`,
  `Theme/{CompanionPalette,Space,SpeechBubble,StatusPill}.swift`; `Sources/LachingShell/{ShortcutCapture,LoginItem}.swift`;
  `Sources/LachingStore/PurchaseStore.swift` (the published state the Support window reads).
- From sunnysays `../sunnysays/macos/SunnySays/`: `Views/{SettingsView,SupportView,DesktopView}.swift`, `Theme/{Spacing,Styles,Fonts}.swift`,
  `App/{ScreenshotMode,PhraseText}.swift`, `Store/SupportModel.swift`.
- From Pagliacci `../pagliacci/macos/Pagliacci/`: `Views/{SettingsView,SupportView}.swift` (where its copy differs, so no
  label gets a default), `Theme/Styles.swift` (its `WindowInset`, `ActionBar`, `PanelRowStyle`, `Pointer`, `PagliacciWindowRoot`
  are the same as sunnysays'; its buttons and tiles are not and stay), `App/{JokeText,ScreenshotMode}.swift`.

## Steps

| Step | Commit | What |
|---|---|---|
| 2.1 | `CompanionUI: settings rows and editors` | `Settings/ActiveHoursEditor.swift`, `QuietHoursEditor.swift`, `ShortcutRecorder.swift`, `LaunchAtLoginToggle.swift`, `LanguageRow.swift`, `NotificationPermissionNote.swift`, `SettingsFooter.swift`, `CompanionSettings+Bindings.swift`, with the signatures in the README: every label a `Text` parameter, every style a generic parameter, no `String(localized:)` in the package. Tests in `Tests/LachingCompanionUITests/Settings/`: the minute ↔ `Date` binding round-trips, the weekday order follows `Calendar.firstWeekday`, `QuietHoursEditor.label` per locale, the channel and flavor bindings write through, `mutedCount` ignores retired ids. |
| 2.2 | `CompanionUI: SupportWindow` | `Support/SupportWindow.swift`, `SupportCopy.swift`, `SupportProducts.swift`: `APP/Views/SupportView.swift`'s state machine (pitch → Supporter; just supported → welcome; supporter → thanks + tips; just tipped → thanks; status: error, pending, prices failed) over `PurchaseStore`, the character and every word injected. Tests with `MockStoreBackend`: the four states render the expected sections (`SupportWindowTests`, hosting the view in an `NSHostingView` and reading the accessibility tree). |
| 2.3 | `CompanionUI: chrome, fonts, localized phrases, the desktop stand` | `Theme/WindowInset.swift` (+ `windowPadding`), `Theme/ActionBar.swift`, `Theme/PanelRowStyle.swift` (hover = `companionPalette.accentSoft`), `Theme/Pointer.swift` (`color`, `lineWidth`, `headLength`: the two apps differ by a point), `Theme/CompanionWindowRoot.swift` (`companionWindowRoot(palette:displayFont:tint:bodySize:)`), `Theme/DisplayWeight.swift`; `LachingShell/FontRegistration.swift`; `Phrase+Localized.swift`; `DesktopCompanion.stand(character:layout:shade:)` and `bubblePreview(text:tail:countdown:layout:)`, and `DesktopCompanion` draws its own window through the same `stand`. A render test: `stand` at the three sizes yields the `DesktopLayout` window sizes. |
| 2.4 | `CompanionUI: ScreenshotScript` | `Screenshots/ScreenshotScript.swift` and `ScreenshotCapture.SampleEnvironment`: `APP/App/ScreenshotMode.swift` lines 1–108 generalized (the loop over appearances, windows by id, a view at a size, a fitting view, the Supporter sequence, custom shots; `wants` filtering). Test: a script over the fixture model writes the expected file names into a temporary folder in both appearances (`LACHING_SCREENSHOT_DIR`). |
| 2.5 | `docs: Settings/, Support/, Screenshots/, Theme/` | `AGENTS.md` layout row, CHANGELOG *Unreleased* "Added" lines. |

## Finish

`swift build && swift test`; `swift-format lint`; `scripts/verify.sh --all` ok; `swift.yml` green. Hand to 5: the
list of label parameters per view (the migration briefs map each to the app's existing string, so no catalog key
changes).

## Outcome (2026-10-12)

Done in one session after sequence 1. Where the API differs from the README, and the label parameters per view (the
migrations map each to the app's existing string, so no catalog key changes):

- **`ActiveHoursEditor(settings:activeHours:from:to:between:days:chipStyle:)`**: five labels, not four. `from` and `to`
  label the pickers for VoiceOver (the apps' `"From"`, `"To"`), `between` is the visible `"to"` between them (comment
  "Between the two active-hours time pickers"). `orderedWeekdays(firstWeekday:)` and
  `minuteBinding(_:_:calendar:)` are public statics.
- **`QuietHoursEditor(settings:title:chipStyle:)`** takes one chip style (the apps pass their chip with
  `horizontalPadding: Space.tight`), not a closure. `label(_:locale:calendar:)`.
- **`ShortcutRecorder(shortcut:pressKeys:record:clear:hint:buttonStyle:onCaptureStarted:onCaptureEnded:)`**: the apps
  pass `$settings.sayShortcut`, `model.suspendHotKey`, `model.registerHotKey`. The cross is `palette.inkSoft`.
- **`LaunchAtLoginToggle(label:errorText:errorColor:)`** owns its state (two rows in a `Form`).
- **`LanguageRow(label:change:buttonStyle:)`**, **`NotificationPermissionNote(permission:text:open:buttonStyle:action:)`**
  (draws nothing unless denied; the app still checks the channel), **`SettingsFooter(_ text: Text)`**.
- **`SupportWindow(purchases:products:copy:look:onDone:)`**: the apps' windows differ in more than buttons, so the
  look is a `SupportLook` (title view, label view, the framed character `(celebrating) -> AnyView` with the apps' own
  sizes, the celebration bubble, `primary`, `largePrimary`, `outline`, `tipPriceOpacity`: sunnysays 0.8, Pagliacci
  0.85). `SupportCopy` has no `title` (the title is a view in the look). The spring is sunnysays' (damping 0.7;
  Pagliacci's 0.75 is not in a screenshot).
- **`DesktopCompanion.stand(character:layout:)`** takes no shade: the palette's from the environment.
  `DesktopLayout.shadowWidthRatio` is new: Pagliacci's sheet drew 0.5 while its desktop window drew 0.42; M7 sets 0.5
  so both match (its desktop screenshot stays the same, the real desktop changes).
- **`ScreenshotScript(model:backend:root:paper:)`** with `run(prepare:shots:)` and `take(_:)`;
  `ScreenshotCapture.SampleEnvironment(prices:now:)` gives the store, clock, providers, signals, silent channels and
  `purchases(catalog:prefix:)`; `SilentNotifier` is public.
- **`Phrase+Localized`** landed with sequence 1 (the model needed it).

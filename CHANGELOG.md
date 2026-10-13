# Changelog

All notable changes to Deskling. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project uses [Semantic Versioning](https://semver.org/). One tag (`vX.Y.Z`) versions the Swift
package and the NuGet packages together.

## Unreleased

## 0.7.7 - 2026-10-13

- **Tools**: `fonts.py` builds more than one face: `fonts.faces` lists further variable fonts under the same
  license (an upright beside an italic), each with its own `family`, pinned `font` and `weights`.
- **Windows.Companion** (new .NET package, `net10.0-windows`, code only): the Windows half of a companion app's shell,
  over `Deskling.Companion.Shell` and `Deskling.Windows`. `Speaker` (`ISpeaking`): a recorded clip, or the text through
  Windows' speech synthesizer with the chosen voice or the language's (`Speaker.Voices(language)` for a voice picker),
  played through a `MediaPlayer` kept out of the system media controls; the clip's rate keeps its pitch and
  `ClipTime` is the clip's own position; a clip that can't play ends the line. `ChimePlayer` (`IChimePlaying`): the app's
  `Sounds/<sound>.wav`, several at once, at the user's volume.
- **Windows.Companion**: the shell's services. `WindowsCompanionHost<TWindow>` (`ICompanionHost`) over the app's
  `CompanionViews` (each window's `WindowSpec` and content, the tray panel, the desktop character); `WindowManager<TWindow>`
  (`IWindowHost`: content under a theme-following title bar, sized in DIPs on its display, centered or by the tray,
  exclusive windows, minimum sizes, always on top); `TrayStatusItem` (`IStatusItem`: the glyph at 8 fps while talking or
  waving, the panel next to the icon sized to its content and closing when it loses focus or on Esc, the model's menu
  built fresh); `GlyphRenderer` (a rig's glyph frame as an `HICON`, black or white ink by the taskbar); `DispatcherTimers`
  (`ITimers`); `HotKey` (`IHotKey`); `CompanionNotifier` (`ICompanionNotifying`: the line as a silent notification with
  the app's buttons); `MenuConversion` (`MenuItem` to `TrayMenuItem`); `ReduceMotion`; and `SystemAudioOutput`,
  `SystemScreenLock`, `SystemPowerSource`, `SystemAppearance` as the signal hub's sources.
- **Windows.Companion**: `CompanionApp`, the app's `Application` (Swift's `CompanionAppDelegate`) and its `App.xaml`
  root. `CompanionApp.Start(instanceKey, makeApp)` is the whole of `Program.Main` (one copy runs; a second launch reopens
  the first); the app overrides `MakeCompanion()` (and `RunScreenshots()` for a screenshot run). The launch shows Home
  unless the startup task started the app at sign-in or a notification click did, and the app lives on with no window
  open. `CompanionNotifier.Configure` takes the click Windows started the app for; `WindowsCompanionHost.Quit` runs the
  app's `OnQuit`, then takes the tray icon, windows and hotkey away and exits.
- **Windows.Companion**: `CharacterCanvas`, the twin of `CharacterView`.
  - It draws a rig from its art: vector parts as XAML paths in a `CompanionPalette`'s paints, raster parts from the
    app's `Assets\Characters\<skin>.<part>.scale-<n>.png`.
  - It poses the character from the mood at up to 30 fps, with the talking mouth on the voice's level and the spring
    back from Shh.
  - It holds still when Windows' animation effects are off, with `Animated = false`, in a screenshot run, and while its
    window is hidden.
  - `IsDrawnAt` answers the desktop window's click-through from the shapes and the images' alpha in the pose on screen.

  New alongside it:
  - `CompanionPalette`: a `ThemedColor` per UI role and per paint, `System`, `Contrast`, and `ThemedColor.Resource`
    to read a color from the app's theme dictionaries.
  - `CharacterMotion`, `CharacterScene`, `CharacterImages` with `AlphaMask`, and `ScreenshotCapture.IsEnabled`.
- **Windows.Companion**: the controls every companion draws the same, in the colors of `CompanionLook.Current` (the
  app's palette and display face; the controls repaint when the theme, the look or Windows' contrast theme changes).
  - `SpeechBubble`: the tail toward the character on any side (`BubbleTail`, `BubbleShape`), a soft shadow, words that
    fill the width or hug, and a countdown dial the compositor drains.
  - `StatusPill`.
  - `ActionBar`: a grid, so a row of buttons fills it, or a star column pushes them to the end.
  - `PanelRow`: a tray panel row that fills with `accentSoft` under the pointer.
  - `Pointer`: down and right toward the tray by default.
  - `Space` and `WindowInset`: the Mac's spacing, plus the title bar's band at the top.

  The windows and the tray panel get the look's paper behind their content.
- **Windows.Companion**: the character on the desktop.
  - `DesktopCompanion` (`IDesktopCompanion`): the app's character view standing on a soft contact shadow, by the
    taskbar's corner or where it was dragged. It is in neither the taskbar nor Alt+Tab, never takes the keyboard, and
    clicks pass through everywhere it isn't drawn.
  - Its speech bubble rides beside it, toward the screen's middle.
  - A click says something, a drag moves it, a right click opens the model's desktop menu, and Ctrl+wheel or a pinch
    resizes it.
  - `DesktopWindow` is the floating window underneath. The host makes the companion from the app's
    `CompanionViews.DesktopCharacter`.
- **Windows.Companion**: the Settings rows and the Support window, in the app's words (copy records) and styles.
  - `ActiveHoursEditor`: the from and to pickers and the weekday chips, in the culture's week order.
  - `QuietHoursEditor`: 24 hour chips, labelled as the culture writes hours.
  - `ShortcutRecorder`: the say-something shortcut.
  - `LaunchAtStartupToggle`: the package's startup task.
  - `LanguageRow`: the app's languages in their own words, through `ApplicationLanguages.PrimaryLanguageOverride`,
    with a restart.
  - `NotificationPermissionNote` and `SettingsFooter`.
  - `SettingsBindings`: channels, flavors and the muted count.
  - `SupportWindow` (with `SupportCopy`, `SupportProducts`, `SupportLook`): the supporter purchase, then the tips, over
    `PurchaseStore`; one ask at a time and only thanks right after a purchase.
- **Windows.Companion**: the screenshot run.
  - `ScreenshotCapture` draws each window as the screen shows it (`PrintWindow`, caption buttons included) to
    `<name>-<appearance>.png`, in light and dark, or in the contrast theme while Windows is in one. It checks each with
    `LayoutCheck.TextProblems`, writes the problems next to the shots, and holds every animation still while it runs.
  - `ScreenshotScript<TWindow, TSettings>` takes the shot list (windows, views at a size or their fitting size, the
    supporter sequence, the app's own) over a model on a `SampleEnvironment`; `CompanionApp.RunScreenshots` starts it.
- **Fixture app**: `scripts/fixture-screenshots.ps1` renders its windows.
- **Companion.Shell**: `ICompanionLaunching` (`Launch`, `Reopen`), which `CompanionAppModel` implements.
- **Fixture app** (`dotnet/Fixture.App`, not packed): a WinUI app over `Deskling.Windows.Companion` and the fixture bank,
  for checking the Windows shell by hand; also the smallest example of a companion app on it.
- **Windows**: `TrayMenuItem.Command` takes `IsChecked` (a check mark), and `TrayIconHost.MenuOpening` is raised before
  the menu opens, so its owner can rebuild it from the current state. Both additive.
- **Companion.Shell** (new .NET package, `net10.0`): the platform-free half of a companion app's shell, the C# twin of
  `DesklingCompanionUI`'s logic, tested on any OS. `SpeechSequencer` and `VoiceLibrary` (with `VoiceId`, `SpeechPart`,
  `SpeechRates.ClipRate`) over `ISpeaking`, `IChimePlaying` and `ITimers`; `DeliveryPerformer`, `PhraseLocalization`
  (`LocalizedParts`, `LocalizedText` through the app's lookup) and `VoiceLibraryBank.PendingIds`; `SignalHub` over
  `IIdleTimeProvider`, `IBusyStateProvider` and the new `IAudioOutputState`, `IScreenLock`, `IPowerSource`, `IAppearance`;
  `DesktopLayout`, `FloatingPlacement` (y-down screen coordinates), `BubbleMetrics`, `DesktopHide`. `SilentSpeaker` and
  `SilentChime`.
- **Companion.Shell**: `CompanionAppModel<TWindow, TSettings>`, the Swift app model in C# (`INotifyPropertyChanged`;
  the same actions and the same overridable surface, view hooks left to the platform), over `ICompanionHost<TWindow>`
  (`IWindowHost`, `IStatusItem`, `IDesktopCompanion`, `IHotKey`, `ITimers`, appearance, quit) and
  `ICompanionNotifying`; `WindowRoles`, `StatusCopy` (times as moments, the next line in minutes: the app formats them),
  `DesktopCopy`, `MenuCopy`, `CompanionSounds`, `CompanionAppConfiguration`, `NotificationPermission`,
  `BubbleCountdown`; `CompanionMenus` building `MenuItem` data for the tray's and the desktop character's menus;
  `SampleEnvironment` (with `FixedClock`, `ConstantIdle`, `SilentNotifier`, `FixedPriceBackend`) for screenshot runs.
- **Testing**: `Deskling.Testing` depends on `Deskling.Companion.Shell` and adds `ManualTimers` (timers a test runs by
  hand, moving a `MockClock`), `FakeSpeaker`, `FakeChime`, `FakeNotifier` and `FakeHost<TWindow>` (with
  `FakeStatusItem`, `FakeWindowHost`, `FakeDesktopCompanion`, `FakeHotKey`).

- **Tools**: `l10n.py status --since REF` and `export --since REF` flag the landing page's changed English when the
  landing page is a submodule (or any nested repository): its files are read from that repository, at the commit the
  app's `REF` records for it, else (not recorded, or not fetched) at its own `REF`. Before, `git show` in the app's
  repository found only the gitlink, so nothing was flagged.

## 0.7.6 - 2026-10-13

- **Companion**: the mouth can follow a recorded voice's loudness. `RiggedCharacter.pose(mood:time:mouthLevel:)` takes
  a level 0–3 (the clip's envelope right now): 0 shows the mood's new optional `restMouth` (the closed mouth between
  words; the first mouth without one) with no pulse, 1–3 step through the talking mouths, least open first, and scale
  the pulse by level / 3. `pose(mood:time:)` (nil) keeps the timer, so every existing pose is unchanged. The art JSON
  writes `restMouth` only when set, and a skin whose talking rig has one also pins `voiced[]` in its poses vector
  (levels 0–3 at 0.2 s); C# mirrors both (`Pose(mood, time, mouthLevel)`, `CharacterReplay`). `tools/character.py`
  reads `restMouth` from a mood in the rig and checks the part exists.
- **Shell**: `Speaking.clipTime` (default nil; `Speaker` reports the playing clip's position), `SpeechPart.mouth` and
  `mouthFPS` (the clip's envelope from the voice manifest), and `SpeechSequencer.mouthLevel`, the envelope's level at
  the clip's position (nil without an envelope, so the system voice keeps the timer; 0 past its end).
- **Companion UI**: `DeliveryPerformer` gives each recorded part its envelope (`VoiceLibrary.mouth(for:voice:)`, which
  nothing read before) and exposes `mouthLevel`, as does `CompanionAppModel`; `CharacterView(…, mouthLevel:)` reads
  it every frame while talking, so pass `{ model.mouthLevel }` where the view gets `talking: model.isVoicing`. Reduce
  Motion and screenshot runs still hold the still pose.

## 0.7.5 - 2026-10-13

- **Shell**: `WindowManager` leaves the activation policy alone in a process hosting unit tests
  (`XCTestConfigurationFilePath` is set). Window tests open and close windows many times a second, and the Dock kept a
  tile for every switch to regular after the test host exited: one run of mybackhurts' `AppFlowTests` left ten dead
  icons, a day of runs a full Dock.

## 0.7.4 - 2026-10-13

- **Shell**: an app macOS opens at login stays in the menu bar again instead of showing Home. `CompanionAppDelegate`
  read the launch Apple Event in `applicationWillFinishLaunching`, before AppKit delivers it, so every launch looked
  like the user's; it now reads it in `applicationDidFinishLaunching` (and no longer implements
  `applicationWillFinishLaunching`). `LaunchContext`'s doc says where the event can be read.

## 0.7.3 - 2026-10-12

- **Tools**: the pre-commit hook's `verify.sh --staged` passes in a linked worktree (`git worktree add`,
  `.claude/worktrees/<name>/`). git in a submodule (the `deskling/` tag the pin check reads, the voices and landing
  checks, `voice.py publish`) runs without the `GIT_DIR`, `GIT_INDEX_FILE`… a git hook exports for the app's repository
  (`companion.git_env`), which pointed `git -C deskling describe` back at the app. The xcodegen check says when a group
  is named after the checkout's folder: a source outside `mac.dir` (`../voices/Voice`) needs a `group:` in the spec,
  or every checkout generates another project.
- **Landing**: the padded icons find the icon's root `<svg>` tag by index, not by regex, so an SVG with many unclosed
  `<svg` or `<?xml` tags can't make the render take quadratic time (CodeQL `js/polynomial-redos`).
- **Tools**: `verify.sh --all` skips the checks of a section the app's `companion.json` doesn't have (no `store`, no
  `windows`…) instead of running them and failing, and reports the store check as `skip` until `store.dir` holds the
  source language's listing. `companion.py env` exports `RE_NEVER` and `STORE_LISTING` for it.
- **Tools for an app without a character** (mybackhurts' adoption, `plans/0.7/6-mybackhurts.md`):
  - `companion.json`: `mac.core` is optional (an app without a core package); `l10n.glossarySources` adds glossary
    keys read from the app's code (each match of a pattern's group in a file, such as a catalog's names);
    `hooks.guards` guards paths with the reason the agent sees, and with `reads` refuses loading them too;
    `hooks.slow` replaces a built-in slow check's paths and label, or adds one, counted as run by a `ran` command
    pattern.
  - `l10n.py`: work files carry `%#@name@` substitutions (a slot per name with every plural category, `%arg` where
    the number goes) and `apply` writes them back with their `argNum` and format; the rules explain them. `.po`
    exports have Polish, Czech, Japanese, Korean and Chinese plural forms.
  - `hooks/hook.py`: a `cloud-stop` mode for Copilot's cloud coding agent (in GitHub Actions only: `verify.sh` on the
    branch against origin's default branch); paths are compared with forward slashes on Windows; Codex's argv
    commands are read as one command line; apply_patch paths are read against the session's directory (`cwd`), not
    the hook's.
  - `store/push.py`: an in-app purchase without `reviewNote` or `reviewShot`, and an app without the review notes
    file, leave App Store Connect's own alone (the schema no longer requires `reviewNote`).
  - `@deskling/landing`: `og` is optional, for an app that renders its own social cards in `extraPlugins`.

## 0.7.2 - 2026-10-12

0.7.1 with `@deskling/landing` on npm: 0.7.1 reached NuGet and GitHub, but its npm job passed the tarball as
`npm/….tgz`, which npm reads as a GitHub repository. The same code as 0.7.1; use 0.7.2.

- **Release**: the npm job stages `./npm/….tgz`, and the Tools workflow dry-runs that same command on every push.

## 0.7.1 - 2026-10-12

0.7.0 as it was meant to ship: its tag exists (SwiftPM sees it), but its release stopped at packing
`@deskling/landing`, before NuGet, npm and the GitHub release. Use 0.7.1 everywhere 0.7.0 is named; nothing else
changes.

- **Release**: the landing job creates the folder it packs into, and the Tools workflow packs the same way on every
  push, so a release can't be the first to run it.

## 0.7.0 - 2026-10-12

The companion shell: what sunnysays and Mister Pagliacci still repeated on top of 0.6 (the app model, its delegate
and menus, the Settings rows, the Support window, the window chrome, the screenshot run, the vector case lists, the
landing page's plumbing and the store tools) is Deskling's now. Every word and the look stay the apps'.

### Migration
- **One name collides.** An app that defines its own `View.windowPadding(bottom:)` (both companion apps do) deletes
  it: the package's is the same, and the two are ambiguous. Nothing else needs changing to build on 0.7.0. To shed the
  rest of an app's copies, follow sunnysays' M10 brief (`plans/milestones/10-companion-shell.md` there).

### Changed
- **`companion.json` no longer requires `character` or `conformance`**: an app without a phrase bank uses the
  tools too; the bank's tools say the section is missing.
- **Tools: a landing page kept in its own repository.** When `landing.dir` is a submodule, `verify.sh` lists the files
  changed inside it (since the commit the app records, so its commits not yet recorded count too) instead of only the
  submodule's path, so the landing checks run; and `setup-linux.sh` fetches it.

### Fixed
- **The desktop bubble's words stay inside it.** A new line used to animate its card and words from the last line's
  shape and place while the window took its new size at once, so for a moment (and in a running app sometimes until
  the bubble went away) the words hung out of the top of the card. `DesktopCompanion` now lays each line out at exactly
  the measured size from the window's top left, as a view of its own (no animation from the line before), and
  `FloatingWindow.setFrame(_:)` lays the content out at the new size before it shows.

### Added
- **The companion shell (`DesklingCompanionUI/Shell/`)**: what every companion app's model, delegate and menus repeated.
  `CompanionAppModel<Window, Settings>` is an `@Observable open class` with the 20 s tick, the channels, the status
  item, the desktop presence and bubble, the windows by role (`WindowRoles`), the hotkey, the store's start, mute with
  Undo, previews (`say(_:)` clears `bubblePhraseID`) and the status pill's words; the app subclasses it and supplies its
  views, character and words through open hooks (`windowContent`, `panelContent`, `menuCopy`, `desktopCharacter`,
  `desktopLayout`, `glyphCharacter`; `glyphMood`, `preferredVoice`, `willSay`, `showBubble`, `desktopBubbleWaits`,
  `voicingChanged`, `handleNotificationAction`, `settingsDidReset`, `willLaunch`, `stopSpeaking`) and
  `CompanionAppConfiguration` (`StatusCopy`, `DesktopCopy`, `CompanionSounds`, the palette and face, the supporter
  product). `observe(_:then:)` watches the app's own preferences. `CompanionNotifying` is the notification it drives
  (actions by raw id). `CompanionAppDelegate` (subclassed, hands over `companion`; `runScreenshots()`),
  `CompanionMenus` over `MenuCopy`, a public `ActionItem`, `DesktopHide`.
- **Settings rows and editors (`DesklingCompanionUI/Settings/`)**: `ActiveHoursEditor`, `QuietHoursEditor`,
  `ShortcutRecorder`, `LaunchAtLoginToggle`, `LanguageRow`, `NotificationPermissionNote`, `SettingsFooter`, each taking
  every label as a `Text` and its chip or button style as a generic parameter; `CompanionSettings` channel and flavor
  bindings and `mutedCount(in:)`.
- **`SupportWindow` (`DesklingCompanionUI/Support/`)**: the supporter purchase, the tips and the thank-yous over
  `PurchaseStore`, with every word in `SupportCopy`, the products in `SupportProducts` and the app's look in
  `SupportLook`; `SupportWindow.stage(...)` is its state machine.
- **The chrome (`Theme/`)**: `WindowInset` and `windowPadding(bottom:)`, `ActionBar`, `PanelRowStyle`, `Pointer`,
  `companionWindowRoot(palette:displayFont:tint:bodySize:)`, `DisplayWeight`; **`FontRegistration`** (DesklingShell)
  registers an app's bundled fonts once.
- **`DesktopCompanion.stand(character:layout:)`** (`CompanionStand`, what the desktop window draws) and
  **`bubblePreview(text:tail:countdown:layout:)`**, for screenshot sheets; **`DesktopLayout.shadowWidthRatio`** (0.42 by
  default).
- **`ScreenshotScript` (`DesklingCompanionUI/Screenshots/`)**: an app's screenshot run as a list of shots in light and
  dark over its model; `ScreenshotCapture.SampleEnvironment` (what the run's model is built on) and `SilentNotifier`.
- **Standard case lists (`DesklingTesting`)**: `QuietTimeline.standardCases(base:dst:)` (the 17 timelines every app
  pins, the DST one named and zoned by the app) and `PickerVector.standardCases(bank:skin:language:single:pair:
  leftEmpty:excluded:recent:)` (the 15 shared picker cases); **`CompanionVectors`** and
  **`ConformanceSuite.companion(_:root:regenerateHint:)`**, an app's whole suite from its content. sunnysays' and
  Pagliacci's committed vectors come out of them byte for byte.
- **`CompanionReplay` (Deskling.Testing)**: one call per vector kind over an app's vectors folder, so its replay test is
  theories and one assertion each.
- **`@deskling/landing` (`tools/landing`)**: a landing page's localized pages, legal pages, sitemap, CSP `_headers` and
  social cards as Vite plugins, from `companionLanding(options)`; the app keeps its data, placeholders, JSON-LD, brand
  and page. An npm package (the landing pages are their own repositories): `release.yml` stages it on npm at the tag's
  version through trusted publishing (no stored token), and an owner approves it with 2FA. sunnysays' and Pagliacci's
  landing builds come out the same.
- **The store tools (`tools/store`)**: `check.py` (each listing field against the stores' limits), `push.py` (App Store
  Connect: text, in-app purchases with their creation, screenshots, previews, review notes, `--status`) and
  `import.py` (a Partner Center import folder), over `companion.json`'s new `store` section; `verify.sh` runs
  `check.py` when a listing file changes. **`l10n.py`** checks `%#@name@` plural substitutions in every category.
- **`DeliveryPerformer.onVoicingChanged`** and **`SpeechSequencer.onVoicingChanged`**: audio started or stopped, false
  in the beat between a joke's parts, so the mouth moves only while words are heard (an app's own voicing wrapper can
  go).
- **`Phrase.localizedParts(bundle:)`, `localizedText`, `Phrase.localize(_:_:bundle:)`**: a phrase in the user's
  language from the app's String Catalog.
- **`FloatingWindow.idealSize(of:maxWidth:)`** measures content before it goes into the window.

## 0.6.5 - 2026-10-12

### Fixed
- **`LayoutCheck.TextProblems` (Deskling.Windows) measures the real text**: it flagged hundreds of false problems in
  French. A detached probe rounds each line to whole DIPs (so a line wrapped at its own width), copied letter spacing
  that a button's label reads back but never applies, and counted a label's `MinWidth` as text. Lines are now measured
  on the real TextBlock with its width limits lifted for the measure (restored after), and the word probe takes the
  spacing that reproduces the box's own width. It lays the tree out again when it's done, so run it after the
  screenshot, not before.

## 0.6.4 - 2026-10-12

### Added
- **`LayoutCheck` (DesklingShell)** finds SwiftUI content taller than its window: it measures the content's ideal
  height at the window's width and skips windows that scroll. `overflow(in:)` for a window, `overflow(of:in:)` for a
  view on its own, `contentRoom(forWindowOfSize:)` for the room below a `WindowManager` window's title bar.
- **`ScreenshotCapture` checks every shot**: a window that couldn't be drawn, a PNG that wasn't written, or content
  taller than its window is a problem (`problems`, `report(_:)` for the app's own), and `finish()` writes them to
  `problems.txt` next to the shots.
- **`tools/screenshots.sh` fails on `problems.txt`** (after copying the shots), and **`tools/check-layout.sh`** renders
  the windows in every language (or the ones named) into `<mac.dir>/build/layout-check/` and names the languages that
  don't fit. Apps add a two-line wrapper like `screenshots.sh`'s.
- **`LayoutCheck` (Deskling.Windows)** finds WinUI text that doesn't fit its box: `TextProblems(root)` lists every
  visible TextBlock cut off with an ellipsis, wider than its room, breaking a word across lines or cut off at the
  bottom. **`XamlCast.As<T>`** reads an untyped XAML object as a type under Native AOT, where a cast throws and
  `as`/`is` quietly fail.

## 0.6.3 - 2026-10-12

### Fixed
- **`voice.py publish` pushes to the voices repository's default branch** (`master`, `main`…), read from `origin`;
  it always pushed to `main`, which created a stray branch in repositories whose default is `master`.

## 0.6.2 - 2026-10-12

### Added
- **A Voiced filter on the voice review pages**, next to Waiting for a voice: only the lines with a current clip.

## 0.6.1 - 2026-10-12

### Added
- **`voice.reviewColors` in `companion.json`** gives the voice review pages and the friends' site the app's own colors
  (per scheme, a token → `#RRGGBB` map: `paper`, `card`, `ink`, `ink-soft`, `line`, `accent`, `accent-soft`, `on-accent`,
  `done`, `done-soft`, `flag`, `flag-soft`, `link`). Apps that set none keep today's colors.

## 0.6.0 - 2026-10-12

### Changed
- **`Frequency` has seven presets**, from every 15 minutes to every 3 hours: `every45Minutes`, `every90Minutes`,
  `every2Hours` and `every3Hours` join `chatty` (15 min), `sometimes` (30 min) and `rarely` (1 h), whose raw values
  stay. The cases are declared from the most frequent to the least (`allCases` and C#'s `Frequencies.All` list them in
  that order, for a picker), so an app's `switch` over `Frequency` needs the new cases.
- **The default frequency is every hour** (`rarely`, was `sometimes`): a character also speaks on its occasions, so
  twice an hour on top was a lot. Settings a user chose stay; the app's `settings.json` vector changes.

### Migration
- Give the four new cases a label wherever the app switches over `Frequency` (its frequency picker), then regenerate
  the app's vectors: `settings.json` gets the new default and presets, and any timeline written for the old default
  that should keep its cadence sets `frequency: "sometimes"`.

## 0.5.2 - 2026-10-12

### Fixed
- `Formats.WeekdayMonthDay` and `Formats.Hour` (Windows) threw in apps built with Native AOT (`PublishAot`, Debug runs
  included): the language list was a collection expression the projection couldn't marshal, so a window that formatted
  a date never opened. `Deskling.Windows.Tests` now builds with `PublishAot` too, so its tests catch this kind of interop.

## 0.5.1 - 2026-10-12

### Fixed
- `tools/dev-icon.swift` makes `AppIcon-Dev.appiconset` (starting from `AppIcon`'s `Contents.json`) when an app doesn't
  have one yet, instead of failing with "Can't write".
- `tools/voice.py site`: the README says "The recorded lines of <character>" without ", in <app>" when the app is
  named after its character, and the LICENSE names `voice.copyrightHolder` (new, optional; default `displayName`).

## 0.5.0 - 2026-10-11

The companion layer: everything under sunnysays' character, made generic so a second companion app can be built on
it. A typed settings store, `DesklingCompanion` / `Deskling.Companion` (phrases, cadence, quiet rules, settings,
coordinator, desktop presence, vector and raster character rigs, skins), `DesklingCompanionUI`, speech and a status
item in `DesklingShell`, `Deskling.Testing`, and `tools/`. Desktop only: macOS 14 and Windows, as before.

### Migration
- **Apps on 0.4 without the companion layer** (mybackhurts): only the settings store is breaking. Swift: drop an
  app-local `SettingsStore` protocol for `DesklingCore`'s; `PurchaseStore(…defaults:)` keeps working. .NET: move
  `ISettingsStore` callers to the typed getters and setters (below). Staying on 0.4.0 until then is fine.
- **Moving a companion app onto it** (sunnysays): import `DesklingCompanion` in place of the app's own copies. The raw
  values, settings keys and vector formats are unchanged, so committed vectors stay byte-identical. Renames: `Character`
  → `DrawableCharacter`, `Shape` → `PathShape`, `PaletteRole` → `PaintRole`, `Phrase.Kind.line(english:)` →
  `line(source:)`, a mood's `cheeks`/`zz` flags → `headExtras`/`overlays` (the rig JSON still accepts the flags), and
  in C# `PartContent.Vector` for Swift's `.shapes`. The app now injects what the package no longer names: its
  `PhraseBank`, `CompanionDefaults(skin:phraseFlavors:sayShortcut:)`, `SkinCatalog`, `CompanionPalette` and voices.
  App-only preferences go in a `CompanionSettings` subclass. Content vectors are generated with `DesklingTesting`'s
  generators and replayed with `Deskling.Testing`; the app's `cadence/` and `desktop.json` vectors retire (they are
  pinned here now). `tools/README.md` has the steps for the `deskling/` submodule and `companion.json`.

### Added
- Swift: `DesklingCompanion`, the pure core of a desk companion (Foundation and Observation only; builds and tests
  on Linux, now a job in `swift.yml`). `Phrases/`: `Tag`, `FlavorID` and `SkinID` (app-named string ids), `Phrase`
  (a line, a tune, or a `sequence` of two or more parts with keys `phrase.<id>.1`, `.2`…; `languages` scopes a
  phrase to the languages it exists in), `PhraseBank` (a container: the app injects its lines), `PhraseContext`
  (skin and language required) and `PhrasePicker` (drops phrases outside the context's language first).
  `Cadence/`: `Frequency`, `ChatterCadence`. `Rules/`: `Snapshot`, `Channels`, `Trigger`, `ChannelPlan`,
  `SkipReason`, `Outcome`, `Manners`, `QuietRules`, `Mood`, `CompanionState`. `Settings/`: `SettingsKeys` (the 23
  generic keys, same strings), `CompanionDefaults` (the package's numbers; skin, flavors and shortcut required),
  `CompanionSettings` (an `@Observable open class` with the 23 write-through properties and `manners`, which an app
  subclasses to add its own keys), `AppearanceSetting`, `DesktopSize`, `DesktopPosition`. `Coordination/`:
  `Delivery`, `CompanionCoordinator` (bank and language injected). `Desktop/`: `DesktopPresence`. `PhraseTiming`
  spaces the parts of a sequence. Moved from sunnysays with the same raw values and keys;
  `Phrase.Kind.line(english:)` is now `line(source:)`.
- Swift: `DesklingTesting` depends on `DesklingCompanion` and has `Fixtures/`: `PhraseBank.fixture`, `FixtureIDs`
  (flavors a/b/c, skins `fixture-vector` and `fixture-raster`) and `CompanionDefaults.fixture`.
- `tools/character.py`: compiles a skin's art and rig into the `CharacterArt` JSON and Swift source, from
  arguments (`--design --skin --out --swift-out --module --mode vector|raster --assets --windows-assets --check`)
  instead of sunnysays' constants, or, without `--design`, every skin from the app's `companion.json`. Rigs take `headExtras`/`overlays` (`cheeks: true`/`zz: true` stay as sugar) and
  require `bubble: {pivot, tuck}`. Raster skins draw parts from `layers/<part>@2x.png` (+ `@3x`), sized frame ×
  scale, and get an imageset per layer and Windows `scale-200`/`scale-300` PNGs. Tests in `tools/tests/`, run by
  the new `tools.yml`; fixture skins in `Tests/Fixtures/characters/`.
- Swift: `DesklingCompanion` `Figures/`: `Point`, `Size`, `Rect`, `PaintRole` (an app-named string; `ink` for
  glyphs), `PathCommand`, `PathShape`, `PartContent` (shapes, or an image asset placed in a frame), `Transform` (with
  `affine`), `Pose` (`headExtras`, `overlays`), `CharacterArt` (`bubblePoint`), `Rig`, `RigGroup`, `Blink`,
  `BubbleAnchor`, `MoodRig`, the `DrawableCharacter` protocol with `PlacedPart`, `RiggedCharacter` (`layers(for:)`;
  `shapes(for:)` skips image parts) and `CharacterArtJSON` (the compiler's exact bytes, and a decoder that refuses a
  rig naming missing parts). `Skins/`: `SkinEntry`, `SkinCatalog` (a locked, refunded or unknown skin draws the
  default). Moved from sunnysays: `Shape` is now `PathShape`, `PaletteRole` is now `PaintRole`, `Character` is now
  `DrawableCharacter`, and a mood's `cheeks`/`zz` flags are `headExtras`/`overlays` lists.
- Swift: `DesklingTesting` `FixtureArt.vector`/`.raster` (compiled by `scripts/fixture-art.sh`) and
  `SkinCatalog.fixture`.
- Swift: `DesklingTesting` `Conformance/`: `ConformanceRoot.find(markerFile:from:)`, `ConformanceSuite` (`checkUpToDate`,
  `replayCommitted`, `checkDeterministic`, `writeIfAsked(flag:)`, each returning its problems so the library never
  links XCTest), `JSONValue`, `SnapshotData`, and the companion generators parameterized by bank, defaults, character
  and cases: `PhrasesVector`, `PickerVector`, `CadenceVector`, `QuietTimeline`, `CharacterPoseVector`,
  `DesktopVector`, `SettingsVector.base(defaults:)` (a tree the app adds its keys to) and
  `BankAudit.floors(bank:languages:skins:minimumPerTag:)`. Apps generate their content vectors with these.
  `ConformanceSupport` moved into the folder; the Core `ConformanceTests` runs on `ConformanceSuite`.
- .NET: `Deskling.Companion` (net10.0, AOT-compatible), the C# mirror of `DesklingCompanion` file for file: the same
  types with `FlavorId`, `SkinId` and `PaintRole` as string record structs, `PhraseKind.Sequence`, `Phrase.Languages`,
  `PhraseTiming`, `CompanionDefaults` (a record; skin, flavors and shortcut required), `CompanionSettings`
  (`INotifyPropertyChanged`, non-sealed, `virtual ResetToDefaults()` and protected `SetProperty`/`SetOrRemove` for an
  app's own keys), `CompanionCoordinator` (bank, language and a `TimeZoneInfo` injected), `SkinCatalog`,
  `PartContent.Vector`/`.Image`, `IDrawableCharacter.Layers`. Loaders: `PhraseBank.Parse(Stream)` (phrases.json,
  strict) and `CharacterArt.Parse(Stream)` (`characters/<skin>.json`, refusing a rig that names missing parts).
- .NET: `Deskling.Testing`: `MockClock`, `MockIdle`, `MockBusy`, `ScriptedRandom`, `CountingRandom`;
  `ConformanceRoot.Find(markerFile)`/`Files(folder)`, `ConformanceSupport`, `VectorSupport`; the companion replays
  `PhrasesReplay`, `SettingsReplay` (generic keys only, so an app's own keys and `skins` sit beside them),
  `PickerReplay`, `CadenceReplay`, `QuietReplay`, `CharacterReplay` and `DesktopReplay`, each
  `Verify(Stream, file, …)` returning its problems; fixtures `FixtureIds`, `FixtureDefaults.Value`,
  `FixtureBank.Load(root)`, `FixtureArt.Load(root, skin)` and `FixtureArt.Catalog(root)`. `Deskling.Core.Tests` uses
  it in place of its own copies. `release.yml` packs both packages beside `Deskling.Core`; `dotnet.yml` tests the
  Core and Companion vectors on Linux and Windows.
- Swift: `DesklingShell` `StatusItemController` (a status item with a popover panel, a right-click menu and a glyph the
  app draws through `image(animating:frame:)`; 8 fps frames, a wave, Reduce Motion) and `Speech/`: `Speaking` and
  `Speaker` (system voice or a recorded clip; `appLanguage(bundle:)`), `SpeechPart` and `SpeechSequencer` (parts with
  a beat between; `isActive` through the beat, `isVoicing`, `stop()` cancels the pending part), `VoiceID` and
  `VoiceLibrary` (manifest format 1; `pendingClipIDs`, `fallbackToSystemVoice`), `ChimePlaying` and `ChimePlayer(bundle:)`.
- Swift: `DesklingCompanionUI` (macOS; depends on Companion, Core, System, Shell and Store): `SignalHub`
  (`lowBatteryPercent` injected), `GlyphRenderer` and `StatusItemController.show(glyph:mood:talking:accessibilityDescription:)`,
  `CharacterView` (vector parts in the palette's paints, image parts from `artBundle` under the rig's transform),
  `DesktopLayout`, `DesktopCompanion`, `DeliveryPerformer` (jokes in beats, one two-line banner at the punchline,
  `unavailableIDs(voice:)`), `VoiceLibrary.pendingIDs(in:voice:text:)`, `CompanionNotifier`, `NotificationPermission`,
  `Theme/`: `CompanionPalette` (`EnvironmentValues.companionPalette`, `.companionDisplayFont`), `Space`, `SpeechBubble`,
  `BubbleShape`, `BubbleCountdown`, `StatusPill`; and `ScreenshotCapture` (`DESKLING_SCREENSHOT_MODE`/`_DIR`) with
  `FixedClock`, `ConstantIdle`, `SilentSpeaker`, `SilentChime` and `FixedPriceBackend`. During a capture run
  `CharacterView` holds the still pose, so the same build shoots the same pixels every time.
- `tools/` for companion apps (vendored as a `deskling/` submodule pinned to the release): `companion.py` and
  `companion.schema.json` (each app's `companion.json`: names, paths, languages, tool settings; `env`, `pins`),
  `l10n.py` and `phrases.py` (language-scoped phrases; a joke's parts as `phrase.<id>.n` keys), `voice.py` and
  `voice-review.html` (a clip per joke part, any voice names), `synth.py`, `fonts.py`, `screenshots.sh`,
  `store-screenshots.sh`/`.py`, `install-dev.sh`, `dev-icon.swift`, `conformance.sh`, `verify.sh` (with a check that
  the submodule, SwiftPM and NuGet pins agree), `setup-linux.sh`, `hooks/hook.py` and eight skills, all moved from
  sunnysays and parameterized; tests over a fixture app in `tools/tests/` (`run.py`).
- Vectors: `conformance/companion/` over the fixtures (`phrases.json`, `settings.json`, `picker/`, `cadence/`,
  `quiet/`, `desktop.json` with `partDelays`, `characters/` for both fixture skins), generated by
  `Tests/DesklingCompanionTests/Conformance` (`scripts/conformance.sh` regenerates both folders).

### Changed (breaking)
- Swift: `SettingsStore` (typed: `bool`/`integer`/`double`/`string`/`strings`/`integers(forKey:)`, the matching
  `set(…:forKey:)` and `remove(forKey:)`), `InMemorySettingsStore` and `UserDefaultsSettingsStore` are in
  `DesklingCore`. `DesklingStore` now depends on `DesklingCore`, and `PurchaseStore` caches owned ids through a
  `SettingsStore`: `PurchaseStore(catalog:backend:settings:cacheKey:logger:)`, `UserDefaultsSettingsStore()` by
  default. `PurchaseStore(catalog:backend:defaults:…)` stays as a convenience and reads the same cache key. Migration:
  none unless you passed `defaults:` without a value; apps that had their own `SettingsStore` protocol can drop it.
- .NET: `ISettingsStore` is typed (`GetBool`/`GetInteger`/`GetDouble`/`GetString`/`GetStrings`/`GetIntegers`, the
  matching setters, `Remove`); `object? Get(key)` and `Set(key, object)` are gone, and so is `DateTimeOffset`.
  `LocalSettingsStore` implements it (scalars native, lists in the same JSON composite as before, `double` added),
  so stored values stay readable. Migration: `Get(key) as string[]` → `GetStrings(key)`, `Get(key) as int?` →
  `GetInteger(key)`, `Set(key, value)` → the typed setter; a stored `DateTimeOffset` becomes a `double` of Unix
  seconds (`SetDouble(key, date.ToUnixTimeMilliseconds() / 1000.0)`).

## 0.4.0 - 2026-10-10

### Added
- Swift (macOS): `FloatingWindow` in `DesklingShell`, a small borderless window that floats above other apps' windows
  on every Space but full-screen ones, for a desktop pet or a sticky note. It never becomes key and never activates
  the app (no Dock icon for an `LSUIElement` app; it isn't one of `WindowManager`'s windows). It stands on an anchor
  (the middle of its bottom edge) so a size change grows it upward in place, stays wholly on a screen when displays
  change, reports clicks, drags, a right or Control click menu and trackpad pinch steps, fades in and out, and can
  carry a second window beside it (`attach(to:)`, for a speech bubble). No Windows twin yet; it comes with the first
  app that needs one.
- Swift (macOS): `FloatingPlacement`, the pure rectangle math behind it: anchor ↔ frame, the default bottom-right
  spot, clamping onto the nearest screen, the side of a window with more room, and a platform-neutral top-left origin
  for storing a position.

## 0.3.1 - 2026-10-10

### Added
- .NET: `PluralRules` knows Czech, Danish, Dutch, Japanese, Korean, Norwegian (Bokmål), Polish, Swedish, Turkish and
  Chinese. Polish counts take one/few/many, Czech one/few/other, and Japanese, Korean and Chinese always take
  "other". Before, these languages fell back to the English rule.

## 0.3.0 - 2026-10-09

### Added
- .NET: the purchase store, mirroring `DesklingStore`. `Deskling.Core.Store` has `ProductCatalog`, `PurchaseStore`
  (owned set cached in an `ISettingsStore`, load state, prices, purchasing/restoring/pending, tip thanks, errors,
  one `Changed` event), `IStoreBackend` and `MockStoreBackend`. The mock ships in `Deskling.Core` (the Swift one is in
  `DesklingTesting`) because apps use it in Debug screenshot and preview modes as well as tests.
- .NET: `Deskling.Windows.Shell.StoreContextBackend`, the Microsoft Store backend over `Windows.Services.Store`.
  Matches add-ons by Partner Center Product ID (`InAppOfferToken`), parents the purchase dialog to a window you name,
  reports consumables fulfilled right after purchase so they can be bought again, and turns `OfflineLicensesChanged`
  into a transaction update on the UI thread. Needs no manifest capability.

## 0.2.1 - 2026-10-08

### Fixed
- `AppNotifier<TAction>.Dispose()` (.NET) unregisters only when `Register()` did, and never throws. Disposing a
  notifier that was never registered (an app's screenshot or test mode, say) threw `COMException` 0x80070490
  "Not Registered for App Notifications!" whenever Windows had no registration left for the package.
  `Register()` is idempotent and leaves nothing subscribed when Windows refuses the registration.

## 0.2.0 - 2026-10-08

### Changed
- The Swift package builds in the Swift 6 language mode (`swift-tools-version: 6.0`), with strict concurrency checking.
  Apps in either language mode can use it; Swift 6 apps no longer need `@preconcurrency import` for its types.
- `NotificationActionSpec`, `NotificationCategorySpec`, `SystemClock`, `NeverBusy`, `SystemRandom`,
  `SystemIdleTimeProvider`, `SystemBusyStateProvider` and the `DesklingTesting` helpers `ScriptedRandom`,
  `MockStoreBackend.Failure` and `ConformanceSupport.VectorError` are `Sendable`.
- `NotificationPoster` answers `willPresent` through the async delegate method; behavior is unchanged.
- **Breaking:** `ScreenLockWatcher` is `@MainActor` (its callback always ran on the main queue). Create and read it
  from the main actor; `lockedName` and `unlockedName` stay usable anywhere.

## 0.1.1 - 2026-10-07

### Changed
- The release workflow publishes to NuGet.org through Trusted Publishing (OIDC) instead of a stored API key, and
  creates the tag itself when run by hand. No code changes; 0.1.0's packages were never pushed to NuGet.org.

## 0.1.0 - 2026-10-07

The first release: the generic parts of mybackhurts, extracted and generalized, for mybackhurts and sunnysays.

### Added
- `DesklingCore`: `ReminderScheduler`, `SchedulerConfig`, `Clock`, `IdleTimeProvider`, `BusyStateProvider` and the
  `BusyReason`s, moved from mybackhurts with the same names and behavior (its 37 scheduler vectors replay byte for
  byte), plus one addition: `SchedulerConfig.intervalJitter`, a whole-second random offset on every interval drawn
  from an injected `RandomSource`; zero by default.
- `DesklingCore`: `RandomSource`, `SystemRandom` and `SplitMix64` (specified bit for bit for the .NET port),
  `KeyShortcut`, `LocalizedKey`.
- `DesklingTesting`: `MockClock`, `MockIdle`, `MockBusy`, `ScriptedRandom`, `CountingRandom`, `ConformanceSupport`.
- `DesklingSystem`: `SystemIdleTimeProvider` and `SystemBusyStateProvider` from mybackhurts, the latter with
  `init(cameraCheck:)` (`.avFoundation`, `.coreMediaIO` for sandboxed apps without the camera entitlement, `.off`);
  new permission-free signals `PowerStateProvider` / `SystemPowerSource` (battery, charging, level, change callback),
  `AudioOutputStateProvider` / `SystemAudioOutput` (muted, route), `AppearanceWatcher` and `ScreenLockWatcher`.
- `DesklingShell`: `WindowManager<ID: WindowID>` (activation-policy dance, exclusive windows, placement, "Bigger"),
  `GlobalHotKey` with several ids per signature, `ShortcutCapture`, `NotificationPoster` with injected categories and
  titles, `LoginItem`, `LaunchContext`, and the drawing helpers `SVGPath` (now also relative commands, `H`/`V`, `T` and
  arcs), `Keyframes`, `Wobble`, `Polyline.circle`, `CGAffineTransform.rotation(degrees:about:)`.
- `DesklingStore`: `ProductCatalog`, `PurchaseStore` (`owned`, `isOwned`, `error: PurchaseError?`, injected cache key
  and logger), `StoreBackend`, `StoreKitBackend`.
- `DesklingTesting`: `MockStoreBackend`.
- `Deskling.Core` (.NET): the file-for-file mirror of `DesklingCore` plus `Localization/PluralRules`, `ReswName` and
  `Settings/ISettingsStore` from mybackhurts; `Deskling.Core.Tests` replays every scheduler vector.
- `Deskling.Windows` (.NET): `SystemIdleTimeProvider`, `SystemBusyStateProvider`, `TrayIconHost`, `TrayMenuItem`,
  `GlobalHotKey` (Carbon masks mapped onto `MOD_*`), `StartupService`, `LocalSettingsStore`, `WavPlayer`,
  `AppNotifier<TAction>`, `SingleInstance`, `Loc` / `LocExtension` / `Formats` with injected `FormatKeys`, a shared
  `MessageWindow`, and the new signals `PowerSource`, `OutputMute`, `AppearanceWatcher`, `SessionLockWatcher`.
- Repository: Swift package layout (`DesklingCore`, `DesklingSystem`, `DesklingShell`, `DesklingStore`,
  `DesklingTesting`), .NET solution (`Deskling.Core`, `Deskling.Windows`, `Deskling.Core.Tests`), conformance
  vectors, CI (`swift.yml` on macOS, `dotnet.yml` on Ubuntu and Windows, `regenerate-vectors.yml`) and the
  release workflow.

#if os(macOS)
    import AppKit
    import DesklingCompanion
    import DesklingShell
    import SwiftUI

    /// Which of the app's windows plays which part. `CompanionAppModel` shows onboarding on the first run and Home
    /// otherwise, closes onboarding on Get started, and acknowledges a tip when the Support window closes.
    public struct WindowRoles<Window: WindowID> {
        public var home: Window
        public var onboarding: Window
        public var settings: Window
        public var support: Window

        public init(home: Window, onboarding: Window, settings: Window, support: Window) {
            self.home = home
            self.onboarding = onboarding
            self.settings = settings
            self.support = support
        }
    }

    /// The status pill's words, already localized by the app (the package ships none). Each is read when the pill
    /// changes, so they follow the app's language.
    public struct StatusCopy {
        /// Shushed until the user wakes the character.
        public var shhUntilYouSay: () -> String
        /// Shushed until the end of today.
        public var shhRestOfToday: () -> String
        /// Shushed until a time today ("3:00 PM").
        public var shhUntil: (_ time: String) -> String
        /// Shushed until a day and time ("Mon 9:00 AM").
        public var shhUntilDay: (_ day: String) -> String
        public var quietCall: () -> String
        public var quietFullScreen: () -> String
        /// Outside the active hours and days.
        public var offHours: () -> String
        /// The user is away or the screen is locked.
        public var asleep: () -> String
        /// The next line is due in `span` ("20 min", "1 hr 5 min").
        public var nextIn: (_ span: String) -> String

        public init(
            shhUntilYouSay: @escaping () -> String, shhRestOfToday: @escaping () -> String, shhUntil: @escaping (_ time: String) -> String,
            shhUntilDay: @escaping (_ day: String) -> String, quietCall: @escaping () -> String, quietFullScreen: @escaping () -> String,
            offHours: @escaping () -> String, asleep: @escaping () -> String, nextIn: @escaping (_ span: String) -> String
        ) {
            self.shhUntilYouSay = shhUntilYouSay
            self.shhRestOfToday = shhRestOfToday
            self.shhUntil = shhUntil
            self.shhUntilDay = shhUntilDay
            self.quietCall = quietCall
            self.quietFullScreen = quietFullScreen
            self.offHours = offHours
            self.asleep = asleep
            self.nextIn = nextIn
        }
    }

    /// The desktop character's words, already localized by the app.
    public struct DesktopCopy {
        /// The VoiceOver action on the desktop character ("Say something").
        public var sayActionName: () -> String
        /// The panel row while it's hidden for the rest of today.
        public var backTomorrow: () -> String
        /// The panel row while it's hidden for a while; the argument is a time such as 3:00 PM.
        public var backAt: (_ time: String) -> String

        public init(
            sayActionName: @escaping () -> String, backTomorrow: @escaping () -> String, backAt: @escaping (_ time: String) -> String
        ) {
            self.sayActionName = sayActionName
            self.backTomorrow = backTomorrow
            self.backAt = backAt
        }
    }

    /// The menus' titles, already localized by the app (`CompanionMenus`).
    public struct MenuCopy {
        /// "Say something".
        public var say: String
        /// Ends Shh.
        public var wakeUp: String
        /// The Shh submenu.
        public var shh: String
        /// Shh and Hide spans.
        public var forAnHour: String
        public var forTheRestOfToday: String
        /// Shh until the user says.
        public var untilISay: String
        /// The menu bar menu's desktop submenu ("Sunny on the desktop").
        public var desktopSubmenu: String
        /// Checked while the character stands on the desktop.
        public var show: String
        /// Ends a hide for a while.
        public var showNow: String
        public var hideForAnHour: String
        public var hideForTheRestOfToday: String
        /// The desktop character's Size submenu.
        public var size: String
        /// The desktop character's Hide submenu.
        public var hide: String
        /// Hide until shown again: turns the desktop character off.
        public var untilIShowItAgain: String
        public var settings: String
        /// Opens the Support window, before the Supporter is bought.
        public var support: String
        /// The menu bar's Support item once the Supporter is bought (the desktop menu drops it).
        public var thanksForSupporting: String
        /// The Quit item (the app decides how its Debug build names itself).
        public var quit: String
        /// A desktop size's name.
        public var sizeLabel: (DesktopSize) -> String

        public init(
            say: String, wakeUp: String, shh: String, forAnHour: String, forTheRestOfToday: String, untilISay: String,
            desktopSubmenu: String, show: String, showNow: String, hideForAnHour: String, hideForTheRestOfToday: String, size: String,
            hide: String, untilIShowItAgain: String, settings: String, support: String, thanksForSupporting: String, quit: String,
            sizeLabel: @escaping (DesktopSize) -> String
        ) {
            self.say = say
            self.wakeUp = wakeUp
            self.shh = shh
            self.forAnHour = forAnHour
            self.forTheRestOfToday = forTheRestOfToday
            self.untilISay = untilISay
            self.desktopSubmenu = desktopSubmenu
            self.show = show
            self.showNow = showNow
            self.hideForAnHour = hideForAnHour
            self.hideForTheRestOfToday = hideForTheRestOfToday
            self.size = size
            self.hide = hide
            self.untilIShowItAgain = untilIShowItAgain
            self.settings = settings
            self.support = support
            self.thanksForSupporting = thanksForSupporting
            self.quit = quit
            self.sizeLabel = sizeLabel
        }
    }

    /// The app's sounds by name (in its bundle's `Sounds/`): the blip before a line, Shh's sound and Home's hello. Nil
    /// plays none.
    public struct CompanionSounds {
        public var blip: String?
        public var shh: String?
        public var hello: String?

        public init(blip: String?, shh: String?, hello: String?) {
            self.blip = blip
            self.shh = shh
            self.hello = hello
        }
    }

    /// What `CompanionAppModel` can't know about the app. No defaults for the words.
    public struct CompanionAppConfiguration {
        /// The global hotkey's signature, four characters unique to the app (`0x534E_4E59` is "SNNY").
        public var hotKeySignature: OSType
        /// The menu bar item's tooltip (the app decides what its Debug build says).
        public var statusItemTooltip: String
        /// The character's name for VoiceOver, on the menu bar glyph.
        public var accessibilityLabel: String
        /// The windows' background.
        public var paper: NSColor
        /// The desktop bubble's colors and face (it lives in a window of its own).
        public var palette: CompanionPalette
        public var displayFont: @Sendable (CGFloat) -> Font
        public var sounds: CompanionSounds
        public var status: StatusCopy
        public var desktop: DesktopCopy
        /// The bubble while a tune plays ("♪ hm hm hmm ♪").
        public var tuneBubbleText: () -> String
        /// A phrase's text in the user's language: the app's String Catalog by default.
        public var localize: (_ catalogKey: String, _ source: String) -> String
        /// The non-consumable that makes the user a supporter: the menus thank them instead of asking. Nil: no such
        /// product.
        public var supporterProduct: String?

        public init(
            hotKeySignature: OSType, statusItemTooltip: String, accessibilityLabel: String, paper: NSColor, palette: CompanionPalette,
            displayFont: @escaping @Sendable (CGFloat) -> Font, sounds: CompanionSounds, status: StatusCopy, desktop: DesktopCopy,
            tuneBubbleText: @escaping () -> String, supporterProduct: String?,
            localize: @escaping (_ catalogKey: String, _ source: String) -> String = { Phrase.localize($0, $1) }
        ) {
            self.hotKeySignature = hotKeySignature
            self.statusItemTooltip = statusItemTooltip
            self.accessibilityLabel = accessibilityLabel
            self.paper = paper
            self.palette = palette
            self.displayFont = displayFont
            self.sounds = sounds
            self.status = status
            self.desktop = desktop
            self.tuneBubbleText = tuneBubbleText
            self.supporterProduct = supporterProduct
            self.localize = localize
        }
    }

    /// How long a hide for a while lasts, from the desktop character's menu or the menu bar's.
    public enum DesktopHide: Sendable {
        case hour, restOfToday
        /// Turns the desktop character off: the setting, until the user turns it back on.
        case untilShown
    }
#endif

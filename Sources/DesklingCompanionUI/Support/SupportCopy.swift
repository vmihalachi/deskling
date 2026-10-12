#if os(macOS)
    import DesklingStore
    import SwiftUI

    /// The products the Support window sells: the one-time supporter purchase and the tips, smallest first.
    public struct SupportProducts: Sendable {
        public var supporter: String
        public var tips: [String]

        public init(supporter: String, tips: [String]) {
            self.supporter = supporter
            self.tips = tips
        }
    }

    /// Every word the Support window shows, already localized by the app. No defaults.
    public struct SupportCopy {
        /// Beside the character before the supporter purchase: who makes the app and what supporting does.
        public var pitch: Text
        /// The supporter button, with its App Store price once loaded (nil before).
        public var becomeSupporter: (_ price: String?) -> Text
        /// Under the supporter button (pay once, Family Sharing).
        public var supporterNote: Text
        /// Beside the character once the user is a supporter, above the tips.
        public var supporterThanks: Text
        /// A tip's name, by product id.
        public var tipTitle: (_ product: String) -> String
        /// Under the tips.
        public var tipsNote: Text
        /// What the character says right after the supporter purchase, and the note under it.
        public var welcomeLine: String
        public var welcomeNote: Text?
        /// What the character says right after a tip.
        public var tipThanksLine: String
        /// A purchase or restore failed.
        public var error: (PurchaseError) -> String
        /// Waiting for a parent's approval.
        public var askToBuy: Text
        /// The prices couldn't be loaded, and the button that tries again.
        public var pricesFailed: Text
        public var tryAgain: Text
        public var restore: Text
        public var done: Text

        public init(
            pitch: Text, becomeSupporter: @escaping (_ price: String?) -> Text, supporterNote: Text, supporterThanks: Text,
            tipTitle: @escaping (_ product: String) -> String, tipsNote: Text, welcomeLine: String, welcomeNote: Text?,
            tipThanksLine: String, error: @escaping (PurchaseError) -> String, askToBuy: Text, pricesFailed: Text, tryAgain: Text,
            restore: Text, done: Text
        ) {
            self.pitch = pitch
            self.becomeSupporter = becomeSupporter
            self.supporterNote = supporterNote
            self.supporterThanks = supporterThanks
            self.tipTitle = tipTitle
            self.tipsNote = tipsNote
            self.welcomeLine = welcomeLine
            self.welcomeNote = welcomeNote
            self.tipThanksLine = tipThanksLine
            self.error = error
            self.askToBuy = askToBuy
            self.pricesFailed = pricesFailed
            self.tryAgain = tryAgain
            self.restore = restore
            self.done = done
        }
    }

    /// How the Support window looks in the app: its title, the header over the tips, its character, the celebration's
    /// bubble and its buttons.
    public struct SupportLook<Primary: ButtonStyle, Outline: ButtonStyle> {
        /// The window title, in the app's title view.
        public var title: AnyView
        /// The header over the tips ("Leave a tip"), in the app's section label view.
        public var tipsHeader: AnyView
        /// The character, already framed: beside the words (`celebrating` false) or large after a purchase.
        public var character: (_ celebrating: Bool) -> AnyView
        /// The bubble over the celebrating character, saying `line`.
        public var bubble: (_ line: String) -> AnyView
        public var primary: Primary
        /// The supporter and tip buttons.
        public var largePrimary: Primary
        public var outline: Outline
        /// A tip's price under its name (12 pt): the app tones it down its own way, such as its button text color at
        /// 80%.
        public var tipPrice: (Text) -> AnyView

        public init(
            title: AnyView, tipsHeader: AnyView, character: @escaping (_ celebrating: Bool) -> AnyView,
            bubble: @escaping (_ line: String) -> AnyView, primary: Primary, largePrimary: Primary, outline: Outline,
            tipPrice: @escaping (Text) -> AnyView
        ) {
            self.title = title
            self.tipsHeader = tipsHeader
            self.character = character
            self.bubble = bubble
            self.primary = primary
            self.largePrimary = largePrimary
            self.outline = outline
            self.tipPrice = tipPrice
        }
    }
#endif

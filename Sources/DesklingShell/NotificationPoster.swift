#if os(macOS)
    import AppKit
    import Foundation
    import UserNotifications

    /// One button on a notification. `title` is already localized by the app: the package ships no strings.
    public struct NotificationActionSpec: Equatable, Sendable {
        public var id: String
        public var title: String
        public var options: UNNotificationActionOptions

        public init(id: String, title: String, options: UNNotificationActionOptions = []) {
            self.id = id
            self.title = title
            self.options = options
        }

        public func makeAction() -> UNNotificationAction {
            UNNotificationAction(identifier: id, title: title, options: options)
        }
    }

    /// A notification category: its buttons, and which action id a tap on the banner itself reports
    /// (`nil` ignores taps).
    public struct NotificationCategorySpec: Equatable, Sendable {
        public var id: String
        public var actions: [NotificationActionSpec]
        public var defaultActionID: String?

        public init(id: String, actions: [NotificationActionSpec], defaultActionID: String? = nil) {
            self.id = id
            self.actions = actions
            self.defaultActionID = defaultActionID
        }

        public func makeCategory() -> UNNotificationCategory {
            UNNotificationCategory(identifier: id, actions: actions.map { $0.makeAction() }, intentIdentifiers: [])
        }
    }

    /// Posts local notifications with action buttons and routes the user's answer back to the app.
    ///
    /// Needs the app's categories up front and `configure()` once at launch (it becomes the notification
    /// center's delegate). Posting needs notification authorization, which `requestAuthorization` asks for
    /// explicitly: nothing here prompts on its own. Dismissals are ignored; a tap on the banner reports the
    /// category's `defaultActionID`. `onAction` runs on the main actor.
    @MainActor
    public final class NotificationPoster: NSObject, UNUserNotificationCenterDelegate {
        /// The user pressed a button (or the banner): the category id and the action id.
        public var onAction: ((_ categoryID: String, _ actionID: String) -> Void)?
        /// How a notification shows while the app is frontmost.
        public var presentationOptions: UNNotificationPresentationOptions = [.banner, .sound]
        public let categories: [NotificationCategorySpec]

        private var center: UNUserNotificationCenter { .current() }

        public init(categories: [NotificationCategorySpec]) {
            self.categories = categories
            super.init()
        }

        /// Registers the categories and takes the delegate. Call once, early in launch, so answers to
        /// notifications that arrive while the app was closed are delivered.
        public func configure() {
            center.delegate = self
            center.setNotificationCategories(Set(categories.map { $0.makeCategory() }))
        }

        public func requestAuthorization(options: UNAuthorizationOptions = [.alert, .sound]) async -> Bool {
            (try? await center.requestAuthorization(options: options)) ?? false
        }

        public func authorizationStatus() async -> UNAuthorizationStatus {
            await center.notificationSettings().authorizationStatus
        }

        /// Posts right away. Reusing an `id` replaces the notification posted with it before, so a reminder
        /// that fires again doesn't pile up in Notification Center.
        public func post(id: String, title: String, body: String, categoryID: String, sound: Bool) {
            let content = UNMutableNotificationContent()
            content.title = title
            content.body = body
            content.categoryIdentifier = categoryID
            if sound { content.sound = .default }
            let request = UNNotificationRequest(identifier: id, content: content, trigger: nil)
            center.add(request)
        }

        /// Takes the notification posted under `id` down again (delivered or not).
        public func remove(id: String) {
            center.removePendingNotificationRequests(withIdentifiers: [id])
            center.removeDeliveredNotifications(withIdentifiers: [id])
        }

        /// Opens System Settings → Notifications on the app's page, for when authorization was denied.
        public static func openSystemSettings(bundleIdentifier: String) {
            let url = "x-apple.systempreferences:com.apple.Notifications-Settings.extension?id=\(bundleIdentifier)"
            if let url = URL(string: url) { NSWorkspace.shared.open(url) }
        }

        // MARK: UNUserNotificationCenterDelegate

        public nonisolated func userNotificationCenter(
            _ center: UNUserNotificationCenter, willPresent notification: UNNotification
        ) async -> UNNotificationPresentationOptions {
            await presentationOptions
        }

        public nonisolated func userNotificationCenter(
            _ center: UNUserNotificationCenter, didReceive response: UNNotificationResponse,
            withCompletionHandler completionHandler: @escaping () -> Void
        ) {
            let categoryID = response.notification.request.content.categoryIdentifier
            let actionID = response.actionIdentifier
            DispatchQueue.main.async { [weak self] in
                MainActor.assumeIsolated { self?.deliver(categoryID: categoryID, actionID: actionID) }
            }
            completionHandler()
        }

        private func deliver(categoryID: String, actionID: String) {
            switch actionID {
            case UNNotificationDismissActionIdentifier:
                return
            case UNNotificationDefaultActionIdentifier:
                guard let defaultID = categories.first(where: { $0.id == categoryID })?.defaultActionID else { return }
                onAction?(categoryID, defaultID)
            default:
                onAction?(categoryID, actionID)
            }
        }
    }
#endif

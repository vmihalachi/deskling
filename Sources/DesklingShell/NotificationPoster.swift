#if os(macOS) || os(iOS)
    #if os(macOS)
        import AppKit
    #else
        import UIKit
    #endif
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

    /// A notification for the system to deliver at `date`, for an app that schedules reminders ahead because it
    /// doesn't run between them (iOS). `userInfo` comes back with the answer (`NotificationAnswer`).
    public struct ScheduledNotification: Equatable, Sendable {
        public var id: String
        public var date: Date
        public var title: String
        public var body: String
        public var categoryID: String
        public var sound: Bool
        public var userInfo: [String: String]

        public init(
            id: String, date: Date, title: String, body: String, categoryID: String, sound: Bool,
            userInfo: [String: String] = [:]
        ) {
            self.id = id
            self.date = date
            self.title = title
            self.body = body
            self.categoryID = categoryID
            self.sound = sound
            self.userInfo = userInfo
        }

        /// A calendar trigger on the local wall-clock time (to the second), so a reminder planned for 9:45 still
        /// comes at 9:45 after a time zone change.
        public func makeRequest(calendar: Calendar = .current) -> UNNotificationRequest {
            let content = UNMutableNotificationContent()
            content.title = title
            content.body = body
            content.categoryIdentifier = categoryID
            if sound { content.sound = .default }
            content.userInfo = userInfo
            let components = calendar.dateComponents([.year, .month, .day, .hour, .minute, .second], from: date)
            let trigger = UNCalendarNotificationTrigger(dateMatching: components, repeats: false)
            return UNNotificationRequest(identifier: id, content: content, trigger: trigger)
        }
    }

    /// The user's answer to a notification: which one (`requestID` and its `userInfo`), and the action id (the
    /// category's `defaultActionID` for a tap on the banner).
    public struct NotificationAnswer: Equatable, Sendable {
        public var requestID: String
        public var categoryID: String
        public var actionID: String
        public var userInfo: [String: String]

        public init(requestID: String, categoryID: String, actionID: String, userInfo: [String: String] = [:]) {
            self.requestID = requestID
            self.categoryID = categoryID
            self.actionID = actionID
            self.userInfo = userInfo
        }
    }

    /// Posts local notifications with action buttons and routes the user's answer back to the app.
    ///
    /// Needs the app's categories up front and `configure()` once at launch (it becomes the notification
    /// center's delegate). Posting needs notification authorization, which `requestAuthorization` asks for
    /// explicitly: nothing here prompts on its own. Dismissals are ignored; a tap on the banner reports the
    /// category's `defaultActionID`. `onAction`, `onAnswer` and `onPresent` run on the main actor.
    @MainActor
    public final class NotificationPoster: NSObject, UNUserNotificationCenterDelegate {
        /// The user pressed a button (or the banner): the category id and the action id.
        public var onAction: ((_ categoryID: String, _ actionID: String) -> Void)?
        /// The same answer with the notification it answers, for apps that schedule ahead and need to know which.
        public var onAnswer: ((NotificationAnswer) -> Void)?
        /// A notification is showing while the app is frontmost: its request id and `userInfo`.
        public var onPresent: ((_ requestID: String, _ userInfo: [String: String]) -> Void)?
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

        /// Makes `notifications` the pending ones among those whose id starts with `prefix`: the rest are removed, and
        /// only new or changed ones are added. iOS keeps at most 64 pending requests per app and drops the rest.
        public func replacePending(prefix: String, with notifications: [ScheduledNotification]) async {
            let pending = await pending(prefix: prefix)
            let wanted = Set(notifications.map(\.id))
            let stale = pending.filter { old in
                !wanted.contains(old.id) || notifications.first { $0.id == old.id } != old
            }
            center.removePendingNotificationRequests(withIdentifiers: stale.map(\.id))
            let kept = Set(pending.map(\.id)).subtracting(stale.map(\.id))
            for notification in notifications where !kept.contains(notification.id) {
                try? await center.add(notification.makeRequest())
            }
        }

        /// The pending notifications whose id starts with `prefix`, in date order. Only calendar-triggered ones (as
        /// `replacePending` makes) are listed.
        public func pending(prefix: String) async -> [ScheduledNotification] {
            await center.pendingNotificationRequests()
                .compactMap { request -> ScheduledNotification? in
                    guard request.identifier.hasPrefix(prefix),
                        let trigger = request.trigger as? UNCalendarNotificationTrigger,
                        let date = trigger.nextTriggerDate()
                    else { return nil }
                    let content = request.content
                    return ScheduledNotification(
                        id: request.identifier, date: date, title: content.title, body: content.body,
                        categoryID: content.categoryIdentifier, sound: content.sound != nil,
                        userInfo: Self.strings(content.userInfo))
                }
                .sorted { $0.date < $1.date }
        }

        /// Opens the system's notification settings for the app (System Settings → Notifications on the Mac, the
        /// app's page in Settings on iOS), for when authorization was denied.
        public static func openSystemSettings(bundleIdentifier: String) {
            #if os(macOS)
                let url = "x-apple.systempreferences:com.apple.Notifications-Settings.extension?id=\(bundleIdentifier)"
                if let url = URL(string: url) { NSWorkspace.shared.open(url) }
            #else
                if let url = URL(string: UIApplication.openNotificationSettingsURLString) {
                    UIApplication.shared.open(url)
                }
            #endif
        }

        /// The string pairs of a notification's `userInfo` (the only kind `ScheduledNotification` stores).
        nonisolated static func strings(_ userInfo: [AnyHashable: Any]) -> [String: String] {
            var out: [String: String] = [:]
            for (key, value) in userInfo {
                if let key = key as? String, let value = value as? String { out[key] = value }
            }
            return out
        }

        // MARK: UNUserNotificationCenterDelegate

        public nonisolated func userNotificationCenter(
            _ center: UNUserNotificationCenter, willPresent notification: UNNotification
        ) async -> UNNotificationPresentationOptions {
            let requestID = notification.request.identifier
            let userInfo = Self.strings(notification.request.content.userInfo)
            return await MainActor.run {
                onPresent?(requestID, userInfo)
                return presentationOptions
            }
        }

        public nonisolated func userNotificationCenter(
            _ center: UNUserNotificationCenter, didReceive response: UNNotificationResponse,
            withCompletionHandler completionHandler: @escaping () -> Void
        ) {
            let request = response.notification.request
            let answer = NotificationAnswer(
                requestID: request.identifier, categoryID: request.content.categoryIdentifier,
                actionID: response.actionIdentifier, userInfo: Self.strings(request.content.userInfo))
            DispatchQueue.main.async { [weak self] in
                MainActor.assumeIsolated { self?.deliver(answer) }
            }
            completionHandler()
        }

        private func deliver(_ answer: NotificationAnswer) {
            var answer = answer
            switch answer.actionID {
            case UNNotificationDismissActionIdentifier:
                return
            case UNNotificationDefaultActionIdentifier:
                guard let defaultID = categories.first(where: { $0.id == answer.categoryID })?.defaultActionID else {
                    return
                }
                answer.actionID = defaultID
            default:
                break
            }
            onAction?(answer.categoryID, answer.actionID)
            onAnswer?(answer)
        }
    }
#endif

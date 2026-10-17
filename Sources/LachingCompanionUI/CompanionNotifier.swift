#if os(macOS)
    import LachingShell
    import Foundation
    import UserNotifications

    /// Whether the app may show notifications.
    public enum NotificationPermission: Equatable, Sendable {
        case notDetermined, authorized, denied

        public init(_ status: UNAuthorizationStatus) {
            switch status {
            case .authorized, .provisional, .ephemeral: self = .authorized
            case .denied: self = .denied
            case .notDetermined: self = .notDetermined
            @unknown default: self = .notDetermined
            }
        }
    }

    /// The character's line as a banner, through `NotificationPoster`, with the app's buttons (`Action`'s raw values
    /// are the registered action ids; the titles are the app's, already localized). One notification id, so a new line
    /// replaces the last banner instead of piling up in Notification Center. Asks for permission only through
    /// `requestAuthorization`. Needs the main actor.
    @MainActor
    public final class CompanionNotifier<Action: RawRepresentable<String>> {
        public let categoryID: String
        public let lineID: String
        /// A button was pressed.
        public var onAction: ((Action) -> Void)?
        private let poster: NotificationPoster

        public init(categoryID: String, actions: [(Action, String)], lineID: String) {
            self.categoryID = categoryID
            self.lineID = lineID
            let specs = actions.map { NotificationActionSpec(id: $0.0.rawValue, title: $0.1) }
            poster = NotificationPoster(categories: [NotificationCategorySpec(id: categoryID, actions: specs)])
            poster.onAction = { [weak self] _, actionID in
                guard let action = Action(rawValue: actionID) else { return }
                self?.onAction?(action)
            }
        }

        /// Registers the category and takes the delegate; once at launch.
        public func configure() {
            poster.configure()
        }

        /// Asks the user. Call it only when the user turns notifications on.
        public func requestAuthorization(completion: @escaping @MainActor (Bool) -> Void) {
            Task {
                let granted = await poster.requestAuthorization()
                completion(granted)
            }
        }

        public func refreshAuthorization(completion: @escaping @MainActor (NotificationPermission) -> Void) {
            Task {
                let status = await poster.authorizationStatus()
                completion(NotificationPermission(status))
            }
        }

        /// Shows `line` (both parts of a joke on two lines) under `title`, replacing the last one.
        public func post(line: String, title: String) {
            poster.post(id: lineID, title: title, body: line, categoryID: categoryID, sound: false)
        }

        /// System Settings → Notifications on the app's page, for when permission was denied.
        public static func openSystemSettings() {
            NotificationPoster.openSystemSettings(bundleIdentifier: Bundle.main.bundleIdentifier ?? "")
        }
    }
#endif

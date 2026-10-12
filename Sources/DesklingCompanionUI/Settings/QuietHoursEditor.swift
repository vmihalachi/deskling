#if os(macOS)
    import DesklingCompanion
    import SwiftUI

    /// The 24 hours as chips in six columns; a selected one is an hour without a word. The title is the app's; the
    /// chips take its toggle style.
    public struct QuietHoursEditor<Chip: ToggleStyle>: View {
        @Bindable var settings: CompanionSettings
        let title: Text
        let chipStyle: Chip

        public init(settings: CompanionSettings, title: Text, chipStyle: Chip) {
            self.settings = settings
            self.title = title
            self.chipStyle = chipStyle
        }

        private static var columns: [GridItem] { Array(repeating: GridItem(.flexible(), spacing: Space.tight), count: 6) }

        public var body: some View {
            VStack(alignment: .leading, spacing: Space.snug) {
                title
                LazyVGrid(columns: Self.columns, spacing: Space.tight) {
                    ForEach(0..<24, id: \.self) { hour in
                        Toggle(isOn: binding(hour)) {
                            Text(verbatim: Self.label(hour)).frame(maxWidth: .infinity)
                        }
                        .toggleStyle(chipStyle)
                    }
                }
            }
        }

        private func binding(_ hour: Int) -> Binding<Bool> {
            Binding(
                get: { settings.quietHours.contains(hour) },
                set: { on in
                    if on { settings.quietHours.insert(hour) } else { settings.quietHours.remove(hour) }
                }
            )
        }

        /// "3 PM" or "15", as `locale` writes hours.
        public static func label(_ hour: Int, locale: Locale = .current, calendar: Calendar = .current) -> String {
            let date = calendar.date(bySettingHour: hour, minute: 0, second: 0, of: Date()) ?? Date()
            var style = Date.FormatStyle.dateTime.hour()
            style.locale = locale
            style.calendar = calendar
            style.timeZone = calendar.timeZone
            return date.formatted(style)
        }
    }
#endif

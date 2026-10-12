#if os(macOS)
    import DesklingCompanion
    import SwiftUI

    /// The active hours' from / to time pickers and the weekday chips (in the order the user's calendar starts its week),
    /// for Settings and onboarding. Every word is the app's; the chips take the app's toggle style.
    public struct ActiveHoursEditor<Chip: ToggleStyle>: View {
        @Bindable var settings: CompanionSettings
        let activeHours: Text
        let from: Text
        let to: Text
        let between: Text
        let days: Text
        let chipStyle: Chip

        /// `from` and `to` label the pickers for VoiceOver; `between` sits between them ("to").
        public init(
            settings: CompanionSettings, activeHours: Text, from: Text, to: Text, between: Text, days: Text, chipStyle: Chip
        ) {
            self.settings = settings
            self.activeHours = activeHours
            self.from = from
            self.to = to
            self.between = between
            self.days = days
            self.chipStyle = chipStyle
        }

        private static var weekdaySymbols: [String] { Calendar.current.veryShortWeekdaySymbols }  // index 0 = Sunday

        public var body: some View {
            VStack(alignment: .leading, spacing: Space.item) {
                HStack {
                    activeHours
                    Spacer()
                    DatePicker(selection: Self.minuteBinding(settings, \.activeStartMinute), displayedComponents: .hourAndMinute) {
                        from
                    }
                    .labelsHidden()
                    between
                    DatePicker(selection: Self.minuteBinding(settings, \.activeEndMinute), displayedComponents: .hourAndMinute) {
                        to
                    }
                    .labelsHidden()
                }
                HStack(spacing: Space.snug) {
                    days
                    Spacer()
                    ForEach(Self.orderedWeekdays(firstWeekday: Calendar.current.firstWeekday), id: \.self) { day in
                        Toggle(Self.weekdaySymbols[day - 1], isOn: Self.weekdayBinding(settings, day))
                            .toggleStyle(chipStyle)
                    }
                }
            }
        }

        /// The weekdays (1 = Sunday … 7 = Saturday) from `firstWeekday` on.
        public static func orderedWeekdays(firstWeekday: Int) -> [Int] {
            (0..<7).map { (firstWeekday - 1 + $0) % 7 + 1 }
        }

        /// A minute of the day (`activeStartMinute`, `activeEndMinute`) as today's date at that time, for a time picker.
        public static func minuteBinding(
            _ settings: CompanionSettings, _ key: ReferenceWritableKeyPath<CompanionSettings, Int>, calendar: Calendar = .current
        ) -> Binding<Date> {
            Binding(
                get: {
                    let minute = settings[keyPath: key]
                    return calendar.date(bySettingHour: minute / 60, minute: minute % 60, second: 0, of: Date()) ?? Date()
                },
                set: { date in
                    let components = calendar.dateComponents([.hour, .minute], from: date)
                    settings[keyPath: key] = (components.hour ?? 0) * 60 + (components.minute ?? 0)
                }
            )
        }

        private static func weekdayBinding(_ settings: CompanionSettings, _ day: Int) -> Binding<Bool> {
            Binding(
                get: { settings.activeWeekdays.contains(day) },
                set: { on in
                    if on { settings.activeWeekdays.insert(day) } else { settings.activeWeekdays.remove(day) }
                }
            )
        }
    }
#endif

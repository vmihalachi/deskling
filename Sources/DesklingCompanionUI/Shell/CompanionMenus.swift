#if os(macOS)
    import AppKit
    import DesklingCompanion
    import DesklingShell

    /// The native menus, in the app's words (`MenuCopy`): the menu bar icon's right-click menu and the desktop
    /// character's. Both have Say something, Shh ▸ (or Wake up), Settings…, Support… and Quit. Once the supporter
    /// product is bought, the menu bar's thanks instead of asking and the desktop character's drops the item. The menu
    /// bar's adds the desktop submenu (show, hide for a while, size); the desktop character's adds Size ▸ and Hide ▸.
    /// Built fresh each time, from the model's state.
    @MainActor
    public enum CompanionMenus {
        public static func menuBar<Window, Settings>(_ model: CompanionAppModel<Window, Settings>, copy: MenuCopy) -> NSMenu {
            let menu = NSMenu()
            addTalk(to: menu, model, copy)
            menu.addItem(.separator())
            let desktop = NSMenuItem(title: copy.desktopSubmenu, action: nil, keyEquivalent: "")
            desktop.submenu = desktopSubmenu(model, copy)
            menu.addItem(desktop)
            menu.addItem(.separator())
            addApp(to: menu, model, copy, support: model.isSupporter ? .thank : .ask)
            return menu
        }

        public static func desktop<Window, Settings>(_ model: CompanionAppModel<Window, Settings>, copy: MenuCopy) -> NSMenu {
            let menu = NSMenu()
            addTalk(to: menu, model, copy)
            menu.addItem(.separator())
            let size = NSMenuItem(title: copy.size, action: nil, keyEquivalent: "")
            let sizes = NSMenu()
            addSizes(to: sizes, model, copy)
            size.submenu = sizes
            menu.addItem(size)
            let hide = NSMenuItem(title: copy.hide, action: nil, keyEquivalent: "")
            let spans = NSMenu()
            spans.addItem(ActionItem(copy.forAnHour) { model.hideDesktop(.hour) })
            spans.addItem(ActionItem(copy.forTheRestOfToday) { model.hideDesktop(.restOfToday) })
            spans.addItem(ActionItem(copy.untilIShowItAgain) { model.hideDesktop(.untilShown) })
            hide.submenu = spans
            menu.addItem(hide)
            menu.addItem(.separator())
            addApp(to: menu, model, copy, support: model.isSupporter ? nil : .ask)
            return menu
        }

        // MARK: Parts

        private static func addTalk<Window, Settings>(to menu: NSMenu, _ model: CompanionAppModel<Window, Settings>, _ copy: MenuCopy) {
            menu.addItem(ActionItem(copy.say) { model.sayNow() })
            if model.isShushed {
                menu.addItem(ActionItem(copy.wakeUp) { model.wake() })
            } else {
                let shh = NSMenuItem(title: copy.shh, action: nil, keyEquivalent: "")
                let submenu = NSMenu()
                submenu.addItem(ActionItem(copy.forAnHour) { model.shush(.hour) })
                submenu.addItem(ActionItem(copy.forTheRestOfToday) { model.shush(.restOfToday) })
                submenu.addItem(ActionItem(copy.untilISay) { model.shush(.untilISay) })
                shh.submenu = submenu
                menu.addItem(shh)
            }
        }

        private static func desktopSubmenu<Window, Settings>(_ model: CompanionAppModel<Window, Settings>, _ copy: MenuCopy) -> NSMenu {
            let menu = NSMenu()
            let on = model.settings.showOnDesktop
            if on, model.desktopHiddenUntil != nil {
                menu.addItem(ActionItem(copy.showNow) { model.showDesktopNow() })
            } else {
                let show = ActionItem(copy.show) { model.setShowOnDesktop(!on) }
                show.state = on ? .on : .off
                menu.addItem(show)
            }
            let hour = ActionItem(copy.hideForAnHour) { model.hideDesktop(.hour) }
            let today = ActionItem(copy.hideForTheRestOfToday) { model.hideDesktop(.restOfToday) }
            for item in [hour, today] {
                item.isEnabled = on && model.desktopHiddenUntil == nil
                menu.addItem(item)
            }
            menu.autoenablesItems = false
            menu.addItem(.separator())
            addSizes(to: menu, model, copy)
            return menu
        }

        private static func addSizes<Window, Settings>(to menu: NSMenu, _ model: CompanionAppModel<Window, Settings>, _ copy: MenuCopy) {
            for size in DesktopSize.allCases {
                let item = ActionItem(copy.sizeLabel(size)) { model.settings.desktopSize = size }
                item.state = model.settings.desktopSize == size ? .on : .off
                menu.addItem(item)
            }
        }

        private enum SupportItem { case ask, thank }

        private static func addApp<Window, Settings>(
            to menu: NSMenu, _ model: CompanionAppModel<Window, Settings>, _ copy: MenuCopy, support: SupportItem?
        ) {
            menu.addItem(ActionItem(copy.settings, key: ",") { model.openSettings() })
            switch support {
            case .ask: menu.addItem(ActionItem(copy.support) { model.openSupport() })
            case .thank: menu.addItem(ActionItem(copy.thanksForSupporting) { model.openSupport() })
            case nil: break
            }
            menu.addItem(.separator())
            menu.addItem(ActionItem(copy.quit, key: "q") { model.quit() })
        }
    }

    /// A menu item that runs a closure. It is its own target, so the menu keeps it alive.
    public final class ActionItem: NSMenuItem {
        private let handler: @MainActor () -> Void

        public init(_ title: String, key: String = "", handler: @escaping @MainActor () -> Void) {
            self.handler = handler
            super.init(title: title, action: #selector(run), keyEquivalent: key)
            target = self
        }

        @available(*, unavailable)
        required init(coder: NSCoder) {
            fatalError("not used")
        }

        /// Menus run their actions on the main thread.
        @objc private func run() {
            let handler = handler
            MainActor.assumeIsolated { handler() }
        }
    }
#endif

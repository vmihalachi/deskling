---
name: add-setting
description: Add or change a user preference in a Laching companion app (a CompanionSettings property or the app's subclass, the Settings window, the cadence config, the settings vector shared with the Windows port). Use when asked to add a setting, option, toggle or preference, or to change a default.
---

# Add a setting

Generic preferences live in Laching's `CompanionSettings` (22 keys in `SettingsKeys`); the app adds its own in its
`CompanionSettings` subclass (Swift: `@Observable final class AppSettings: CompanionSettings`; C#:
`AppSettings : CompanionSettings` with `SetProperty`/`SetOrRemove`). App specifics are in `.agents/notes/add-setting.md`.

1. **The property**, in the app's subclass: a stored property whose `didSet` (C#: setter through `SetProperty`) writes
   through `store` under a new key constant, read in `init` with a default from the app's defaults; reset it in the
   `resetToDefaults()` override (calling `super`). Keys are persisted on users' machines: never rename one. A change
   to a generic preference or its default goes to Laching instead (its `CompanionDefaults`), not here.
2. **The control** in the app's Settings view (native controls; the app's components and `Space` steps).
3. **Behavior**: if it affects when the character speaks, it reaches `ChatterCadence.config(for:)` or `QuietRules`
   through Laching (a Laching change, with its own vectors) or the app's own logic, with a unit test. The app
   debounces changes and calls `coordinator.settingsChanged()`.
4. **Strings**: `localization` skill.
5. **Vector**: add the key and default to the app's settings vector (it extends `SettingsVector.base(defaults:)`) and
   run `laching/tools/conformance.sh`; the Windows port reads defaults from `settings.json`.
6. **Windows**: the same key in the C# subclass and its view (`windows-port` skill).
7. README (the settings list) and `scripts/verify.sh`.

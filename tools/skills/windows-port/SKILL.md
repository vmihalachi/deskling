---
name: windows-port
description: Work on a Deskling companion app's Windows port (companion.json `windows.*`; C# .NET 10, WinUI 3). Use before changing or reading anything under the Windows folder, porting Swift behavior to C#, fixing failing `dotnet test` conformance replays, adding a Windows window, view, service or string, or building the WinUI app.
---

# Windows port

The Windows app mirrors the Mac app on Deskling's .NET packages: `Deskling.Core` (scheduler, settings store, store),
`Deskling.Companion` (phrases, picker, cadence, quiet rules, settings, coordinator, desktop presence, skins, the rig),
`Deskling.Windows` (tray, hotkey, idle, busy, startup, notifications, single instance, `.resw` lookup) and, in tests,
`Deskling.Testing` (fakes, fixture loaders, the conformance replay bodies). The app's own core (`windows.core`) holds
what's app-specific; its tests (`windows.coreTests`) replay the app's vectors. Platform differences and the folder
map are in the app's `.agents/notes/windows-port.md` (or its parity doc).

- Core tests run anywhere with the .NET 10 SDK: `dotnet test <windows.dir>/<windows.coreTests>`. The WinUI app's
  XAML compiler needs Windows: push and run the app's Windows workflow.
- Formatting: `dotnet format whitespace <windows.dir> --folder` (the app's `.editorconfig`).
- Swift → C#: the same type and member names, PascalCased; durations as `double` seconds with a `Seconds` suffix;
  string-backed ids (`FlavorId`, `SkinId`, `PaintRole`) as record structs; `PartContent.Vector` for Swift's `.shapes`.
  The app's bank and art load from the generated JSON (`PhraseBank.Parse`, `CharacterArt.Parse`), so new phrases and
  art need no port.

## Conventions

- User-facing text uses the Mac's catalog keys (`Loc.Get("…")`, `Loc.Plural(key, count, args)`, `{l:Loc Key='…'}` in
  XAML). Strings only Windows uses go in with `l10n.py add`, then the `localization` skill, then
  `l10n.py export-windows`. Never read or edit the generated `.resw` files.
- Colors come from the app's palette resources (`{ThemeResource}`), never hard-coded.
- When Swift behavior covered by vectors changes, regenerate them (`conformance` skill); failing `dotnet test` cases
  tell you what to port.
- **Voices**: link the shared voices folder into the package, keeping the tree the Mac bundles
  (`<Content Include="..\..\<voice.dir>\Voice\**\*" Link="Voice\%(RecursiveDir)%(Filename)%(Extension)" …/>`), and read
  it from `Path.Combine(AppContext.BaseDirectory, "Voice")`. Never write into it from Windows code or by hand.
- Native AOT: no reflection-based `JsonSerializer` (source-generated contexts only), `x:Bind` over `{Binding}`;
  warnings are errors.
- No third-party code beyond Deskling and CommunityToolkit.Mvvm. No `internetClient`, `webcam` or `microphone`
  capability in `Package.appxmanifest`, ever.

---
name: port-to-csharp
description: Use when a Swift change in Sources/ needs its C# twin in dotnet/, or when dotnet test fails on a conformance vector after a Swift change. Covers the file-for-file mapping, naming, JSON and AOT rules the port must follow.
---

# Port a Swift change to C#

The C# side mirrors Swift folder for folder and file for file. Port in the same change as the Swift edit.

## Where the twin lives

| Swift | C# |
|---|---|
| `Sources/DesklingCore/<Folder>/<Name>.swift` | `dotnet/Deskling.Core/<Folder>/<Name>.cs` |
| `Sources/DesklingSystem/<Name>.swift` | `dotnet/Deskling.Windows/System/<Name>.cs` |
| `Sources/DesklingShell/<Name>.swift` | `dotnet/Deskling.Windows/Shell/<Name>.cs` |
| `Tests/DesklingCoreTests/<Folder>/<Name>Tests.swift` | `dotnet/Deskling.Core.Tests/<Folder>/<Name>Tests.cs` |
| `Tests/DesklingCoreTests/Conformance/` | `dotnet/Deskling.Core.Tests/Conformance/` |

System and Shell names follow the platform where the concept differs (`ScreenLockWatcher` ↔ `SessionLockWatcher`).
Some files exist on one side only (`DesklingStore`, `TrayIconHost`); don't invent a twin for those.

## Translation rules

- Same type and member names, PascalCased. Durations are `double` seconds with a `Seconds` suffix
  (`interval: TimeInterval` ↔ `IntervalSeconds`). Config types use `{ get; init; }` with the same defaults.
- Same doc comment content (what it does, what it needs, what it never does), as `///` XML docs.
- Same algorithm in the same order. Calendar math uses the vector's IANA time zone and the Gregorian calendar; see
  `conformance/README.md` for rounding rules (for example `floor(x + 0.5)`, not banker's rounding).
- **JSON: source-generated `JsonSerializerContext` only**, never reflection-based `JsonSerializer` overloads.
  Apps publish with Native AOT, and `TreatWarningsAsErrors` turns trim/AOT warnings into build failures.
- `Deskling.Core` stays platform-neutral (`net10.0`, `IsAotCompatible`). Win32 / Windows App SDK code goes in
  `Deskling.Windows`.
- The package ships no strings: anything user-visible is a parameter.

## Verify

```sh
cd dotnet && dotnet test Deskling.Core.Tests   # replays every vector; failures name the vector and step
dotnet format whitespace dotnet --folder
```

`Deskling.Windows` can't fully build off Windows (MakePri). Its CI job in `dotnet.yml` is the check: push the branch.

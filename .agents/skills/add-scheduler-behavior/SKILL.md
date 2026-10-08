---
name: add-scheduler-behavior
description: Use when adding or changing how ReminderScheduler decides to remind, postpone or hold (a new SchedulerConfig field, a new busy reason, a new step op), in Swift or C#. Walks the additive, vector-tested path so existing conformance vectors stay byte-identical and the C# port lands in the same change.
---

# Add scheduler behavior

Swift is the reference. Vectors are generated from it and replayed by C#. Do every step in one change.

## 1. Config field (Swift)

`Sources/DesklingCore/Scheduling/SchedulerConfig.swift`:

- Add a `public var` with a doc comment that says what it does and what the default means.
- **The default must reproduce today's behavior exactly** (like `intervalJitter = 0`, which never reads the random
  source). Add it to the `init` with the same default, at the end of the parameter list.

## 2. Behavior (Swift)

`Sources/DesklingCore/Scheduling/ReminderScheduler.swift`. With the default value the code path must be identical to
before: guard the new branch on the field, don't reorder existing decisions.

## 3. Unit tests (Swift)

`Tests/DesklingCoreTests/Scheduling/ReminderSchedulerTests.swift`: the new behavior, and that the default changes
nothing. The jitter tests (`config.intervalJitter = …`) are the model.

## 4. Vector format and case

`Tests/DesklingCoreTests/Conformance/SchedulerVectors.swift`:

- `Config`: add the field as an **optional** that is `nil` at the default (see `intervalJitterSeconds`), in both
  `init(_:)` and `scheduler`. A non-optional field would rewrite all existing vectors.
- `cases`: add one or more `Case(name:description:config:…)` that exercise the behavior. Names are kebab-case and
  become `conformance/scheduler/<name>.json`.
- New step op? Add it to `Script`, to `evaluate`'s `switch step.op`, and document it in `conformance/README.md`.

Document the new config key in `conformance/README.md` (`scheduler/*.json` → `config`).

## 5. Regenerate and check nothing else moved

```sh
scripts/conformance.sh
git status --short conformance   # only your new files: any modified vector means the default isn't neutral
```

No Mac: push the branch and dispatch `regenerate-vectors.yml`, then pull its commit. Never hand-edit vectors.

## 6. C# port

Follow the `port-to-csharp` skill. For the scheduler that means:

- `dotnet/Deskling.Core/Scheduling/SchedulerConfig.cs`: `{ get; init; }` property, same default, durations in
  `…Seconds` doubles.
- `dotnet/Deskling.Core/Scheduling/ReminderScheduler.cs`: the same logic, line for line where possible.
- `dotnet/Deskling.Core.Tests/Conformance/SchedulerVectorsTests.cs` → `ReadConfig`: read the key with
  `OptionalDouble(c, "<key>", <default>)` (or a matching optional reader). New ops go in the replay `switch`.
- `dotnet/Deskling.Core.Tests/Scheduling/ReminderSchedulerTests.cs`: mirror the Swift unit tests.

## 7. Finish

- `CHANGELOG.md` → *Unreleased* → *Added*: one line naming the field on both sides.
- `scripts/verify.sh` (runs swift-format, swift test, vectors-current, dotnet format and dotnet test as applicable).

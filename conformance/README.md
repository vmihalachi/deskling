# Conformance vectors

Language-neutral test data that keeps the Swift package and the .NET port behaving the same. The Swift code
is the reference: every file here is **generated** by `Tests/DesklingCoreTests/Conformance` and replayed by
`dotnet/Deskling.Core.Tests/Conformance`.

**Never edit a vector by hand.** Change the Swift code, then regenerate:

```sh
scripts/conformance.sh        # DESKLING_WRITE_CONFORMANCE=1 swift test --filter Conformance
```

`ConformanceTests` fails whenever the committed files differ from what the Swift code generates, and it
replays every committed vector. To add a case, add it to the `cases` list in
`Tests/DesklingCoreTests/Conformance/SchedulerVectors.swift` and regenerate.

Files are byte-exact (`.gitattributes` turns off line-ending conversion). Ports parse the JSON and compare values.

## Shared conventions

- **Dates** are ISO 8601 with an offset (`2026-09-28T10:00:00+03:00`, `Z` for UTC), written in the vector's
  `timeZone`, with milliseconds only when the date has a fraction of a second.
- **`timeZone`** is an IANA ID. Every calendar computation (days, hours, weekdays, midnight) uses the Gregorian
  calendar in that zone. Weekdays are 1 = Sunday … 7 = Saturday.
- **Durations** are whole seconds (`…Seconds`) or minutes (`…Minutes`). Minutes of the day (`activeStartMinute`)
  count from local midnight.
- **Optional values** are either written as `null` or left out; treat both as "no value".

## `scheduler/*.json`

`ReminderScheduler`: a config, then a timeline of steps, each with the expected result and state after it.
The first 37 vectors are mybackhurts' `conformance/scheduler`, byte for byte: the package behaves exactly like
the code it was extracted from. The `jitter-*` vectors add the one new behavior.

- `start`: the clock when the scheduler is created. `initialState`: its state right then. `seed` (optional): the
  `SplitMix64` seed of the scheduler's random source; absent when the vector uses no jitter.
- `config`: `intervalSeconds`, `activeStartMinute`, `activeEndMinute` (end before start means overnight),
  `activeWeekdays`, `quietHours` (local hours 0–23), `idleThresholdSeconds`, `postponeDuringCalls`,
  `postponeDuringFullScreen`, `busyGraceSeconds`, `afterCallFallbackSeconds`, and `intervalJitterSeconds`
  (optional, absent when zero). Each new interval is `intervalSeconds` plus `floor((2u − 1) × jitter + 0.5)`
  seconds, where `u` is the next unit from SplitMix64 (top 53 bits over 2^53); with zero jitter the source is
  never read.
- `steps[].op`:
  - `ticks`: repeat `count` times: advance the clock by `everySeconds` (0 or missing: don't), then call `tick()`
    with `idleSeconds` since the last input and the `busy` reasons (`call`, `fullScreen`) active.
  - `setTime` (`time`), `snooze` (`minutes`), `snoozeUntilAfterCall`, `pauseFor` (`seconds`), `pauseUntil` (`time`),
    `pauseUntilEndOfToday`, `resume`, `sessionCompleted`, `updateConfig` (`config`).
  - `isWithinActiveHours`: ask about `time`; the answer is `expect.result`.
- `steps[].expect`: `decisions` (for `ticks`: the 1-based ticks that returned `remind` or `postpone`; every other
  tick returned none), `result`, and `state`: `now`, `status` (`{type: "scheduled"|"paused", date}`,
  `{type: "outsideActiveHours"|"away"}` or `{type: "postponed", reason}`), `nextFireDate`, `pausedUntil`,
  `isAway`, `isHolding`, `isWaitingForCall`, `endOfToday`.

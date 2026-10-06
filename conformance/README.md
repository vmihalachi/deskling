# Conformance vectors

Language-neutral test data that keeps the Swift package and the .NET port behaving the same. The Swift code
is the reference: every file here is **generated** by `Tests/ConformanceTests` and replayed by
`dotnet/Deskling.Core.Tests`.

**Never edit a vector by hand.** Change the Swift code, then regenerate:

```sh
scripts/conformance.sh        # DESKLING_WRITE_CONFORMANCE=1 swift test --filter ConformanceTests
```

`ConformanceTests` fails whenever the committed files differ from what the Swift code generates, and it
replays every committed vector.

## `schedule/*.json`

Arrives with milestone 1 (the interval scheduler), in the format of mybackhurts' `conformance/scheduler/`
vectors plus the optional `intervalJitterSeconds` config field and `seed`.

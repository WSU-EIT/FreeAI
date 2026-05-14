# FreeServicesHub.TestMe

A CLI test harness for validating the FreeServicesHub Agent and its installer from the command line. Runs from source (`dotnet run`) or from pre-built artifacts, with selectable test modes and configurable timeouts.

## Tests

Run with `--test=N` to select a specific test, or omit to see the list of available tests.

### Test 1 — Agent Console Mode

Starts the agent process (either by building from source with `dotnet run` or by launching the pre-built exe), watches stdout for lines containing "Heartbeat", verifies the expected output format, then kills the process. Succeeds when the heartbeat output appears within the timeout.

### Test 3 — Installer CLI Headless

Invokes the `FreeServicesHub.Agent.Installer` in non-interactive mode: runs `build`, then `configure` with `--NonInteractive`, checks that the configured marker is written, then runs `remove` to verify clean teardown. No elevation required.

### Test 4 — Agent Standalone Full

Starts the agent, waits for at least N heartbeat lines (configurable with `--heartbeats`), verifies that each line matches the expected "Heartbeat" format with timestamps and metric labels, then kills the process.

## Usage

```
dotnet run -- --test=1 --heartbeats=3 --interval=5 --timeout=60
dotnet run -- --test=3 --installerdir=C:\path\to\installer --servicedir=C:\path\to\agent
dotnet run -- --test=4 --heartbeats=5 --interval=2 --timeout=90 --config=Release
```

| Flag | Default | Meaning |
|---|---|---|
| `--test` | (none — shows list) | Test number to run: 1, 3, or 4 |
| `--heartbeats` | 3 | How many heartbeat lines to wait for (Tests 1 and 4) |
| `--interval` | 5 | Agent heartbeat interval in seconds |
| `--timeout` | 60 | Max seconds to wait before failing |
| `--config` | Debug | Build configuration (`Debug` or `Release`) |
| `--servicedir` | auto-detected | Path to the `FreeServicesHub.Agent` project or build output |
| `--installerdir` | auto-detected | Path to the `FreeServicesHub.Agent.Installer` project |

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` — console executable |
| Output type | `Exe` |
| Target framework | net10.0 |
| NuGet packages | None (no dependencies beyond the .NET 10 BCL) |

## Project references

None (launches other projects as child processes; no compile-time dependency).

Part of the **FreeServicesHub** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A CLI test harness that validates the **agent and its installer** without needing the hub. It launches the agent (from source or a built exe), watches its console output for "Heartbeat" lines in the expected format, and confirms they appear within a timeout. It can also run the installer headlessly (`build` → `configure` → `remove`) to prove a clean install/uninstall.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Console test harness | Launch agent/installer, assert output | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.TestMe/Program.cs) |
| Child-process control | Start/watch/kill the agent process | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.TestMe/Program.cs) |

**Why does this exist?**
To smoke-test the agent quickly — "does it start and emit heartbeats?" and "does the installer install/remove cleanly?" — with no hub, no database, and no dependencies beyond the BCL.

**What does it accomplish that other tools don't?**
- Tests the agent **as a black box** (watches real stdout), the way it actually runs in production.
- **No elevation required** for the installer headless test, so it runs in plain CI.

**Terminology & "can I see it?"**
- **Test harness** — a small program that drives another program and checks its behavior.
- **Headless** — runs with no prompts/UI, suitable for pipelines.

**The hard part, drawn** — black-box agent check:

```
  dotnet run -- --test=4 --heartbeats=5
        │ start the agent (dotnet run or built exe)
        ▼ watch stdout for ≥5 lines matching the "Heartbeat" format (CPU/RAM/disk + timestamp)
        ▼ all seen before timeout? ─▶ PASS ; else ─▶ FAIL ─▶ kill the process
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

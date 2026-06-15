# A11yAudit

One-click accessibility scan orchestrator for FreeA11yChecker. Auto-discovers scan targets from `appsettings.*.json` files, launches each target's server process (or verifies an external site is reachable), runs `FreeA11yChecker.Console` against it, copies evidence to the target's named subdirectory, and shuts down local servers cleanly. Results are committed to source control as a living accessibility evidence trail.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | `Exe` |

## What It Does

- Scans `A11yAudit/` for files matching `appsettings.*.json`. Each file defines one scan target. The target name is taken from the `*` segment of the filename.
- For local targets (no `"External": true`), `StartServer` launches `dotnet run` in the target's project directory, drains stdout/stderr asynchronously to prevent deadlocks, and waits up to 60 seconds for the health URL to respond.
- For external targets (`"External": true`), only a reachability check against the configured health URL is performed — no server is launched.
- After the server is ready, runs `FreeA11yChecker.Console` via `RunScanner` with the target's config file and an output directory under `A11yAudit/runs/{target-name}/`.
- Exit code 1 from the scanner means violations were found (scan succeeded); exit code > 1 means the scanner itself failed.
- Copies scan results (screenshots, JSON, HTML reports, markdown summaries) to the target's evidence subdirectory.
- `ProcessKillTree` terminates the server process and its entire child process tree on Windows.

## CLI Flags

| Argument | Effect |
|---|---|
| *(none)* | Scan all discovered targets |
| `<TargetName>` | Scan only the named target |
| `--external-only` | Scan only targets marked `"External": true` |
| `--local-only` | Scan only local (non-external) targets |

## Configured Scan Targets

| Config file | Target |
|---|---|
| `appsettings.blazorapp1.json` | BlazorApp1 (localhost, seeded Identity user) |
| `appsettings.freea11ychecker.json` | FreeA11yChecker main app (localhost) |
| `appsettings.wsu-admissions.json` | WSU Admissions (external) |
| `appsettings.wsu-financialaid.json` | WSU Financial Aid (external) |
| `appsettings.wsu-hrs.json` | WSU HRS (external) |
| `appsettings.wsu-its.json` | WSU ITS (external) |
| `appsettings.wsu-main.json` | WSU main site (external) |
| `appsettings.wsu-registrar.json` | WSU Registrar (external) |

## Key Classes / Methods

| Function | Purpose |
|---|---|
| `DiscoverTargets(auditDir, solutionDir)` | Reads all `appsettings.*.json` files and returns a list of `ScanTarget` records |
| `ScanTarget(target)` | Orchestrates one full scan: start server (if local), wait for health, run scanner, copy evidence, stop server |
| `StartServer(projectDir)` | Launches `dotnet run` and begins async stdout/stderr drain |
| `StopServer(proc)` | Kills the server process tree |
| `WaitForHealthy(url, timeout)` | Polls the health URL with exponential back-off until reachable |

## Project References

None — the orchestrator invokes `FreeA11yChecker.Console` as a child process.

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.Extensions.Configuration.UserSecrets` | User secrets support |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A one-click orchestrator. It finds every `appsettings.*.json` in its folder (each file = one scan target), starts each local app with `dotnet run` (or just pings external sites), runs the Console scanner against it, copies the evidence into a per-target folder, and shuts the app down cleanly. The committed results become a living accessibility evidence trail in git.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Process orchestration | Start/health-check/kill each target app | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/A11yAudit/Program.cs) |
| FreeA11yChecker.Console (child process) | The actual scanning | [the Console project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.Console) |
| Config-file target discovery | One `appsettings.*.json` per target | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/A11yAudit/Program.cs) |

**Why does this exist?**
To turn "audit all of our apps" into a single command whose output is version-controlled proof over time — rather than a manual, one-app-at-a-time chore.

**What does it accomplish that other tools don't?**
- **Discovers** its own targets from config files — add a new app by dropping in one JSON file.
- Manages **real server processes** safely: starts them, waits for health, and kills the *entire* child process tree on Windows when done.
- Treats **git history as the audit log** — every run is a reviewable diff.

**Terminology & "can I see it?"**
- **Orchestrator** — code that coordinates other programs rather than doing the work itself.
- **Kill-tree** — terminating a process *and* every process it spawned (so no orphaned servers linger).
- **External target** — a site that's already live; the tool only pings it, never launches it.

**The hard part, drawn** — many apps, one evidence trail:

```
  A11yAudit ──▶ find appsettings.*.json   (one per target)
       for each target:
          start local app (or ping external) ──▶ wait until healthy
              ──▶ run FreeA11yChecker.Console ──▶ copy evidence to runs/{target}/
              ──▶ kill the server's whole process tree
       all results committed to git  =  an accessibility evidence trail over time
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

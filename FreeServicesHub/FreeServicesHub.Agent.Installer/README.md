# FreeServicesHub.Agent.Installer

A Windows-only CLI and interactive console tool for building, deploying, and managing the FreeServicesHub Agent as a Windows Service (`sc.exe`). Supports both interactive menus and fully non-interactive CI/CD pipelines via command-line flags.

## What it does

`Program.cs` reads configuration from `appsettings.json` then either:

- **Interactive mode** — presents a numbered menu (Build, Configure, Remove, Start, Stop, Status, Destroy) with a live status line showing whether the agent service is configured.
- **CLI mode** — dispatches directly to an action and exits, suitable for pipelines. All configuration values can be overridden with `--Key:Property=value` flags.

Actions:

| Action | What happens |
|---|---|
| `build` | Runs `dotnet publish` on `FreeServicesHub.Agent`, targeting `win-x64`, single-file self-contained |
| `configure` / `install` | Copies publish output to `InstallPath` (default `C:\FreeServicesHubAgent`), creates the Windows Service with `sc.exe create`, writes the registration key to the agent's `appsettings.json` |
| `remove` / `uninstall` | Stops and deletes the service with `sc.exe`, removes the install directory |
| `start` / `stop` | Calls `sc.exe start` / `sc.exe stop` |
| `status` | Calls `sc.exe query` and shows recent lines from `agent.log` |
| `destroy` | Nuclear option: remove + wipe all artifacts |

The `--NonInteractive` flag (auto-set in CLI mode) suppresses all `Console.ReadLine()` prompts, making every action runnable from Azure Pipelines.

## Key public classes

| Class | Purpose |
|---|---|
| `InstallerConfig` | Root configuration model bound from `appsettings.json` |
| `ServiceSettings` | Service name, display name, exe path, install path |
| `PublishSettings` | Project path, output path, runtime (`win-x64`), self-contained, single-file |
| `SecuritySettings` | Registration key written to agent's `appsettings.json` on install |

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` — console executable |
| Output type | `Exe` |
| Target framework | net10.0 |
| Platform | Windows only (`sc.exe` required) |

## Notable NuGet packages

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Extensions.Configuration` | 10.0.0-preview.3 | Configuration infrastructure |
| `Microsoft.Extensions.Configuration.Json` | 10.0.0-preview.3 | `appsettings.json` support |
| `Microsoft.Extensions.Configuration.CommandLine` | 10.0.0-preview.3 | CLI flag overrides |
| `Microsoft.Extensions.Configuration.Binder` | 10.0.0-preview.3 | Strongly-typed config binding |

Part of the **FreeServicesHub** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A Windows-only tool that turns the agent into an installed Windows Service. It can `build` the agent (`dotnet publish`, single-file self-contained `win-x64`), `configure`/`install` it (copy files, `sc.exe create`, write the registration key), `start`/`stop`/`status` it, and `remove`/`destroy` it. It runs as an interactive menu *or* fully non-interactive for CI/CD (every config value overridable with `--Key:Property=value`).

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Installer CLI / menu | Build / install / manage the service | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Agent.Installer/Program.cs) |
| `sc.exe` (Windows SCM) | Create/start/stop/delete the service | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Agent.Installer/Program.cs) |

**Why does this exist?**
Deploying a Windows Service by hand (publish, copy, `sc create`, set the key, start) is fiddly and easy to get wrong. This makes it one command — and the same command works in an Azure Pipeline.

**What does it accomplish that other tools don't?**
- **One tool for the whole lifecycle**: build → install → start → status → remove, interactive or scripted.
- **CI-ready** — `--NonInteractive` suppresses all prompts so pipelines never hang.

**Terminology & "can I see it?"**
- **`sc.exe`** — the built-in Windows command to manage services.
- **Self-contained single-file** — the published agent bundles the .NET runtime, so the target box needs nothing pre-installed.

**The hard part, drawn** — agent project to running service:

```
  build ──▶ dotnet publish (win-x64, single-file, self-contained)
  install ─▶ copy to C:\FreeServicesHubAgent  ·  sc.exe create  ·  write registration key
  start ───▶ sc.exe start ─▶ agent registers with the hub & begins heartbeating
  status ──▶ sc.exe query + tail agent.log      remove ─▶ sc.exe stop/delete + clean up
        (every step also runnable head-less from CI via --NonInteractive)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

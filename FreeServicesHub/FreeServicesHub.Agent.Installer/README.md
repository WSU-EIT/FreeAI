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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

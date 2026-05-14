# FreeExamples.NuGetClientPublisher

Interactive console tool for packing and publishing the `FreeExamples.Client` NuGet package. Wraps `dotnet pack` and `dotnet nuget push` behind a safety menu with dry-run mode.

---

## What It Does

`FreeExamples.NuGetClientPublisher` is an interactive CLI that guides you through the pack-and-publish workflow for the `FreeExamples.NuGetClient` library. It reads configuration from `appsettings.json` and user secrets, supports dry-run mode so you can preview operations without making live changes, and lets you look up existing versions on NuGet.org before pushing.

---

## Command-Line Arguments

```
dotnet run                         # Start the interactive menu (reads appsettings.json)
dotnet run -- --version 1.2.3     # Override the package version before starting
```

---

## Interactive Menu

| Key | Action |
|-----|--------|
| `1` | View current configuration |
| `2` | Verify the project builds |
| `3` | Pack — produces `.nupkg` |
| `4` | Push to NuGet.org |
| `5` | Full publish (Clean → Build → Pack → Push) |
| `L` | Look up existing versions on NuGet.org |
| `V` | Change version number for this session |
| `D` | Toggle dry-run mode (default: ON) |
| `0` | Exit |

Dry-run mode is enabled by default. No changes are made until you toggle it off with `D`.

---

## Configuration (appsettings.json)

```json
{
  "NuGet": {
    "PackageId":    "FreeExamples.Client",
    "Version":      "1.0.0",
    "Source":       "https://api.nuget.org/v3/index.json",
    "ProjectPath":  "FreeExamples.NuGetClient\\FreeExamples.NuGetClient.csproj",
    "SolutionRoot": "",
    "Configuration": "Release",
    "SkipDuplicate": true,
    "IncludeSymbols": true
  }
}
```

Store the NuGet API key in user secrets (never commit it):

```bash
dotnet user-secrets set "NuGet:ApiKey" "your-nuget-api-key"
```

---

## Key Public Classes/Methods

| Member | Description |
|--------|-------------|
| `Program.Main` | Entry point — loads config, starts interactive loop |
| `NuGetConfig` | Configuration POCO bound from `appsettings.json` |
| `VerifyBuild()` | Runs `dotnet build` and reports pass/fail |
| `PackNuGet()` | Runs `dotnet pack` with version override |
| `PushToNuGet()` | Runs `dotnet nuget push` with API key |
| `FullPublish()` | Orchestrates Clean → Build → Pack → Push |
| `LookupVersions()` | Queries NuGet.org flat-container API |
| `ToggleDryRun()` | Switches between dry-run and live mode |

---

## Project References and NuGet Packages

| Type | Reference |
|------|-----------|
| NuGet | `Microsoft.Extensions.Configuration` 9.0.0 |
| NuGet | `Microsoft.Extensions.Configuration.Binder` 9.0.0 |
| NuGet | `Microsoft.Extensions.Configuration.CommandLine` 9.0.0 |
| NuGet | `Microsoft.Extensions.Configuration.Json` 9.0.0 |
| NuGet | `Microsoft.Extensions.Configuration.UserSecrets` 9.0.0 |

---

## Build Details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Output type | `Exe` |
| Target framework | `net10.0` |
| User Secrets | `FreeExamples.NuGetClientPublisher` |

---

Part of the FreeTools solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

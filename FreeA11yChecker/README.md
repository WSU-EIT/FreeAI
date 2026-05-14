# FreeA11yChecker

**Created by [Washington State University Enrollment Information Technology (WSU-EIT)](https://em.wsu.edu/eit/) · Free and open-source**

An open-source web accessibility auditing platform built in C# / Blazor WebAssembly / .NET 10.
FreeA11yChecker automatically scans websites for WCAG 2.1 and WCAG 2.2 Level AA violations,
generates rich visual reports, and provides a full multi-tenant web UI for managing scan targets,
reviewing findings, and tracking remediation progress.

Built by the Enrollment Information Technology (EIT) team at Washington State University to
satisfy obligations under ADA Title II, Washington State Technology Policy USER-01, and
UPPM 10.45 — and released for free so any organization can use it. WCAG 2.1 AA is required
today; WCAG 2.2 AA is required by **July 1, 2026**.

---

## What It Does

| Capability | Detail |
|-----------|--------|
| **4-engine consensus scanning** | axe-core · IBM Equal Access · HTML CodeSniffer · custom HtmlChecker — results merged and deduplicated |
| **WCAG 2.1 & 2.2 Level AA** | Covers all A and AA success criteria across Perceivable, Operable, Understandable, Robust |
| **Visual overlays** | QuickPeek region map · landmark wireframe · screen reader linearized view |
| **Color Vision Deficiency simulations** | 7 CVD types rendered as side-by-side screenshots |
| **Structural checks** | Heading hierarchy · skip-link presence · `<main>` landmark · `<table>` headers · button names |
| **Keyboard & mobile** | Keyboard nav simulation · mobile viewport scan · text-spacing test |
| **Reading level analysis** | Flesch-Kincaid and related metrics flagged per page |
| **SSL / certificate audit** | Cert expiry and configuration checked per site |
| **PDF + HTML reports** | Full per-page and per-site reports generated via QuestPDF |
| **Scheduled & on-demand scans** | NCrontab-driven background service or triggered from the web UI |
| **CLI tool** | `dotnet run -- scan --url https://example.com` — no web UI required |
| **Static source analysis** | Scan Blazor/Razor/HTML source files for ARIA, image, form, and structural issues before deployment |
| **Multi-tenant** | One deployment serves multiple sites/organizations with isolated data |
| **Plugin extensibility** | Drop `.cs` or `.plugin` files into `PluginFiles/` — no recompile of the core required |

---

## Projects at a Glance

| Project | Type | Accessibility Role |
|---------|------|--------------------|
| **FreeA11yChecker** | Blazor Server host | API, auth, SignalR hub, scheduled scan service, PDF report delivery |
| **FreeA11yChecker.Client** | Blazor WebAssembly | Web UI — scan dashboards, issue review, site management, real-time progress via SignalR |
| **FreeA11yChecker.Scanner** | Class library | Core scanning engine — Playwright + 4 a11y engines + overlays + report generator |
| **FreeA11yChecker.Console** | CLI executable | Standalone scanner — scan any URL from the terminal; static source-code a11y analysis; AI handoff report |
| **FreeA11yChecker.DataAccess** | Class library | All database I/O, business logic, PDF generation, Azure AD / LDAP auth, MS Graph |
| **FreeA11yChecker.EFModels** | Class library | EF Core entities + DbContext (SQL Server · SQLite · PostgreSQL · MySQL) |
| **FreeA11yChecker.DataObjects** | Class library | Shared DTOs — scan models, site configs, SignalR payloads — used by both server and WASM client |
| **FreeA11yChecker.Plugins** | Class library | Roslyn dynamic C# compiler host — powers drop-in plugin and Blazor component extensibility |
| **Docs** | Documentation | All architecture guides, patterns, component docs, and this quickstart |

---

## Quick Start

```bash
git clone https://github.com/WSU-EIT/FreeA11yChecker.git
cd FreeA11yChecker
dotnet restore
dotnet build
dotnet run --project FreeA11yChecker
```

Navigate to `https://localhost:5001` to open the web UI.

### CLI — Scan Without the Web UI

```bash
# Scan sites configured in appsettings.json
dotnet run --project FreeA11yChecker.Console -- scan

# Quick scan of any URL
dotnet run --project FreeA11yChecker.Console -- scan --url https://example.com

# Scan specific pages
dotnet run --project FreeA11yChecker.Console -- scan --url https://example.com --pages /,/about,/contact

# Scan with authentication
dotnet run --project FreeA11yChecker.Console -- scan --url https://example.com --user admin --pass secret

# Interactive menu
dotnet run --project FreeA11yChecker.Console
```

### Configuration (User Secrets)

The web app and CLI share the same `UserSecretsId` — set credentials once, both tools pick them up.

```bash
dotnet user-secrets set "ConnectionStrings:Default" "<connection-string>" --project FreeA11yChecker
dotnet user-secrets set "App:SomeKey" "<value>" --project FreeA11yChecker
```

---

## Plugins

FreeA11yChecker supports a plugin architecture for extending functionality without modifying
core source files. Plugins live in `FreeA11yChecker/PluginFiles/` and are loaded at startup.

**Built-in plugin types:**

| Type | Purpose |
|------|---------|
| `Auth` | Custom authentication strategy (LDAP, SSO, etc.) |
| `BackgroundProcess` | Task executed on each background service tick (e.g., post-scan webhooks, custom alerting) |
| `Example` | General-purpose extensibility hook |
| `UserUpdate` | Fires when a user record is saved |
| Blazor Components | `.blazor`/`.razor` files — rendered dynamically in the UI without recompiling |

Plugin files use `.cs` (compiled with the solution) or `.plugin` (plain text, loaded at runtime —
useful for code that references external DLLs not in the solution). A `.assemblies` sidecar
file lists any external DLL paths the plugin needs:

```
.\MyPlugin\MyPlugin.dll
typeof(SomeNameSpace.SomeType).Assembly.Location
```

See `FreeA11yChecker/PluginFiles/Plugins.md` and the example files in that folder for full details.

---

## Background Service (Scheduled Scanning)

The built-in background service drives scheduled accessibility scans. Configure it in
`appsettings.json` under the `BackgroundService` key:

| Setting | Purpose |
|---------|---------|
| `Enabled` | Turn the scheduler on or off |
| `IntervalSeconds` | How often to run (default: 60) |
| `StartOnLoad` | `true` = run immediately at startup; `false` = wait for first interval |
| `LogFilePath` | Folder for detailed scheduler log files |
| `LoadBalancingFilter` | Restrict to a specific server name — prevents duplicate scans in load-balanced deployments |

To add custom work to the scheduler, either override `ProcessBackgroundTasksApp` in
`DataAccess.App.cs` or create a `BackgroundProcess` plugin (see `ExampleBackgroundProcess.cs`).

### IIS Hosting

To keep the scheduler running continuously under IIS:
- Set **Application Pool Start Mode** to `AlwaysRunning`
- Enable **Preload** on the application (requires the IIS Application Initialization feature)

---

## Compliance Context

| Standard | Applies To | Deadline |
|---------|-----------|---------|
| WCAG 2.1 Level AA | All WSU digital services (USER-01 + UPPM 10.45) | Now |
| WCAG 2.2 Level AA | All WSU digital services (USER-01) | July 1, 2026 |
| ADA Title II (DOJ rule) | State/local government web + mobile | April 26, 2027 (50K+ entities) |

See `ACCESSIBILITY_GUIDELINES.md` for the full compliance pyramid, WCAG criterion checklists,
and a breakdown of what each scanning engine covers.

---

## Documentation

All docs live in the `Docs/` folder and are indexed in `Docs/009_project_architecture_overview.md`.

| Doc | Topic |
|-----|-------|
| `000_quickstart.md` | Local setup, CLI commands, AI agent commands |
| `006_architecture.md` | Architecture index and hook pattern guide |
| `007_patterns.playwright.md` | Playwright scanning patterns |
| `007_patterns.crud_api.md` | Three-endpoint API convention |
| `007_patterns.signalr.md` | Real-time scan progress via SignalR |
| `008_components.md` | UI component index |
| `009_project_architecture_overview.md` | Full solution map with ASCII diagrams |
| `ACCESSIBILITY_GUIDELINES.md` | WCAG 2.1/2.2 checklists and compliance pyramid |

---

## About WSU-EIT

FreeA11yChecker is developed and maintained by
[Enrollment Information Technology (EIT)](https://em.wsu.edu/eit/) at
[Washington State University](https://wsu.edu). EIT builds and operates the
technology systems that support enrollment services across WSU, and created
FreeA11yChecker to help the university — and anyone else who needs it — meet
web accessibility standards.

This project is open-source under the [MIT License](LICENSE). Contributions,
bug reports, and feature requests are welcome — open an issue or submit a pull
request on [GitHub](https://github.com/WSU-EIT/FreeA11yChecker).

---

*© Washington State University — Enrollment Information Technology (EIT)*  
*[github.com/WSU-EIT](https://github.com/WSU-EIT) · [em.wsu.edu/eit](https://em.wsu.edu/eit/)*
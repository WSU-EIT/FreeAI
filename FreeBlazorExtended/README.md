# FreeBlazorExtended

Shared Blazor component library and companion showcase application extracted from WSU-EIT project patterns.

`FreeBlazorExtended` is a Razor class library (net10.0) that bundles reusable UI components, feature services, and SignalR infrastructure for downstream Blazor apps. `FreeBlazorExample` is the living showcase host that demonstrates every component across ~17 pages. `FreeBlazorExtended.Agent` is a Windows Service that registers with the hub, sends host telemetry heartbeats, and executes remote Windows Service and IIS app-pool commands.

## Projects in this solution

| Project | SDK | Framework | Role |
|---------|-----|-----------|------|
| `FreeBlazorExtended` | `Microsoft.NET.Sdk.Razor` | net10.0 | Component library; 12 UI components + 6 feature services + SignalR hubs |
| `FreeBlazorExtended.Agent` | `Microsoft.NET.Sdk.Worker` | net10.0-windows | Windows Service worker; heartbeats, service/IIS control via AgentHub |
| `FreeBlazorExample` | `Microsoft.NET.Sdk.Web` | net10.0 | ASP.NET Core host for the showcase |
| `FreeBlazorExample.Client` | `Microsoft.NET.Sdk.BlazorWebAssembly` | net10.0 | WASM client; standard pages + `/showcase/*` demo pages |
| `FreeBlazorExample.DataAccess` | `Microsoft.NET.Sdk` | net10.0 | Server-side business logic and EF Core repositories |
| `FreeBlazorExample.DataObjects` | `Microsoft.NET.Sdk` | net10.0 | Shared DTOs and endpoint constants |
| `FreeBlazorExample.EFModels` | `Microsoft.NET.Sdk` | net10.0 | EF Core DbContext; supports SQL Server, SQLite, MySQL, PostgreSQL |
| `FreeBlazorExample.Plugins` | `Microsoft.NET.Sdk` | net10.0 | Roslyn-based dynamic C# plugin runtime |
| `FreeBlazorExample.ShowcaseTool` | `Microsoft.NET.Sdk` | net10.0 | Console tool; Playwright headless screenshots + Magick.NET GIF generation |

See each project's `README.md` for its specific role.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This solution is three pieces that work together: **`FreeBlazorExtended`** is the reusable component *library* (the toolbox of ~20 UI components + services); **`FreeBlazorExample`** is a living *showcase app* that demonstrates every component across ~17 pages; and **`FreeBlazorExtended.Agent`** is a Windows Service that lets the showcase remotely control Windows Services and IIS app pools on a server.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Razor class library | The reusable components + services | [FreeBlazorExtended/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended) |
| Blazor WASM showcase app | Demonstrates every component | [FreeBlazorExample/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExample) |
| Windows Service agent | Remote service/IIS control | [FreeBlazorExtended.Agent/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended.Agent) |

**Why does this exist?**
To extract proven UI components out of WSU's apps into one library other projects can pull from — with a runnable showcase so you can *see* each component before adopting it.

**What does it accomplish that other tools don't?**
- A **library + live showcase + companion agent** in one place: read the component's README, see it running, then cherry-pick it.

**Terminology & "can I see it?"**
- **Library vs. showcase** — the library is what you reuse; the showcase is the demo app that exercises it.
- *See it:* run `FreeBlazorExample` and open `/showcase`.

**The hard part, drawn** — how the three projects relate:

```
  FreeBlazorExtended (library) ──referenced by──▶ FreeBlazorExample (showcase app, /showcase)
            ▲                                              │ Feature 105 dashboard
            │ AgentMonitoring service                      ▼ SignalR commands
            └────────────── FreeBlazorExtended.Agent (Windows Service on a target server)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

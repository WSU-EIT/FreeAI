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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

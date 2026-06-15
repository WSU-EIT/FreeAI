# FreeAI

ASP.NET Core Blazor Server + WebAssembly host application (Blazor United / Interactive WebAssembly render mode).

This is the FreeCRM scaffold renamed to the FreeAI namespace with no app-specific logic added yet. It wires up the full FreeCRM multi-tenant pipeline: ASP.NET Core host, cookie/OAuth authentication (Google, Microsoft, Apple, Facebook, OpenId), Roslyn-based plugin loading from a `Plugins/` folder, SignalR hub (`/freeaiHub`) with optional Azure SignalR Service fallback, QuestPDF Community license registration, and DI registration for `IDataAccess`, `IConfigurationHelper`, and `ICustomAuthentication`. The `Program.App.cs` extension points (`AppModifyBuilderStart`, `AppModifyBuilderEnd`, `AppModifyStart`, `AppModifyEnd`) are present but empty, and `AuthenticationPoliciesApp` returns no additional policies.

## Key controllers / hubs

| Class | File | Purpose |
|---|---|---|
| `DataController` | `Controllers/DataController*.cs` | REST API split across partial files (auth, users, tenants, departments, plugins, file storage, etc.) |
| `SetupController` | `Controllers/SetupController.cs` | First-run database setup endpoint |
| `AuthorizationController` | `Controllers/AuthorizationController.cs` | OAuth callback handling |
| `freeaiHub` | `Hubs/signalrHub.cs` | SignalR real-time update hub |

## Project references

| Reference | Role |
|---|---|
| `FreeAI.DataAccess` | Server-side data access and business logic |
| `FreeAI.Client` | Blazor WASM client assembly |
| `FreeAI.Plugins` | Plugin runtime |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.Azure.SignalR` | Azure SignalR Service integration |
| `AspNet.Security.OAuth.Apple` | Sign in with Apple |
| `Microsoft.AspNetCore.Authentication.*` | Google, Facebook, MicrosoftAccount, OpenIdConnect |

## Build info

| Field | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target framework | `net9.0` |
| Output type | Web executable |

Part of the ChatWithAI solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is the platform's web server (Blazor United — pages render on the server *and* as WebAssembly in the browser). It handles login/OAuth, exposes the REST API, runs the SignalR hub for live updates, and loads Roslyn plugins at startup. It's the FreeCRM scaffold renamed to `FreeAI`; the app-specific extension hooks (`AppModifyBuilderStart/End`, etc.) are present but **empty**, waiting for app logic.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| ASP.NET Core host + DI + auth wiring | Boots the app, registers services, OAuth | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI/Program.cs) |
| REST API (partial-class controllers) | What the browser UI calls | [Controllers/DataController.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI/Controllers/DataController.cs) |
| OAuth callbacks | Google / Microsoft / Apple / Facebook / OIDC | [Controllers/AuthorizationController.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI/Controllers/AuthorizationController.cs) |

**Why does this exist?**
A single host that gives any app multi-tenant auth + REST API + real-time updates + runtime plugins out of the box, so a new product starts at "platform done" rather than "platform from scratch."

**What does it accomplish that other tools don't?**
- **Blazor United** render mode (server-side speed for first paint, WASM for rich interactivity).
- **Pluggable OAuth** providers and an optional **Azure SignalR Service** fallback, switchable by config.
- **Runtime plugins** loaded from a `Plugins/` folder via Roslyn — extend without rebuilding.

**Terminology & "can I see it?"**
- **Blazor United** — a render mode mixing server-rendered and browser-run (WASM) C#.
- **SignalR hub** (`/freeaiHub`) — the endpoint that pushes live updates to browsers.
- **Extension hook** — a named empty method (e.g. `AppModifyBuilderStart`) where app-specific code is meant to go.

**The hard part, drawn** — one host, four responsibilities:

```
  Browser (Blazor United) ──REST──▶ DataController ──▶ DataAccess ──▶ EF Core (5 DBs)
        ▲                                                      │
        └──────────── freeaiHub (SignalR live updates) ◀───────┘
  startup ─▶ load Roslyn plugins from Plugins/   +   wire OAuth (Google/MS/Apple/Facebook/OIDC)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

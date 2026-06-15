# FreeLLM

ASP.NET Core host for the FreeLLM Blazor application.

Runs the Kestrel/IIS web server, wires authentication (local accounts, Google, Facebook, Microsoft, Apple, OpenID Connect), maps the SignalR hub at `/freellmHub` (local or Azure SignalR), hosts the three file-access API endpoints gated behind the `AppAdmin` policy, loads C# plugins from the `Plugins/` folder at startup via Roslyn, and serves the Blazor WebAssembly client.

## @page routes

Routes are defined in `FreeLLM.Client`. The server maps the Blazor component tree via `MapRazorComponents<App>().AddInteractiveWebAssemblyRenderMode()`.

## Key classes

| Class | File | Purpose |
|-------|------|---------|
| `Program` | `Program.cs` + `Program.App.cs` | Host builder; wires DI, auth policies, SignalR, plugin loader, EF connection |
| `DataController` | `Controllers/DataController.cs` | Base controller with auth helpers and policy constants |
| `DataController` (partial) | `Controllers/DataController.App.FreeLLM.cs` | `GetFiles`, `GetFileMetadata`, `GetFileContents` endpoints |
| `freellmHub` | `Hubs/signalrHub.cs` | SignalR hub for real-time client notifications |
| `ConfigurationHelper` | `Classes/ConfigurationHelper.cs` + `.App.cs` | Typed configuration loader; exposes auth providers, base path, analytics code |
| `CustomAuthenticationHandler` | `Classes/CustomAuthenticationHandler.cs` | Pluggable auth handler for custom login providers |

## Project references and notable packages

**Project references:** `FreeLLM.DataAccess`, `FreeLLM.Client`, `FreeLLM.Plugins`

| Package | Version | Use |
|---------|---------|-----|
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | 9.0.9 | Serves the WASM bundle |
| `Microsoft.Azure.SignalR` | 1.32.0 | Azure SignalR fallback |
| `AspNet.Security.OAuth.Apple` | 9.4.0 | Apple Sign-In |
| `Microsoft.AspNetCore.Authentication.Google/Facebook/MicrosoftAccount/OpenIdConnect` | 9.0.9 | OAuth providers |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target framework | net9.0 |
| Nullable | enabled |
| User secrets | `ffba3dfe-46f7-436d-8b09-e77396f7920b` |

Part of the **FreeLLM** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The web server. It does the usual platform jobs (auth, the SignalR hub at `/freellmHub`, Roslyn plugins, serving the WASM client) **plus** the three file-access endpoints — `GetFiles`, `GetFileMetadata`, `GetFileContents` — that power the curation UI. Those endpoints are gated behind the `AppAdmin` policy because reading local files is sensitive.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| ASP.NET Core host + DI + auth | Boots the app, wires providers | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM/Program.cs) |
| The three file endpoints (admin-gated) | Enumerate / count / read files | [Controllers/DataController.App.FreeLLM.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM/Controllers/DataController.App.FreeLLM.cs) |

**Why does this exist?**
To serve the browser app *and* expose the small, guarded file API that the curation workflow depends on — in one host.

**What does it accomplish that other tools don't?**
- The file API sits behind the **`AppAdmin` policy**, so only authorized users can have the server read from disk.
- Results are **cached for 5 minutes** server-side, so re-filtering a big folder doesn't re-read every file.

**Terminology & "can I see it?"**
- **Policy** — a named authorization rule (`AppAdmin`) endpoints can require.
- **`IMemoryCache`** — an in-process cache that holds file results briefly.

**The hard part, drawn** — the guarded file API behind the UI:

```
  Browser (curation UI) ──REST──▶ DataController.App.FreeLLM   [requires AppAdmin policy]
        GetFiles / GetFileMetadata / GetFileContents ──▶ disk   (results held 5 min in IMemoryCache)
  startup ─▶ auth + freellmHub (SignalR) + Roslyn plugins + serve the WASM client
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

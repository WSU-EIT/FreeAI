# FreeExamples (Server)

> ASP.NET Core host for the FreeExamples Blazor WebAssembly application — API controllers, authentication middleware, SignalR hub, plugin system, and background service.

**Target:** .NET 10 · **Type:** ASP.NET Core Web App (Blazor WASM hosted)

---

## What This Project Contains

| Area | Files | Purpose |
|------|-------|---------|
| **Controllers** | `DataController.*.cs` | Entity CRUD endpoints (Users, Departments, Tags, etc.) |
| **App Controllers** | `FreeExamples.App.API.*.cs` | Example-specific APIs (ApiKeyDemo, CodePlayground, GitBrowser, CommentThread, SampleData) |
| **Middleware** | `ApiKeyDemoMiddleware.cs` | SHA-256 hashed API key authentication |
| **Authentication** | `CustomAuthenticationHandler.cs` | Custom auth with LDAP, OAuth, local login support |
| **SignalR** | `signalrHub.cs` | Real-time communication hub |
| **Background** | `BackgroundProcessor.cs` | Configurable periodic task runner |
| **Services** | `GitBrowserService.cs`, `CodeSnippetService.cs`, `CommentService.cs` | App-specific business logic |
| **Plugins** | `PluginFiles/` | Example plugins (Example1-3, BackgroundProcess, LoginWithPrompts, UserUpdate) |
| **Startup** | `Program.cs`, `Program.App.cs` | Service registration and middleware pipeline |

---

## Key Dependencies

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | Blazor WASM hosting |
| `Microsoft.Azure.SignalR` | Azure SignalR Service support |
| `AspNet.Security.OAuth.Apple` | Apple OAuth provider |
| `Microsoft.AspNetCore.Authentication.*` | Google, Facebook, Microsoft, OpenID Connect |
| `Serilog.Extensions.Logging.File` | File-based logging |

---

Part of the FreeTools solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The web host for the FreeExamples app. Beyond the standard FreeCRM host duties, it adds the example-specific APIs: an **API-key demo** (SHA-256-hashed key middleware), a **code playground**, a **git browser**, and comment threads — plus the usual SignalR hub, background processor, and plugins.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| API-key middleware | SHA-256 Bearer-token auth for the demo API | [Middleware/ApiKeyDemoMiddleware.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeExamples/FreeExamples/Middleware/ApiKeyDemoMiddleware.cs) |
| Example APIs | ApiKeyDemo, CodePlayground, GitBrowser | [FreeExamples.App.API.*.cs](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeExamples/FreeExamples) |
| Host wiring | DI, auth, SignalR, plugins | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeExamples/FreeExamples/Program.cs) |

**Why does this exist?** To host the example app *and* demonstrate the API-key pattern end-to-end (the middleware here is what the `FreeExamples.NuGetClient` authenticates against).

**What does it beat?** It shows **a complete API-key flow** — generate a key in the UI, protect endpoints with hashed-key middleware, consume them from a typed NuGet client — in one runnable app.

**Terminology:** **Middleware** — code that runs on every request before the controller (here, the API-key check).

**The hard part, drawn:**
```
  external caller (NuGetClient) ─Bearer key─▶ ApiKeyDemoMiddleware (SHA-256 verify) ─▶ App.API endpoint
  browser (Blazor client) ─REST─▶ DataController ─▶ DataAccess ─▶ DB    ·    SignalR live updates
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

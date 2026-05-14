# ChatWithAI -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure

| Project | SDK | Framework | Role |
|---------|-----|-----------|------|
| `FreeAI` | `Microsoft.NET.Sdk.Web` | net10.0 | ASP.NET Core host; auth, REST API, SignalR, plugin loader |
| `FreeAI.Client` | `Microsoft.NET.Sdk.BlazorWebAssembly` | net10.0 | WASM UI; all admin and user-facing pages |
| `FreeAI.DataAccess` | `Microsoft.NET.Sdk` | net10.0 | EF Core repositories; auth, files, PDF, Graph API, `ChatWithAi()` |
| `FreeAI.DataObjects` | `Microsoft.NET.Sdk` | net10.0 | Shared DTOs and startup-state flags |
| `FreeAI.EFModels` | `Microsoft.NET.Sdk` | net10.0 | `EFDataModel` DbContext; all core entity tables |
| `FreeAI.Plugins` | `Microsoft.NET.Sdk` | net10.0 | Roslyn runtime; loads/compiles `.cs`/`.plugin` files |
| `FreeAI.LocalTests` | Console Exe | net10.0 | Azure OpenAI token-budgeted chat demo |

## Data flow -- Azure OpenAI chat

```
FreeAI.LocalTests
  -> loads AzureOpenAiSettings from appsettings.json + user secrets
  -> GptEncoding (SharpToken) counts tokens across conversation history
  -> trims messages to fit promptBudget = MaxContextTokens - ReplyMaxTokens
  -> HttpClient POST /openai/deployments/{deployment}/chat/completions
  -> prints JSON response to stdout
```

## Data flow -- web requests

```
Browser (WASM)  --HTTP/SignalR-->  FreeAI (ASP.NET Core)
                                     |-> DataAccess (EF Core)
                                     |     |-> Database (SQL/InMemory)
                                     |     |-> Microsoft Graph / LDAP
                                     |-> Plugins (Roslyn)
                                     |-> SignalR hub (/freeaiHub)
```

## Key extension points

| File | Purpose |
|------|---------|
| `Program.App.cs` | Add server-side DI registrations or middleware |
| `DataAccess.App.cs` | Add custom data-access methods |
| `DataController.App.cs` | Add custom REST endpoints |
| `Index.App.razor` | Add content to the home page |
| `PluginFiles/` | Drop `.cs`/`.plugin` files for runtime-loaded plugins |

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*
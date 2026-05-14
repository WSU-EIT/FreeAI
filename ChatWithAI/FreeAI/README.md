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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

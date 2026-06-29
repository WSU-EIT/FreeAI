# FreeGLBA.NugetClient

The publishable .NET client library (`FreeGLBA.Client` on NuGet) for integrating any application with a FreeGLBA server. Provides strongly-typed access to all GLBA event logging and query endpoints with automatic retry and dependency injection support.

## What It Does

Wraps the FreeGLBA REST API in a typed `GlbaClient` class. Applications that access protected financial information call this library to log access events to the central FreeGLBA server instead of making raw HTTP requests. The library handles:

- Authentication via `Authorization: Bearer {api-key}` headers
- Automatic exponential-backoff retry (configurable, default 3 attempts) for transient 5xx errors
- Typed exception hierarchy (`GlbaAuthenticationException`, `GlbaValidationException`, `GlbaDuplicateException`)
- Dependency injection via `AddGlbaClient(options => ...)` extension method
- Optional user JWT bearer token for internal dashboard endpoints (`SetBearerToken` / `ClearBearerToken`)

## Key Public Classes/Methods

| Class / Method | Description |
|----------------|-------------|
| `IGlbaClient` | Interface for DI / mocking — all public methods are declared here |
| `GlbaClient.LogAccessAsync` | Posts a single access event; throws typed exceptions on failure |
| `GlbaClient.LogAccessBatchAsync` | Posts up to 1,000 events in one request |
| `GlbaClient.TryLogAccessAsync` | Fire-and-forget logging — returns `false` on error instead of throwing |
| `GlbaClient.LogViewAsync` | Convenience method for single-subject view events |
| `GlbaClient.LogExportAsync` | Convenience method for single-subject export events |
| `GlbaClient.LogBulkViewAsync` | Logs a view event covering multiple subject IDs (`SubjectIds` array) |
| `GlbaClient.LogBulkExportAsync` | Logs a bulk export with optional `AgreementText` capture |
| `GlbaClient.GetStatsAsync` | Retrieves dashboard statistics (requires user bearer token) |
| `ServiceCollectionExtensions.AddGlbaClient` | Registers `IGlbaClient` in DI with `HttpClientFactory` |
| `GlbaClientOptions` | Configuration: `Endpoint`, `ApiKey`, `Timeout`, `RetryCount`, `ThrowOnError` |

## NuGet Installation

```bash
dotnet add package FreeGLBA.Client
```

## Quick Start

```csharp
// Direct instantiation
var client = new GlbaClient("https://your-glba-server.com", "YOUR_API_KEY");
await client.LogViewAsync("jsmith", "STU-12345");

// Dependency injection (Program.cs)
builder.Services.AddGlbaClient(options => {
    options.Endpoint = "https://your-glba-server.com";
    options.ApiKey = builder.Configuration["GlbaApiKey"];
});
```

## Project References

None — this project has no local project references. It is a standalone class library.

## Notable NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.Extensions.Http` | `IHttpClientFactory` integration for DI scenarios |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `IServiceCollection` extension method |
| `Microsoft.Extensions.Options` | Strongly-typed `GlbaClientOptions` configuration |
| `Microsoft.SourceLink.GitHub` | Source-link debugging from NuGet packages |

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target Framework | `net10.0` |
| Output Type | Class Library (NuGet package `FreeGLBA.Client`) |
| NuGet Package ID | `FreeGLBA.Client` |
| Current Version | `1.1.0` |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is the **published `FreeGLBA.Client` NuGet package** — the drop-in library external apps add to report access events. It wraps the FreeGLBA REST API in a typed `GlbaClient`: set an endpoint + API key, then call `LogAccessAsync` (or convenience methods like `LogViewAsync`, `LogExportAsync`, `LogBulkViewAsync`). It handles the `Bearer` header, automatic exponential-backoff retry on transient 5xx errors, typed exceptions, and DI registration.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Typed HTTP client | All the log/query methods | [GlbaClient.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.NugetClient/GlbaClient.cs) |
| DI registration | `AddGlbaClient(options => …)` with `HttpClientFactory` | [Extensions/ServiceCollectionExtensions.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.NugetClient/Extensions/ServiceCollectionExtensions.cs) |
| SourceLink | Step into package source while debugging | [GlbaClient.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.NugetClient/GlbaClient.cs) |

**Why does this exist?**
So an integrating team doesn't hand-roll HTTP calls, retries, and error handling. `dotnet add package FreeGLBA.Client` and one method call makes any .NET app GLBA-compliant.

**What does it accomplish that other tools don't?**
- **Typed exceptions** (`GlbaAuthenticationException`, `GlbaValidationException`, `GlbaDuplicateException`) so callers handle failures precisely.
- **Fire-and-forget option** (`TryLogAccessAsync` returns `false` instead of throwing) for paths that must never break on a logging hiccup.
- **Batch + bulk** helpers and built-in **retry** — production-grade, not a sample.

**Terminology & "can I see it?"**
- **NuGet package** — a shareable, versioned .NET library (`FreeGLBA.Client`, currently 1.1.0).
- **Exponential backoff** — retry after 1s, 2s, 4s… so a brief server blip self-heals.

**The hard part, drawn** — one call, with retry and typed failures:

```
  your app ─▶ GlbaClient.LogAccessAsync(event)
                 │ add Bearer {api-key}  ·  POST /api/glba/events
                 ▼
            transient 5xx? ──▶ retry (backoff, default 3×) ──┐
                 │ success                                    │ still failing
                 ▼                                            ▼
            returns EventId                         throws typed exception
                                                    (or TryLog… returns false)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeGLBA](../README.md) solution.

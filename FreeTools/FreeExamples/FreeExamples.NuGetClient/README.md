# FreeExamples.NuGetClient

Strongly-typed .NET client library for the FreeExamples API, protected by API key middleware. Published to NuGet as the `FreeExamples.Client` package.

---

## What It Does

`FreeExamples.NuGetClient` provides a turnkey HTTP client for consumers of the FreeExamples external API. It handles Bearer token authentication (SHA-256 hashed API keys), automatic retry with exponential backoff, dependency injection, and a typed exception hierarchy. The pattern is derived from `FreeGLBA.Client`.

---

## Installation

```bash
dotnet add package FreeExamples.Client
```

---

## Quick Start

```csharp
using FreeExamples.Client;

// Direct construction
using var client = new FreeExamplesClient("https://your-server.com", "your-api-key");

// Health check
var pong = await client.PingAsync();
Console.WriteLine(pong.Message); // "pong"

// Post data
var response = await client.PostDataAsync(new ApiTestRequest { Message = "Hello!" });
Console.WriteLine(response.AuthenticatedAs); // API key display name
```

---

## Dependency Injection

```csharp
// Program.cs
builder.Services.AddFreeExamplesClient(options =>
{
    options.Endpoint = "https://your-server.com";
    options.ApiKey   = builder.Configuration["FreeExamples:ApiKey"]!;
});

// Consuming service
public class MyService(IFreeExamplesClient client)
{
    public async Task DoWork()
        => await client.PostDataAsync(new ApiTestRequest { Message = "From DI" });
}
```

---

## Key Public Classes/Methods

| Member | Description |
|--------|-------------|
| `FreeExamplesClient` | Main client class — direct construction with endpoint + API key |
| `IFreeExamplesClient` | DI interface |
| `PingAsync()` | Health-check the protected endpoint; returns `PongResponse` |
| `PostDataAsync(request)` | Send data to the API-key-protected endpoint |
| `TryPostDataAsync(request)` | Fire-and-forget overload — returns `bool`, never throws |
| `AddFreeExamplesClient(options)` | `IServiceCollection` extension for DI registration |
| `FreeExamplesAuthenticationException` | Thrown on 401 — invalid or revoked API key |
| `FreeExamplesException` | Base exception for all client errors |

---

## Error Handling

```csharp
try {
    await client.PostDataAsync(new ApiTestRequest { Message = "test" });
}
catch (FreeExamplesAuthenticationException) {
    // 401 — key is invalid or revoked
}
catch (FreeExamplesValidationException) {
    // 400 — bad request
}
catch (FreeExamplesException ex) {
    Console.WriteLine($"Status {ex.StatusCode}: {ex.Message}");
}
```

---

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `Endpoint` | required | Base URL of the FreeExamples server |
| `ApiKey` | required | API key from the API Key Demo page |
| `Timeout` | 30 s | HTTP request timeout |
| `RetryCount` | 3 | Retry attempts for transient failures |
| `ThrowOnError` | `true` | Throw exceptions vs. return error responses |

---

## Project References and NuGet Packages

| Type | Reference |
|------|-----------|
| NuGet | `Microsoft.Extensions.DependencyInjection.Abstractions` 9.0.0 |
| NuGet | `Microsoft.Extensions.Http` 9.0.0 |
| NuGet | `Microsoft.Extensions.Options` 9.0.0 |
| NuGet | `Microsoft.SourceLink.GitHub` 8.0.0 (SourceLink) |

---

## Build Details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Output type | Class Library / NuGet Package |
| Target framework | `net10.0` |
| Package ID | `FreeExamples.Client` |
| SourceLink | GitHub |
| Symbols | `.snupkg` |

---

Part of the FreeTools solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The published `FreeExamples.Client` NuGet package — a typed HTTP client for the FreeExamples API-key-protected endpoints. Set an endpoint + API key, then call `PingAsync`/`PostDataAsync`. It handles the Bearer header, retry with backoff, DI registration, and typed exceptions. (Same pattern as `FreeGLBA.Client`.)

**What tech & where?** [FreeExamplesClient.cs / the project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeExamples/FreeExamples.NuGetClient) (`Microsoft.Extensions.Http` + SourceLink).

**Why does this exist?** So a consumer of the FreeExamples API doesn't hand-roll HTTP, auth, and retries — and as a **reference for the API-key-client pattern** other apps reuse.

**What does it beat?** Typed exceptions (`FreeExamplesAuthenticationException` on 401), a fire-and-forget `TryPostDataAsync` that never throws, and DI via `AddFreeExamplesClient` — production-grade, not a snippet.

**Terminology:** **API key** — the secret a caller presents; **SourceLink** — lets you step into package source while debugging.

**The hard part, drawn:**
```
  your app ─▶ FreeExamplesClient.PostDataAsync(req)  (Bearer {api-key})
        transient failure ─▶ retry (backoff, default 3×)
        401 ─▶ FreeExamplesAuthenticationException   ·   success ─▶ typed response
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

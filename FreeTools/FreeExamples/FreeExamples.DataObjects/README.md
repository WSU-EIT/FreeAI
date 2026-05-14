# FreeExamples.DataObjects

Shared DTO and model library used by both the FreeExamples server and the Blazor WebAssembly client.

---

## What It Does

`FreeExamples.DataObjects` holds the data-transfer objects (DTOs), request/response models, and shared contracts that cross the server/client boundary. Because Blazor WASM runs in the browser, any type shared between the ASP.NET Core host and the client must live in a project that both can reference — this is that project.

It also provides an in-memory cache layer via `System.Runtime.Caching`.

---

## Key Public Classes/Methods

| Class | Description |
|-------|-------------|
| `ApiTestRequest` | Request model for the API key demo endpoint |
| `ApiTestResponse` | Response model including `AuthenticatedAs` |
| `PongResponse` | Health-check response from `PingAsync` |
| Plugin interfaces | Shared contracts between server and `FreeExamples.Plugins` |

---

## Project References and NuGet Packages

| Type | Reference |
|------|-----------|
| Project | `FreeExamples.Plugins` |
| NuGet | `System.Runtime.Caching` 10.0.3 |

---

## Build Details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Output type | Class Library |
| Target framework | `net10.0` |

---

Part of the FreeTools solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A pure "data shapes" library shared by the server and the browser client. It holds the DTOs that cross that boundary — including the API-demo shapes (`ApiTestRequest`, `ApiTestResponse`, `PongResponse`) that the NuGet client and server both use — plus an in-memory cache helper.

**What tech & where?** [the DataObjects project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeExamples/FreeExamples.DataObjects) (`System.Runtime.Caching`).

**Why does this exist?** So a contract-breaking change fails at build time, not at runtime — the server and the browser (and the NuGet client) all compile against the same shapes.

**What does it beat?** One shared definition of the API contract used by C# on the server *and* in the browser *and* in the published client library.

**Terminology:** **DTO** — a plain data shape with no behavior.

**The hard part, drawn:**
```
  Server ─┐                                  ┌─ Browser client
          ├─ DataObjects (ApiTestRequest …) ──┤
  NuGet client ─────────────────────────────┘   all three bind the SAME shapes
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

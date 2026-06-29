# FreeExamples

> Open-source Blazor WebAssembly examples application built with .NET 10 — demonstrates real-world patterns for dashboards, file management, authentication, plugins, API clients, and more.

---

## What Is This?

**FreeExamples** is a full-featured Blazor WebAssembly application that serves as both a working reference implementation and a collection of interactive examples. It's based on the [FreeCRM](https://github.com/WSU-EIT/FreeCRM) architecture and demonstrates how to build production-ready Blazor apps with:

- Multi-tenant architecture with tenant-scoped data
- Custom authentication (local, LDAP, OAuth providers)
- Plugin system with runtime compilation
- Background processing service
- File storage and management
- API key middleware and NuGet client pattern
- SignalR real-time features
- Code playground with in-browser C#/Razor compilation

---

## Project Structure

```
FreeExamples/
├── FreeExamples/                  ← Server (ASP.NET Core host)
├── FreeExamples.Client/           ← Blazor WebAssembly client
├── FreeExamples.DataAccess/       ← Data access layer (EF Core, Graph, LDAP)
├── FreeExamples.DataObjects/      ← Shared DTOs and models
├── FreeExamples.EFModels/         ← EF Core DbContext and entity models
├── FreeExamples.Plugins/          ← Plugin runtime compiler
├── FreeExamples.NuGetClient/      ← NuGet-publishable API client library
├── FreeExamples.NuGetClientPublisher/ ← Tool to pack & push the NuGet package
├── FreeExamples.TestClient/       ← Console app to test the NuGet client
├── FreeExamples.DatabaseMigration/ ← Database copy/migration tool
└── Docs/                          ← Documentation
```

| Project | Type | Purpose |
|---------|------|---------|
| **FreeExamples** | ASP.NET Core Server | Host, API controllers, middleware, SignalR hub, background service |
| **FreeExamples.Client** | Blazor WASM | UI pages, components, code playground, dynamic Blazor support |
| **FreeExamples.DataAccess** | Class Library | EF Core queries, Microsoft Graph, LDAP, PDF generation (QuestPDF) |
| **FreeExamples.DataObjects** | Class Library | Shared DTOs, request/response models, caching |
| **FreeExamples.EFModels** | Class Library | EF Core `EFDataModel` DbContext, entity classes, multi-provider support |
| **FreeExamples.Plugins** | Class Library | Runtime C# compilation for plugin loading |
| **FreeExamples.NuGetClient** | NuGet Package | Strongly-typed API client with retry, DI support, typed exceptions |
| **FreeExamples.NuGetClientPublisher** | Console Tool | Interactive pack & push tool for the NuGet package |
| **FreeExamples.TestClient** | Console Tool | Exercises the NuGet client against API key-protected endpoints |
| **FreeExamples.DatabaseMigration** | Console Tool | Same-schema database copy with bulk insert, EF schema management |

---

## Quick Start

```bash
# 1. Clone and restore
git clone https://github.com/WSU-EIT/FreeTools.git
cd FreeTools/FreeExamples
dotnet restore

# 2. Run the server (launches Blazor WASM client)
dotnet run --project FreeExamples
```

The app starts at `https://localhost:7271` with an in-memory database by default.

---

## Key Features

### Plugins

FreeExamples supports a plugin architecture with runtime C# compilation. Plugin types include `Auth`, `BackgroundProcess`, `Example`, and `UserUpdate`. Plugins can be `.cs` source files or `.plugin` files with external assembly references.

See the `PluginFiles/` folder for examples.

### Background Service

A configurable background service runs periodic tasks (controlled by `BackgroundService` section in `appsettings.json`). Supports:
- Configurable interval (default: 60 seconds)
- Start-on-load or delayed start
- Plugin-based task registration
- Load balancing filter for multi-instance deployments
- File-based logging

For IIS hosting, set Application Pool Start Mode to `AlwaysRunning` and enable Preload.

### API Key Middleware

Demonstrates a SHA-256 hashed API key pattern with:
- Bearer token authentication
- Key generation UI (API Key Demo page)
- Middleware-protected endpoints
- NuGet client library for consumers

### Database Providers

The EFModels project supports multiple providers:
- SQL Server
- SQLite
- MySQL
- PostgreSQL
- In-Memory (default for development)

---

## Customization

### Renaming the Project

Use the [ForkCRM tool](../FreeTools/FreeTools.ForkCRM/) to clone, remove modules, and rename:

```bash
cd FreeTools/FreeTools.ForkCRM
dotnet run -- --name MyProject --modules remove:all --output "C:\repos\MyProject"
```

### Removing Optional Modules

Optional modules (Tags, etc.) can be stripped using the FreeCRM utilities or the ForkCRM tool.

### Adding Custom Data Access

Add application-specific methods to `DataAccess.App.cs` and `IDataAccess` partial interface.

### Adding Custom Language Tags

Override built-in language tags or add custom ones in the `AppLanguage` dictionary in `DataAccess.App.cs`.

---

*Part of the [FreeTools](https://github.com/WSU-EIT/FreeTools) suite — developed by [Enrollment Information Technology](https://em.wsu.edu/eit/meet-our-staff/) at Washington State University.*

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** FreeExamples is the **working sample app** of the FreeTools suite — a full FreeCRM-based Blazor app that the analysis tools target, and that demonstrates real patterns: multi-tenant data, plugins with runtime compilation, a background service, an API-key-protected external API with a matching NuGet client, SignalR, and an in-browser C#/Razor code playground.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| FreeCRM scaffold (Blazor WASM, .NET 10) | The host app | [the FreeExamples solution](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeExamples) |
| API-key middleware + NuGet client | A protected external API + its client library | [FreeExamples.NuGetClient/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeExamples/FreeExamples.NuGetClient) |
| Database migration tool | Same-schema DB copy | [FreeExamples.DatabaseMigration/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeExamples/FreeExamples.DatabaseMigration) |

**Why does this exist?** To be both the **default analysis target** for the FreeTools pipeline *and* a reference implementation of FreeCRM patterns developers can copy from.

**What does it beat?** It bundles several **production patterns in one runnable app** — API-key auth + client library, a robust DB-migration tool, a code playground — rather than scattering them across separate samples.

**Terminology & "can I see it?"**
- **Reference implementation** — a complete, correct example you learn from.
- *See it:* `dotnet run --project FreeExamples` → `https://localhost:7271`.

**The hard part, drawn** — one sample app, many patterns:

```
  FreeExamples (FreeCRM scaffold)
        ├─ multi-tenant · plugins (runtime compile) · background service · SignalR · code playground
        ├─ API-key middleware ──▶ FreeExamples.NuGetClient (the consumer library)
        └─ DatabaseMigration tool   ← the whole app is the FreeTools pipeline's default --target
```
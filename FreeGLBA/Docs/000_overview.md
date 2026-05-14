# FreeGLBA -- Overview

> **Category:** Overview
> **Purpose:** What this project is, why it exists, and how to get started.

---

## What it is

FreeGLBA is a GLBA (Gramm-Leach-Bliley Act) compliance data-access tracking system built on the FreeCRM framework with ASP.NET Core and Blazor WebAssembly (.NET 10). It provides a centralized platform for logging, auditing, and reporting who accessed protected financial data -- when, by whom, and for what purpose. It also ships a NuGet client library (`FreeGLBA.NugetClient`) so any other application can log GLBA access events via a simple REST call.

## Why it exists

Educational institutions that handle student financial aid data are required by GLBA to track and audit access to protected financial information. FreeGLBA gives WSU-EIT a free, open-source tool to satisfy that obligation and share it with the wider higher-education community.

## Who it is for

- Enrollment systems that touch student financial records (aid, billing, accounts)
- Compliance and audit teams needing real-time dashboards and exportable reports
- Developers who need a simple client library to emit GLBA access events from any .NET application

## Quick start

```bash
cd FreeGLBA/FreeGLBA
dotnet run
```

Navigate to `http://localhost:5001`.

### Client library integration

```bash
dotnet add package FreeGLBA.Client
```

```csharp
var client = new GlbaClient("https://your-server.com", "your-api-key");
await client.LogAccessAsync(new GlbaEventRequest {
    UserId    = "jsmith",
    SubjectId = "S12345678",
    AccessType = "View",
    Purpose   = "Enrollment verification"
});
```

## Related projects

- [FreeCRM](https://github.com/WSU-EIT/FreeCRM) -- source framework
- [FreeManager](../FreeManager/README.md) -- code-generation platform (FreeGLBA was generated with `FreeAudit` template)

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*
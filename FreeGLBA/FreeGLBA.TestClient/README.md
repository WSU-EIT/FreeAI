# FreeGLBA.TestClient

Test client application for validating the FreeGLBA.NugetClient library using project references.

Developed by **Enrollment Information Technology** at **Washington State University**.

## Purpose

This project is a console application used for:
- **Integration Testing** - Test client library against local server
- **Development** - Debug client library with project references
- **Examples** - Demonstrate client library usage patterns

This project references the NugetClient project directly (not the NuGet package), making it ideal for development and debugging scenarios.

## Technology Stack

- **.NET 10** - Console Application
- **Microsoft.Extensions.Configuration** - Configuration management
- **User Secrets** - Secure credential storage

## Dependencies

| Package | Purpose |
|---------|---------|
| Microsoft.Extensions.Configuration | Configuration system |
| Microsoft.Extensions.Configuration.Json | JSON config files |
| Microsoft.Extensions.Configuration.UserSecrets | Secure secrets |

### Project References
- **FreeGLBA.NugetClient** - Client library (project reference)

## Configuration

Create user secrets for the API key:

```bash
cd FreeGLBA.TestClient
dotnet user-secrets set \"GlbaApiKey\" \"your-api-key-here\"
```

Or use appsettings.json:

```json
{
  \"GlbaEndpoint\": \"https://localhost:5001\",
  \"GlbaApiKey\": \"your-api-key\"
}
```

## Running

```bash
cd FreeGLBA.TestClient
dotnet run
```

## vs FreeGLBA.TestClientWithNugetPackage

| Feature | TestClient | TestClientWithNugetPackage |
|---------|------------|----------------------------|
| Reference | Project reference | NuGet package |
| Use Case | Development/debugging | Package validation |
| Debugging | Full source debugging | Package symbols only |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A small console app that exercises the `GlbaClient` library against a local FreeGLBA server — referencing the client **as a project** (not the NuGet package), so you get full source-level debugging while developing the library.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Console test harness | Calls `GlbaClient` methods end-to-end | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.TestClient/Program.cs) |
| Project reference to the client | Step into client source while debugging | [GlbaClient.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.NugetClient/GlbaClient.cs) |

**Why does this exist?**
To develop and debug the client library quickly — set a breakpoint anywhere in `GlbaClient` and watch a real request flow to a local server.

**What does it accomplish that other tools don't?**
- **Full-source debugging** of the client (its sibling, `TestClientWithNugetPackage`, instead validates the *published* package).

**Terminology & "can I see it?"**
- **Project reference** — depending on the source project directly (vs. a built NuGet package).

**The hard part, drawn** — the development feedback loop:

```
  FreeGLBA.TestClient ──project ref──▶ GlbaClient (source) ──▶ local FreeGLBA server
        breakpoints work end-to-end → fast develop/debug of the library
```

## About

Part of the [FreeGLBA](../README.md) project.

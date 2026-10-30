# FreeGLBA.TestClientWithNugetPackage

Test client application for validating the published FreeGLBA.Client NuGet package from nuget.org.

Developed by **Enrollment Information Technology** at **Washington State University**.

## Purpose

This project is a console application used for:
- **Package Validation** - Test the published NuGet package works correctly
- **Version Testing** - Verify new package versions before release announcements
- **Documentation** - Provide real-world usage examples

This project references the FreeGLBA.Client NuGet package (not project reference), simulating how external consumers use the library.

## Technology Stack

- **.NET 10** - Console Application
- **FreeGLBA.Client** - Published NuGet package
- **Microsoft.Extensions.Configuration** - Configuration management

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| FreeGLBA.Client | 1.0.5 | GLBA client library (from nuget.org) |
| Microsoft.Extensions.Configuration | 9.0.0 | Configuration system |
| Microsoft.Extensions.Configuration.Json | 9.0.0 | JSON config files |
| Microsoft.Extensions.Configuration.UserSecrets | 9.0.0 | Secure secrets |

## Configuration

Create user secrets for the API key:

```bash
cd FreeGLBA.TestClientWithNugetPackage
dotnet user-secrets init
dotnet user-secrets set \"GlbaApiKey\" \"your-api-key-here\"
```

Or use appsettings.json:

```json
{
  \"GlbaEndpoint\": \"https://your-server.com\",
  \"GlbaApiKey\": \"your-api-key\"
}
```

## Running

```bash
cd FreeGLBA.TestClientWithNugetPackage
dotnet run
```

## Updating the Package Version

To test a newer version of the NuGet package:

```bash
dotnet add package FreeGLBA.Client --version X.Y.Z
```

## vs FreeGLBA.TestClient

| Feature | TestClient | TestClientWithNugetPackage |
|---------|------------|----------------------------|
| Reference | Project reference | NuGet package |
| Use Case | Development/debugging | Package validation |
| Debugging | Full source debugging | Package symbols only |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A console app that exercises the **published** `FreeGLBA.Client` NuGet package — referencing it the same way an outside team would, so it validates that the released package actually works before announcing a new version.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Console validation harness | Runs against the published package | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.TestClientWithNugetPackage/Program.cs) |
| NuGet package reference | Tests exactly what consumers install | `FreeGLBA.Client` (from nuget.org) |

**Why does this exist?**
To catch packaging problems (missing files, broken dependencies, wrong version) that a project reference would hide — a final check before release.

**What does it accomplish that other tools don't?**
- Validates the package **as an external consumer sees it** (symbols only, no source) — the counterpart to `TestClient`, which uses a project reference for development.

**Terminology & "can I see it?"**
- **Package reference** — depending on a built, versioned NuGet package (vs. the source project).
- **Package validation** — confirming the shipped artifact works, not just the source.

**The hard part, drawn** — pre-release confidence:

```
  FreeGLBA.TestClientWithNugetPackage ──NuGet pkg──▶ FreeGLBA.Client (published) ──▶ server
        proves the SHIPPED package works before a release is announced
```

## About

Part of the [FreeGLBA](../README.md) project.

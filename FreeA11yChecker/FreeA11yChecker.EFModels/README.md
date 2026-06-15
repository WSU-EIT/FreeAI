# FreeA11yChecker.EFModels

Entity Framework Core model library for FreeA11yChecker. Contains the `EFDataModel` DbContext, all entity classes, and the `EFDataModelDesignTimeFactory` for `dotnet ef` tooling. Supports four database providers: SQL Server, SQLite, PostgreSQL, and MySQL.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | Class library |

## What Is in Here

### Core entities

| Entity class | Table |
|---|---|
| `User` | Users and their profile data, roles, and preferences |
| `Tenant` | Multi-tenant tenant records |
| `UserGroup` / `UserInGroup` | Group membership |
| `Department` / `DepartmentGroup` | Department hierarchy |
| `Setting` | Key-value settings store (typed, per-tenant) |
| `Tag` / `TagItem` | Tagging system |
| `UDFLabel` | User-defined field labels |
| `FileStorage` | Binary file storage records |
| `PluginCache` | Persisted plugin compilation cache |

### Accessibility-specific entities

| Entity class | Table |
|---|---|
| `Site` | Audit target sites |
| `SitePage` | Individual pages configured for scanning |
| `SiteCredential` | Per-site authentication credentials |
| `ScanRun` | One execution of the scanner against a site |
| `PageScanResult` | Per-page scan results and metrics |
| `A11yViolation` | Individual accessibility violations |
| `ScanScreenshot` | Screenshot binary data per page |
| `ScanImage` | Image inventory per page |
| `ScanArtifact` | Arbitrary scan artifacts (overlays, reports) |
| `ScanCertificate` | SSL certificate audit result per page |
| `ScanRankedRule` | Consensus-ranked rule per page |
| `ViolationSuppression` | Per-tenant rule suppressions |
| `ManualCheckResult` | Manually completed WCAG criterion results |

### DbContext and provider selection

`EFDataModel` (`EFModels/EFDataModel.cs`) is a partial class. `EFModelOverrides.cs` adds `ConfigureConventions` to apply a `GuidToStringConverter` on all `Guid` properties when running against MySQL, PostgreSQL, or SQLite (those providers store GUIDs as strings). SQL Server and InMemory use native GUID storage.

`EFDataModelDesignTimeFactory` enables `dotnet ef migrations add` and `dotnet ef database update` from the command line by constructing a design-time `DbContext` with a configurable connection string.

## Key Classes / Methods

| Class | Purpose |
|---|---|
| `EFDataModel` | Main `DbContext`; exposes all `DbSet<T>` properties |
| `EFModelOverrides` | `ConfigureConventions` — applies GUID-to-string converter for non-SQL-Server providers |
| `EFDataModelDesignTimeFactory` | `IDesignTimeDbContextFactory<EFDataModel>` for EF Core tooling |

## Project References

None — this library has no project references.

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Sqlite` | SQLite provider |
| `Microsoft.EntityFrameworkCore.InMemory` | In-memory provider (testing / BlazorApp1) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider |
| `MySql.EntityFrameworkCore` | MySQL provider |
| `Microsoft.EntityFrameworkCore.Tools` | `dotnet ef` CLI tooling |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This library *defines the database*: the `EFDataModel` "DbContext" and every table (sites, pages, scan runs, violations, screenshots, users, tenants). One model targets **five** database engines; a small converter stores GUIDs as text on MySQL/PostgreSQL/SQLite, while SQL Server and InMemory keep native GUIDs.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| EF Core DbContext | The schema — all tables as `DbSet<T>` | [EFModels/EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.EFModels/EFModels/EFDataModel.cs) |
| Provider-agnostic GUID handling | GUID→string on MySQL/PG/SQLite | [EFModelOverrides.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.EFModels/EFModelOverrides.cs) |
| Design-time factory | Makes `dotnet ef migrations` work | [EFDataModelDesignTimeFactory.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.EFModels/EFModels/EFDataModelDesignTimeFactory.cs) |
| Accessibility entities | The scan tables | [App.ScanRun.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.EFModels/EFModels/FreeA11yChecker.App.ScanRun.cs) · [App.A11yViolation.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.EFModels/EFModels/FreeA11yChecker.App.A11yViolation.cs) |

**Why does this exist?**
A single schema definition that works across whatever database a customer already runs — no separate model per engine.

**What does it accomplish that other tools don't?**
- **Five engines from one model** (SQL Server, PostgreSQL, MySQL, SQLite, InMemory), with the GUID-storage difference handled in *one* place.
- A **design-time factory** so migrations can be generated from the command line without booting the whole app.

**Terminology & "can I see it?"**
- **DbContext** — the C# object that represents your database connection and tables.
- **Entity** — a C# class that maps to one database table.
- **Migration** — the generated script that creates/updates tables for a given engine.

**The hard part, drawn** — one model, five databases:

```
   EFDataModel (DbContext)
        │  DbSet<T> ▶ Site · SitePage · ScanRun · A11yViolation · ScanScreenshot · User · Tenant …
        │  EFModelOverrides.ConfigureConventions()
        ▼
   ┌─ GUID → string ──▶  MySQL · PostgreSQL · SQLite
   └─ native GUID  ───▶  SQL Server · InMemory
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

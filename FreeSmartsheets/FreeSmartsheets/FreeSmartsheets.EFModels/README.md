# FreeSmartsheets.EFModels

Entity Framework Core database model project for FreeSmartsheets. Defines the `EFDataModel` DbContext, all entity classes, and code-first migrations for all supported database engines.

## What It Does

- Declares the `EFDataModel` DbContext with DbSets for all platform tables
- Defines entity classes for all core tables in `EFModels/`
- Contains code-first migrations in `Migrations/` for the configured database provider
- Applies EF model overrides in `EFModelOverrides.cs` (column type customizations, index definitions, etc.)
- Supports SQL Server, PostgreSQL, SQLite, MySQL, and InMemory database providers

## Database Tables

| Entity | Description |
|--------|-------------|
| `User` | Application users with authentication and profile data |
| `UserGroup` / `UserInGroup` | User group definitions and membership |
| `Tenant` | Multi-tenant organization records and per-tenant settings |
| `Department` / `DepartmentGroup` | Organizational department hierarchy |
| `Tag` / `TagItem` | Tagging system for any record type |
| `Setting` | Key-value application and tenant settings store |
| `FileStorage` | Binary file storage records |
| `EmailTemplate` | Customizable email template definitions |
| `PluginCache` | Cached compiled plugin assemblies |
| `UDFLabel` | User-defined field label configuration |

## Key Public Classes/Methods

| Class / Method | Description |
|----------------|-------------|
| `EFDataModel` | Main DbContext; takes provider name and connection string in constructor |
| `EFModelOverrides` | Fluent API overrides applied in `OnModelCreating` |

## Project References

None — this project has no local project references.

## Notable NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Sqlite` | SQLite provider |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider |
| `MySql.EntityFrameworkCore` | MySQL provider |
| `Microsoft.EntityFrameworkCore.InMemory` | InMemory provider for testing |
| `Microsoft.EntityFrameworkCore.Tools` | EF Core CLI migration tools |

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target Framework | `net10.0` |
| Output Type | Class Library |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This library *defines the database*: the `EFDataModel` DbContext, one entity class per table (users, groups, tenants, departments, tags, settings, files, email templates, plugin cache, UDF labels), and code-first migrations. One model targets SQL Server, PostgreSQL, SQLite, MySQL, and an in-memory option.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| EF Core DbContext | The schema as `DbSet<T>` | [EFModels/EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.EFModels/EFModels/EFDataModel.cs) |
| Fluent API overrides | Column types, indexes | [EFModelOverrides.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.EFModels/EFModelOverrides.cs) |

**Why does this exist?**
One schema that runs on whatever database is already in place — no per-engine rewrite.

**What does it accomplish that other tools don't?**
- **Five engines from one model**, with zero-setup InMemory for first runs and tests.

**Terminology & "can I see it?"**
- **DbContext** — the C# object that represents the database and its tables.
- **Entity** — a C# class mapped to one table.

**The hard part, drawn** — one model, five databases:

```
  EFDataModel (DbContext)
       └ DbSet ▶ User · UserGroup · Tenant · Department · Tag · Setting · FileStorage · EmailTemplate · PluginCache · UDFLabel
       └ providers ▶ SQL Server · PostgreSQL · SQLite · MySQL · InMemory
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.

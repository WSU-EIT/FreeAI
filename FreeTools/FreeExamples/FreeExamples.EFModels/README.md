# FreeExamples.EFModels

> Entity Framework Core DbContext and entity models — supports SQL Server, SQLite, MySQL, PostgreSQL, and In-Memory providers.

**Target:** .NET 10 · **Type:** Class Library

---

## What This Project Contains

| File | Description |
|------|-------------|
| `EFDataModel.cs` | Partial `DbContext` with `DbSet<>` properties and `OnModelCreating` configuration |
| `Department.cs` | Department entity |
| `DepartmentGroup.cs` | Department group entity |
| `FileStorage.cs` | Binary file storage entity |
| `PluginCache.cs` | Compiled plugin cache entity |
| `Setting.cs` | Application settings entity |
| `Tag.cs` | Tag entity |
| `TagItem.cs` | Tag-to-entity association entity |
| `Tenant.cs` | Multi-tenant entity |
| `UDFLabel.cs` | User-defined field labels entity |
| `User.cs` | User entity |
| `UserGroup.cs` | User group entity |
| `UserInGroup.cs` | User-group membership entity |

---

## Database Providers

| Provider | Package | Status |
|----------|---------|--------|
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` | ✅ Primary |
| SQLite | `Microsoft.EntityFrameworkCore.Sqlite` | ✅ Supported |
| MySQL | `MySql.EntityFrameworkCore` | ✅ Supported |
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` | ✅ Supported |
| In-Memory | `Microsoft.EntityFrameworkCore.InMemory` | ✅ Development |

---

## Migrations

This project does not ship with pre-built migration files. The [DatabaseMigration tool](../FreeExamples.DatabaseMigration/) can generate fresh migrations on demand using menu option **M** or CLI `--Migration:AutoRun=efmigrate`.

To generate migrations manually:

```bash
# Uncomment OnConfiguring in EFDataModel.cs with your connection string, then:
dotnet ef migrations add InitialMigration --project FreeExamples.EFModels
dotnet ef database update --project FreeExamples.EFModels
```

---

Part of the FreeTools solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** Defines the database: the `EFDataModel` DbContext and entity classes, targeting five providers (SQL Server, SQLite, MySQL, PostgreSQL, InMemory). It deliberately ships **no pre-built migrations** — the sibling `DatabaseMigration` tool generates them fresh on demand.

**What tech & where?** [EFModels/EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeExamples/FreeExamples.EFModels/EFModels/EFDataModel.cs).

**Why does this exist?** One schema that runs on whatever database is available, with migrations generated on demand rather than committed (so they always match the current model).

**What does it beat?** **Five engines from one model**, and **fresh-migration generation** (via the DatabaseMigration tool) instead of stale committed migration files.

**Terminology:** **DbContext** — the C# object representing the database; **migration** — the generated script that builds the tables.

**The hard part, drawn:**
```
  EFDataModel ─DbSet─▶ User · Tenant · Department · Setting · FileStorage · PluginCache · Tag · UDFLabel
        providers ▶ SQL Server · SQLite · MySQL · PostgreSQL · InMemory
        no shipped migrations ─▶ DatabaseMigration tool generates them fresh
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

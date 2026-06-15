# FreeAI.DataAccess

Class library — server-side data access layer.

This is the FreeCRM scaffold data access layer, renamed to the FreeAI namespace with placeholder app-specific extension points that contain no real logic yet. The `DataAccess` partial class connects to SQL Server, MySQL, PostgreSQL, SQLite, or in-memory via Entity Framework Core, runs custom SQL migration scripts on startup, seeds default data, and exposes a large `IDataAccess` interface split across partial files covering authentication (local, LDAP, Active Directory, OAuth), user/tenant/department/group management, file storage, encryption, language/UDF labels, SignalR updates, plugin cache persistence, JWT helpers, PDF generation (QuestPDF), CSV import/export, NuGet package queries, and Microsoft Graph API calls. `DataAccess.App.cs` provides the app-specific hook methods (`DataAccessAppInit`, `GetBlazorDataModelApp`, `DeleteTenantApp`, etc.) but all are stubs. `Utilities.App.cs` and `GraphAPI.App.cs` follow the same pattern.

## Key public classes

| Class | File | Purpose |
|---|---|---|
| `DataAccess` | `DataAccess.cs` | Main partial class; EF Core setup, DB selection, startup migration |
| `IDataAccess` | `DataAccess.cs` + partials | Full service interface consumed by controllers |
| `GraphAPI` | `GraphAPI.cs` | Microsoft Graph calls (mail, user lookup) |
| `RandomPasswordGenerator` | `RandomPasswordGenerator.cs` | Cryptographically-random password creation |

## Project references

| Reference | Role |
|---|---|
| `FreeAI.DataObjects` | Shared DTOs |
| `FreeAI.EFModels` | EF Core DbContext and entity models |
| `FreeAI.Plugins` | Plugin runtime (for executing dynamic code inside data access) |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.Graph` | Microsoft Graph API client |
| `Novell.Directory.Ldap.NETStandard` | LDAP / Active Directory lookups |
| `QuestPDF` | PDF generation |
| `JWTHelpers` | JWT token creation and validation |
| `Brad.Wickett_Sql2LINQ` | SQL-to-LINQ query helper |
| `Azure.Identity` | Azure managed identity and credential support |
| `NuGet.Protocol` | NuGet feed queries |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn (used in dynamic code execution helpers) |

## Build info

| Field | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net9.0` |
| Output type | Library |

Part of the ChatWithAI solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The server-only data layer. It connects to one of five databases via EF Core, runs migration scripts on startup, seeds defaults, and implements the big `IDataAccess` interface — auth (local/LDAP/AD/OAuth), users, tenants, files, encryption, JWT, PDF (QuestPDF), CSV, NuGet queries, and Microsoft Graph. The app-specific hooks (`DataAccessAppInit`, etc.) are present but are stubs.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| EF Core (5 providers) | All database I/O | [DataAccess.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.DataAccess/DataAccess.cs) |
| Microsoft Graph | Mail / user lookup | [GraphAPI.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.DataAccess/GraphAPI.cs) |
| QuestPDF · JWTHelpers · LDAP | PDF, tokens, directory auth | [DataAccess.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.DataAccess/DataAccess.cs) |

**Why does this exist?**
To keep all data access, business logic, and integrations in one server-only layer so the UI and DTOs stay thin and database-agnostic — swap the engine or auth provider here and nothing else changes.

**What does it accomplish that other tools don't?**
- One code path supports **five** database engines.
- Enterprise auth (LDAP / Active Directory / OAuth) and Microsoft Graph built in.
- Runs its own SQL migration scripts at startup — no separate migration step to forget.

**Terminology & "can I see it?"**
- **DTO** — a plain data shape passed between layers (no logic).
- **Partial class** — one class (`DataAccess`) split across many files for readability.

**The hard part, drawn** — one interface, many backends:

```
  Controllers ─▶ IDataAccess (DataAccess.*)  ─ EF Core ─▶ SQL Server | MySQL | PostgreSQL | SQLite | InMemory
                        ├─ QuestPDF (PDF)   ├─ JWT   ├─ AES   └─ Graph / LDAP (directory)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

# FreeExamples.DataAccess

> Data access layer — EF Core database queries, Microsoft Graph integration, LDAP authentication, PDF generation, plugin execution, and background task processing.

**Target:** .NET 10 · **Type:** Class Library

---

## What This Project Contains

| Area | Description |
|------|-------------|
| **Entity CRUD** | Database operations for all entities (Users, Departments, Tags, Settings, etc.) |
| **Authentication** | Local login, LDAP (Novell.Directory.Ldap), and Microsoft Graph (Azure AD) |
| **File Storage** | Binary file storage and retrieval |
| **PDF Generation** | Document generation using QuestPDF |
| **Plugin Execution** | Runtime loading and execution of compiled plugins |
| **Background Tasks** | `ProcessBackgroundTasksApp` for periodic processing |
| **App Customization** | `DataAccess.App.cs` — application-specific methods and language tags |

---

## Key Dependencies

| Package | Purpose |
|---------|---------|
| `Brad.Wickett_Sql2LINQ` | LINQ query builder |
| `Microsoft.Graph` | Microsoft Graph API for Azure AD users/groups |
| `Azure.Identity` | Azure credential management |
| `Novell.Directory.Ldap.NETStandard` | LDAP/Active Directory authentication |
| `QuestPDF` | PDF document generation |
| `CsvHelper` | CSV import/export |
| `JWTHelpers` | JWT token creation and validation |

---

## Customization

Add application-specific data access methods to `DataAccess.App.cs`:

```csharp
public partial class DataAccess
{
    public async Task<BooleanResponse> ProcessBackgroundTasksApp(Guid TenantId, long Iteration)
    {
        // Your periodic background tasks here
    }
}
```

Override or add language tags in the `AppLanguage` dictionary for localization.

---

Part of the FreeTools solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The standard server-side data layer: EF Core queries for all entities, auth (local/LDAP/Graph), file storage, PDF (QuestPDF), plugin execution, and a background-task hook (`ProcessBackgroundTasksApp`). App-specific methods go in `DataAccess.App.cs`.

**What tech & where?** [DataAccess.App.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeExamples/FreeExamples.DataAccess/DataAccess.App.cs) (the customization hook) + the [DataAccess project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeExamples/FreeExamples.DataAccess).

**Why does this exist?** To keep all data access and integrations server-side so the UI and DTOs stay thin and database-agnostic.

**What does it beat?** One layer across five DB engines, with enterprise auth (LDAP/Graph) and a clear `.App.` extension point for app-specific logic.

**Terminology:** **`.App.` hook** — the partial-class method where custom code is added without touching framework files.

**The hard part, drawn:**
```
  Controllers ─▶ IDataAccess (DataAccess.*) ─ EF Core ─▶ SQL Server | SQLite | MySQL | PostgreSQL | InMemory
        ├─ auth (local/LDAP/Graph) · QuestPDF · plugin execution
        └─ DataAccess.App.cs ─▶ app-specific methods + ProcessBackgroundTasksApp hook
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

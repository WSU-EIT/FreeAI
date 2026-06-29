# FreeGLBA.EFModels

Entity Framework Core models and database context for the FreeGLBA GLBA Compliance Data Access Tracking System.

Developed by **Enrollment Information Technology** at **Washington State University**.

## Purpose

This project contains all Entity Framework Core entity models and the `EFDataModel` database context that defines the database schema for FreeGLBA. It supports multiple database providers for maximum deployment flexibility.

## Supported Database Providers

| Provider | Package | Use Case |
|----------|---------|----------|
| **SQL Server** | `Microsoft.EntityFrameworkCore.SqlServer` | Production, Enterprise |
| **PostgreSQL** | `Npgsql.EntityFrameworkCore.PostgreSQL` | Production, Open Source |
| **MySQL** | `MySql.EntityFrameworkCore` | Production, Web Hosting |
| **SQLite** | `Microsoft.EntityFrameworkCore.Sqlite` | Development, Testing |
| **In-Memory** | `Microsoft.EntityFrameworkCore.InMemory` | Unit Testing |

## Dependencies

- `Microsoft.EntityFrameworkCore` 10.0.1
- `Microsoft.EntityFrameworkCore.Tools` 10.0.1

## Project Structure

```
FreeGLBA.EFModels/
├── FreeGLBA.EFModels.csproj
├── README.md
├── EFModelOverrides.cs              # Partial class extensions
└── EFModels/
    ├── EFDataModel.cs               # Base DbContext
    ├── FreeGLBA.App.EFDataModel.cs  # GLBA-specific DbContext extensions
    │
    │── # Core Framework Entities
    ├── User.cs                      # Application users
    ├── UserGroup.cs                 # User groups/roles
    ├── UserInGroup.cs               # User-group membership
    ├── Department.cs                # Organizational departments
    ├── DepartmentGroup.cs           # Department-group associations
    ├── Tenant.cs                    # Multi-tenancy support
    ├── Setting.cs                   # Application settings
    ├── Tag.cs                       # Tagging system
    ├── TagItem.cs                   # Tag assignments
    ├── UDFLabel.cs                  # User-defined field labels
    ├── FileStorage.cs               # File storage metadata
    ├── EmailTemplate.cs             # Email templates
    ├── PluginCache.cs               # Plugin caching
    │
    └── # GLBA-Specific Entities
    ├── FreeGLBA.App.AccessEvent.cs      # Access event records
    ├── FreeGLBA.App.SourceSystem.cs     # External source systems
    ├── FreeGLBA.App.DataSubject.cs      # Data subjects (students, etc.)
    └── FreeGLBA.App.ComplianceReport.cs # Compliance reports
```

## Key Entities

### GLBA-Specific Entities

#### AccessEventItem
Records every data access event for GLBA compliance tracking.

```csharp
public class AccessEventItem
{
    public Guid AccessEventId { get; set; }
    public Guid SourceSystemId { get; set; }
    public string SourceEventId { get; set; }      // Deduplication key
    public DateTime AccessedAt { get; set; }        // When access occurred
    public DateTime ReceivedAt { get; set; }        // When logged
    public string UserId { get; set; }              // Who accessed
    public string SubjectId { get; set; }           // Whose data (e.g., student ID)
    public string AccessType { get; set; }          // View, Export, Print, etc.
    public string Purpose { get; set; }             // Business justification
    // ... additional fields
}
```

#### SourceSystemItem
Represents external systems that send access events via API.

```csharp
public class SourceSystemItem
{
    public Guid SourceSystemId { get; set; }
    public string Name { get; set; }
    public string ApiKey { get; set; }              // API authentication
    public bool IsActive { get; set; }
    public long EventCount { get; set; }            // Statistics
    public DateTime? LastEventReceivedAt { get; set; }
}
```

### Core Framework Entities

| Entity | Table | Purpose |
|--------|-------|---------|
| `User` | Users | Application user accounts |
| `UserGroup` | UserGroups | Role-based groupings |
| `UserInGroup` | UsersInGroups | Many-to-many user-group |
| `Department` | Departments | Organizational structure |
| `Tenant` | Tenants | Multi-tenant isolation |
| `Setting` | Settings | Key-value configuration |
| `Tag` | Tags | Categorization tags |
| `FileStorage` | FileStorage | Stored file metadata |

## Database Context

The `EFDataModel` class provides the Entity Framework DbContext:

```csharp
public partial class EFDataModel : DbContext
{
    // GLBA Entities
    public DbSet<AccessEventItem> AccessEvents { get; set; }
    public DbSet<SourceSystemItem> SourceSystems { get; set; }
    public DbSet<DataSubjectItem> DataSubjects { get; set; }
    public DbSet<ComplianceReportItem> ComplianceReports { get; set; }
    
    // Core Entities
    public DbSet<User> Users { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    // ... etc
}
```

## Usage

### Configuration (in Program.cs)

```csharp
// SQL Server
builder.Services.AddDbContext<EFDataModel>(options =>
    options.UseSqlServer(connectionString));

// PostgreSQL
builder.Services.AddDbContext<EFDataModel>(options =>
    options.UseNpgsql(connectionString));

// SQLite (development)
builder.Services.AddDbContext<EFDataModel>(options =>
    options.UseSqlite("Data Source=freeglba.db"));
```

### Querying

```csharp
// Get recent access events
var recentEvents = await _context.AccessEvents
    .Where(e => e.AccessedAt > DateTime.UtcNow.AddDays(-7))
    .OrderByDescending(e => e.AccessedAt)
    .Take(100)
    .ToListAsync();

// Get events by subject
var subjectEvents = await _context.AccessEvents
    .Where(e => e.SubjectId == studentId)
    .Include(e => e.SourceSystem)
    .ToListAsync();
```

## Extending the Model

Use partial classes in `EFModelOverrides.cs` to add computed properties or methods:

```csharp
public partial class AccessEventItem
{
    public string DisplayName => $"{UserName} accessed {SubjectId}";
    public bool IsRecent => AccessedAt > DateTime.UtcNow.AddHours(-24);
}
```

## File Listing

| File | Description |
|------|-------------|
| `EFModels/EFDataModel.cs` | Base Entity Framework DbContext |
| `EFModels/FreeGLBA.App.EFDataModel.cs` | GLBA-specific DbContext extensions |
| `EFModels/FreeGLBA.App.AccessEvent.cs` | Access event entity (GLBA core) |
| `EFModels/FreeGLBA.App.SourceSystem.cs` | Source system entity |
| `EFModels/FreeGLBA.App.DataSubject.cs` | Data subject entity |
| `EFModels/FreeGLBA.App.ComplianceReport.cs` | Compliance report entity |
| `EFModels/User.cs` | User account entity |
| `EFModels/UserGroup.cs` | User group entity |
| `EFModels/UserInGroup.cs` | User-group junction entity |
| `EFModels/Department.cs` | Department entity |
| `EFModels/DepartmentGroup.cs` | Department-group junction entity |
| `EFModels/Tenant.cs` | Multi-tenant entity |
| `EFModels/Setting.cs` | Application settings entity |
| `EFModels/Tag.cs` | Tag entity |
| `EFModels/TagItem.cs` | Tag assignment entity |
| `EFModels/UDFLabel.cs` | User-defined field labels |
| `EFModels/FileStorage.cs` | File storage metadata entity |
| `EFModels/EmailTemplate.cs` | Email template entity |
| `EFModels/PluginCache.cs` | Plugin cache entity |
| `EFModelOverrides.cs` | Partial class extensions |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This library *defines the database*. Alongside the core FreeCRM tables it adds the **GLBA audit tables**: `AccessEvent` (who accessed whose data, when, how, why), `SourceSystem` (each registered external system, its API key, and its event stats), `DataSubject`, and `ComplianceReport`. One model targets four engines (+ InMemory).

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| EF Core DbContext | The schema (core + GLBA tables) | [EFModels/EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.EFModels/EFModels/EFDataModel.cs) |
| Access-event entity | The audit record | [FreeGLBA.App.AccessEvent.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.EFModels/EFModels/FreeGLBA.App.AccessEvent.cs) |
| Source-system entity | Registered systems + API key + stats | [FreeGLBA.App.SourceSystem.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.EFModels/EFModels/FreeGLBA.App.SourceSystem.cs) |

**Why does this exist?**
One schema that stores the compliance audit trail on whatever database the institution already runs.

**What does it accomplish that other tools don't?**
- The audit trail is **first-class, queryable data** (`AccessEvent` rows), not a flat log file — so dashboards and reports are just queries.
- **Five engines from one model**, InMemory included for tests.

**Terminology & "can I see it?"**
- **Entity** — a C# class mapped to one table (`AccessEvent` → the audit table).
- **`SourceEventId`** — the dedup key stored on each access event.

**The hard part, drawn** — the audit schema at the center:

```
  EFDataModel (DbContext)
       └ GLBA ▶ AccessEvent (who / whose / when / how / why) · SourceSystem (apikey + stats)
                · DataSubject · ComplianceReport
       └ core ▶ User · Tenant · Department · Setting · PluginCache
       └ providers ▶ SQL Server · PostgreSQL · MySQL · SQLite · InMemory
```

## About

**FreeGLBA** is developed and maintained by the **Enrollment Information Technology** team at **Washington State University**.

🔗 [Meet Our Staff](https://em.wsu.edu/eit/meet-our-staff/)

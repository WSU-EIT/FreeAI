# 300 — Research: Database Migration Tool Patterns

> **Document ID:** 300  
> **Category:** Research  
> **Purpose:** Deep-dive comparison of three internal database migration tools to inform a new open-source FreeExamples.DatabaseMigration project.  
> **Audience:** Devs, AI agents, contributors.  
> **Outcome:** 🔄 Research complete — ready to build FreeExamples migration example.

---

## Document Index

| Section | Description |
|---------|-------------|
| [Subject Tools](#subject-tools) | The three tools analyzed |
| [Architecture Overview](#architecture-overview) | Shared structure across all three |
| [Feature Comparison](#feature-comparison-matrix) | Side-by-side capability matrix |
| [Shared Patterns](#shared-patterns-all-three-tools) | Common code patterns extracted |
| [Unique Features by Tool](#unique-features-by-tool) | What each adds beyond the common base |
| [Code Snippets](#key-code-snippets) | Representative code from each tool |
| [Recommendations](#recommendations-for-freeexamples-migration) | What to include in the example project |

---

## Subject Tools

Three internal database migration/import tools, all console apps targeting .NET 10:

| Tool | Location | Lines | Purpose | Schema Relationship |
|------|----------|-------|---------|---------------------|
| **Touchpoints.DatabaseImport** (V1) | `_Repos/TwilioFlex5/TouchpointsV2/` | ~2,275 | Migrate V1 → V2 (different schemas) | Cross-schema with column mapping & transformation |
| **Touchpoints.DatabaseImportV2** (V2) | `_Repos/TwilioFlex5/TouchpointsV2/` | ~713 | Copy V2 → V2 (identical schemas) | Same-schema bulk copy |
| **ACP.MigrationTool** | `_Repos/V3_AcademicCalendarPetitions/` | ~1,347 (+191 UserMigration.cs) | Migrate V0 (int IDs) → V3 (GUID IDs) | Cross-schema with ID type conversion |

---

## Architecture Overview

All three tools share a remarkably consistent architecture. This is clearly a proven internal pattern.

### Common Structure (Single `Program.cs`)

```
┌─────────────────────────────────────────────────────────────┐
│  Constants: Connection strings, URLs, bulk settings         │
├─────────────────────────────────────────────────────────────┤
│  Main(): Init logging → banner → connectivity test → menu  │
├─────────────────────────────────────────────────────────────┤
│  #region Logging           — Init, Log, LogColored, etc.   │
│  #region UI Helpers        — Menu display, mode toggle      │
│  #region Verification      — Compare counts source vs target│
│  #region Phase N: [name]   — One region per migration phase │
│  #region Data Integrity    — CSV export + SHA256 compare    │
│  #region Column Profiling  — Schema analysis / reporting    │
│  #region Helpers           — BulkInsert, GetTableCount, etc.│
└─────────────────────────────────────────────────────────────┘
```

### Startup Sequence (All Three)

1. **Initialize logging** — creates timestamped `.log` file in `runs/` folder
2. **Print banner** — box-drawing ASCII art with tool name
3. **Display connection strings** — masked for security
4. **Test database connectivity** — reports ✓ or FAILED with color
5. **Test web endpoints** (V1 only) — HTTP status + latency
6. **Interactive menu loop** — `Console.ReadKey()` dispatcher

### State Machine

```
[DRY RUN MODE (default)] ←→ Toggle ←→ [LIVE MODE]
         │                                   │
    Preview only                     Actually writes data
    Cyan colored output              Red/Yellow warnings
    "Would migrate: N"               "Migrated: N"
```

---

## Feature Comparison Matrix

| Feature | V1 (DatabaseImport) | V2 (DatabaseImportV2) | ACP (MigrationTool) |
|---------|:-------------------:|:---------------------:|:-------------------:|
| **Console menu UI** | ✅ | ✅ | ✅ |
| **Dry run mode** | ✅ | ✅ | ✅ |
| **Timestamped log files** | ✅ | ✅ | ✅ |
| **Color-coded output** | ✅ | ✅ | ✅ |
| **Connection masking** | ✅ | ✅ | ✅ |
| **Database connectivity test** | ✅ | ✅ | ✅ |
| **Web endpoint test** | ✅ | ❌ | ❌ |
| **Verification (count compare)** | ✅ table format | ✅ box-drawing table | ✅ table format |
| **Phased migration** | ✅ 7 phases (numbered) | ✅ 6 phases (lettered A-F) | ✅ 3 phases + utilities |
| **Run all phases** | ✅ option `9` | ✅ option `9` | ✅ option `6` |
| **Schema approach** | EF contexts (V1 + V2) | Raw SQL (generic) | EF contexts (V0 + V3) |
| **Bulk insert** | ✅ SqlBulkCopy | ✅ SqlBulkCopy | ❌ EF SaveChanges |
| **Batch size** | 5,000 | 5,000 | 100 (configured) |
| **Live progress stats** | ✅ rate + ETA | ✅ rate + ETA | ✅ rate + count |
| **Identity insert handling** | ❌ | ✅ auto-detect | ❌ |
| **Truncate all tables** | ❌ | ✅ with FK disable/re-enable | ✅ (re-import option) |
| **Data integrity verify** | ✅ CSV + SHA256 | ❌ | ✅ CSV + SHA256 |
| **Column profiling** | ✅ (min/max/null analysis) | ❌ | ✅ (min/max/null analysis) |
| **Sample data preview** | ✅ | ❌ | ✅ |
| **FK mapping tables** | ❌ (same GUIDs) | ❌ (same schema) | ✅ MigrationTracking |
| **ID transformation** | ❌ (GUID→GUID) | ❌ (same PKs) | ✅ (int→GUID) |
| **Schema dump on startup** | ✅ | ❌ | ❌ |
| **Config via appsettings.json** | ❌ (constants) | ❌ (constants) | ✅ |
| **User secrets support** | ❌ | ❌ | ✅ |
| **Separate files** | 1 file | 1 file | 2 files (Program.cs + UserMigration.cs) |
| **Dependencies** | EF + SqlClient | SqlClient only | EF + SqlClient + Config |

---

## Shared Patterns (All Three Tools)

### 1. Logging Infrastructure

All three tools use an identical logging pattern:

```csharp
// State
private static string _runsDirectory = null!;
private static string _logFilePath = null!;
private static StreamWriter? _logWriter;
private static DateTime _runStartTime;

// Core methods
private static void InitializeLogging()     // Create timestamped log in runs/
private static void FinalizeLogging()       // Write duration, close stream
private static void Log(string message, bool logOnly = false)  // Dual console+file
private static void LogColored(string message, ConsoleColor color, bool logOnly = false)
private static void LogWrite(string message) // No newline variant
private static void LogError(string title, string message, string? innerMessage = null)
```

**Key detail:** `logOnly: true` allows writing to the log file without cluttering the console (e.g., raw key presses).

### 2. Dry Run Mode

```csharp
private static bool _dryRun = true;  // ALWAYS starts in dry run

private static void ToggleDryRunMode()
{
    _dryRun = !_dryRun;
    if (_dryRun)
        LogColored("🔒 DRY RUN MODE ENABLED", ConsoleColor.Cyan);
    else
        LogColored("⚠️  LIVE MODE ENABLED", ConsoleColor.Red);
}

private static void DisplayModeHeader()
{
    if (_dryRun)
        LogColored("  MENU - 🔒 DRY RUN MODE (No writes)", ConsoleColor.Cyan);
    else
        LogColored("  MENU - ⚠️  LIVE MODE (Will write data!)", ConsoleColor.Red);
}
```

### 3. Connection Masking

```csharp
private static string MaskConnectionString(string cs)
{
    var builder = new SqlConnectionStringBuilder(cs);
    if (!string.IsNullOrEmpty(builder.Password)) builder.Password = "****";
    return builder.ConnectionString;
}
```

### 4. Database Connectivity Test

```csharp
private static async Task TestDatabaseConnectivity(string name, string cs)
{
    LogWrite($"  {name}: ");
    try {
        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();
        LogColored("Connected ✓", ConsoleColor.Green);
    } catch (Exception ex) {
        LogColored($"FAILED - {ex.Message}", ConsoleColor.Red);
    }
}
```

### 5. Box-Drawing UI

All tools use Unicode box-drawing characters for structured output:

```
═══════════════════════════════════════  (section headers)
─────────────────────────────────────── (sub-dividers)
╔══╗ ║  ╚══╝                            (banners)
┌──┬──┐ │  │ ├──┼──┤ └──┴──┘            (data tables)
```

### 6. Migration Result Reporting

```csharp
private static void PrintMigrationResult(int migrated, int skipped)
{
    var msg = $"✓ {(_dryRun ? "Would migrate" : "Migrated")}: {migrated:N0}, " +
              $"{(_dryRun ? "Would skip" : "Skipped")}: {skipped:N0}";
    LogColored(msg, _dryRun ? ConsoleColor.Cyan : ConsoleColor.Green);
}
```

### 7. Project Source Directory Finder

```csharp
private static string FindProjectSourceDirectory()
{
    var currentDir = AppContext.BaseDirectory;
    for (int i = 0; i < 10; i++) {
        if (File.Exists(Path.Combine(currentDir, "Program.cs")))
            return currentDir;
        var parent = Directory.GetParent(currentDir);
        if (parent == null) break;
        currentDir = parent.FullName;
    }
    return Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? ".";
}
```

Walks up from `bin/Debug/net10.0/` to find the project root so `runs/` folder lives next to `Program.cs`.

---

## Unique Features by Tool

### V1 (Touchpoints.DatabaseImport) — Cross-Schema Migration

**What makes it unique:**
- Migrates between **two different schemas** (V1 → V2) with column-by-column mapping
- References **two EF model projects**: `Touchpoints_v1_efmodel_scaffold` (source) and `Touchpoints.EFModels` (target)
- Uses EF DbContexts for reading, SqlBulkCopy for writing
- Has **7 granular numbered phases** organized by entity dependency
- Includes **web endpoint health checks** (tests IIS-hosted APIs)
- Has **schema dump on startup** showing all table structures
- The **most comprehensive** — 2,275 lines, handles every edge case
- Includes **data integrity verification** with CSV export and SHA256 hash comparison
- Includes **column profiling** — analyzes min/max lengths, null counts per column

**Phase Architecture:**
```
Phase 1: Tenants & Settings (small, foundational)
Phase 2: Users & Groups (small, depends on Phase 1)
Phase 3a: Sources (config tables)
Phase 3b: Tags & File Storage
Phase 4: Touchpoints (bulk — millions of rows)
Phase 5: TouchpointTags (bulk, depends on Phase 4)
Phase 6: FlexCrmCalls (source-specific data)
Phase 7: Other Data (remaining tables)
```

### V2 (Touchpoints.DatabaseImportV2) — Same-Schema Bulk Copy

**What makes it unique:**
- Copies between **identical schemas** (V2 prod → V2 local)
- Uses **pure SQL** — no EF models needed for data transfer
- **Generic table copier** — one method (`MigrateV2Table`) handles ANY table
- **Table definitions as string arrays** — phases defined declaratively
- Auto-detects **PK columns** and **identity columns** from schema metadata
- Handles `IDENTITY_INSERT ON/OFF` automatically
- Has **TRUNCATE ALL** option with FK constraint disable/re-enable
- **Smallest tool** — 713 lines, cleanest architecture
- Uses `INFORMATION_SCHEMA` queries to discover columns dynamically
- Skips duplicates by loading target PKs into a `HashSet<object>`

**Generic Table Copier Pattern:**
```csharp
// Declarative phase definitions — just list table names
private static readonly string[] PhaseA_Foundation = ["Tenants", "PluginCache"];
private static readonly string[] PhaseB_UsersAndConfig = [
    "DepartmentGroups", "Departments", "Settings", "UDFLabels",
    "UserGroups", "Users", "UserInGroups", "Sources", "Tags",
    "FileStorage", "EmailTemplates"
];

// One generic method handles everything
private static async Task MigrateV2Table(string tableName) { ... }
```

### ACP (AcademicCalendarPetitions.MigrationTool) — ID Type Transformation

**What makes it unique:**
- Transforms **int primary keys → GUID primary keys** during migration
- Uses a **MigrationTracking table** in the target DB to map old IDs to new GUIDs
- Loads FK maps at migration time to resolve relationships
- Uses **appsettings.json + User Secrets** for configuration (no hardcoded strings)
- Has a **"Generate GUIDs Code"** feature — outputs C# constants for default lookup values
- Tracks migration state — can resume interrupted migrations
- Separate **UserMigration.cs** file for complex user transformation logic
- Uses **EF SaveChanges** (not bulk copy) because each record needs transformation
- Has a **Re-import** option that drops and re-creates all V3 data
- Uses **Microsoft.Extensions.Configuration** stack

**FK Mapping Pattern:**
```csharp
// Load mapping: OldId (int) → NewGuid
private static async Task<Dictionary<int, Guid>> LoadFkMap(EFDataModel db, string tableName)

// Use during migration:
if (!statusMap.TryGetValue(old.StatusId, out var statusGuid)) { errors++; continue; }
newRecord.StatusId = statusGuid;
```

---

## Key Code Snippets

### V2's Generic Table Copier (Recommended Base for Example)

This is the cleanest pattern — works with any same-schema table:

```csharp
private static async Task MigrateV2Table(string tableName)
{
    var sourceCount = await GetTableCountAsync(SourceDb, tableName);
    var targetCount = await GetTableCountAsync(TargetDb, tableName);

    Log($"  [{tableName}] Source: {sourceCount:N0}, Target: {targetCount:N0}");
    if (sourceCount <= 0) { Log("    Nothing to import."); return; }
    if (_dryRun) { /* preview only */ return; }

    await using var sourceConn = new SqlConnection(SourceDb);
    await using var targetConn = new SqlConnection(TargetDb);
    await sourceConn.OpenAsync();
    await targetConn.OpenAsync();

    // Discover columns dynamically
    var columns = new List<string>();
    // ... INFORMATION_SCHEMA query ...

    // Detect PK and identity columns
    var pkColumn = await GetPrimaryKeyColumn(sourceConn, tableName);
    var hasIdentity = await HasIdentityColumn(targetConn, tableName);

    // Load existing PKs to skip duplicates
    var existingPks = new HashSet<object>();
    // ... load from target ...

    // Stream rows from source, batch into DataTable, bulk insert
    int migrated = 0, skipped = 0;
    var stopwatch = Stopwatch.StartNew();

    // ... read rows, skip duplicates, batch insert ...
    // Live progress: rate + ETA
    Console.Write($"\r  Batch {n}: {migrated:N0}/{total:N0} " +
                  $"({rate:N0}/sec, ~{remaining}m remaining)");

    PrintMigrationResult(migrated, skipped);
}
```

### V1's Data Integrity Verification

```csharp
// Export both databases to CSV, compute SHA256, compare
private static async Task RunDataIntegrityVerification()
{
    // Export source records to CSV (normalized, sorted)
    // Export target records to CSV (normalized, sorted)
    // SHA256 hash each file
    // Compare hashes — MATCH = data is identical
}
```

### ACP's FK Mapping with MigrationTracking

```csharp
// Track every old→new ID mapping for later phases
newDb.MigrationTrackings.Add(new MigrationTracking {
    MigrationTrackingId = Guid.NewGuid(),
    TableName = "Status",
    OldId = oldRecord.StatusId,       // int
    NewGuid = newGuid,                // Guid
    MigratedAt = DateTime.UtcNow
});
await newDb.SaveChangesAsync();
```

---

## Bulk Insert Helper (V1 and V2)

```csharp
private static async Task BulkInsert(
    SqlConnection connection, string tableName,
    DataTable dataTable, bool keepIdentity = false)
{
    var options = keepIdentity
        ? SqlBulkCopyOptions.KeepIdentity
        : SqlBulkCopyOptions.Default;

    using var bulkCopy = new SqlBulkCopy(connection, options, null) {
        DestinationTableName = tableName,
        BulkCopyTimeout = BulkTimeoutSeconds,  // 300s
        BatchSize = BulkBatchSize              // 5,000
    };

    foreach (DataColumn col in dataTable.Columns)
        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);

    await bulkCopy.WriteToServerAsync(dataTable);
}
```

---

## Recommendations for FreeExamples Migration

### Target: `FreeExamples.DatabaseMigration` console app

Based on the analysis, the FreeExamples migration example should:

### 1. Use V2 as the Primary Blueprint

V2 (DatabaseImportV2) is the cleanest, most generic, and most reusable:
- 713 lines vs 2,275 (V1) and 1,538 (ACP)
- Generic table copier works with ANY table — no entity-specific code
- Declarative phase definitions via string arrays
- Pure SQL approach — no EF model dependency for data transfer

### 2. Include These Shared Patterns

| Pattern | Source | Priority |
|---------|--------|----------|
| Logging infrastructure | All three | MUST |
| Dry run mode | All three | MUST |
| Connection masking | All three | MUST |
| Connectivity test | All three | MUST |
| Interactive console menu | All three | MUST |
| Phased migration | All three | MUST |
| Verification (count compare) | All three | MUST |
| Live progress with rate + ETA | V1, V2 | MUST |
| Box-drawing UI | All three | SHOULD |
| SqlBulkCopy helper | V1, V2 | MUST |

### 3. Add Comments Showing Transform Capability

Even though the example will be same-schema (like V2), include commented code or documentation showing how to:
- Map columns between different schemas (V1 pattern)
- Transform ID types — int→GUID (ACP pattern)
- Use FK mapping tables (ACP pattern)
- Apply data transformations during migration

### 4. Use FreeExamples.EFModels

Reference the existing `FreeExamples.EFModels` project. The example will copy from a "source" to a "destination" database using the same schema — closest to V2's approach.

### 5. Suggested Phase Structure

```
Phase A: Foundation (Tenants, Settings, PluginCache)
Phase B: Users & Config (Departments, Users, UserGroups, Sources, Tags)
Phase C: Application Data (bulk tables)
```

### 6. File Organization

Following the naming convention:
```
FreeExamples.DatabaseMigration/
├── Program.cs                    # Main entry point + all migration logic
├── FreeExamples.DatabaseMigration.csproj
├── README.md                     # Usage instructions
└── runs/                         # Log files (gitignored)
```

### 7. Dependencies

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="6.1.4" />
<!-- EF only if using EF contexts for reading, otherwise pure SQL like V2 -->
```

---

## Summary Table

| Aspect | V1 (Cross-Schema) | V2 (Same-Schema) | ACP (ID Transform) | **Example (Planned)** |
|--------|:------------------:|:-----------------:|:-------------------:|:---------------------:|
| Complexity | High | Low | Medium | **Low** |
| Approach | EF read → BulkCopy | SQL read → BulkCopy | EF read → EF write | **SQL read → BulkCopy** |
| Schema | Different | Same | Different | **Same** |
| Transforms | Column mapping | None | int→GUID | **None (commented examples)** |
| Config | Constants | Constants | appsettings.json | **Constants** |
| Lines | ~2,275 | ~713 | ~1,538 | **~500–700 target** |

---

*Created: 2025-07-25*  
*Maintained by: [Quality]*

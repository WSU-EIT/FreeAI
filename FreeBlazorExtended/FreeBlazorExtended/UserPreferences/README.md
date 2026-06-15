# Feature 104 — UserPreferences

> Per-(tenant, user) preferences — theme, density, recent items, and a typed JSON blob of per-entity settings — without a custom table per setting.

## What this feature does
Stores one `UserPreferences` record per (tenant, user). Standard fields (theme, density) are first-class properties. A `PerEntityJson` blob holds arbitrary typed settings keyed by entity name; `GetPerEntityPreference<T>` deserializes them on read. A `RecentItemsJson` list is capped at 20 entries — push a new item and the oldest falls off. Lets every page persist UX state without each feature inventing its own preference table.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `UserPreferencesService.cs` | Get/set preferences, generic per-entity accessor, recent-items ring buffer; delegates persistence to `IUserPreferencesStore` | 174 |
| `UserPreferences.cs` | `UserPreferences` POCO with `PerEntityJson`, `RecentItemsJson`, theme/density | 45 |
| `IUserPreferencesStore.cs` | Persistence abstraction (Get/Save/Delete) | 14 |
| `InMemoryUserPreferencesStore.cs` | Default `ConcurrentDictionary`-backed store; used by WASM client and as a fallback | 38 |

## Dependencies
- **NuGet packages:** `System.Text.Json` (BCL)
- **Cross-feature dependencies:** none
- **SignalR:** not used
- **EF Core:** **REAL — SQL via EF Core (in-memory fallback for WASM client and unconfigured hosts)**

## Persistence model
Two-store pattern:
- **`InMemoryUserPreferencesStore`** — ships with this library. No EF dependency. Used by Blazor WASM (no SQL connection), tests, and any host that hasn't registered an alternative.
- **EF-backed store** — implemented per-host. The reference implementation lives at `FreeBlazorExample/FreeBlazorExample.DataAccess/EFUserPreferencesStore.cs` and writes to a `UserPreferences` SQL table via the project's existing `IDataAccess`/`EFDataModel`.

Pick the store at DI registration time. The service auto-detects which store is in play and exposes a `UseInMemory` boolean property for diagnostics.

## DI registration

### Server (Blazor Server / hosted Blazor) — SQL persistence
```csharp
// Register the EF-backed store BEFORE the service so the service ctor picks it up.
builder.Services.AddScoped<FreeBlazorExtended.UserPreferences.IUserPreferencesStore, EFUserPreferencesStore>();
builder.Services.AddScoped<FreeBlazorExtended.UserPreferences.UserPreferencesService>();
```

### Blazor WASM client — in-memory fallback
```csharp
// No store registration — the parameterless ctor uses InMemoryUserPreferencesStore.
// Persistence happens server-side; the WASM client should call the server for real prefs.
builder.Services.AddSingleton<FreeBlazorExtended.UserPreferences.UserPreferencesService>();
```

The WASM client cannot persist directly to SQL; it must call the server (typically via the existing `BlazorDataModel` proxy). The in-memory fallback exists so the showcase keeps working in WASM-prerender mode and pure-WASM dev sessions.

## Migration

This codebase does **not** use the `dotnet ef migrations add ...` tooling. Schema changes are added as raw SQL steps in `FreeBlazorExample/FreeBlazorExample.DataAccess/DataMigrations.<Provider>.cs`. The `DataAccess` ctor calls `EnsureCreated()` first (which creates tables based on the current `EFDataModel`) and then applies any pending entries from `DataMigrations` in order.

Migration `002 — UserPreferences` is included for all four supported providers:
- `DataMigrations.SQLServer.cs`
- `DataMigrations.PostgreSQL.cs`
- `DataMigrations.SQLite.cs`
- `DataMigrations.MySQL.cs`

To apply, just run the app — `DatabaseApplyLatestMigrations()` runs on first request. The migration is idempotent (`IF OBJECT_ID … IS NULL` / `CREATE TABLE IF NOT EXISTS`).

If you ever **do** wire up `dotnet ef`:
```bash
dotnet ef migrations add AddUserPreferences \
  --project FreeBlazorExample/FreeBlazorExample.EFModels \
  --startup-project FreeBlazorExample/FreeBlazorExample
dotnet ef database update \
  --project FreeBlazorExample/FreeBlazorExample.EFModels \
  --startup-project FreeBlazorExample/FreeBlazorExample
```

## Cherry-pick instructions
1. Copy the entire `FreeBlazorExtended/UserPreferences/` folder into your project.
2. Also copy `Foundation/Helpers.cs` and `Foundation/DataObjects.cs` if not already present.
3. **For SQL persistence (server)**: add a `DbSet<UserPreferences>` to your `DbContext` with a unique index on `(TenantId, UserId)`, copy the migration SQL from `DataMigrations.<Provider>.cs` step 002, implement an `IUserPreferencesStore` over your DbContext (see `EFUserPreferencesStore.cs` as a reference), and register it server-side.
4. **For WASM-only / in-memory**: just register `UserPreferencesService` as Singleton — done.
5. There are no Razor components — consume the service from your existing pages.

## Showcase
The interactive demo lives at `/showcase/feature104-user-preferences` in the FreeBlazorExample app:
- Page: `FreeBlazorExample/FreeBlazorExample.Client/Pages/Showcase/Feature104_UserPreferences.razor`

The page is unchanged — the same calls (`GetUserPreferences`, `SaveUserPreferences`, etc.) now persist to SQL when invoked server-side.

## Status
- Implementation: **REAL**
- Persistence: SQL via EF Core (server) / in-memory fallback (WASM client, unconfigured hosts)
- Known gaps: no encryption-at-rest for the JSON blob; no schema migration helper if a stored `PerEntityJson` shape changes between versions.

## Effort to integrate
**S** — four C# files, one DI line, zero components. EF wiring is one entity, one fluent-API block, and one migration step.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** One preferences record per (tenant, user). Common settings (theme, density) are real fields; everything else lives in a `PerEntityJson` blob you read back as a typed object (`GetPerEntityPreference<T>`). A `RecentItemsJson` list auto-caps at 20 (oldest falls off). Persistence is pluggable via `IUserPreferencesStore` — **real SQL via EF Core** on the server, an in-memory fallback in the WASM client.

**What tech & where?** [UserPreferencesService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/UserPreferences/UserPreferencesService.cs) · [IUserPreferencesStore.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/UserPreferences/IUserPreferencesStore.cs) (the swap point) · [InMemoryUserPreferencesStore.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/UserPreferences/InMemoryUserPreferencesStore.cs).

**Why does this exist?** So every page can save UX state (last view, filters, theme) without each feature inventing its own settings table.

**What does it beat?** It's the **only feature service here with real SQL persistence** (the others are in-memory), and the JSON blob means new per-entity settings need *no schema change*. The two-store pattern keeps the same code working in WASM (in-memory) and on the server (SQL).

**Terminology:** **Two-store pattern** — one interface, two implementations (SQL vs in-memory), chosen at DI time. **Ring buffer** — the 20-item recent list that drops the oldest.

**The hard part, drawn:**
```
  GetUserPreferences(tenant,user) ─▶ IUserPreferencesStore
        server ─▶ EF Core ─▶ SQL UserPreferences table
        WASM   ─▶ InMemoryUserPreferencesStore (fallback)
  PerEntityJson blob ─▶ GetPerEntityPreference<T>()   ·   RecentItems capped at 20 (oldest off)
```

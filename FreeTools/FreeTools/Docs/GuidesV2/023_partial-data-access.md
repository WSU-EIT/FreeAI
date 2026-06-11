# 023 — Inside the Partial Data-Access Layer

> **Document ID:** 023  ·  **Category:** Reference  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Explain how the partial data-access layer is structured and where your queries belong so they survive upgrades.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 02x (The Data Stack) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why Partial Classes Matter Here](#why-it-matters) | What "partial" means, why the layer is split, and why placement decides upgrade safety |
| 2 | [Anatomy of the Data-Access Layer](#anatomy) | The real files in `CRM.DataAccess`, what each owns, and the one constructor that ties them together |
| 3 | [Generated vs. Custom Code](#generated-vs-custom) | Which files the framework re-stamps, which ones are yours, and the `.App.` naming rule |
| 4 | [Where Your Queries Belong](#where-queries-go) | The `DataAccess.App.cs` file, its hook methods, and how to add a brand-new domain |
| 5 | [Worked Examples](#examples) | Real method shapes copied from the source, annotated for a non-engineer |
| 6 | [Upgrade Safety Checklist](#upgrade-safety) | A short pass to confirm your code lives where an upgrade won't touch it |
| 7 | [Common Pitfalls](#pitfalls) | The specific mistakes that get your code overwritten or your data leaked |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Partial Classes Matter Here

**Why this matters first:** FreeCRM is meant to be *upgraded underneath you*. The team that maintains the framework periodically re-generates large chunks of the data layer. If your custom query is sitting in a file the framework owns, your next upgrade quietly deletes it. The whole point of this doc is to show you where your code is safe.

**Plain-language definitions:**

- **Data-access layer** — the only place in the app that talks to the database. Controllers and pages never run SQL directly; they call into this layer. In FreeCRM that layer is a single C# class named `DataAccess`, living in the `CRM` namespace inside the `CRM.DataAccess` project.
- **Partial class** — a normal C# feature. A `partial` class is one class whose source is *split across several files*. The compiler stitches them back into one type. So `DataAccess.cs`, `DataAccess.Users.cs`, `DataAccess.Locations.cs`, and ~30 siblings are all the *same* `DataAccess` class — just organized by topic so no single file is thousands of lines long.
- **Regeneration** — the framework re-writes certain files from a template when you upgrade or scaffold a new entity. Anything in a regenerated file is disposable from the framework's point of view.

Here is the class header, copied from `DataAccess.cs`. Notice the `partial` keyword and the two interfaces it implements:

```csharp
namespace CRM;

public partial class DataAccess: IDisposable, IDataAccess
{
    ...
}
```

Because the class is partial, **the file a method lives in is a real decision, not a cosmetic one.** A method in `DataAccess.Locations.cs` behaves identically to one in `DataAccess.App.cs` at runtime — but only one of those files is yours to keep. Section 3 draws that line precisely.

---

<a id="anatomy"></a>
## 2. Anatomy of the Data-Access Layer

**Why this matters:** Before you can know where *your* code goes, you need a mental map of what's already there. The layer is split by **domain** (a business topic such as Users, Invoices, Appointments). One file per domain.

**The constructor is the spine.** Only `DataAccess.cs` has the constructor. It takes a connection string, a database type, and a service provider, then builds the Entity Framework context (`EFDataModel`) that every other file shares. Entity Framework Core (abbreviated "EF Core") is the library that maps database tables to C# objects, so you write LINQ instead of raw SQL. The constructor picks the provider from the database type string:

```csharp
switch (_databaseType.ToLower()) {
    case "inmemory":   optionsBuilder.UseInMemoryDatabase("InMemory"); ...
    case "mysql":      optionsBuilder.UseMySQL(_connectionString, ...);
    case "postgresql": optionsBuilder.UseNpgsql(_connectionString, ...);
    case "sqlite":     optionsBuilder.UseSqlite(_connectionString);
    case "sqlserver":  optionsBuilder.UseSqlServer(_connectionString, ...);
}

data = new EFDataModel(optionsBuilder.Options);
```

That single `data` field (`private EFDataModel data;`) is how every domain file reaches the database. When you see `data.Locations` or `data.Users` in any partial file, that's the shared context.

**The domain files.** Each partial file owns one topic. A representative slice (from the project's own `README.md`):

| File | Owns |
|------|------|
| `DataAccess.cs` | Constructor, EF context setup, core initialization |
| `DataAccess.App.cs` | **Application-specific overrides and hook methods — this is yours** |
| `DataAccess.Users.cs` | User CRUD, password hashing, lockout |
| `DataAccess.Tenants.cs` | Tenant CRUD and tenant-context resolution |
| `DataAccess.Appointments.cs` | Appointment CRUD, notes, services, attendance |
| `DataAccess.Invoices.cs` | Invoice CRUD and appointment-invoice linking |
| `DataAccess.Locations.cs` | Location CRUD |
| `DataAccess.Settings.cs` | Per-tenant settings storage |
| `DataAccess.SignalR.cs` | SignalR active-user tracking (live updates to the browser) |
| `DataAccess.Utilities.cs` | Shared helpers (`AdminUser`, `StringValue`, `GuidValue`, …) |
| `DataMigrations.*.cs` | Per-provider SQL migration scripts (SQLite, SQL Server, MySQL, PostgreSQL) |

There are around 30 such files in total; the table above is the orientation set, not the full list.

**The interface is partial too.** The contract `IDataAccess` — the list of methods controllers are allowed to call — is *also* split the same way. Each domain file declares its slice of the interface right above its implementation. From `DataAccess.Locations.cs`:

```csharp
public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> DeleteLocation(Guid LocationId, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false);
    Task<DataObjects.Location> GetLocation(Guid LocationId, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.Location>> GetLocations(Guid TenantId, DataObjects.User? CurrentUser = null);
    Task<DataObjects.Location> SaveLocation(DataObjects.Location location, DataObjects.User? CurrentUser = null);
}

public partial class DataAccess
{
    public async Task<DataObjects.BooleanResponse> DeleteLocation(...) { ... }
    ...
}
```

So a domain file usually contains *two* partials: a chunk of the `IDataAccess` interface and the matching chunk of the `DataAccess` class. When you add a method you want controllers to call, you add it in both places.

---

<a id="generated-vs-custom"></a>
## 3. Generated vs. Custom Code

**Why this matters:** This is the single most important rule in the doc. Get it right and upgrades are painless; get it wrong and you lose work.

**The naming convention does the work for you.** Files come in two flavors:

- **Framework-owned (regenerated):** the plain domain files — `DataAccess.Users.cs`, `DataAccess.Locations.cs`, `DataAccess.Invoices.cs`, and so on. Treat these as **read-only**. They are scaffolded and re-stamped by the framework. Editing them is allowed in an emergency, but assume an upgrade can overwrite your edit.
- **Yours (preserved):** files with **`.App.`** in the name. The framework deliberately leaves these alone. The flagship is `DataAccess.App.cs`, whose comment header states the rule outright:

```csharp
// Use this file as a place to put any application-specific data access methods.
```

The `.App.` convention isn't unique to `DataAccess`. The same paired pattern repeats across the project for every regenerated helper:

| Framework-owned (regenerated) | Your companion (preserved) |
|-------------------------------|----------------------------|
| `DataAccess.cs` + all `DataAccess.<Domain>.cs` | `DataAccess.App.cs` |
| `GraphAPI.cs` | `GraphAPI.App.cs` |
| `Utilities.cs` | `Utilities.App.cs` |
| `RandomPasswordGenerator.cs` | `RandomPasswordGenerator.App.cs` |

Each `.App.` file is a `partial` that merges into the same class. For example, `Utilities.App.cs` is just:

```csharp
namespace CRM;

public static partial class Utilities
{
    // Add your app-specific utility methods here.
}
```

**The mental model:** the framework files are the engine; the `.App.` files are your seat belt. If a method is in a `.App.` file, an upgrade will not touch it. If it's anywhere else, assume it can be overwritten.

---

<a id="where-queries-go"></a>
## 4. Where Your Queries Belong

**Why this matters:** Now we make it concrete. There are exactly two safe destinations for custom data code, and `DataAccess.App.cs` is the hub for both.

**Option A — fill in the supplied hook methods.** `DataAccess.App.cs` ships with a set of empty methods that the framework *calls for you* at the right moments. You don't wire anything up; you just put your logic in the body. These hooks let you extend the standard flows without editing the standard files. The real ones in the source include:

| Hook method | The framework calls it when… | What you put in it |
|-------------|------------------------------|--------------------|
| `GetDataApp(object Rec, object DataObject, ...)` | A `Get` finishes mapping a record | Copy *your* extra columns from the EF record onto the returned DTO |
| `SaveDataApp(object Rec, object DataObject, ...)` | A `Save` is about to persist | Copy *your* extra fields from the DTO back onto the EF record |
| `DeleteRecordsApp(object Rec, ...)` | A record is being deleted | Remove your related/child rows first |
| `ProcessBackgroundTasksApp(Guid TenantId, long Iteration)` | The background processor ticks | Run scheduled/recurring work |
| `GetBlazorDataModelApp(...)` | The page data model loads | Add your data to what the UI receives |
| `GetFilterColumnsApp(...)` | A filtered list is built | Register your custom filter/sort columns |

Each hook is a partial method on `DataAccess` and arrives pre-stubbed. For example, `GetDataApp` already contains a `type-switch` skeleton; you just uncomment a line per entity:

```csharp
if (Rec is EFModels.EFModels.Location && DataObject is DataObjects.Location) {
    var rec = Rec as EFModels.EFModels.Location;
    var location = DataObject as DataObjects.Location;

    if (rec != null && location != null) {
        //location.Property = rec.Property;   // <- your custom field here
    }
    return;
}
```

`EFModels.EFModels.Location` is the **EF model** (the database row), and `DataObjects.Location` is the **DTO** (the shape the app passes around). The hook is exactly the seam where you bridge your additions between the two — see docs 021 and 022 for those two layers.

**Option B — add a whole new domain in a new `.App.` file.** If you're building a feature the framework doesn't know about (say, "WidgetOrders"), don't shoehorn it into a framework file. Create your own partial file — for example `DataAccess.WidgetOrders.App.cs` — and follow the exact two-part shape the framework uses:

```csharp
namespace CRM;

public partial interface IDataAccess
{
    Task<List<DataObjects.WidgetOrder>> GetWidgetOrders(Guid TenantId, DataObjects.User? CurrentUser = null);
}

public partial class DataAccess
{
    public async Task<List<DataObjects.WidgetOrder>> GetWidgetOrders(Guid TenantId, DataObjects.User? CurrentUser = null)
    {
        // your query against `data.WidgetOrders` here
    }
}
```

Because it carries `.App.` in the name, the framework leaves it alone, and because it declares its slice of `IDataAccess`, controllers and DI can call it like any built-in method. `DataAccess.App.cs` already demonstrates this two-part shape with its own `ProcessBackgroundTasksApp` and `YourMethod` entries on the interface.

**Naming conventions to copy from the source:**

- Read one: `GetThing(Guid Id, DataObjects.User? CurrentUser = null)`
- Read many: `GetThings(Guid TenantId, DataObjects.User? CurrentUser = null)`
- Write: `SaveThing(DataObjects.Thing thing, DataObjects.User? CurrentUser = null)`
- Delete: `DeleteThing(Guid Id, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false)`
- Almost every method takes a trailing `DataObjects.User? CurrentUser = null` — that's how permission and tenant checks are enforced.

---

<a id="examples"></a>
## 5. Worked Examples

These are real shapes from `DataAccess.Locations.cs`, annotated. Copy this structure into your `.App.` files.

**Example 1 — a "get many" that respects tenants and soft-delete.** Notice it filters by `TenantId` (so one customer never sees another's data) and hides deleted rows unless the caller is an admin:

```csharp
public async Task<List<DataObjects.Location>> GetLocations(Guid TenantId, DataObjects.User? CurrentUser = null)
{
    var output = new List<DataObjects.Location>();

    List<Location>? recs = null;

    if (AdminUser(CurrentUser)) {
        recs = await data.Locations.Where(x => x.TenantId == TenantId).ToListAsync();
    } else {
        recs = await data.Locations.Where(x => x.TenantId == TenantId && x.Deleted != true).ToListAsync();
    }

    if (recs != null && recs.Any()) {
        foreach (var rec in recs) {
            var l = new DataObjects.Location { /* map fields ... */ };
            GetDataApp(rec, l, CurrentUser);   // <- your custom fields get added here
            output.Add(l);
        }
    }

    return output;
}
```

Two things to copy faithfully:
- **`AdminUser(CurrentUser)`** is a shared helper (declared in `DataAccess.Utilities.cs`); it decides whether deleted rows are visible. Use it instead of inventing your own check.
- **`GetDataApp(rec, l, CurrentUser)`** is called once per record. That's the hook from Section 4 — it's why filling in `GetDataApp` is enough to surface your extra columns everywhere.

**Example 2 — a "delete" that is soft by default.** Records aren't usually erased; they're flagged `Deleted = true` so they can be recovered. The method only hard-deletes when forced or when the tenant's preference says so:

```csharp
if (ForceDeleteImmediately || tenantSettings.DeletePreference == DataObjects.DeletePreference.Immediate) {
    var deleteAppRecords = await DeleteRecordsApp(rec, CurrentUser);   // your child rows go first
    if (!deleteAppRecords.Result) { ... return output; }

    data.Locations.Remove(rec);
    await data.SaveChangesAsync();
} else {
    rec.Deleted = true;
    rec.DeletedAt = now;
    rec.LastModified = now;
    await data.SaveChangesAsync();
}
```

Note the call to **`DeleteRecordsApp(rec, CurrentUser)`** *before* the hard delete — that's your hook to clean up related records you own. (See doc 025 for the soft-delete model in full.)

**Example 3 — a background task using settings.** `DataAccess.App.cs` shows the intended idiom inside `ProcessBackgroundTasksApp`: use the `Iteration` counter for cadence and the typed settings helpers for persistence.

```csharp
// Run a task roughly every 10 minutes:
var lastRun = GetSetting<DateTime>("MyCustomProcessLastRunDate", DataObjects.SettingType.DateTime);
if (lastRun == default(DateTime) || lastRun < DateTime.UtcNow.AddMinutes(-10)) {
    // ... your work ...
    SaveSetting("MyCustomProcessLastRunDate", DataObjects.SettingType.DateTime, DateTime.UtcNow);
}
```

`GetSetting<T>` / `SaveSetting` are the framework's typed key-value store (see `DataAccess.Settings.cs` / `DataAccess.Utilities.cs`). Use them rather than rolling your own table for small bits of state.

---

<a id="upgrade-safety"></a>
## 6. Upgrade Safety Checklist

Run this quick pass before you commit. Each item maps to a rule established above.

1. **Is every custom method in a `.App.` file?** Open the file. If its name lacks `.App.` (e.g. `DataAccess.Users.cs`), move the method into `DataAccess.App.cs` or a new `DataAccess.<Domain>.App.cs`. Framework files get re-stamped.
2. **Did you extend records via the hooks, not by editing the mapper?** Extra columns belong in `GetDataApp` / `SaveDataApp`, not inside the regenerated `GetLocation` / `SaveLocation` bodies.
3. **Did you delete child rows in `DeleteRecordsApp`?** If your feature has related tables, the only upgrade-safe place to clean them up is that hook.
4. **New public methods declared on the partial `IDataAccess` interface?** A method controllers must call has to appear in the interface slice too — and that interface slice should also be in your `.App.` file.
5. **Tenant and admin guards present?** Confirm each query filters by `TenantId` and uses `AdminUser(CurrentUser)` for visibility, matching the framework pattern. Missing these isn't an upgrade risk — it's a data-leak risk.
6. **No duplicate method signatures?** Because everything merges into one `DataAccess` class, two files defining the same method name/signature won't compile. After a major upgrade, a new framework method could collide with one you added — rename yours if so.

---

<a id="pitfalls"></a>
## 7. Common Pitfalls

- **Writing your query in a framework file.** The most common and most expensive mistake. Code in `DataAccess.Users.cs` or any plain `DataAccess.<Domain>.cs` is on borrowed time. It compiles, it runs, and then an upgrade erases it. Always reach for a `.App.` file.
- **Editing the regenerated `GetData`/`SaveData` mapping instead of the `*App` hook.** Same failure mode, subtler: the surrounding mapping survives but your inserted line vanishes. Put field mapping in `GetDataApp` / `SaveDataApp`.
- **Forgetting the interface declaration.** You add a public method to the `DataAccess` partial but not to the `IDataAccess` partial. It compiles inside the project, but controllers that depend on `IDataAccess` can't see it. Add both halves.
- **Dropping the tenant filter.** Every `data.<Table>.Where(...)` should include `x.TenantId == TenantId`. Omit it and you return one customer's data to another. The framework's own methods never skip this — don't let yours.
- **Ignoring soft-delete.** Querying without `&& x.Deleted != true` (for non-admins) surfaces records the user "deleted." Mirror the `AdminUser(CurrentUser)` branch shown in Section 5.
- **Hard-deleting when the tenant expects soft-delete.** Calling `Remove(...)` directly skips both the `DeletePreference` check and the `DeleteRecordsApp` cleanup hook. Follow the delete shape in Example 2.
- **Talking to the database from a controller or page.** The `data` context is private to `DataAccess`. Anything that needs the database calls an `IDataAccess` method. Bypassing the layer defeats every guard above.

---

<a id="related-docs"></a>
## 8. Related Docs

- [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md) — the full stack overview
- [041 — Code the Framework Can Update Underneath](041_upgrade-safe-model.md) — partials that survive upgrades
- [024 — API Controllers: The Tenant-Aware Request Surface](024_api-controllers.md) — controllers call into this layer
- [086 — Performance at Scale](086_performance-at-scale.md) — writing data-access queries that stay fast as data grows

---
*GuidesV2 023 · drafted from source 2026-06-05 · grounded in `CRM.DataAccess` (`DataAccess` partial class, `.App.` convention).*

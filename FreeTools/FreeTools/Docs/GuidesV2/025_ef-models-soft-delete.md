# 025 — EF Models and the Records That Never Truly Vanish

> **Document ID:** 025  ·  **Category:** Reference  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Describe the EF model conventions and the soft-delete strategy that keeps deleted data recoverable.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 02x (The Data Stack) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Soft Delete Matters](#why-it-matters) | What "soft delete" means and why this app keeps deleted data around |
| 2 | [EF Model Conventions](#model-conventions) | How entity classes are shaped: keys, the TenantId, and the shared audit/delete columns |
| 3 | [The Soft-Delete Field Reference](#field-reference) | The two real marker columns — `Deleted` and `DeletedAt` — and the computed `DeletedBy` |
| 4 | [How Deleted Rows Stay Hidden](#query-filters) | There is no global filter — every read query opts out by hand with `Deleted != true` |
| 5 | [Deleting and Restoring Records](#delete-restore) | The delete-method signature, soft vs. immediate, and the undelete path |
| 6 | [Querying Deleted Data](#querying-deleted) | The "Deleted Records" screens and the background purge that empties the trash |
| 7 | [Common Pitfalls and Gotchas](#pitfalls) | The manual-filter trap, the missing `DeletedBy` column, and cascade gaps |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Soft Delete Matters

**Why this matters first:** when a user clicks "Delete," you almost never want the row to actually disappear. People delete the wrong customer, the wrong appointment, the wrong file — and then they want it back. A system that truly erases data on every delete turns a two-second mistake into a support ticket, a restore-from-backup, or a permanent loss.

**Soft delete** is the fix. Instead of removing the row from the database, the app *flags* it as deleted and hides it from normal views. The data physically stays in the table, so it can be restored ("undeleted") at any time — until a scheduled cleanup eventually removes it for good. Think of it as a recycle bin: the file is gone from your desktop, but it is sitting in the trash, recoverable, until the trash is emptied.

A few plain-language terms used throughout this doc:

- **EF / Entity Framework** — Microsoft's "ORM" (object-relational mapper). It lets C# code talk to a database table as if it were a normal list of C# objects. Each table maps to a C# class (an **entity**). Those classes live in `CRM.EFModels`.
- **Hard delete** — the row is physically removed (`DELETE FROM ...`). It is gone.
- **Soft delete** — the row stays; a flag marks it as deleted so the rest of the app ignores it.
- **Purge** — the later, permanent removal of rows that have been soft-deleted long enough. This is the "emptying the trash" step, and it runs on a schedule in the background.

This app's default behavior is soft delete. Each tenant (customer account) can choose between two modes via a `DeletePreference` setting:

```csharp
public enum DeletePreference
{
    Immediate,
    MarkAsDeleted,
}
```

`MarkAsDeleted` is the default, and soft-deleted records are purged after a configurable number of days (90 by default). The rest of this doc explains exactly how that works in the real code.

---

<a id="model-conventions"></a>
## 2. EF Model Conventions

**Why this matters:** every feature you build reads and writes these entity classes, so knowing their shared shape saves you from re-learning each table. The conventions are consistent, which is what makes the soft-delete strategy possible across the whole app.

The entity classes live in `c:\Users\pepkad\source\repo2\FreeCRM\CRM.EFModels\EFModels\` — one file per table (for example `User.cs`, `Appointment.cs`, `FileStorage.cs`, `Service.cs`). They are all in the namespace `CRM.EFModels.EFModels` and are declared `partial` so generated and hand-written pieces can coexist.

The repeating conventions you will see on almost every entity:

- **A GUID primary key named after the entity.** A **GUID** is a 128-bit globally unique identifier (something like `f47ac10b-58cc-...`), used instead of an auto-incrementing integer. The `User` table's key is `UserId`, `FileStorage`'s is `FileId`, `Appointment`'s is `AppointmentId`, and so on.
- **A `TenantId` GUID** that scopes the row to one tenant. (See doc 021 for why every table carries a tenant.)
- **Audit columns** that record who touched the row and when: `Added`, `AddedBy`, `LastModified`, `LastModifiedBy`. The `*By` fields are strings holding the acting user's identifier.
- **The two soft-delete columns:** `Deleted` and `DeletedAt`.

Here is the tail end of the real `User` entity (`User.cs`), which shows the audit columns sitting right next to the delete markers:

```csharp
public DateTime Added { get; set; }
public string? AddedBy { get; set; }
public DateTime LastModified { get; set; }
public string? LastModifiedBy { get; set; }
public bool Deleted { get; set; }
public DateTime? DeletedAt { get; set; }
```

The same `Deleted` / `DeletedAt` pair appears verbatim on the other soft-deletable entities — for example `FileStorage.cs` carries the identical two lines. The database column type for `DeletedAt` is configured centrally in `EFDataModel.cs`, where each entity gets `entity.Property(e => e.DeletedAt).HasColumnType("datetime")`. There is **no** base class or interface forcing these columns; the convention is simply applied to every table that supports recovery.

> One subtlety worth flagging now: a handful of pure join/link tables (the rows that connect two entities, like `UserInGroup` or `AppointmentUser`) are *not* soft-deleted — when a parent is deleted, those links are physically removed with raw SQL. Soft delete applies to the "real" records a user would recognize, not the plumbing between them.

---

<a id="field-reference"></a>
## 3. The Soft-Delete Field Reference

**Why this matters:** if you get the field names wrong, your queries either show deleted rows to users or crash at compile time. There are exactly **two** real columns, and a third value that *looks* like a column but is computed. Knowing the difference prevents a common mistake.

| Field | Type | Where it lives | Meaning |
|-------|------|----------------|---------|
| `Deleted` | `bool` | EF entity (DB column) | `true` = soft-deleted (hidden); `false` = live. This is the flag every query checks. |
| `DeletedAt` | `DateTime?` | EF entity (DB column) | UTC timestamp of when the row was soft-deleted. `null` when the row is live. Drives the purge ("delete things older than X"). |
| `DeletedBy` | `string` | **DTO only — computed** | A display name shown on the Deleted Records screens. It is **not** stored in the database. |

> **Heads-up — there is no `DeletedBy` column and no `IsDeleted` column.** The flag is plain `Deleted` (not `IsDeleted`). And `DeletedBy` exists only on the `DataObjects.DeletedRecordItem` DTO (the lightweight object sent to the UI), where it is filled in at read time from the row's `LastModifiedBy` value:

```csharp
appointments.Add(new DataObjects.DeletedRecordItem {
    DeletedAt = item.DeletedAt.HasValue ? (DateTime)item.DeletedAt : DateTime.Now,
    DeletedBy = LastModifiedDisplayName(item.LastModifiedBy),
    Display   = FormatAppointmentTitle(item.Title, item.Start, item.End, item.AllDay),
    ItemId    = item.AppointmentId,
});
```

The takeaway: "who deleted this" is reconstructed from the audit column `LastModifiedBy` — because the delete operation updates `LastModified` / `LastModifiedBy` as part of flipping `Deleted` to `true`. The DTO's `DeletedBy` is just a friendly display of that.

A **DTO** ("data transfer object") is a plain shape used to move data between layers and to the browser. The EF entity is the database row; the DTO is what the rest of the app passes around. They are deliberately different objects, which is why `DeletedBy` can exist on one and not the other.

---

<a id="query-filters"></a>
## 4. How Deleted Rows Stay Hidden

**Why this matters:** this is the single most important thing to internalize before you write a query. In many EF apps, soft-deleted rows are hidden *automatically* by a feature called a **global query filter** (`HasQueryFilter`), so you never see them unless you explicitly ask. **This app does not use that feature.** A search of the entire codebase finds zero uses of `HasQueryFilter` and zero uses of `IgnoreQueryFilters`.

Instead, hiding deleted rows is **manual and explicit**: every read query that should ignore deleted data appends `&& x.Deleted != true` to its `Where` clause. If you forget it, deleted rows leak into your results — there is no safety net.

You will see this pattern everywhere in `CRM.DataAccess`. For example, in `DataAccess.Users.cs`:

```csharp
var rec = await data.Users
    .FirstOrDefaultAsync(x => x.UserId == (Guid)UserId && x.Deleted != true);
```

```csharp
var recs = await data.Users
    .Where(x => x.TenantId == TenantId && x.Deleted != true)
    ...
```

Note the idiom is `Deleted != true` rather than `Deleted == false`. Because `Deleted` is a non-null `bool` this is logically the same, but the `!= true` form is the consistent house style across the data layer — match it for consistency.

**Why was it done this way?** A manual filter is more verbose, but it is also fully visible: anyone reading a query can see exactly what is and isn't included, and the queries that *want* deleted rows (the trash screens, the purge) simply omit the clause instead of fighting a hidden global rule. The trade-off is discipline — see the Pitfalls section.

---

<a id="delete-restore"></a>
## 5. Deleting and Restoring Records

**Why this matters:** "delete" in this app is not one behavior — it is two, chosen at runtime by the tenant's `DeletePreference`. Understanding the branch keeps you from accidentally hard-deleting data a customer expected to be recoverable.

### The delete-method signature

Each entity has a delete method that follows the same shape. From `DataAccess.Users.cs`:

```csharp
public async Task<DataObjects.BooleanResponse> DeleteUser(
    Guid UserId,
    DataObjects.User? CurrentUser = null,
    bool ForceDeleteImmediately = false)
```

Three things to read off this signature:

- It returns a **`BooleanResponse`** — the app's standard pass/fail result object (a `Result` flag plus `Messages`). See doc 026.
- `CurrentUser` is optional and is used to stamp `LastModifiedBy` (so the trash screen can show who deleted it).
- `ForceDeleteImmediately` lets a caller bypass the soft-delete preference and hard-delete right now.

### Soft vs. immediate — the branch

Inside `DeleteUser`, the decision is made by combining the flag and the tenant setting:

```csharp
if (ForceDeleteImmediately || tenantSettings.DeletePreference == DataObjects.DeletePreference.Immediate) {
    data.Users.Remove(rec);            // HARD delete: row is physically removed
} else {
    rec.Deleted = true;                // SOFT delete: flag it and stamp the audit fields
    rec.DeletedAt = now;
    rec.LastModified = now;
    if (CurrentUser != null) {
        rec.LastModifiedBy = CurrentUserIdString(CurrentUser);
    }
}
```

So the rule is: **hard-delete only when explicitly forced, or when the tenant chose `Immediate`. Otherwise soft-delete.** Note that even on a soft delete, the user is removed from any live appointments immediately — soft delete preserves the record, not necessarily every relationship it participated in.

When a record is hard-deleted (immediate mode), the method also does extra cleanup first: it rewrites `LastModifiedBy`/`AddedBy` references across related tables (so the deleted user's GUID is replaced with their display name) and removes dependent join rows. That cleanup is skipped in soft-delete mode, because the record — and its history — is meant to survive.

### Restoring (undelete)

Restoring is the mirror image: flip `Deleted` back to `false` and clear the timestamp. The central `UndeleteRecord` method in `DataAccess.Utilities.cs` switches on a type string and does, for each case:

```csharp
var recAppt = await data.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == RecordId);
if (recAppt != null) {
    recAppt.Deleted = false;
    recAppt.DeletedAt = null;
    await data.SaveChangesAsync();
    output.Result = true;
    ...
}
```

Because soft delete never removed the row, undelete is a simple two-field update — no backup, no re-insert. That is the entire payoff of the strategy.

---

<a id="querying-deleted"></a>
## 6. Querying Deleted Data

**Why this matters:** two kinds of code deliberately *want* the deleted rows — the "recycle bin" UI that lets users restore them, and the background job that permanently purges them. Since there is no global filter to suppress, these simply query with `Deleted == true`.

### Seeing what's in the trash

`DataAccess.Utilities.cs` exposes two read methods used by the Deleted Records screens (`CRM.Client/Pages/Settings/Misc/DeletedRecords.razor`):

- `GetDeletedRecordCounts(Guid TenantId)` — how many deleted rows exist per entity type, e.g.

  ```csharp
  var users = await data.Users.CountAsync(x => x.TenantId == TenantId && x.Deleted == true);
  ```

- `GetDeletedRecords(Guid TenantId)` — the actual deleted items, projected into `DeletedRecordItem` DTOs (the ones that compute `DeletedBy` from `LastModifiedBy`, as shown in Section 3).

From those screens a user can restore a record (calls `UndeleteRecord`) or delete it for good right now (calls `DeleteRecordImmediately`, which dispatches to the right entity's delete method with `ForceDeleteImmediately: true`).

### The background purge (emptying the trash)

Soft-deleted rows do not live forever. The purge is driven by two tenant settings:

```csharp
public DeletePreference DeletePreference { get; set; } = DeletePreference.MarkAsDeleted;
public int DeleteMarkedRecordsAfterDays { get; set; } = 90;
```

A hosted background service, `BackgroundProcessor` (`CRM/Classes/BackgroundProcessor.cs`, an `IHostedService`/`BackgroundService` that runs on a timer), periodically walks each enabled tenant and, for tenants in `MarkAsDeleted` mode, purges anything older than the cutoff:

```csharp
if (tenant.TenantSettings.DeletePreference == DataObjects.DeletePreference.MarkAsDeleted) {
    if (tenant.TenantSettings.DeleteMarkedRecordsAfterDays > 0) {
        var olderThan = now.AddDays(-tenant.TenantSettings.DeleteMarkedRecordsAfterDays);
        var deletedRecords = await da.DeleteAllPendingDeletedRecords(tenant.TenantId, olderThan);
        ...
    }
}
```

To keep the load light, this purge runs only on the first iteration and then once every 100 iterations of the processing loop — not on every tick.

`DeleteAllPendingDeletedRecords(TenantId, OlderThan)` is where the permanent removal happens. For each entity it finds the eligible rows and calls that entity's own delete method with the force flag, so all related cleanup runs:

```csharp
var users = await data.Users
    .Where(x => x.TenantId == TenantId
             && x.Deleted == true
             && (x.DeletedAt == null || x.DeletedAt < OlderThan))
    .ToListAsync();
foreach (var rec in users) {
    var result = await DeleteUser(rec.UserId, null, true);   // ForceDeleteImmediately
    ...
}
```

The same logic is reachable on demand: there is a `DeletePendingDeletedRecords()` method (looping all tenants) exposed via the API at `~/api/Data/DeletePendingDeletedRecords`, useful for testing or a manual sweep. Note the `DeletedAt == null` guard — a row flagged `Deleted` but somehow missing a timestamp is treated as eligible, so stray rows can't get stuck in the trash forever.

---

<a id="pitfalls"></a>
## 7. Common Pitfalls and Gotchas

**Why this matters:** the soft-delete design is simple, but its safety depends on conventions a compiler can't enforce. These are the mistakes most likely to bite you.

- **Forgetting `Deleted != true` on a new query.** This is the big one. Because there is no global query filter, a `Where` clause without `&& x.Deleted != true` will happily return soft-deleted rows to users. Every new read against a soft-deletable entity must add the clause by hand. When in doubt, copy an existing query for that entity.

- **Reaching for `IsDeleted` or `DeletedBy` as columns.** The flag is `Deleted`, not `IsDeleted`. And `DeletedBy` is *not* a database column — it is a display value computed from `LastModifiedBy` on the DTO. Don't try to filter or store on a `DeletedBy` column; it doesn't exist.

- **Assuming a global filter exists.** If you have used other EF codebases, you may expect `HasQueryFilter` to hide deleted rows automatically, and `IgnoreQueryFilters()` to reveal them. Neither is used here. The trash screens and the purge see deleted rows simply by querying `Deleted == true` — there is nothing to "ignore."

- **Cascade gaps between soft and hard delete.** A soft delete intentionally leaves related history intact, but it still tidies up *live* relationships immediately (e.g., a soft-deleted user is pulled out of upcoming appointments right away). A hard/immediate delete does much more — rewriting `*By` references to display names and removing join rows. If you add a new entity with relationships, mirror the existing delete method: handle both the soft path (flag + minimal relationship cleanup) and the immediate path (full cleanup) so the purge later can finish the job cleanly.

- **`DeletedAt` is UTC.** It is stamped with `DateTime.UtcNow`. The purge cutoff (`now.AddDays(-days)`) is also UTC. Don't compare it against local time, or your "older than 90 days" math will drift by your timezone offset.

- **Immediate mode means no recycle bin.** If a tenant's `DeletePreference` is `Immediate`, deletes are permanent the moment they happen — nothing lands in the Deleted Records screen, and there is nothing to undelete. Confirm the tenant's preference before assuming a delete is recoverable.

---

<a id="related-docs"></a>
## 8. Related Docs

- [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md) — the full stack overview
- [026 — The Standard Pass/Fail Result](026_standard-result.md) — operations return the result type

---
*GuidesV2 · 025 · drafted from source (CRM.EFModels, CRM.DataAccess, BackgroundProcessor) · 2026-06-05.*

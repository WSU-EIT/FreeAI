# 021 — Anatomy of the Layered Data Stack

> **Document ID:** 021  ·  **Category:** Reference  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Trace a single entity from DTO through the data-access layer and controllers down to its EF model.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 02x (The Data Stack) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Layers Matter](#why-layers) | Plain-language definitions of DTO, data-access, and EF model, and why they are kept apart |
| 2 | [The Layer Map](#layer-map) | The four projects, what each one owns, and where the boundaries fall |
| 3 | [The DTO](#dto) | `DataObjects.Tag` — the shape that crosses the wire and its `ActionResponse` contract |
| 4 | [Controller Layer](#controller) | `DataController` endpoints that receive and return the DTO |
| 5 | [Data-Access Layer](#data-access) | `DataAccess` methods that map between DTO and entity and apply the rules |
| 6 | [The EF Model](#ef-model) | The `Tag` entity, its `DbSet`, and how it maps to a database table |
| 7 | [End-to-End Trace](#trace) | One `GetTag` call followed through all four layers, line by line |
| 8 | [Pitfalls & Conventions](#pitfalls) | Two-Tag confusion, soft-delete, leakage, and naming rules |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-layers"></a>
## 1. Why Layers Matter

**Why this matters first:** almost every feature in FreeCRM is the same shape repeated — a thing comes in from the browser, gets checked and reshaped, lands in a database table, and travels back out. If you understand that shape once, you can read any feature in the codebase. This doc teaches the shape using one small entity, the **Tag**, and follows it through every layer.

Three terms you will see constantly. Plain-language definitions first:

- **DTO** — *Data Transfer Object*. A plain class whose only job is to carry data between two places (here, between the browser and the server). It has properties but no real behavior. In FreeCRM the DTOs live in the `CRM.DataObjects` project. Think of a DTO as the **shipping box**: it is shaped for the journey, not for storage.
- **EF model** — the *Entity Framework* model. Entity Framework Core ("EF Core") is Microsoft's library that turns C# classes into database rows and back. An EF model class is a one-to-one picture of a database **table**: one property per column. These live in `CRM.EFModels`. Think of the EF model as the **warehouse shelf**: shaped for permanent storage.
- **Data-access layer** — the code in the middle that opens the box, decides what is allowed, copies values onto the shelf (or off it), and packs a fresh box for the trip home. In FreeCRM this is the `CRM.DataAccess` project, and its main class is `DataAccess`.

**Why keep them separate instead of using one class everywhere?** Because the box and the shelf have different jobs and different audiences:

- The **DTO** can carry display-friendly extras the database does not store. For example, the `Tag` DTO carries an `ActionResponse` object that reports "did this operation succeed, and if not, why" — that never goes in the database.
- The **EF model** can carry storage-only details the browser should never see — navigation links to other tables, foreign keys, columns you do not want to expose.
- Keeping them apart means a change to the database schema does not automatically change the public API the browser depends on, and vice versa. The data-access layer is the single, deliberate translation point between the two.

That deliberate translation — copying field by field from entity to DTO and back — is the heartbeat of this entire stack. The rest of this doc shows exactly where it happens.

---

<a id="layer-map"></a>
## 2. The Layer Map

FreeCRM splits these responsibilities across four C# projects. Each has one job, and they reference each other in one direction only (top depends on those below it):

```
Browser (HTTP request: GET /api/Data/GetTag/{id})
   │
   ▼
┌─────────────────────────────────────────────────────────────┐
│ CRM/Controllers           DataController (partial)           │  ← the web surface
│   namespace CRM.Server.Controllers                           │     accepts/returns DTOs
│   [ApiController] : ControllerBase                           │
└─────────────────────────────────────────────────────────────┘
   │ calls da.GetTag(...)
   ▼
┌─────────────────────────────────────────────────────────────┐
│ CRM.DataAccess           DataAccess (partial) : IDataAccess  │  ← the rules + translation
│   namespace CRM                                              │     maps entity ⇄ DTO
└─────────────────────────────────────────────────────────────┘
   │ reads/writes via the `data` DbContext            │ returns
   ▼                                                  ▼
┌──────────────────────────────┐      ┌──────────────────────────────┐
│ CRM.EFModels                 │      │ CRM.DataObjects              │
│   Tag entity (one per table) │      │   DataObjects.Tag (the DTO)  │
│   EFDataModel : DbContext    │      │   namespace CRM              │
│   namespace CRM.EFModels...  │      │                              │
└──────────────────────────────┘      └──────────────────────────────┘
        the warehouse shelf                    the shipping box
```

The boundaries, stated as rules:

- **Controllers** know about HTTP (routes, verbs, authorization) and about DTOs. They do **not** touch the database directly. They hold a field `da` of type `IDataAccess` and delegate every real operation to it.
- **`DataAccess`** knows about both worlds. It is the *only* layer allowed to hold the EF `DbContext` (a field named `data`) and the *only* layer that copies values between a `Tag` entity and a `DataObjects.Tag` DTO.
- **`CRM.EFModels`** knows only about the database. Its classes have no idea a web server exists.
- **`CRM.DataObjects`** knows only about shape. Its classes have no idea a database exists.

One subtlety worth flagging now: both `CRM.DataObjects` and `CRM.DataAccess` use the namespace `CRM`, while the EF models live in `CRM.EFModels.EFModels`. So inside `DataAccess`, the bare name `Tag` means the **EF entity**, and `DataObjects.Tag` means the **DTO**. That naming collision is intentional and is the single most common source of confusion — see [Pitfalls](#pitfalls).

A recurring detail across these files: most classes are declared `partial`. A **partial class** is one C# class whose source is split across many files; the compiler stitches them together. `DataAccess`, `DataController`, `DataObjects`, and `EFDataModel` are each one logical class spread over dozens of files (one per feature area, e.g. `DataAccess.Tags.cs`). When you work on a feature, you touch that feature's slice of each partial class.

---

<a id="dto"></a>
## 3. The DTO

**Why it matters:** the DTO is the contract. Whatever properties it has are exactly what the browser can send and receive. Read the DTO and you know the API.

The Tag DTO lives in `c:\Users\pepkad\source\repo2\FreeCRM\CRM.DataObjects\DataObjects.Tags.cs`. Here it is, copied faithfully:

```csharp
namespace CRM;

public partial class DataObjects
{
    public partial class Tag : ActionResponseObject
    {
        public Guid TagId { get; set; }
        public Guid TenantId { get; set; }
        public string? Name { get; set; }
        public string? Style { get; set; }
        public bool Enabled { get; set; }
        public bool UseInAppointments { get; set; }
        public bool UseInEmailTemplates { get; set; }
        public bool UseInServices { get; set; }
        public DateTime Added { get; set; }
        public string? AddedBy { get; set; }
        public DateTime LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
```

Things to notice, with the "why":

- It is a **nested class**: `Tag` lives inside the outer `DataObjects` class, which is why every reference elsewhere is `DataObjects.Tag`. The `DataObjects` outer class is a container that groups every DTO in the system; it has no behavior of its own.
- It **inherits `ActionResponseObject`**. That base class adds one property, and that property is the whole reason DTOs are richer than entities:

  ```csharp
  public partial class ActionResponseObject
  {
      public BooleanResponse ActionResponse { get; set; } = new BooleanResponse();
  }
  ```

  `BooleanResponse` is FreeCRM's universal pass/fail envelope:

  ```csharp
  public partial class BooleanResponse
  {
      public List<string> Messages { get; set; } = new List<string>();
      public bool Result { get; set; }
  }
  ```

  So every Tag the server returns can also say `ActionResponse.Result = true/false` and carry human-readable `Messages` explaining what happened. The database table has no such column — this is a DTO-only concern. (The standard result type gets its own treatment in [doc 026](026_standard-result.md).)

- The string properties are **nullable** (`string?`). The DTO is permissive about what the browser sends; the data-access layer is where missing or oversized values get cleaned up.
- `TenantId` rides along on the DTO because FreeCRM is **multi-tenant** — many customers share one database, and every record is stamped with the tenant it belongs to. The Tag belongs to exactly one tenant.

Compare this to the EF entity in [section 6](#ef-model): same business fields, but the entity has *no* `ActionResponse`, and instead has a `TagItems` collection the DTO does not. That difference is the box-versus-shelf distinction made concrete.

---

<a id="controller"></a>
## 4. Controller Layer

**Why it matters:** the controller is the front door. It maps a URL and an HTTP verb to a C# method, enforces who is allowed in, and hands the work to the data-access layer. It contains almost no logic of its own — that is by design.

The Tag endpoints live in `c:\Users\pepkad\source\repo2\FreeCRM\CRM\Controllers\DataController.Tags.cs`. The whole file:

```csharp
public partial class DataController
{
    [HttpGet]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/DeleteTag/{id}")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteTag(Guid id)
    {
        var output = await da.DeleteTag(id, CurrentUser);
        return Ok(output);
    }

    [HttpGet]
    [Authorize]
    [Route("~/api/Data/GetTag/{id}")]
    public async Task<ActionResult<DataObjects.Tag>> GetTag(Guid id)
    {
        var output = await da.GetTag(id, CurrentUser);
        return Ok(output);
    }

    [HttpGet]
    [Authorize]
    [Route("~/api/Data/GetTags")]
    public async Task<ActionResult<List<DataObjects.Tag>>> GetTags()
    {
        var output = await da.GetTags(CurrentUser.TenantId, CurrentUser);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/SaveTag")]
    public async Task<ActionResult<DataObjects.Tag>> SaveTag(DataObjects.Tag tag)
    {
        var output = await da.SaveTag(tag, CurrentUser);
        return Ok(output);
    }
}
```

How to read this, term by term:

- The **attributes** in square brackets are instructions to ASP.NET Core, the web framework:
  - `[HttpGet]` / `[HttpPost]` — which HTTP verb triggers this method.
  - `[Route("~/api/Data/GetTag/{id}")]` — the URL pattern. `{id}` is a placeholder filled from the URL and passed into the `Guid id` parameter.
  - `[Authorize]` — the caller must be logged in. `[Authorize(Policy = Policies.Admin)]` is stricter: only administrators. Notice **reads** (`GetTag`, `GetTags`) require any logged-in user, while **mutations** (`SaveTag`, `DeleteTag`) require admin. Authorization is a controller concern, decided here at the door.
- Each method is a one-liner of real work: call the matching method on `da` (the injected `IDataAccess`), then wrap the result in `Ok(...)`, which is the HTTP `200 OK` response carrying the DTO as JSON.
- `CurrentUser` is passed into every call. This is the logged-in user, resolved once in the `DataController` constructor from the request's token (see `DataController.cs`). The data-access layer needs it to make permission decisions — for example, whether to include soft-deleted records.
- `SaveTag` is the one that takes a DTO **in** (`DataObjects.Tag tag`). ASP.NET Core deserializes the incoming JSON body into that DTO automatically. Everything else takes only an id.

The class is `partial class DataController` here; the constructor, dependency injection, and the `da` / `CurrentUser` fields live in the base file `DataController.cs`, where the class is declared `[ApiController]` and `: ControllerBase` (the ASP.NET base class that gives helpers like `Ok(...)`). Controllers are covered in depth in [doc 024](024_api-controllers.md).

---

<a id="data-access"></a>
## 5. Data-Access Layer

**Why it matters:** this is where everything actually happens — permission checks, the database query, the field-by-field translation between entity and DTO, soft-delete rules, and the success/failure envelope. If a feature has a bug, it is almost always here.

The Tag logic lives in `c:\Users\pepkad\source\repo2\FreeCRM\CRM.DataAccess\DataAccess.Tags.cs`. The file declares two things: the **interface** methods (the contract controllers depend on) and the **implementation**.

```csharp
public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> DeleteTag(Guid TagId, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false);
    Task<DataObjects.Tag> GetTag(Guid TagId, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.Tag>> GetTags(Guid TenantId, DataObjects.User? CurrentUser = null);
    Task<DataObjects.Tag> SaveTag(DataObjects.Tag tag, DataObjects.User? CurrentUser = null);
}
```

The **interface** (`IDataAccess`) is the abstract list of capabilities; the controller only ever sees this interface, never the concrete class. That is what lets the controller stay ignorant of the database. The methods accept and return DTOs (`DataObjects.Tag`), never entities — the entity type never escapes this project.

The read path, `GetTag`, shows the translation clearly:

```csharp
public async Task<DataObjects.Tag> GetTag(Guid TagId, DataObjects.User? CurrentUser = null)
{
    var output = new DataObjects.Tag();

    Tag? rec = null;

    if (AdminUser(CurrentUser)) {
        rec = await data.Tags.FirstOrDefaultAsync(x => x.TagId == TagId);
    } else {
        rec = await data.Tags.FirstOrDefaultAsync(x => x.TagId == TagId && x.Deleted != true);
    }

    if (rec != null) {
        output = new DataObjects.Tag {
            ActionResponse = GetNewActionResponse(true),
            TagId = rec.TagId,
            TenantId = rec.TenantId,
            Name = rec.Name,
            Style = rec.Style,
            // ... every business field copied across ...
            Deleted = rec.Deleted,
            DeletedAt = rec.DeletedAt,
        };
    } else {
        output.ActionResponse.Messages.Add("Tag '" + TagId.ToString() + "' No Longer Exists");
    }

    return output;
}
```

What to take from this:

- `data` is the **`DbContext`** — the live database session, of type `EFDataModel`. `data.Tags` is the table; `FirstOrDefaultAsync(...)` is a LINQ query that EF Core turns into SQL and runs against whichever database is configured. The result, `rec`, is a `Tag` **entity** (note: the bare name, so the EF type).
- The permission branch is the rule made visible: admins can see everything; everyone else only sees records where `Deleted != true`. Soft-deleted rows are hidden, not gone (more in [section 8](#pitfalls)).
- The big object initializer is **the translation**: each entity field is copied onto a brand-new `DataObjects.Tag`. This is the box-packing step. It is *mostly* a verbatim field-by-field copy, with one twist: the audit fields aren't copied raw. `AddedBy` and `LastModifiedBy` are passed through `LastModifiedDisplayName(...)`, which takes the stored user-ID GUID and resolves it into a human-readable display name (falling back to the stored value if it isn't a known user). So what lands on the DTO for those two fields is a name, not the raw id. `GetNewActionResponse(true)` stamps a successful envelope:

  ```csharp
  public DataObjects.BooleanResponse GetNewActionResponse(bool result = false, string? message = null)
  {
      DataObjects.BooleanResponse output = new DataObjects.BooleanResponse {
          Result = result,
          Messages = new List<string>()
      };
      // ...
      return output;
  }
  ```

- If nothing was found, the method still returns a `DataObjects.Tag` — just one whose `ActionResponse.Result` is false (the default) with a "No Longer Exists" message. **It never returns null.** The caller always gets a well-formed envelope it can inspect.

The write path, `SaveTag`, runs the translation in reverse and adds the create-vs-update decision:

```csharp
public async Task<DataObjects.Tag> SaveTag(DataObjects.Tag tag, DataObjects.User? CurrentUser = null)
{
    var output = tag;
    output.ActionResponse = GetNewActionResponse();

    bool newRecord = false;
    DateTime now = DateTime.UtcNow;

    var rec = await data.Tags.FirstOrDefaultAsync(x => x.TagId == output.TagId);

    if (rec == null) {
        if (output.TagId == Guid.Empty) {
            newRecord = true;
            output.TagId = Guid.NewGuid();
            rec = new Tag {
                TagId = output.TagId,
                TenantId = output.TenantId,
                Deleted = false,
                Added = now,
                AddedBy = CurrentUserIdString(CurrentUser),
            };
        } else {
            output.ActionResponse.Messages.Add("Tag '" + output.TagId.ToString() + "' No Longer Exists");
            return output;
        }
    }

    output.Name = MaxStringLength(output.Name, 200);

    rec.Name = output.Name;
    rec.Style = output.Style;
    rec.Enabled = output.Enabled;
    // ... copy the rest of the editable fields onto the entity ...
    rec.LastModified = now;
    rec.LastModifiedBy = CurrentUserIdString(CurrentUser);

    try {
        if (newRecord) {
            await data.Tags.AddAsync(rec);
        }
        await data.SaveChangesAsync();
        output.ActionResponse.Result = true;
        // ... SignalRUpdate broadcast ...
    } catch (Exception ex) {
        output.ActionResponse.Messages.Add("Error Saving Tag " + output.TagId.ToString());
        output.ActionResponse.Messages.AddRange(RecurseException(ex));
    }

    return output;
}
```

Key moves, with the "why":

- **New vs. existing** is decided by looking up the id. If no row matches and the incoming `TagId` is `Guid.Empty`, it is a create: a fresh `Guid` is generated, a new `Tag` entity is built, and `Added`/`AddedBy` are stamped. If the id is non-empty but no row matches, the record was deleted out from under the caller, so it bails with a message.
- **`MaxStringLength(output.Name, 200)`** trims the name to the column's maximum. The DTO accepted any-length string; the data-access layer enforces the storage limit. This is exactly the cleanup the permissive DTO defers.
- **`data.SaveChangesAsync()`** is the moment EF Core writes the in-memory changes to the actual database, as an `INSERT` or `UPDATE`. Nothing is persisted before this call.
- The whole write is wrapped in `try/catch`; any failure is reported through `ActionResponse.Messages` rather than thrown to the browser. `RecurseException` flattens nested exception messages into the list.
- On success it calls `SignalRUpdate(...)`, which pushes a real-time notification so other connected users see the change immediately. That is a side effect the data-access layer owns, not the controller.

`DataAccess` is partial: `data`, the constructor, and shared helpers like `GuidValue`, `AdminUser`, and `GetNewActionResponse` live in other files (`DataAccess.cs`, `DataAccess.Utilities.cs`). This layer is the subject of [doc 023](023_partial-data-access.md).

---

<a id="ef-model"></a>
## 6. The EF Model

**Why it matters:** the EF model is the database, expressed as C#. It defines what columns the `Tag` table has, their types and sizes, and how the table relates to others. Change this and you change the schema.

The entity lives in `c:\Users\pepkad\source\repo2\FreeCRM\CRM.EFModels\EFModels\Tag.cs`:

```csharp
namespace CRM.EFModels.EFModels;

public partial class Tag
{
    public Guid TagId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Style { get; set; }
    public bool Enabled { get; set; }
    public bool UseInAppointments { get; set; }
    public bool UseInEmailTemplates { get; set; }
    public bool UseInServices { get; set; }
    public DateTime Added { get; set; }
    public string? AddedBy { get; set; }
    public DateTime LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool Deleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<TagItem> TagItems { get; set; } = new List<TagItem>();
}
```

Compare it to the DTO from [section 3](#dto):

- **Same business fields** (`Name`, `Style`, `Enabled`, the audit columns, the soft-delete columns). This is why the translation in [section 5](#data-access) is mostly a straight field-by-field copy — the one exception being the audit fields `AddedBy` and `LastModifiedBy`, which on the read path are run through `LastModifiedDisplayName(...)` to turn a stored user-ID GUID into a display name rather than copied verbatim.
- **No `ActionResponse`.** The entity is storage only; pass/fail envelopes are a transport concern.
- **An extra `TagItems` navigation property.** This is a **navigation property**: an in-memory link to related rows (the join rows that attach this tag to appointments, services, or email templates). It is marked `virtual` so EF Core can load it lazily. The DTO has no equivalent because the browser does not need the raw join rows. Navigation properties are storage-only and must never leak into a DTO.
- `Name` is non-nullable here (`= null!`) but nullable on the DTO — the database requires a value; the DTO tolerates its absence and the data-access layer fills the gap.

**How a C# class becomes a table.** The entity is just properties; the mapping is configured separately in the `DbContext`, `EFDataModel`, in `EFModels\EFDataModel.cs`. Two relevant pieces. First, the table is exposed as a `DbSet` — the collection name (`data.Tags`) you saw queried in the data-access layer:

```csharp
public virtual DbSet<Tag> Tags { get; set; }
public virtual DbSet<TagItem> TagItems { get; set; }
```

Second, `OnModelCreating` describes column details with the *fluent API* (chained configuration calls):

```csharp
modelBuilder.Entity<Tag>(entity =>
{
    entity.Property(e => e.TagId).ValueGeneratedNever();
    entity.Property(e => e.Added).HasColumnType("datetime");
    entity.Property(e => e.AddedBy).HasMaxLength(100);
    entity.Property(e => e.DeletedAt).HasColumnType("datetime");
    entity.Property(e => e.LastModified).HasColumnType("datetime");
    entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
    entity.Property(e => e.Name).HasMaxLength(200);
});

modelBuilder.Entity<TagItem>(entity =>
{
    entity.Property(e => e.TagItemId).ValueGeneratedNever();

    entity.HasOne(d => d.Tag).WithMany(p => p.TagItems)
        .HasForeignKey(d => d.TagId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("FK_TagItems_Tags");
});
```

What this tells you:

- `ValueGeneratedNever()` on `TagId` means the database does **not** auto-generate the key — the application supplies the `Guid` itself (recall `output.TagId = Guid.NewGuid()` in `SaveTag`).
- `HasMaxLength(200)` on `Name` is the very limit `MaxStringLength(output.Name, 200)` enforces in the data-access layer. The two numbers must agree, and they do.
- The `TagItem` block defines the **relationship**: each `TagItem` `HasOne` `Tag`, which `WithMany` `TagItems`, joined on `TagId`. That is the foreign key the `TagItems` navigation property rides on.

`EFDataModel` is itself a partial class. A second slice, `EFModelOverrides.cs`, adapts GUID storage per database provider — on SQLite, MySQL, and PostgreSQL it converts every `Guid` to a string, because those providers store GUIDs as text. That is why the same entity works unchanged across SQL Server, SQLite, MySQL, and PostgreSQL. The EF model layer, including soft-delete, is the subject of [doc 025](025_ef-models-soft-delete.md).

---

<a id="trace"></a>
## 7. End-to-End Trace

Putting it together: one `GetTag` request, followed all the way down and back. Imagine the browser asks for the tag with id `a1b2…`.

1. **Browser → HTTP.** The browser issues `GET /api/Data/GetTag/a1b2...` with the user's auth token in the request headers.

2. **Controller (`DataController.Tags.cs`).** ASP.NET matches the route `~/api/Data/GetTag/{id}`, binds `a1b2…` into the `Guid id` parameter, and checks `[Authorize]` (must be logged in). The constructor in `DataController.cs` has already turned the token into a `CurrentUser`. The method runs its one line:

   ```csharp
   var output = await da.GetTag(id, CurrentUser);
   return Ok(output);
   ```

   It calls the interface, not the database.

3. **Data-access (`DataAccess.Tags.cs`).** `GetTag` runs. It checks `AdminUser(CurrentUser)` to decide whether soft-deleted rows are visible, then queries the database through the `DbContext`:

   ```csharp
   rec = await data.Tags.FirstOrDefaultAsync(x => x.TagId == TagId && x.Deleted != true);
   ```

4. **EF model → SQL.** EF Core translates that LINQ into a `SELECT` against the `Tags` table mapped in `EFDataModel`, runs it, and materializes the matching row into a `Tag` **entity** (`rec`). If the provider is SQLite/MySQL/PostgreSQL, the GUID converter from `EFModelOverrides.cs` handles the id comparison transparently.

5. **Translation (back up in `DataAccess`).** With `rec` in hand, the method packs a fresh `DataObjects.Tag`, copying each business field across and stamping `ActionResponse = GetNewActionResponse(true)`. The entity stays behind in this layer; only the DTO travels onward. If no row matched, it instead returns an empty DTO whose `ActionResponse.Result` is false with a "No Longer Exists" message — never null.

6. **Controller → HTTP.** `Ok(output)` serializes the `DataObjects.Tag` to JSON and returns `200 OK`.

7. **Browser.** The browser receives the JSON, including the `ActionResponse` envelope, and can render the tag or surface any message.

The same trip in reverse describes `SaveTag`: JSON body → `DataObjects.Tag` → controller passes it to `da.SaveTag` → data-access decides create-vs-update, trims the name, copies DTO fields onto a `Tag` entity, calls `SaveChangesAsync()` (the `INSERT`/`UPDATE`), broadcasts via SignalR, and returns the same DTO with `ActionResponse.Result = true`.

For a UI-first version of this same journey — starting from a button click in a Blazor component rather than a raw HTTP call — see [doc 017](017_click-to-database.md).

---

<a id="pitfalls"></a>
## 8. Pitfalls & Conventions

**The two-Tag trap.** There are two types named `Tag`: the DTO `DataObjects.Tag` (namespace `CRM`) and the entity `CRM.EFModels.EFModels.Tag`. Inside `DataAccess`, the bare name `Tag` resolves to the **entity** and `DataObjects.Tag` to the **DTO**. Mixing them up is the classic mistake. Rule of thumb: the variable holding a database row is conventionally named `rec` (an entity); the variable being returned to the caller is `output` (a DTO).

**Soft delete is not deletion.** By default, deleting a Tag does not remove the row. `DeleteTag` checks the tenant's `DeletePreference`: only when it is `Immediate` (or `ForceDeleteImmediately` is passed) does it call `data.Tags.Remove(rec)`. Otherwise it sets `rec.Deleted = true` and `rec.DeletedAt = now` and saves. Consequences you must respect:
- Non-admin queries must filter `x.Deleted != true` (every Tag read does). Forgetting that filter leaks "deleted" records back into view.
- A deleted record still occupies its id, so attempts to re-save it are gated by an admin check.

**Never leak entity-only members into a DTO.** The `TagItems` navigation property exists only on the entity. Copying it (or a raw foreign key, or the live `DbContext`) onto a DTO breaks the boundary, can trigger surprise database loads during JSON serialization, and exposes storage internals to the browser. Translation is always field-by-field of *business* values, exactly as `GetTag` does it.

**Never return null from a data-access method.** Every method returns a fully-formed DTO (or list). Failure is reported through `ActionResponse.Result` and `Messages`, produced by `GetNewActionResponse(...)`. Callers should branch on `Result`, not on null-checks.

**Keep the size limits in sync.** `Name` is capped at 200 in two places: `HasMaxLength(200)` in `EFDataModel` and `MaxStringLength(output.Name, 200)` in `SaveTag`. If you change the column, change both.

**Naming and file conventions.**
- One feature, one slice per partial class: `DataObjects.Tags.cs`, `DataController.Tags.cs`, `DataAccess.Tags.cs`, plus `EFModels\Tag.cs`. Find the feature, find these four files.
- Data-access methods follow `Verb + Entity`: `GetTag`, `GetTags`, `SaveTag`, `DeleteTag` — a single `Save` handles both create and update.
- Controllers stay thin: authorize, call `da`, `Ok(...)`. Business logic belongs in `DataAccess`, not in the controller.
- Every tenant-scoped read filters by `TenantId`; multi-tenancy is enforced in the data-access layer, never assumed.

---

<a id="related-docs"></a>
## 9. Related Docs

- [017 — Following a Click to the Database](017_click-to-database.md) — the same journey from a UI click, end to end
- [022 — Shaping Records With Nested Partial DTOs](022_nested-partial-dtos.md) — the DTO layer
- [023 — Inside the Partial Data-Access Layer](023_partial-data-access.md) — the data-access layer
- [024 — API Controllers: The Tenant-Aware Request Surface](024_api-controllers.md) — the controller layer
- [025 — EF Models and the Records That Never Truly Vanish](025_ef-models-soft-delete.md) — the EF model layer
- [026 — The Standard Pass/Fail Result](026_standard-result.md) — the result type

---
*GuidesV2 021 · drafted from source.*

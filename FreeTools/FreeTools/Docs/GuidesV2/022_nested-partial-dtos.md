# 022 — Shaping Records With Nested Partial DTOs

> **Document ID:** 022  ·  **Category:** Reference  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Show how nested partial DTOs model and transport data while keeping app and framework fields apart.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 02x (The Data Stack) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why Nested Partial DTOs Matter](#why-it-matters) | What a DTO, a "partial" class, and "nesting" mean, and why this design pays off |
| 2 | [The Partial DTO Building Block](#partial-block) | The anatomy of one real DTO inside `public partial class DataObjects` |
| 3 | [App Fields vs Framework Fields](#field-separation) | The `.App.cs` split and the `ActionResponseObject` base that keeps your fields yours |
| 4 | [Nesting DTOs to Model Relationships](#nesting) | How `Appointment` composes `Notes`, `Services`, and `Users` into one shape |
| 5 | [Transporting Data Across Boundaries](#transport) | How these DTOs cross the wire as JSON through API controllers |
| 6 | [Reference Example](#example) | An annotated, end-to-end walk through the real `Appointment` DTO |
| 7 | [Pitfalls and Edge Cases](#pitfalls) | Real traps: nulls, defaults, the `[Sensitive]` attribute, and module markers |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Nested Partial DTOs Matter

Before any code, three plain-language definitions, because the rest of this doc leans on them.

**DTO** stands for *Data Transfer Object*. It is a plain class whose only job is to *carry data* from one place to another — for example, from the database layer up to a web page, or from a browser back down to the server. A DTO has no clever behavior; it is a labeled container. In FreeCRM/FreeTools, every DTO lives as a nested class inside one big outer class named `DataObjects`, in the `CRM` namespace. So you refer to them as `DataObjects.User`, `DataObjects.Appointment`, and so on.

**Partial** is a C# keyword. A `partial class` is one class whose definition is *split across several files*. The compiler stitches all the pieces back into a single class. You will see `public partial class DataObjects` at the top of roughly a dozen different `.cs` files — `DataObjects.cs`, `DataObjects.Appointments.cs`, `DataObjects.Invoices.cs`, and more — yet they all describe *the same* `DataObjects` class. Why split it? So that related DTOs sit together (all appointment shapes in one file, all invoice shapes in another) without forcing one giant unreadable file.

**Nesting** here means two things at once, and it helps to keep them straight:
1. The DTO classes are nested *inside* `DataObjects` (that is the outer container).
2. A DTO can hold *other DTOs* as properties — for example, an `Appointment` holds a `List<AppointmentNote>`. That is a DTO nested inside a DTO, and it is how the code models real-world relationships ("an appointment *has* notes").

**Why this matters.** The payoff is separation and safety. Because every DTO lives in the same `DataObjects` family, there is one obvious place to look for "the shape of a user" or "the shape of an appointment." Because the classes are `partial`, the framework can ship its own fields while you add yours in a separate file that upgrades never overwrite (see section 3). And because DTOs nest, a single object — say, one appointment with its notes, services, and attendees — travels across the network as one tidy JSON payload instead of five disconnected calls. The reader's takeaway: this is the load-bearing data design of the whole app, and it is deliberately boring so the data is predictable.

---

<a id="partial-block"></a>
## 2. The Partial DTO Building Block

Every DTO follows the same simple recipe. Open `CRM.DataObjects/DataObjects.cs` and you see the outer shell first:

```csharp
namespace CRM;

public partial class DataObjects
{
    public partial class User : ActionResponseObject
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        // ...many more properties...
    }
}
```

Read that from the outside in:

- `namespace CRM;` — the family name. Everything in the data-objects project lives under `CRM`.
- `public partial class DataObjects` — the outer container. The word `partial` tells you this file holds only *part* of `DataObjects`; the rest is in sibling files.
- `public partial class User` — one DTO. It too is `partial`, which is the hook that lets you extend it later (section 3).
- `: ActionResponseObject` — `User` *inherits from* a base class. Inheriting means `User` automatically gets every field the base defines, for free. That base is where the framework's "did this operation succeed?" plumbing lives.
- The properties are auto-properties: `public string? FirstName { get; set; }`. The `?` after `string` means the value is *nullable* — it is allowed to be empty/`null`. A `Guid` (a globally unique 128-bit identifier, the app's standard primary key) without a `?` is never null; it defaults to all-zeros.

**Why it is built this way.** A DTO is intentionally dumb: just `get; set;` properties. That makes it trivial to create, copy, and turn into JSON. The intent is clarity, not cleverness. When you need "the data the app means by a user," you read `DataObjects.User` top to bottom and you have the complete contract — no hidden logic to chase.

One more recurring detail worth naming now: default initializers. Collection and object properties are usually given a starting value so they are never `null` by surprise, e.g. `public List<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();`. That habit prevents a whole category of null-reference crashes (more in section 7).

---

<a id="field-separation"></a>
## 3. App Fields vs Framework Fields

This is the section that makes the `partial` keyword earn its keep. There are two kinds of "separation" happening, and both protect *your* fields.

**Separation #1 — your file vs the framework's files.** FreeCRM/FreeTools is a template you build on top of. When a new version of the template ships, it overwrites the framework's own files. To keep your custom fields from being wiped out, the project gives you a dedicated file: `CRM.DataObjects/DataObjects.App.cs`. Its header literally says so:

```csharp
namespace CRM;

// Use this file as a place to put any application-specific data objects.

public partial class DataObjects
{
    public partial class User
    {
        //public string? MyCustomUserProperty { get; set; }
    }
}
```

Because `User` is `partial`, the version here and the version in `DataObjects.cs` are *the same class*. You uncomment that line, add your property, and now `DataObjects.User` has your field — without you ever touching the framework's copy. Upgrades replace `DataObjects.cs`; they leave `DataObjects.App.cs` (your file) alone. That is the "naming law" that keeps your code yours, covered in depth in [042](042_file-naming-law.md).

**Separation #2 — domain fields vs framework plumbing fields.** Inside a single DTO, some fields describe the business (a user's `Email`, an appointment's `Start` time) and some fields are framework machinery (did the save succeed? what messages came back?). The plumbing is *not* repeated on every DTO; it is inherited from a tiny base class found in `DataObjects.cs`:

```csharp
public partial class ActionResponseObject
{
    public BooleanResponse ActionResponse { get; set; } = new BooleanResponse();
}

public partial class BooleanResponse
{
    public List<string> Messages { get; set; } = new List<string>();
    public bool Result { get; set; }
}
```

Any DTO declared as `: ActionResponseObject` (such as `User`, `Appointment`, `Invoice`, `Tenant`, `FileStorage`) automatically carries an `ActionResponse` — a pass/fail flag plus a list of human-readable `Messages`. So a method can return a saved `Appointment` *and* report success/failure on the very same object. The standard result type is detailed in [026](026_standard-result.md).

**Why it matters.** The domain fields tell you *what the thing is*; the inherited `ActionResponse` tells you *how the last operation on it went*. Keeping the plumbing in a base class means it is defined once, behaves identically everywhere, and never clutters the business fields you actually read.

---

<a id="nesting"></a>
## 4. Nesting DTOs to Model Relationships

Real records are not flat. An appointment is not just a title and a time — it has notes attached, services rendered, and people invited. Nested DTOs model exactly those "has-many" relationships by giving a parent DTO properties that are *lists of child DTOs*.

Here is the real `Appointment` from `CRM.DataObjects/DataObjects.Appointments.cs`, trimmed to the nesting:

```csharp
public partial class Appointment : ActionResponseObject
{
    public Guid AppointmentId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Title { get; set; } = String.Empty;

    public List<AppointmentNote> Notes { get; set; } = new List<AppointmentNote>();
    public List<AppointmentService> Services { get; set; } = new List<AppointmentService>();
    public List<AppointmentUser> Users { get; set; } = new List<AppointmentUser>();
    // {{ModuleItemStart:Invoices}}
    public List<Invoice> Invoices { get; set; } = new List<Invoice>();
    // {{ModuleItemEnd:Invoices}}
}
```

The three child types are themselves DTOs in the same file:

```csharp
public partial class AppointmentNote : ActionResponseObject { /* ... */ }
public partial class AppointmentService { /* ... */ }
public partial class AppointmentUser
{
    public Guid UserId { get; set; }
    public string AttendanceCode { get; set; } = "invited";
    public string DisplayName { get; set; } = String.Empty;
    public decimal Fees { get; set; }
}
```

Read it plainly: an `Appointment` *has many* `Notes`, *has many* `Services`, *has many* `Users`, and (when the Invoices module is on) *has many* `Invoices`. Each `List<...>` is initialized to an empty list, so a brand-new appointment already has empty-but-real collections — never `null`.

**Why nest instead of separate calls.** Two reasons. First, fidelity: the DTO mirrors the real relationship, so anyone reading `Appointment` understands the domain at a glance. Second, transport efficiency: the parent and all its children move together as a single object (next section). One fetch returns the appointment *and* its notes, services, and attendees, instead of four round-trips that could disagree with each other.

Note that not every child needs to be a heavyweight type. `AppointmentUser` is intentionally lean — just enough to render an attendee row (`DisplayName`, `AttendanceCode`, `Fees`) — rather than a full `User`. Nesting lets you choose a *purpose-built* child shape instead of dragging the entire related record along.

---

<a id="transport"></a>
## 5. Transporting Data Across Boundaries

A DTO's reason to exist is *movement*. The most important boundary it crosses is the network hop between the C# server and the browser, and that crossing happens as **JSON** — *JavaScript Object Notation*, a plain-text format of names and values that both sides understand.

The handoff lives in the API controllers (the classes that answer web requests). From `CRM/Controllers/DataController.Appointments.cs`:

```csharp
public async Task<ActionResult<DataObjects.Appointment>> GetAppointment(Guid id)
{
    var output = /* load the appointment */;
    return Ok(output);
}

public async Task<ActionResult<List<DataObjects.Appointment>>> GetAppointments(DataObjects.AppoinmentLoader loader)
{
    var output = /* load many */;
    return Ok(output);
}

public async Task<ActionResult<DataObjects.Appointment>> SaveAppointment(DataObjects.Appointment appointment)
{
    var output = /* save and return the saved shape */;
    return Ok(output);
}
```

A few facts the reader should take from this:

- The method *signature* is the contract. `ActionResult<DataObjects.Appointment>` says "this endpoint returns one appointment DTO." `ActionResult<List<DataObjects.Appointment>>` says "a list of them." The DTO type *is* the API's promise about the response shape.
- `return Ok(output)` hands the DTO to the web framework, which serializes it to JSON automatically. "Serialize" means *turn the in-memory object into text* so it can travel over the wire; the browser then *deserializes* (parses) that text back into an object.
- Nesting travels for free. Because `Appointment` contains its `Notes`, `Services`, and `Users` lists, serializing the appointment serializes the whole tree. The browser receives one nested JSON document, not four flat ones.
- DTOs flow *both* directions. `SaveAppointment(DataObjects.Appointment appointment)` *receives* a DTO posted from the browser, deserialized from JSON back into the same `DataObjects.Appointment` type. One class definition therefore governs both the request and the response — which is precisely why a single, well-shaped DTO is so valuable.

**Why it matters.** Because the same `DataObjects.Appointment` type is named on the server method, posted by the client, and serialized to JSON in between, all three layers agree on the shape by construction. Change the DTO once and every boundary updates together. That single source of truth is the entire point of routing all data through `DataObjects`.

---

<a id="example"></a>
## 6. Reference Example

Putting sections 2–5 together, here is one DTO that demonstrates every idea at once — the real `Appointment`, annotated. (Source: `CRM.DataObjects/DataObjects.Appointments.cs`.)

```csharp
namespace CRM;                                  // 1. the CRM family

public partial class DataObjects                // 2. outer container, split across files
{
    public partial class Appointment            // 3. one DTO, itself partial (extensible)
        : ActionResponseObject                  // 4. inherits ActionResponse pass/fail plumbing
    {
        public Guid AppointmentId { get; set; } // 5. domain id (never null; defaults to empty Guid)
        public Guid TenantId { get; set; }       //    every record is scoped to a tenant
        public DateTime Start { get; set; }      //    domain fields: what the appointment IS
        public DateTime End { get; set; }
        public string Title { get; set; } = String.Empty;  // 6. default so it is never null
        public string? Note { get; set; }                  //    nullable: may legitimately be empty

        public List<AppointmentNote> Notes        // 7. nested child DTOs: "has many notes"
            { get; set; } = new List<AppointmentNote>();    //    empty-but-real, never null
        public List<AppointmentService> Services
            { get; set; } = new List<AppointmentService>();
        public List<AppointmentUser> Users
            { get; set; } = new List<AppointmentUser>();

        // {{ModuleItemStart:Invoices}}            // 8. module marker: this block exists only
        public List<Invoice> Invoices             //    when the Invoices module is enabled
            { get; set; } = new List<Invoice>();
        // {{ModuleItemEnd:Invoices}}
    }
}
```

Trace what each annotation buys you:

1–2. The DTO is addressable as `DataObjects.Appointment` anywhere in the codebase, and it sits in the appointments file alongside its relatives because the class is `partial`.
3. `Appointment` being `partial` means you could add your own appointment field in `DataObjects.App.cs` without editing this file.
4. Inheriting `ActionResponseObject` gives the appointment a free `ActionResponse` so a save can return the record *and* its success/messages together.
5. `Guid` ids and the `TenantId` are the domain's spine — non-nullable, always present.
6. `= String.Empty` and the list initializers guarantee non-null defaults; `string? Note` deliberately allows null where emptiness is meaningful.
7. The three `List<...>` properties are the nesting from section 4 — an appointment carries its own notes, services, and attendees.
8. The `{{ModuleItemStart:...}}` / `{{ModuleItemEnd:...}}` comments are processing markers used to include or strip optional modules; the `Invoices` list only ships when that module is on.

The end-to-end story: a controller calls `GetAppointment`, the data-access layer fills this object (including its child lists), `return Ok(output)` serializes the whole tree to JSON, the browser renders it, the user edits it, and `SaveAppointment(DataObjects.Appointment appointment)` posts the same shape back. One class, every layer, both directions.

---

<a id="pitfalls"></a>
## 7. Pitfalls and Edge Cases

Concrete traps, each grounded in the real code:

- **Forgetting the default initializer on a collection.** A `List<T>` property without `= new List<T>()` defaults to `null`, and looping over it (or letting JSON deserialization skip it) becomes a null-reference crash. The codebase consistently initializes lists, e.g. `public List<AppointmentNote> Notes { get; set; } = new List<AppointmentNote>();`. Follow that convention for every collection you add.

- **Confusing "nullable" with "empty."** `string? Note` (with the `?`) may be `null`; `string Title { get; set; } = String.Empty` is non-nullable and starts blank. They behave differently when you check for "has a value." Read the `?` deliberately — it is a contract, not decoration.

- **Putting custom fields in the wrong file.** Add your fields in `DataObjects.App.cs` (your file), never in `DataObjects.cs` or the framework module files. The framework files can be overwritten on upgrade; your `.App.cs` is yours. This is the single most common way people lose their own work.

- **Leaking secrets through serialization.** Some fields are sensitive and tagged with the `[Sensitive]` attribute (defined as `public class SensitiveAttribute : System.Attribute { }` in `DataObjects.cs`). For example, in `TenantSettings`: `[Sensitive] public string? CustomAuthenticationCode { get; set; }` and the LDAP/JWT keys. Treat `[Sensitive]` fields as need-to-know — do not blindly ship a DTO full of them to the browser. When in doubt, project to a leaner shape (like the slim `UserListing` instead of full `User`).

- **Assuming a module's nested list is always there.** Anything wrapped in `// {{ModuleItemStart:X}} ... // {{ModuleItemEnd:X}}` (such as `Appointment.Invoices`, gated on the Invoices module) may be absent in a build where module X is disabled. Do not write code that hard-requires an optional nested branch.

- **Dragging the whole related record when a slim child will do.** Nesting tempts you to embed full DTOs everywhere. Note that `AppointmentUser` is a *purpose-built* slim attendee (`UserId`, `DisplayName`, `AttendanceCode`, `Fees`) rather than a full `DataObjects.User`. Prefer the smallest child shape that satisfies the view; it keeps payloads light and avoids leaking unrelated fields.

- **Editing one partial half and forgetting the other.** Because a class like `User` is split across `DataObjects.cs` and `DataObjects.App.cs`, search for *all* declarations of a DTO before assuming you have seen its full set of fields. Use a project-wide search for `class User` rather than trusting a single file.

---

<a id="related-docs"></a>
## 8. Related Docs

- [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md) — the full stack overview
- [042 — The Naming Law That Keeps Your Code Yours](042_file-naming-law.md) — the App naming that splits your fields
- [026 — The Standard Pass/Fail Result](026_standard-result.md) — a DTO every operation returns

---
*GuidesV2 022 · drafted from source ·  `CRM.DataObjects/DataObjects*.cs`.*

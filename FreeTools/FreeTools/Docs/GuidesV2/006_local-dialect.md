# 006 — Speaking the Local Dialect

> **Document ID:** 006  ·  **Category:** Onboarding  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Define the ecosystem vocabulary (tenant, wrapper, partial segregation, soft-delete, result type) and link each to its deep doc.
> **Audience:** Brand-new adopters  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 00x (Landing Zone: From Clone to Login) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why the Dialect Matters](#why-it-matters) | Plain-language tour defining every core term |
| 2 | [Tenant](#tenant) | The `Guid TenantId` that isolates every customer's data |
| 3 | [Wrapper](#wrapper) | The `Helpers.GetOrPost<T>` layer that hides HTTP plumbing |
| 4 | [Partial Segregation](#partial-segregation) | How `.App.` partial classes keep your code from being overwritten |
| 5 | [Soft-Delete](#soft-delete) | The `Deleted` / `DeletedAt` flags that hide records without erasing them |
| 6 | [Result Type](#result-type) | The `BooleanResponse` pass/fail object every operation returns |
| 7 | [Glossary & Deep-Doc Map](#glossary) | Quick-reference table linking terms to deep docs |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why the Dialect Matters

Every codebase has its own slang. If you join this one without it, you will read a method like `Helpers.GetOrPost<DataObjects.User>(...)` and have no idea whether it is talking to a database, a web server, or thin air. This doc is the phrasebook. Learn five words here and the rest of the code stops looking like a foreign language.

The whole platform is a multi-customer web application written in C# using **Blazor** (Microsoft's framework for building web apps in C# instead of JavaScript). Two .NET projects matter for this doc:

- **`CRM.DataObjects`** — a library, in the namespace `CRM`, that holds the plain data shapes (the "nouns" — users, tenants, settings). Everything lives inside one big `public partial class DataObjects`.
- **`CRM.Client`** — the Blazor front-end, in the namespace `CRM.Client`, that runs in the browser and talks to the server.

Here is the whole dialect in one breath, so the rest of the doc has somewhere to hang:

- A **tenant** is one customer account. The platform serves many customers from one running app, and a tenant is the wall between them so Customer A never sees Customer B's data.
- A **wrapper** is a helper method that hides ugly plumbing. Instead of every page writing raw web-request code, pages call one tidy helper (`GetOrPost<T>`) that does the boring, error-prone parts for them.
- **Partial segregation** is a code-organization trick. The framework's files and your custom files are kept physically separate (your code goes in files ending in `.App.`) so a framework upgrade can overwrite its files without touching yours.
- **Soft-delete** means "delete" usually does not erase anything. The record is flagged as gone (`Deleted = true`) and stamped with a time (`DeletedAt`), so it can be recovered or purged later.
- A **result type** is the standard answer an operation gives back: did it work, and if not, why? Here that answer is a small object called `BooleanResponse`.

Why this matters in one sentence: these five ideas show up on almost every screen and in almost every server call, so knowing them is the difference between editing this app with confidence and editing it by guesswork. Each section below defines one term and points you at the deep doc that covers it fully.

<a id="tenant"></a>
## 2. Tenant

**Why it matters:** A tenant is the most important word in the whole system. The app is *multi-tenant* — one deployment serves many separate customers — and the tenant is the boundary that keeps each customer's data private. Get tenant-awareness wrong and Customer A sees Customer B's records. Get it right and isolation is automatic.

**Plain definition:** A **tenant** is one isolated customer account. Think of an office building (the running app) divided into locked suites (tenants). Same building, same plumbing, but each suite has its own key.

In code, a tenant is identified by a single `Guid` (a globally unique ID) called `TenantId`. The `Tenant` itself is a data object in the `CRM` namespace:

```csharp
public partial class Tenant : ActionResponseObject
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string TenantCode { get; set; } = null!;
    public bool Enabled { get; set; }
    ...
    public TenantSettings TenantSettings { get; set; } = new TenantSettings();
}
```

Notice `TenantCode` (a short text handle used in URLs) and `TenantSettings` (per-customer configuration). Notice too that `Tenant` inherits `ActionResponseObject`, which means it carries a result object — see [Section 6](#result-type).

The tenant boundary is not just a database concept; it rides along on every server call. When the browser asks the server for data, the wrapper attaches the current tenant's ID as an HTTP header so the server knows whose data to return:

```csharp
Http.DefaultRequestHeaders.Add("TenantId", Model.User.TenantId.ToString());
```

(from `CRM.Client/Helpers.cs`, inside the `GetOrPost<T>` wrapper)

That single line is tenant isolation in action: the server trusts the request only within that tenant's data. **Deep doc:** [016 — Everything Knows Its Tenant](016_tenant-aware-thinking.md).

<a id="wrapper"></a>
## 3. Wrapper

**Why it matters:** Without a wrapper, every page that needs data would have to write the same dozen lines of web-request code — set headers, handle errors, parse the response, log failures — and any one of those pages could get it subtly wrong. A wrapper writes those lines once so the other 200 callers can ignore them.

**Plain definition:** A **wrapper** is a helper method that hides messy, repetitive plumbing behind a simple call. You hand it what you want; it does the boring part and hands back a clean result.

The central wrapper here is `GetOrPost<T>` in `CRM.Client/Helpers.cs`. Its signature reads like a promise — "give me a URL, optionally some data to send, and I'll give you back a `T`":

```csharp
public static async Task<T?> GetOrPost<T>(string url, object? post = null, bool logResults = false)
```

The `<T>` is a *generic type parameter*: you tell it what shape you expect back and it deserializes the server's JSON into exactly that. So a page that wants a user writes one readable line instead of a block of HTTP code:

```csharp
var loadedUser = await GetOrPost<DataObjects.User>("api/Data/GetUser/" + UserId.ToString());
```

Behind that one line, the wrapper quietly:

- clears and re-attaches request headers, including the `TenantId` from [Section 2](#tenant) and an auth `Token`;
- chooses `POST` if you passed data (`post != null`) or `GET` if you did not;
- checks `response.IsSuccessStatusCode` and logs a readable message on failure;
- catches exceptions so a network hiccup never crashes the page.

The name `GetOrPost` is literal: it does a GET or a POST depending on whether you handed it a payload. That is the wrapper philosophy — push the plumbing down so the calling code stays short and readable. **Deep doc:** [011 — Why We Wrap the Framework](011_wrapper-philosophy.md).

A close cousin of the wrapper is the **state container**, `BlazorDataModel` (in `CRM.Client/DataModel.cs`), the shared object every page reads from. Its own comment says it best:

```csharp
/// <summary>
/// The Model used on every page in the Blazor application to share database in the interface.
/// </summary>
public partial class BlazorDataModel
```

It holds the current `User`, `TenantId`, `View`, `LoggedIn` flag, and more. When one of those changes it raises an event so every screen refreshes itself:

```csharp
private void NotifyDataChanged() => OnChange?.Invoke();
```

**Deep doc for the state container:** [014 — The Living State Container](014_state-container.md).

<a id="partial-segregation"></a>
## 4. Partial Segregation

**Why it matters:** This platform is a *starter framework* — you clone it and build your product on top. The catch: the framework keeps improving, and you will want to pull in upgrades. If your custom code were mixed into the framework's own files, every upgrade would overwrite your work. Partial segregation prevents that collision entirely.

**Plain definition:** **Partial segregation** is the practice of keeping framework code and your code in physically separate files, even when they describe the *same* class. C# allows this through `partial class` — a single class whose definition is split across multiple files, which the compiler stitches together.

The convention is a filename marker: framework files use the plain name, and the matching place for *your* additions uses `.App.` in the name. For example, `DataObjects.cs` (framework, do not edit) is paired with `DataObjects.App.cs` (yours to edit). The `.App.` file ships nearly empty, with a comment telling you exactly what it is for:

```csharp
namespace CRM;

// Use this file as a place to put any application-specific data objects.

public partial class DataObjects
{
    public partial class User
    {
        //public string? MyCustomUserProperty { get; set; }
    }
    ...
}
```

(from `CRM.DataObjects/DataObjects.App.cs`)

Because both files declare `public partial class DataObjects` and `public partial class User`, the property you uncomment lands on the *same* `User` class the framework defined — without you touching the framework's file. When an upgrade replaces `DataObjects.cs`, your `DataObjects.App.cs` is left alone. This same `.cs` / `.App.cs` pairing recurs across the projects (for example `GlobalSettings.cs` / `GlobalSettings.App.cs`, `ConfigurationHelper.cs` / `ConfigurationHelper.App.cs`).

The rule of thumb: **if a file has `.App.` in its name, it is yours; otherwise treat it as framework code you should not edit.** That one habit keeps you upgrade-safe.

<a id="soft-delete"></a>
## 5. Soft-Delete

**Why it matters:** Real users delete things by accident, and auditors ask "who deleted this and when?" If "delete" truly erased the row, you could answer neither question. Soft-delete keeps an undo button and an audit trail without any extra effort from the calling code.

**Plain definition:** **Soft-delete** means a record is *marked* as deleted rather than physically removed. The row stays in the database but is flagged so normal queries skip it. Most data objects carry two fields for this:

```csharp
public bool Deleted { get; set; }
public DateTime? DeletedAt { get; set; }
```

`Deleted` is the on/off flag ("is this hidden?"), and `DeletedAt` is the nullable timestamp ("when was it hidden?", or `null` if it never was). You will see this pair on `User`, `UserListing`, and many other objects throughout `DataObjects.cs`.

Whether a delete is soft or permanent is itself a per-tenant choice, controlled by a setting on `TenantSettings`:

```csharp
public enum DeletePreference
{
    Immediate,
    MarkAsDeleted,
}
...
public DeletePreference DeletePreference { get; set; } = DeletePreference.MarkAsDeleted;
public int DeleteMarkedRecordsAfterDays { get; set; } = 90;
```

The default is `MarkAsDeleted` — soft-delete — and `DeleteMarkedRecordsAfterDays` defaults to `90`, meaning marked records are cleaned up automatically after 90 days. A tenant that wants hard deletes can choose `Immediate` instead.

Because records are only hidden, the app can offer a recovery view. The data objects `DeletedRecordCounts` and `DeletedRecords` (with `DeletedRecordItem` carrying `DeletedAt` and `DeletedBy`) exist precisely so the UI can list and restore soft-deleted items. **Deep doc:** [025 — EF Models and the Records That Never Truly Vanish](025_ef-models-soft-delete.md).

<a id="result-type"></a>
## 6. Result Type

**Why it matters:** When you call an operation that might fail — save a user, validate a setting — you need a consistent answer to two questions: *did it work?* and *if not, what should I tell the user?* If every operation answered differently, error handling would be chaos. A shared result type makes the answer uniform everywhere.

**Plain definition:** A **result type** is the standard object an operation returns to report success or failure. Here it is a deliberately tiny class called `BooleanResponse`:

```csharp
public partial class BooleanResponse
{
    public List<string> Messages { get; set; } = new List<string>();
    public bool Result { get; set; }
}
```

Just two members. `Result` is the pass/fail boolean — `true` means it worked. `Messages` is a list of human-readable strings explaining what happened, which is where validation errors or failure reasons go so the UI can show them. The pattern in calling code is always the same: check `Result`; if it is `false`, surface `Messages` to the user.

This is also how richer objects report their own success. Recall from [Section 2](#tenant) that `Tenant` inherits `ActionResponseObject` — and that base class is nothing but a carrier for a `BooleanResponse`:

```csharp
public partial class ActionResponseObject
{
    public BooleanResponse ActionResponse { get; set; } = new BooleanResponse();
}
```

So any object inheriting `ActionResponseObject` (like `Tenant` or `ConnectionStringConfig`) hands you the full data *and* a `.ActionResponse` you can check for pass/fail in one round trip. One small, predictable shape — used everywhere — is what keeps error handling sane across the whole app. **Deep doc:** [026 — The Standard Pass/Fail Result](026_standard-result.md).

<a id="glossary"></a>
## 7. Glossary & Deep-Doc Map

Quick reference. Each term links forward to the doc that covers it in depth.

| Term | One-line meaning | Lives in (real source) | Deep doc |
|------|------------------|------------------------|----------|
| **Tenant** | One isolated customer account, keyed by `Guid TenantId` | `DataObjects.cs` → `class Tenant : ActionResponseObject` | [016 — Everything Knows Its Tenant](016_tenant-aware-thinking.md) |
| **Wrapper** | Helper that hides HTTP plumbing behind one call | `Helpers.cs` → `GetOrPost<T>(...)` | [011 — Why We Wrap the Framework](011_wrapper-philosophy.md) |
| **State container** | Shared model every page reads/writes | `DataModel.cs` → `BlazorDataModel` | [014 — The Living State Container](014_state-container.md) |
| **Partial segregation** | Your code in `.App.` files, framework code separate, both `partial class` | `DataObjects.App.cs` (and other `*.App.cs`) | this doc, §4 |
| **Soft-delete** | Mark `Deleted = true` / set `DeletedAt` instead of erasing | `DataObjects.cs` → `Deleted` / `DeletedAt`; `DeletePreference` enum | [025 — EF Models and the Records That Never Truly Vanish](025_ef-models-soft-delete.md) |
| **Result type** | Pass/fail object with `Result` + `Messages` | `DataObjects.cs` → `class BooleanResponse` | [026 — The Standard Pass/Fail Result](026_standard-result.md) |

---

<a id="related-docs"></a>
## 8. Related Docs

- [011 — Why We Wrap the Framework](011_wrapper-philosophy.md) — wrapper philosophy
- [014 — The Living State Container](014_state-container.md) — the state container
- [016 — Everything Knows Its Tenant](016_tenant-aware-thinking.md) — tenant awareness
- [025 — EF Models and the Records That Never Truly Vanish](025_ef-models-soft-delete.md) — soft-delete
- [026 — The Standard Pass/Fail Result](026_standard-result.md) — the result type

---
*GuidesV2 · 006 · drafted from source (`CRM.DataObjects`, `CRM.Client`) on 2026-06-05.*

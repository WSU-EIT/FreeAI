# 017 — Following a Click to the Database

> **Document ID:** 017  ·  **Category:** Concept  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Trace one user action through component, wrapper, controller, data layer, and back to reinforce the architecture.
> **Audience:** Newcomers gaining competence  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 01x (Mental Models: How This Differs From Stock .NET) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
| --- | --- | --- |
| 1 | [Why Follow a Click?](#why-follow) | How tracing one Save reveals the whole front-to-back architecture |
| 2 | [The Four Layers at a Glance](#four-layers) | Page component, Helpers wrapper, API controller, data-access layer |
| 3 | [Step 1 — The Click in the Component](#click-component) | `EditTag.razor` captures the click, validates, and calls a wrapper |
| 4 | [Step 2 — Through the Wrapper](#through-wrapper) | `Helpers.GetOrPost` adds auth headers and POSTs JSON to the API |
| 5 | [Step 3 — Controller to Data Layer](#controller-data) | `DataController.SaveTag` hands off to `DataAccess.SaveTag` and EF |
| 6 | [Step 4 — The Round Trip Back](#round-trip) | The saved object, its `ActionResponse`, and SignalR return to the UI |
| 7 | [Common Pitfalls and What to Remember](#pitfalls) | Mistakes to avoid and the one-sentence takeaway |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-follow"></a>
## 1. Why Follow a Click?

**Why it matters:** This codebase is large, but it is also *repetitive in a good way*. Almost every screen — Tags, Users, Locations, Invoices, Appointments — follows the exact same path from "the user clicked Save" to "a row changed in the database." If you learn that one path once, you have effectively learned how hundreds of features work. The names change (`SaveTag`, `SaveUser`, `SaveLocation`), but the shape never does.

So instead of trying to read everything, we are going to follow a single click all the way down and all the way back up. We will use the **Tag** feature because it is one of the smallest end-to-end flows in the app, so nothing distracts from the pattern itself.

Here is the whole journey in one breath, and the rest of this doc just slows it down:

1. The user clicks **Save** on the *Edit Tag* screen (a Blazor page component).
2. The component runs some quick validation, then calls a helper called `GetOrPost`.
3. `GetOrPost` attaches identity headers and sends the tag as JSON over HTTP to a web API endpoint.
4. The API **controller** receives it and immediately hands the work to the **data-access layer**.
5. The data-access layer maps the incoming object onto a database row and asks Entity Framework to save it.
6. The result travels back up the same ladder, the UI navigates away, and other users are notified live.

A "**Blazor component**," by the way, is just a reusable piece of UI written in a `.razor` file — it mixes HTML markup with C# code in the same file. The "**data-access layer**" is the only layer in the whole app that is allowed to talk to the database directly; everyone else has to go through it. Keeping that rule is what makes the codebase predictable.

The real files we touch on this trace:

- **Component:** `FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor`
- **Wrapper:** `FreeCRM/CRM.Client/Helpers.cs` (the `GetOrPost<T>` method)
- **Controller:** `FreeCRM/CRM/Controllers/DataController.Tags.cs`
- **Data layer:** `FreeCRM/CRM.DataAccess/DataAccess.Tags.cs`

---

<a id="four-layers"></a>
## 2. The Four Layers at a Glance

**Why it matters:** Before we walk the steps, it helps to have a name and a one-line job description for each layer. When something breaks, the *first* question an engineer here asks is "which layer is this?" — because that tells you which file to open.

| Layer | Where it lives | Plain-language job | Concrete example for "Save Tag" |
| --- | --- | --- | --- |
| **Page component** | `CRM.Client/Pages/**/*.razor` | Draw the screen, capture clicks, do light validation | `EditTag.razor` `Save()` method |
| **Wrapper (Helpers)** | `CRM.Client/Helpers.cs` | Turn a method call into an authenticated HTTP request | `Helpers.GetOrPost<DataObjects.Tag>(...)` |
| **API controller** | `CRM/Controllers/DataController.*.cs` | The web entry point; check permission, delegate | `DataController.SaveTag(...)` |
| **Data-access layer** | `CRM.DataAccess/DataAccess.*.cs` | The only place that reads/writes the database | `DataAccess.SaveTag(...)` |

A few terms defined on first use:

- **Wrapper** here means a thin helper that hides repetitive plumbing. `GetOrPost` is a wrapper around raw HTTP calls so that no page ever has to remember to attach the auth token by hand.
- **Controller** is the standard ASP.NET term for the class that receives an incoming web request and decides what to do with it. In this app, controllers are deliberately *boring* — they almost never contain business logic.
- **Data-access layer** (often shortened to "the data layer" or `da` in the code) is the project `CRM.DataAccess`. It owns **Entity Framework**, which is the library that maps C# objects to database tables so you rarely write raw SQL.

> **Forward-pointer (read this later, not now):** Layers 3 and 4 — the controllers and the data-access layer — are taught *in full* in **band 02x**. This doc only shows enough of them to make the round trip make sense. If a detail of the controller or data layer feels under-explained here, that is on purpose; see [024 — API Controllers](024_api-controllers.md) and [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md).

Notice that the front end (`CRM.Client`) and the back end (`CRM`, `CRM.DataAccess`) are *separate projects*. They never call each other in-process — the only bridge between them is HTTP. That HTTP hop is exactly what Step 2 builds.

---

<a id="click-component"></a>
## 3. Step 1 — The Click in the Component

**Why it matters:** This is the layer the user actually sees and touches. Its job is to react to the click, make sure the data is at least *plausible* before bothering the server, and then delegate. Crucially, the component does **not** know anything about databases — it only knows how to ask the server to do things.

In `EditTag.razor`, the Save button is plain markup wired to a C# method with Blazor's `@onclick`:

```razor
<button type="button" class="btn btn-success" @onclick="Save">
    <Language Tag="Save" IncludeIcon="true" />
</button>
```

`@onclick="Save"` means "when this button is clicked, run the C# method named `Save`." That method lives in the `@code { ... }` block at the bottom of the same file. Here is the shape of it (trimmed to the essentials):

```csharp
protected async Task Save()
{
    Model.ClearMessages();

    List<string> errors = new List<string>();
    string focus = String.Empty;

    if (String.IsNullOrWhiteSpace(_tag.Name)) {
        errors.Add(Helpers.MissingRequiredField("TagName"));
        if (focus == String.Empty) { focus = "edit-tag-Name"; }
    }

    // ... more checks (at least one module must be enabled) ...

    if (errors.Any()) {
        Model.ErrorMessages(errors);
        await Helpers.DelayedFocus(focus);
        return;
    }

    Model.Message_Saving();

    var saved = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/SaveTag", _tag);

    // ... handle the result (Step 4) ...
}
```

A few things worth naming for a non-engineer:

- `_tag` is the in-memory object bound to the form. Because the inputs use Blazor's `@bind` (e.g. `@bind="_tag.Name"`), typing in a box updates `_tag` automatically. By the time `Save` runs, `_tag` already holds whatever the user typed.
- The validation here is intentionally *lightweight* — empty name, no module selected. This is a courtesy to the user (instant feedback) and not the real gatekeeper. The server validates again; the client check just avoids a pointless round trip.
- `Model.Message_Saving()` puts a "Saving..." banner on screen. `Model` is a shared **`BlazorDataModel`** injected into the page (`@inject BlazorDataModel Model`) — think of it as the app-wide state that every page can read and update.
- The one line that actually starts the trip to the server is:

```csharp
var saved = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/SaveTag", _tag);
```

That is the handoff. The component is now done deciding *what* it wants ("save this tag"); the wrapper takes over deciding *how* to ask for it.

---

<a id="through-wrapper"></a>
## 4. Step 2 — Through the Wrapper

**Why it matters:** Every screen needs to call the server, and every one of those calls needs the same boilerplate: which tenant am I, what is my auth token, what is my device fingerprint, how do I turn a C# object into JSON, what if the server returns an error page instead of data? Rather than copy that into hundreds of methods, the app funnels *every* server call through one method: `Helpers.GetOrPost<T>`. Learn this one method and you understand how the entire front end talks to the back end.

The signature (from `CRM.Client/Helpers.cs`):

```csharp
public static async Task<T?> GetOrPost<T>(string url, object? post = null, bool logResults = false)
```

The generic `T` is "the type you expect back." For our save, `T` is `DataObjects.Tag`, so we get a `Tag` back. The clever bit is the name: **GetOrPost**. If you pass a `post` object, it does an HTTP **POST** (sending data up); if you don't, it does an HTTP **GET** (just fetching). Same method, two behaviors:

```csharp
if (post != null) {
    response = await Http.PostAsJsonAsync(url, post);
} else {
    response = await Http.GetAsync(url);
}
```

Because our `Save` passed `_tag` as the `post` argument, this is a POST, and `PostAsJsonAsync` automatically serializes (turns into JSON text) the tag for transmission.

Before sending, the wrapper attaches the caller's identity as HTTP headers:

```csharp
Http.DefaultRequestHeaders.Clear();

if (Model != null) {
    Http.DefaultRequestHeaders.Add("TenantId", Model.User.TenantId.ToString());

    if (String.IsNullOrWhiteSpace(Model.User.AuthToken)) {
        Model.User.AuthToken = await Token();
    }
    if (!String.IsNullOrWhiteSpace(Model.User.AuthToken)) {
        if (Model.User.AuthToken != "na") {
            Http.DefaultRequestHeaders.Add("Token", Model.User.AuthToken);
        }
    }
    // ...
    if (!String.IsNullOrWhiteSpace(Model.Fingerprint)) {
        Http.DefaultRequestHeaders.Add("Fingerprint", Model.Fingerprint);
    }
}
```

In plain language, three identity facts ride along with every request:

- **`TenantId`** — which customer/organization this user belongs to. This app is **multi-tenant** (one running system serves many separate organizations), so almost everything is scoped by tenant.
- **`Token`** — the user's authentication token, proving who they are. The wrapper fetches one if it doesn't have it yet.
- **`Fingerprint`** — an identifier for the specific browser/device, used to tie the token to the session it was issued for.

When the response comes back, the wrapper is defensive. It only reads the body on success, it guards against accidentally receiving an HTML page (a sign the URL was wrong), and it deserializes (turns JSON text back into a C# object) into `T`:

```csharp
if (response.IsSuccessStatusCode) {
    var content = await response.Content.ReadAsStringAsync();
    if (!String.IsNullOrWhiteSpace(content)) {
        if (content.ToUpper().StartsWith("<!DOCTYPE")) {
            await ConsoleLog("Not a valid API endpoint - " + url);
        } else {
            // ... reads JSON into T ...
            var result = await response.Content.ReadFromJsonAsync<T>();
            if (result != null) { output = result; }
        }
    }
}
```

And the whole thing is wrapped in a `try/catch`, so a network failure logs a message and returns `null` rather than crashing the page. That is why callers like `Save` always check "did I get something back, or `null`?" — `null` is the wrapper's universal "something went wrong" signal.

The URL passed in — `"api/Data/SaveTag"` — is the address of the controller endpoint we hit next.

---

<a id="controller-data"></a>
## 5. Step 3 — Controller to Data Layer

**Why it matters:** The HTTP request now crosses from the browser into the server. The first server code it meets is the **controller**. The single most important thing to internalize about controllers in this app: *they do almost nothing themselves.* They check that the caller is allowed in, then immediately hand the real work to the data-access layer. That discipline is what keeps business logic in exactly one place.

The endpoint our wrapper called maps to this method in `CRM/Controllers/DataController.Tags.cs`:

```csharp
[HttpPost]
[Authorize(Policy = Policies.Admin)]
[Route("~/api/Data/SaveTag")]
public async Task<ActionResult<DataObjects.Tag>> SaveTag(DataObjects.Tag tag)
{
    var output = await da.SaveTag(tag, CurrentUser);
    return Ok(output);
}
```

Reading the **attributes** (the `[...]` lines, which are metadata the framework acts on):

- `[HttpPost]` — this method only answers POST requests, matching the POST our wrapper sent.
- `[Authorize(Policy = Policies.Admin)]` — the **gatekeeper**. The request is rejected before this method even runs unless the caller satisfies the "Admin" policy. This is the real permission check; the client-side validation in Step 1 was just for user comfort.
- `[Route("~/api/Data/SaveTag")]` — declares the URL, which is exactly the string the wrapper used.

The body is just two lines, and that is the whole point. `da` is the injected data-access layer (`private IDataAccess da;` on the controller). `CurrentUser` is the authenticated user the controller resolved from the `Token`/`TenantId`/`Fingerprint` headers in its constructor — so the data layer always knows *who* is making the change. `Ok(output)` wraps the result in a `200 OK` HTTP response so it can travel back to the browser.

Now the data-access layer takes over. `DataAccess.SaveTag` (in `CRM.DataAccess/DataAccess.Tags.cs`) is where the database is finally touched. The flow:

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
    // ... copy the rest of the fields ...
    rec.LastModified = now;
    rec.LastModifiedBy = CurrentUserIdString(CurrentUser);

    try {
        if (newRecord) {
            await data.Tags.AddAsync(rec);
        }
        await data.SaveChangesAsync();

        output.ActionResponse.Result = true;
        // ... SignalR broadcast (Step 4) ...
    } catch (Exception ex) {
        output.ActionResponse.Messages.Add("Error Saving Tag " + output.TagId.ToString());
        output.ActionResponse.Messages.AddRange(RecurseException(ex));
    }

    return output;
}
```

The important ideas, in plain language:

- **`data`** is the **Entity Framework `DbContext`** (`private EFDataModel data;`). It is the live connection to the database, and `data.Tags` represents the Tags table. `FirstOrDefaultAsync(...)` runs a query: "find the row with this id, or give me nothing."
- **New vs. existing is decided here, not by the client.** If no row exists *and* the incoming id is empty (`Guid.Empty`), it is a brand-new tag, so the layer mints a fresh `Guid` id and stamps `Added`/`AddedBy`. If no row exists but an id *was* supplied, the record must have been deleted out from under the user, so it returns an error instead of silently recreating it.
- **There are two object types with the same idea but different jobs.** The incoming `DataObjects.Tag` (the **DTO** — *Data Transfer Object*, the shape that travels over the wire) is *mapped onto* a separate `Tag` **EF entity** (`rec`, the shape that maps to the database table). The data layer copies field by field. This separation is taught fully in band 02x.
- **`await data.SaveChangesAsync()`** is the actual write. Up to this line, EF has only been tracking changes in memory; this is the moment they hit the database in a transaction.
- **Everything risky is inside `try/catch`.** If the save throws, the error is captured into the response's `Messages` rather than crashing the request. The user gets a readable error; the server stays up.

> **Forward-pointer:** This is a deliberately shallow tour of layers 3 and 4. The permission policies, the DTO-to-entity mapping conventions, soft-delete handling, and the `IDataAccess` interface are all covered properly in band 02x — see [024 — API Controllers](024_api-controllers.md), [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md), and [025 — EF Models and Soft Delete](025_ef-models-soft-delete.md).

---

<a id="round-trip"></a>
## 6. Step 4 — The Round Trip Back

**Why it matters:** A save is not finished when the row is written. The UI needs to know whether it succeeded, the user needs to be moved on, and — uniquely in this app — *other people looking at the same data* need to see the change without refreshing. Following the result back up shows how all three happen.

**Down at the data layer**, on success, two things are set before returning:

```csharp
output.ActionResponse.Result = true;

await SignalRUpdate(new DataObjects.SignalRUpdate {
    TenantId = output.TenantId,
    ItemId = output.TagId,
    UpdateType = DataObjects.SignalRUpdateType.Tag,
    Message = "Saved",
    UserId = CurrentUserId(CurrentUser),
    Object = output,
});
```

- **`ActionResponse`** is a small standard envelope every data object carries. Every `DataObjects.Tag` inherits from `ActionResponseObject`, which gives it an `ActionResponse` of type `BooleanResponse`:

  ```csharp
  public partial class BooleanResponse
  {
      public List<string> Messages { get; set; } = new List<string>();
      public bool Result { get; set; }
  }
  ```

  So the answer to "did it work?" rides *inside* the returned object as `ActionResponse.Result` (a yes/no), and any human-readable problems ride in `ActionResponse.Messages`. This same envelope is used across the whole codebase — it is the universal success/failure signal.
- **`SignalRUpdate(...)`** broadcasts a live notification. **SignalR** is a real-time messaging library: the server can push a message to connected browsers instead of waiting to be asked. Here it tells every other user in the same tenant "a Tag was Saved, here it is," so their screens update on the spot.

**Back up in the controller**, the returned tag is wrapped in `Ok(output)` → a `200 OK` carrying the tag (now including its `ActionResponse` and any new id) as JSON.

**Back up in the wrapper**, `GetOrPost` deserializes that JSON into a `DataObjects.Tag` and hands it to the page as the `saved` variable.

**Back up in the component**, `Save` inspects the result:

```csharp
var saved = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/SaveTag", _tag);

Model.ClearMessages();

if (saved != null) {
    if (saved.ActionResponse.Result) {
        Helpers.NavigateTo("Settings/Tags");
    } else {
        Model.ErrorMessages(saved.ActionResponse.Messages);
    }
} else {
    Model.UnknownError();
}
```

Three outcomes, three behaviors — and notice they line up exactly with the layers below:

- **`saved == null`** → the wrapper caught a network/transport failure; show a generic "unknown error."
- **`saved.ActionResponse.Result == false`** → the request reached the server but the *save itself* failed; show the specific `Messages` the data layer produced.
- **`saved.ActionResponse.Result == true`** → success; clear the "Saving..." banner and navigate back to the Tags list.

Separately, the **other** users' browsers receive the SignalR "Saved" message. The Edit Tag page even handles being open *on that very record* elsewhere, refreshing it in place. That is why this is genuinely a *round trip* and not a one-way street: the click ends not just with a database row, but with every relevant screen in sync.

---

<a id="pitfalls"></a>
## 7. Common Pitfalls and What to Remember

**Why it matters:** Most confusion for newcomers comes from putting code in the wrong layer or misreading where the "real" decision happens. A short list of traps:

- **"The client validated it, so the server can trust it." — No.** The client check in `Save` is a convenience. The authoritative checks are the controller's `[Authorize(Policy = ...)]` and the data layer's own logic. Never move a real rule out of the server.
- **Putting database code in a component.** A `.razor` page must never touch `data`/Entity Framework. If a page needs data, it calls `Helpers.GetOrPost` against an API endpoint. The data layer is the *only* place EF lives.
- **Forgetting the result might be `null`.** `GetOrPost` returns `null` on any transport failure. Every caller must handle the `null` case (here, `Model.UnknownError()`), or a hiccup becomes a silent no-op.
- **Confusing the DTO with the EF entity.** `DataObjects.Tag` (travels over HTTP) and the `Tag` EF entity (maps to a table) are two different types that *look* similar. The data layer maps between them on purpose. Editing one expecting the other to change is a classic mistake.
- **Thinking new-vs-update is decided on the client.** It isn't. The data layer decides based on whether a matching row exists and whether the id is `Guid.Empty`. The client just sends the object.
- **Expecting the GET/POST choice to be configured somewhere.** It is decided by one thing only: whether you passed a `post` object to `GetOrPost`. Object present → POST. Object absent → GET.

**The one thing to remember:** *Every* feature in this app is the same four-layer relay — **component → Helpers wrapper → controller → data layer**, and then the result, an `ActionResponse`, and a SignalR broadcast climb back up. Learn the Tag trace and you can read any other `SaveX`/`GetX`/`DeleteX` flow in the codebase on sight.

---

<a id="related-docs"></a>
## 8. Related Docs

- [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md) — the data stack this trace passes through
- [012 — Wrapped Navigation, HTTP, Localization, and Serialization](012_wrapped-plumbing.md) — the wrappers on the path
- [024 — API Controllers: The Tenant-Aware Request Surface](024_api-controllers.md) — the controller in the middle

---
*GuidesV2 · 017 · drafted from source 2026-06-05.*

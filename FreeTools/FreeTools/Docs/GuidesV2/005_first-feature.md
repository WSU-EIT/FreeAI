# 005 — A Guided Tour of One Working Feature

> **Document ID:** 005  ·  **Category:** Onboarding  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Walk through one real, already-shipped list-plus-edit feature (Settings → Tags) end to end, so a newcomer can see how the layers connect before building anything.
> **Audience:** Brand-new adopters  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 00x (Landing Zone: From Clone to Login) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Take This Tour](#why-it-matters) | What a list-plus-edit feature is and why we observe a real one first |
| 2 | [Before You Start](#prerequisites) | A running app and a tour mindset (not an authoring exercise) |
| 3 | [The Record We Are Touring](#define-record) | The Tag record and the fields it carries |
| 4 | [The List View](#list-view) | How `Tags.razor` shows every Tag in a table |
| 5 | [The Edit Form](#edit-form) | How `EditTag.razor` creates and updates one Tag |
| 6 | [How It Is Wired Together](#wire-together) | The path from a click down to the database and back |
| 7 | [Watch It Work](#run-verify) | Drive the feature in the browser and confirm each layer fires |
| 8 | [What You Saw and Where to Go Next](#next-steps) | Recap and the bands that teach you to build this |
| 9 | [Related Docs](#related-docs) | Sibling and forward-reference docs |

---

<a id="why-it-matters"></a>
## 1. Why Take This Tour

Before you write a single line of your own, it pays to watch one finished feature work and trace how it is put together. That is what this document is: a guided tour, not a build exercise. You will not type any code here — you will read real code that already ships in the product and follow the wiring from the screen down to the database.

The feature we tour is a **list-plus-edit feature**. That phrase describes the most common shape of screen in a business application: one page shows a *list* of records in a table (browse, scan, search), and a second page lets you *edit* one record at a time (create a new one or change an existing one). Almost every screen in FreeCRM follows this shape — Users, Locations, Departments, Email Templates, and the one we will study, **Tags**.

Why does watching this matter to you as a future builder?

- **It is the template you will copy.** When you build your own feature later, you will not start from a blank file. You will copy an existing list-plus-edit pair and adapt it. Knowing one cold makes every future feature feel familiar.
- **It shows the layers honestly.** A clean feature touches the browser page, a shared in-memory state object, a small HTTP wrapper, an API controller, a data-access method, and a database table. Seeing all six in one short trip demystifies "where does my code go?"
- **It builds confidence cheaply.** You get the mental model without the risk of breaking anything, because you are only reading and clicking, never changing.

A "Tag" is just a colored label you can attach to other things in the system (appointments, email templates, services). It is a small, self-contained record, which makes it the perfect specimen for a tour: big enough to be real, small enough to hold in your head.

---

<a id="prerequisites"></a>
## 2. Before You Start

This is an **observe-the-working-feature tour**, so the bar is low. You need two things.

1. **A running, logged-in app.** If you have completed [003 — Clone, Build, Seed, and Sign In](003_zero-to-login.md), you already have this. You should be able to sign in as an administrator and reach the Settings area. Tags is an admin-only feature, so an admin login matters here.
2. **A tour mindset.** You are not authoring anything. Do not worry yet about *how* you would write these files — only about *what each piece does and how they connect*. The "how to build it yourself" parts live in later bands and are pointed to at the end.

A couple of plain-language terms used throughout, defined once here:

- **Razor page / component** — a `.razor` file that mixes HTML markup with C# code. This is how Blazor (the framework that runs C# in the browser) builds screens. The list page and the edit page are each one Razor file.
- **API endpoint** — a URL on the server (for example `api/Data/GetTags`) that the browser calls to fetch or save data. Think of it as a doorway between the browser and the server.
- **Data-access method** — a C# method on the server that actually talks to the database (reads, writes, soft-deletes a row).

The four files we tour, all real and unmodified:

| Layer | File |
|-------|------|
| List page (browser) | `FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor` |
| Edit page (browser) | `FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor` |
| API controller (server) | `FreeCRM/CRM/Controllers/DataController.Tags.cs` |
| Data access (server) | `FreeCRM/CRM.DataAccess/DataAccess.Tags.cs` |

---

<a id="define-record"></a>
## 3. The Record We Are Touring

Every feature is organized around a **record** — one logical thing the screen manages. For our tour it is the **Tag**. Before looking at screens, look at the shape of the data, because everything else exists to move this shape around.

The record's public shape is a **DTO** — a Data Transfer Object, which is plain language for "a simple class whose only job is to carry data between the browser and the server." The Tag DTO lives in `FreeCRM/CRM.DataObjects/DataObjects.Tags.cs`:

```csharp
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
```

Read this top to bottom and notice three things that recur across the whole codebase:

- **An identity and a tenant.** `TagId` is the unique identity of this record (a `Guid`, a globally-unique 128-bit id). `TenantId` ties the record to one customer/organization — FreeCRM is multi-tenant, meaning many separate organizations share one database, and every record is stamped with the tenant it belongs to. You will see this `TenantId` everywhere.
- **The real payload.** `Name`, `Style`, `Enabled`, and the three `UseIn...` flags are the fields a human actually edits — the label text, its color styling, whether it is on, and which modules it applies to.
- **Bookkeeping and soft-delete.** `Added`/`AddedBy`/`LastModified`/`LastModifiedBy` record who touched it and when. `Deleted` and `DeletedAt` implement **soft delete**: instead of erasing a row, the system flags it as deleted and keeps it. That is why, on the list, you can choose to show deleted items again.

The fact that `Tag` inherits from `ActionResponseObject` is a small but important detail: it means a saved Tag can carry a success/failure result back with it. We will see that pay off in the edit form.

---

<a id="list-view"></a>
## 4. The List View

The list page answers one question for the user: "what Tags exist, and which one do I want to open?" Its file is `FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor`.

**It owns two URLs.** The top of the file declares the routes that bring the browser here:

```razor
@page "/Settings/Tags"
@page "/{TenantCode}/Settings/Tags"
```

The second form includes a `TenantCode` in the address — that is how the app keeps one organization's screens distinct from another's.

**It reads from shared state, not from a fresh server call on every render.** Near the top the page injects a shared object:

```razor
@inject BlazorDataModel Model
```

`BlazorDataModel` (referred to as `Model`) is the **state container** — one in-memory object, shared across the whole browser session, that holds the currently-loaded data (the logged-in user, the list of Tags, and much more). The table loops over `Model.Tags`:

```razor
@foreach (var tag in Model.Tags.Where(x => x.Enabled == true || !Model.User.UserPreferences.EnabledItemsOnly).OrderBy(x => x.Name)) {
```

So the list does not fetch on its own; it renders whatever is already in `Model.Tags`. The data gets there in `LoadData`, which simply calls a helper:

```csharp
protected async Task LoadData()
{
    _loading = true;
    await Helpers.LoadTags();
    _loading = false;
    StateHasChanged();
}
```

`Helpers.LoadTags()` is the one line that does the fetching:

```csharp
public async static Task LoadTags()
{
    var items = await GetOrPost<List<DataObjects.Tag>>("api/Data/GetTags");
    Model.Tags = items != null && items.Any() ? items : new List<DataObjects.Tag>();
}
```

That is the whole loading story: call the `api/Data/GetTags` endpoint, drop the result into `Model.Tags`, done.

**It gates itself.** Tags is admin-only and behind a feature flag, so the page checks before doing anything:

```csharp
if (!Model.FeatureEnabledTags || !Model.User.Admin) {
    Helpers.NavigateToRoot();
    return;
}
```

If you are not an admin (or the Tags feature is off for this tenant), you get bounced to the root. This is why section 2 told you to sign in as an admin.

**Each row offers one action — edit.** The interesting cell in each row is the Edit button:

```razor
<button type="button" class="btn btn-xs btn-primary nowrap" @onclick="@(() => EditTag(tag.TagId))">
    <Language Tag="Edit" IncludeIcon="true" />
</button>
```

`@onclick` wires the browser click to a C# method. `EditTag` just navigates to the edit page, passing this row's id:

```csharp
protected void EditTag(Guid TagId)
{
    Helpers.NavigateTo("Settings/EditTag/" + TagId.ToString());
}
```

The `<Language Tag="Edit" />` you see instead of a plain word "Edit" is the **localization** wrapper: text is looked up by a tag name so the app can show it in different languages. You will meet this everywhere; for the tour, just read `<Language Tag="X" />` as "the translated word for X."

The top of the page also has an **Add New Tag** link and two toggles — "Include Deleted Records" and "Enabled Items Only" — which is exactly the soft-delete and enabled flags from section 3 showing up as user controls.

---

<a id="edit-form"></a>
## 5. The Edit Form

The edit page does double duty: it both **creates a new Tag** and **edits an existing one**. The file is `FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor`.

**One page, four routes — and that is the trick.** Look at the top:

```razor
@page "/Settings/EditTag/{id}"
@page "/{TenantCode}/Settings/EditTag/{id}"
@page "/Settings/AddTag"
@page "/{TenantCode}/Settings/AddTag"
```

The `EditTag/{id}` routes arrive with an id (edit an existing record); the `AddTag` routes arrive without one (create a new record). The page decides which mode it is in by whether `id` is present. In `LoadTag`:

```csharp
if (!String.IsNullOrWhiteSpace(id)) {
    // edit mode: fetch the existing record
    var getTag = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/GetTag/" + id);
    if (getTag != null) { _tag = getTag; }
} else {
    // add mode: start a blank record
    _newTag = true;
    _tag = new DataObjects.Tag {
        TenantId = Model.TenantId,
        TagId = Guid.Empty,
        Enabled = true,
    };
}
```

Notice the brand-new Tag is given `TagId = Guid.Empty` — an all-zeros id that means "no identity yet." That empty id is the signal the server uses later to know it is creating, not updating.

**The fields bind two ways.** The form uses `@bind`, which keeps an input box and a C# field in sync automatically — type in the box and the C# value changes; change the value and the box updates. The Name input is a good example:

```razor
<input type="text" id="edit-tag-Name" @bind="_tag.Name" @bind:event="oninput"
    class="@Helpers.MissingValue(_tag.Name, "form-control")" />
```

`@bind:event="oninput"` makes it update on every keystroke. The `Helpers.MissingValue(...)` call swaps in a "required field is empty" style while the box is blank — lightweight inline validation.

**Save validates first, then calls the server.** The Save button calls the `Save` method, which collects errors before touching the network:

```csharp
if (String.IsNullOrWhiteSpace(_tag.Name)) {
    errors.Add(Helpers.MissingRequiredField("TagName"));
    if (focus == String.Empty) { focus = "edit-tag-Name"; }
}
```

If anything is wrong, it shows the messages and moves the cursor to the offending field instead of calling the server. Only when the record is valid does it post:

```csharp
var saved = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/SaveTag", _tag);

if (saved != null) {
    if (saved.ActionResponse.Result) {
        Helpers.NavigateTo("Settings/Tags");
    } else {
        Model.ErrorMessages(saved.ActionResponse.Messages);
    }
}
```

Here is where `ActionResponseObject` from section 3 earns its keep: the saved Tag comes back carrying `ActionResponse.Result` (did it work?) and `ActionResponse.Messages` (what went wrong, if anything). Success navigates back to the list; failure shows the server's messages.

**Delete is soft and confirmed.** The page shows a delete confirmation only when editing an existing record (not when adding), and deleting calls a dedicated endpoint:

```csharp
var deleted = await Helpers.GetOrPost<DataObjects.BooleanResponse>("api/Data/DeleteTag/" + id);
```

Because deletes are soft (section 3), the record is flagged rather than destroyed. That is also why, when you open a deleted Tag, the page shows an "undelete" banner instead of the normal form.

---

<a id="wire-together"></a>
## 6. How It Is Wired Together

You have now seen the two browser pages. This section follows one round trip all the way down and back, so the layers stop being abstract. Take the simplest action — pressing **Save** on the edit form — and follow it.

**Step 1 — Browser page.** `Save` in `EditTag.razor` validates the `_tag` object, then hands it to a helper:

```csharp
var saved = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/SaveTag", _tag);
```

**Step 2 — HTTP wrapper.** `Helpers.GetOrPost` is a thin wrapper around the browser's HTTP client. Its whole job is to serialize the Tag to JSON, POST it to the URL, and deserialize the JSON that comes back into a `DataObjects.Tag`. You call one method instead of writing fetch-and-parse plumbing each time.

**Step 3 — API controller.** The request lands on the server in `FreeCRM/CRM/Controllers/DataController.Tags.cs`. The controller is the server's front door; each method maps one URL to one data-access call:

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

Read the attributes — they are the security: `[HttpPost]` says this URL accepts a POST, `[Authorize(Policy = Policies.Admin)]` enforces that only an admin may call it (the same rule the browser page checked, now enforced on the server where it counts), and `[Route(...)]` is the URL. The method itself is almost nothing: it forwards to `da.SaveTag`. The same file holds `GetTags`, `GetTag`, and `DeleteTag`, each just as thin.

**Step 4 — Data access.** `da` is the data-access layer, and `SaveTag` lives in `FreeCRM/CRM.DataAccess/DataAccess.Tags.cs`. This is the only layer that talks to the database. It looks up the existing row, or creates a new one when the id is empty:

```csharp
var rec = await data.Tags.FirstOrDefaultAsync(x => x.TagId == output.TagId);
...
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
    }
}
```

Remember the `Guid.Empty` the add form set in section 5? This is where it is read: empty id means "make a new row and give it a fresh `Guid.NewGuid()`." Otherwise it updates the row it found. It then copies the editable fields across, stamps `LastModified`/`LastModifiedBy`, saves, and returns the Tag with its `ActionResponse` filled in — which is exactly what the browser page checked in step 1's response.

So the full loop is: **Razor page → `Helpers.GetOrPost` → `DataController` endpoint → `DataAccess` method → database**, and the result travels back up the same chain. Every feature in the product is built from this same five-stop round trip. Reads (`GetTags`) take the identical path with no body going out; deletes take it with a `BooleanResponse` coming back.

One more wire worth noticing: both pages subscribe to **SignalR** updates. SignalR is a real-time push channel — when another admin saves or deletes a Tag, the server pushes a notice and the open page refreshes or shows a message without you reloading. You can see it in the `SignalRUpdate` methods on both files. For the tour, just know the screen can update itself when someone else changes the same data.

---

<a id="run-verify"></a>
## 7. Watch It Work

Now drive the feature yourself and watch each layer fire. Keep your browser's developer tools open on the **Network** tab — that is where you can literally see the HTTP calls from section 6.

1. **Reach the list.** Sign in as an admin and navigate to **Settings → Tags** (URL `/Settings/Tags`). Confirm a table of Tags appears. In the Network tab you should see one call to `api/Data/GetTags` — that is `Helpers.LoadTags` filling `Model.Tags`.
2. **Toggle the flags.** Flip **Enabled Items Only** and **Include Deleted Records**. Notice the table re-filters instantly with no new network call — proof the list renders from in-memory `Model.Tags`, not a fresh fetch.
3. **Open one record.** Click **Edit** on any row. The URL becomes `/Settings/EditTag/{id}` and the Network tab shows a `api/Data/GetTag/{id}` call. The form fills with that Tag's Name, Style, Enabled switch, and module switches.
4. **Watch validation.** Clear the **Name** field and press **Save**. No network call goes out; you get a "required field" message and the cursor jumps to Name. This is the client-side guard from section 5.
5. **Save a real change.** Put the name back (or tweak the Style), press **Save**, and watch a POST to `api/Data/SaveTag`. On success the app returns you to the list and your change is visible.
6. **Create a new one.** Click **Add New Tag** (URL `/Settings/AddTag`, no id). Notice there is no `GetTag` call this time — it is add mode. Give it a name and a color, Save, and confirm it appears in the list. Behind the scenes the server saw `Guid.Empty` and minted a new id.
7. **Soft-delete it.** Open your new Tag, use the delete confirmation, and watch the `api/Data/DeleteTag/{id}` call. The row disappears from the default list — but turn on **Include Deleted Records** and it reappears, flagged rather than gone. That is soft delete in action.

If every step above behaved as described, you have personally observed all five stops of the round trip and the soft-delete and localization behaviors. That is the entire shape of a feature.

---

<a id="next-steps"></a>
## 8. What You Saw and Where to Go Next

Stepping back, here is the model you now carry:

- A feature is organized around **one record** (a DTO like `Tag`) with an identity, a tenant stamp, editable fields, and soft-delete bookkeeping.
- A **list page** renders that record from a shared **state container** (`BlazorDataModel` / `Model`), loaded once via a helper.
- An **edit page** does both create and update, decided by whether an id is in the URL, with two-way `@bind` inputs and client-side validation before any save.
- A click travels a fixed **five-stop round trip**: Razor page → `Helpers.GetOrPost` → `DataController` endpoint → `DataAccess` method → database, and back.
- Cross-cutting niceties — **authorization**, **localization**, **soft delete**, and **real-time SignalR updates** — show up the same way in every feature.

You did this by observing, not building, on purpose. When you are ready to build, these bands teach each layer you just toured:

- You will learn to **build the list and edit pages** from the house templates in band 03x — start with [031 — List and Edit, the House Pattern](031_crud-templates.md).
- You will learn the **data layers** behind the screen — the DTO, controller, data access, and EF model — in band 02x, starting with [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md).
- You will learn the **wrappers** the screen leans on — navigation, HTTP (`GetOrPost`), localization, and serialization — in band 01x via [012 — Wrapped Navigation, HTTP, Localization, and Serialization](012_wrapped-plumbing.md).

Tour complete. You have seen one real feature work end to end; everything you build later is a variation on what you just watched.

---

<a id="related-docs"></a>
## 9. Related Docs

- [031 — List and Edit, the House Pattern](031_crud-templates.md) — you will learn to build the list/edit pages you just toured in band 03x
- [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md) — you will learn to build the data layers behind the screen in band 02x
- [012 — Wrapped Navigation, HTTP, Localization, and Serialization](012_wrapped-plumbing.md) — you will learn the wrappers the screen calls in band 01x

---
*GuidesV2 005 · drafted from source 2026-06-05 · an observe-the-working-feature tour of Settings → Tags.*

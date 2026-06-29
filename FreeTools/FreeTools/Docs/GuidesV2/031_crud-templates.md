# 031 — List and Edit, the House Pattern

> **Document ID:** 031  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Walk through the standard CRUD list and edit page templates that most screens are stamped from.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 03x (Core Craft: Everyday Screens and Components) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why the House Pattern Exists](#why-house-pattern) | What "CRUD" means, what these templates are, and why every screen is shaped the same way |
| 2 | [When to Stamp a New Screen](#when-to-stamp) | The "list page plus edit page" pair, and when it fits versus going custom |
| 3 | [Anatomy of the List Page](#list-page) | The four state flags, the lifecycle methods, the table, and the row "Edit" button |
| 4 | [Anatomy of the Edit Page](#edit-page) | Load, the form, Save, Delete, and the new-vs-existing record split |
| 5 | [Wiring the Templates Together](#wiring) | Routing, `Model.View`, navigation between the pair, and live updates over SignalR |
| 6 | [Customizing Without Breaking the Mold](#customizing) | Where it is safe to add fields and behavior, and the parts you must not touch |
| 7 | [Common Pitfalls and Fixes](#pitfalls) | The mistakes that quietly break a stamped screen, and the fix for each |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-house-pattern"></a>
## 1. Why the House Pattern Exists

**"CRUD" is shorthand for the four things you ever really do to a record: Create, Read, Update, Delete.** Almost every settings screen in this application is just CRUD wearing different labels — Tags, Services, Locations, Departments, Users. They all show a list of things and let you add, change, or remove one. Because the *job* is the same every time, the *shape of the code* is the same every time too.

That sameness is deliberate, and it is the whole point of this guide. When every screen is built from the same template, a new developer who has read one screen has, in effect, read them all. The state flags are named the same, the lifecycle methods do the same work in the same order, the Save button calls the server the same way. There are no surprises hiding in screen number forty.

There is no scaffolding tool or code generator here — "stamping" means a developer copies an existing list/edit pair (Tags is a good clean one) and adapts it. So the template is a *convention you follow by hand*, not a wizard that writes the file for you. The payoff is twofold:

- **Speed.** You are not solving "how do I load data, draw a table, and save a form" from scratch. You are renaming `Tag` to your new thing and adjusting the columns.
- **Consistency.** Loading spinners, "no items to show" messages, deleted-record handling, live multi-user updates, and tenant safety all come along for free because they are baked into the template. (A **tenant** is one customer's isolated slice of the system — many organizations share one running app, and each must only ever see its own data.)

The two halves of the pattern are:

- A **list page** — a read-only table of all the records, with an "Add New" button and an "Edit" button on each row. In the source these are files like `Tags.razor` and `Services.razor`.
- An **edit page** — a form for one single record, with Save and Delete. These are files like `EditTag.razor` and `EditService.razor`. The same edit page handles both "add a brand-new record" and "change an existing one."

Everything below is grounded in the real pair `CRM.Client/Pages/Settings/Tags/Tags.razor` and `EditTag.razor`. If you open those two files alongside this doc, every snippet here will match what you see.

---

<a id="when-to-stamp"></a>
## 2. When to Stamp a New Screen

**Reach for this template whenever your feature is "manage a list of some kind of record."** If a user needs to see all the Widgets, add a Widget, edit a Widget, and delete a Widget, you want a `Widgets.razor` list page and an `EditWidget.razor` edit page. That is the default, and you should feel friction before deviating from it.

Good signs the house pattern fits:

- The thing you are building has a clear **record type** behind it — a row in a table, a `DataObjects.Something` class. `EditTag.razor` works with `DataObjects.Tag`; your screen will work with its own type the same way.
- Users will create more than one of them over time, and will come back to edit or remove them.
- The data is **tenant-scoped** — it belongs to one customer's slice — which is the normal case for settings.

Signs you may need something more custom:

- The screen is a **dashboard or report** — it only reads and visualizes data, never edits a single record. There is no "edit one row" flow, so the edit-page half does not apply.
- The interaction is a **wizard or multi-step flow**, not a flat form.
- There is exactly **one** of the thing for the whole tenant (app-wide settings, for example). Then you do not need a list page at all — `AppSettings.razor` is a single edit-style page with no list in front of it.

Even when you go custom, copy the *plumbing* from the template — the four state flags and the four lifecycle methods in Section 3. They handle tenant validation, loading-once, and live updates correctly, and getting that plumbing wrong is exactly the kind of bug that does not show up until production. Borrow the skeleton even when you discard the table.

---

<a id="list-page"></a>
## 3. Anatomy of the List Page

The list page has two parts: a block of markup that draws the table, and a `@code { }` block that manages state. Read the code block first — once you understand the four flags and the four lifecycle methods, the markup is obvious.

### The four state flags

Every list page declares the same private fields. They are the page's memory of "where am I in my own startup."

```csharp
protected bool _loadedData = false;
protected bool _loading = true;
protected bool _validatedUrl = false;

protected string _pageName = "tags";
```

- **`_loading`** — true while data is being fetched. The markup shows a `<LoadingMessage />` spinner when this is true, and the real table when it is false. This is *why it matters*: without it, the page would flash an empty table before the data arrives.
- **`_loadedData`** — a one-time latch. It starts `false`, flips to `true` the first time data loads, and stops the page from re-fetching on every re-render. (Blazor — the framework that runs the UI in the browser — can re-run the render method many times; this flag makes "load the data" happen once.)
- **`_validatedUrl`** — another one-time latch, this one guarding the tenant-URL check (more on that in Section 5).
- **`_pageName`** — a short lowercase identifier for this screen, here `"tags"`. It is compared against `Model.View` so the page only renders and reacts when *it* is the screen being shown. Think of it as the page's name tag.

`Model` here is the **`BlazorDataModel`** — one shared object, injected into every page with `@inject BlazorDataModel Model`, that holds the logged-in user, the loaded data, and which view is active. It is the application's single source of truth.

### The four lifecycle methods

These run in a fixed order and are nearly identical on every list page. Blazor calls them for you; you just fill them in.

**`OnInitialized`** runs once when the page is created. It subscribes to two events and announces which view is now active:

```csharp
protected override void OnInitialized()
{
    Model.OnChange += OnDataModelUpdated;
    Model.OnSignalRUpdate += SignalRUpdate;
    Model.View = _pageName;
}
```

`OnChange` fires whenever the shared data model changes, so the page can re-draw. `OnSignalRUpdate` fires when *another user* changes data live (Section 5). Setting `Model.View = _pageName` tells the whole app "Tags is the screen on display now."

**`OnAfterRenderAsync`** runs after each render and does the real startup work — but guarded so the expensive parts happen only once:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender) {
        Model.TenantCodeFromUrl = TenantCode;
    }

    if (Model.Loaded && Model.LoggedIn) {
        if (!Model.FeatureEnabledTags || !Model.User.Admin) {
            Helpers.NavigateToRoot();
            return;
        }

        if (!_validatedUrl) {
            _validatedUrl = true;
            await Helpers.ValidateUrl(TenantCode);
        }

        if (!_loadedData) {
            _loadedData = true;
            await LoadData();
        }
    }
}
```

Read it top to bottom: capture the tenant code from the URL on first render; do nothing until the app is loaded and the user is logged in; bounce non-admins (or users without this feature) back to the root; validate the tenant in the URL once; then load the data once. The two `if (!_flag) { _flag = true; ... }` guards are the load-once pattern in action.

**`Dispose`** runs when you leave the page and *unsubscribes* the two events. This matters: forgetting it leaks event handlers and causes dead pages to keep reacting in the background.

```csharp
public void Dispose()
{
    Model.OnChange -= OnDataModelUpdated;
    Model.OnSignalRUpdate -= SignalRUpdate;
}
```

(The page declares `@implements IDisposable` at the top so Blazor knows to call this.)

**`LoadData`** is the small method that actually fetches. It deliberately re-loads every time it is called, sets `_loading` around the fetch, and asks for a redraw:

```csharp
protected async Task LoadData()
{
    // Always reload the data in the data model.
    _loading = true;
    await Helpers.LoadTags();
    _loading = false;

    StateHasChanged();
}
```

`Helpers.LoadTags()` is a wrapper that fills `Model.Tags` from the server in a tenant-safe way. `StateHasChanged()` is Blazor's "please re-draw now" call.

### The markup: table and row actions

The visible part is a Bootstrap table. The header offers an "Add New" button, the body loops the records, and each row carries an "Edit" button. The whole thing only renders when this is the active, loaded view:

```razor
@if (Model.Loaded && Model.View == _pageName) {
    @if (_loading) {
        ...<LoadingMessage />
    } else {
        ...
        @foreach (var tag in Model.Tags.Where(...).OrderBy(x => x.Name)) {
            <tr class="@itemClass">
                <td>
                    <button type="button" class="btn btn-xs btn-primary nowrap"
                            @onclick="@(() => EditTag(tag.TagId))">
                        <Language Tag="Edit" IncludeIcon="true" />
                    </button>
                </td>
                <td>@tag.Name</td>
                ...
            </tr>
        }
    }
}
```

Three things worth naming for a newcomer:

- **`<Language Tag="..." />`** is how *all* user-facing text is shown. You never hard-code an English string; you pass a tag and the component renders it in the user's chosen language. "Edit", "Tags", "AddNewTag" are all language tags, not literal labels.
- The **"Edit" button** does not navigate by URL directly — it calls a tiny method that does: `EditTag(tag.TagId)`, which calls `Helpers.NavigateTo("Settings/EditTag/" + TagId)`. That is the handoff from the list page to the edit page.
- **Deleted and disabled rows** are styled, not hidden — note the `item-deleted` / `disabled` CSS class. The template keeps deleted records visible (behind a toggle) so they can be restored. Soft delete is the house rule; nothing is truly gone.

The list also offers preference toggles like "Include Deleted Records" and "Enabled Items Only," bound to `Model.User.UserPreferences`. Those come from the template and you usually keep them.

---

<a id="edit-page"></a>
## 4. Anatomy of the Edit Page

The edit page is the other half. It looks busier than the list page, but it is the same skeleton with three additions: it takes a record **id**, it holds **one record object**, and it has **Save** and **Delete**.

### What is different from the list page

```csharp
[Parameter] public string? id { get; set; }
[Parameter] public string? TenantCode { get; set; }

protected bool _loading = true;
protected bool _loadedData = false;
protected bool _newTag = false;
protected string _title = String.Empty;
protected DataObjects.Tag _tag = new DataObjects.Tag();
protected bool _validatedUrl = false;

protected string _pageName = "edittag";
```

- **`id`** is a route parameter — the part of the URL that says *which* record. When it is present you are editing an existing record; when it is absent you are adding a new one.
- **`_tag`** is the single record the form binds to. Everything the user types goes into this object's properties.
- **`_newTag`** records which mode you are in. It changes the title ("Add New Tag" vs "Edit Tag") and decides whether the Delete button appears.
- **`_title`** is the language tag for the heading, set during load.

The lifecycle methods (`OnInitialized`, `Dispose`, `OnAfterRenderAsync`) are the *same* as the list page, with one extra wrinkle in `OnAfterRenderAsync`: it reloads when the id in the URL changes, not just on first load, so clicking from one record straight to another works:

```csharp
if (!_loadedData || Helpers.StringValue(Model.NavigationId) != Helpers.StringValue(id)) {
    _loadedData = true;
    await LoadTag();
}
```

### Loading one record

`LoadTag` decides between the two modes. If there is an `id`, it fetches that record from the server; if not, it builds a fresh blank one:

```csharp
if (!String.IsNullOrWhiteSpace(id)) {
    Model.NavigationId = id;
    Model.ViewIsEditPage = true;
    _newTag = false;
    _title = "EditTag";

    var getTag = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/GetTag/" + id);
    if (getTag != null) {
        _tag = getTag;
    } else {
        Model.UnknownError();
    }
} else {
    _newTag = true;
    _title = "AddNewTag";
    _tag = new DataObjects.Tag {
        TenantId = Model.TenantId,
        TagId = Guid.Empty,
        Enabled = true,
    };
}
```

**`Helpers.GetOrPost<T>`** is the single wrapper used for *all* server calls. Its signature is:

```csharp
public static async Task<T?> GetOrPost<T>(string url, object? post = null, bool logResults = false)
```

The clever bit is the second argument. **If you pass no `post` object, it does an HTTP GET; if you pass one, it does a POST** with that object as JSON. So one method covers both "fetch a record" and "save a record." It also attaches the tenant id and auth token to every request automatically — which is exactly why you call it instead of using a raw `HttpClient`. The `<T>` is the type you expect back; here `DataObjects.Tag` (a single record) or, below, `DataObjects.BooleanResponse` (a yes/no result with messages).

### The form

The body is a series of labeled inputs bound to `_tag` with `@bind`. A required text field looks like this:

```razor
<div class="mb-2">
    <label for="edit-tag-Name">
        <Language Tag="TagName" Required="true" />
    </label>
    <input type="text" id="edit-tag-Name" @bind="_tag.Name" @bind:event="oninput"
           class="@Helpers.MissingValue(_tag.Name, "form-control")" />
</div>
```

`@bind="_tag.Name"` ties the textbox to the record property in both directions. `@bind:event="oninput"` updates as you type. `Helpers.MissingValue(...)` adds a red "this is missing" style when the field is empty — instant visual validation without writing any CSS logic.

### Saving

`Save` does three things in order: validate, then POST, then react. Validation collects human-readable errors and the id of the first field to focus:

```csharp
List<string> errors = new List<string>();
string focus = String.Empty;

if (String.IsNullOrWhiteSpace(_tag.Name)) {
    errors.Add(Helpers.MissingRequiredField("TagName"));
    if (focus == String.Empty) { focus = "edit-tag-Name"; }
}

if (errors.Any()) {
    Model.ErrorMessages(errors);
    await Helpers.DelayedFocus(focus);
    return;
}
```

If validation passes, it shows a "saving" message and POSTs the whole record. Notice the same `GetOrPost` — but now with a second argument, so it becomes a POST:

```csharp
Model.Message_Saving();

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

The returned object carries an `ActionResponse` that says whether the save succeeded (`Result`) and, if not, why (`Messages`). On success the page navigates back to the list; on failure it shows the server's messages; if nothing comes back at all, it shows a generic error. **One record-type covers both add and update** — the same `SaveTag` endpoint handles a brand-new record (id `Guid.Empty`) and an existing one.

### Deleting

`Delete` is shorter. It calls a delete endpoint that returns a simple boolean response, and on success removes the record from the in-memory list and navigates back:

```csharp
var deleted = await Helpers.GetOrPost<DataObjects.BooleanResponse>("api/Data/DeleteTag/" + id);

if (deleted != null) {
    if (deleted.Result) {
        Model.Tags = Model.Tags.Where(x => x.TagId.ToString() != id).ToList();
        Helpers.NavigateTo("Settings/Tags");
    } else {
        Model.ErrorMessages(deleted.Messages);
    }
} else {
    Model.UnknownError();
}
```

The Delete button itself is wrapped in a `<DeleteConfirmation ... />` component so a user cannot delete with a single misclick — they confirm first. And the button only renders when `!_newTag`, because there is nothing to delete on a record you have not saved yet. If the record was already soft-deleted, the page instead shows an `<UndeleteMessage ... />` offering to restore it rather than the normal form. Nothing is hard-deleted from the UI.

---

<a id="wiring"></a>
## 5. Wiring the Templates Together

The two pages are joined by four threads: routing, the active-view flag, navigation, and live updates.

### Routing — two URLs per page, for tenants

Each page declares its routes at the very top with `@page`. Notice there are always two forms — one plain, one with a tenant code prefix:

```razor
@page "/Settings/Tags"
@page "/{TenantCode}/Settings/Tags"
```

The edit page declares even more, because it serves both add and edit:

```razor
@page "/Settings/EditTag/{id}"
@page "/{TenantCode}/Settings/EditTag/{id}"
@page "/Settings/AddTag"
@page "/{TenantCode}/Settings/AddTag"
```

`{TenantCode}` and `{id}` are **route parameters** — placeholders in the URL that arrive in your code as the `[Parameter]` properties of the same name. The tenant-prefixed forms let a URL name its tenant explicitly, which is why the very first thing `OnAfterRenderAsync` does is `Model.TenantCodeFromUrl = TenantCode;` and why it later calls `Helpers.ValidateUrl(TenantCode)` — to make sure the URL's tenant matches the logged-in user's. This is a core safety check; do not remove it from a stamped screen.

### `Model.View` — only one screen is "live"

Every page sets `Model.View = _pageName` in `OnInitialized`, and every page wraps its markup in `@if (Model.Loaded && Model.View == _pageName)`. Together these guarantee that only the screen currently on display renders and reacts. When you navigate from Tags to EditTag, `Model.View` flips from `"tags"` to `"edittag"`, the old page goes quiet, and the new one comes alive. This is also how the shared event handlers know whether an update is meant for them.

### Navigation — list to edit and back

You never write raw `<a href>` to move between the pair (except for plain "Back" links). You call the wrapper:

- List → Add: an anchor to `Helpers.BuildUrl("Settings/AddTag")`.
- List → Edit: `Helpers.NavigateTo("Settings/EditTag/" + TagId)`.
- Edit → List (Back, after Save, after Delete): `Helpers.NavigateTo("Settings/Tags")`.

`BuildUrl` and `NavigateTo` both keep the tenant context in the URL for you, so navigation never accidentally drops the tenant.

### Live updates — SignalR

**SignalR is the technology that lets the server push a message to the browser in real time**, so two people editing the same data stay in sync. Both pages subscribe to `Model.OnSignalRUpdate` in `OnInitialized` and unsubscribe in `Dispose`. The handler checks that the update is for *this* view, is the right kind, and did not come from *this* user — only then does it react:

```csharp
protected void SignalRUpdate(DataObjects.SignalRUpdate update)
{
    if (update.UpdateType == DataObjects.SignalRUpdateType.Tag
        && Model.View == _pageName
        && update.UserId != Model.User.UserId) {
        StateHasChanged();
    }
}
```

On the edit page the handler is richer: if another user *deletes* the record you are editing, it navigates you away with a message; if they *save* it, it swaps in their updated version so you are not editing a stale copy. The `update.UserId != Model.User.UserId` check is what stops your own changes from echoing back at you. You get all of this for free by keeping the subscribe/handle/unsubscribe trio intact.

---

<a id="customizing"></a>
## 6. Customizing Without Breaking the Mold

The template gives you a lot of safe room. The rule of thumb: **change the record-specific content freely, leave the plumbing alone.**

Safe to change — this is the *expected* customization:

- **The columns** on the list table. Add, remove, reorder `<th>`/`<td>` to show your record's fields.
- **The form fields** on the edit page. Each is a self-contained `<div class="mb-2">` with a label and a bound input. Copy one, point its `@bind` at a different property, give it a unique `id`, and add a matching `<Language Tag="..." />`.
- **Validation rules** in `Save`. Add more `if (...) { errors.Add(...); }` blocks before the POST. Follow the existing shape so the first-error focus keeps working.
- **The record type** itself — your screen works with `DataObjects.YourThing` instead of `DataObjects.Tag`, and calls `GetYourThing` / `SaveYourThing` / `DeleteYourThing` endpoints.

Designed extension points — hooks the template already provides so you do not have to fork it:

- **`BlazorPlugins`** blocks (`Top`, `Bottom`, toolbar `Button`) let optional plugin code inject UI into the page without editing it. You will see `Helpers.HaveBlazorPlugins(_pageName, "Top")` guards in the markup.
- **`_App` companion components** like `<EditTag_App ... />` are designated spots for app-specific extra fields, kept separate so the core stays clean. The Save method even calls `AppModule.Save(_tag)` and folds its errors into the same list.
- **`{{ModuleItemStart:...}}` / `{{ModuleItemEnd:...}}` comment markers** wrap feature-specific blocks. A build utility can strip these out when a module is not in use, so leave the markers in place even if you do not touch what is inside them.

Leave alone unless you truly know why — touching these is how stamped screens break:

- The **four state flags** and their load-once guards.
- The **`@page` route declarations**, especially the tenant-prefixed forms.
- **`OnInitialized` / `Dispose`** event subscribe/unsubscribe — they must stay paired.
- The **`TenantCodeFromUrl` / `ValidateUrl`** tenant check in `OnAfterRenderAsync`.
- The **`Model.View == _pageName`** guard wrapping the markup.

If you find yourself wanting to rip out the plumbing, that is the signal you may be building something that is not really CRUD — revisit Section 2.

---

<a id="pitfalls"></a>
## 7. Common Pitfalls and Fixes

These are the failures that do not show up at compile time and only bite later.

- **Forgetting to unsubscribe in `Dispose` (or dropping `@implements IDisposable`).** The page keeps reacting after you leave it, leaking handlers and causing phantom redraws on the wrong screen. *Fix:* every `+=` in `OnInitialized` needs a matching `-=` in `Dispose`, and the page must declare `@implements IDisposable`.

- **A stale or wrong `_pageName`.** If you copy `Tags.razor` to make `Widgets.razor` and leave `_pageName = "tags"`, two pages now claim the same view. The wrong page renders, or neither does. *Fix:* give every page a unique lowercase `_pageName` and use it consistently — it must match what the page sets into `Model.View`.

- **Loading data on every render instead of once.** If you drop the `if (!_loadedData) { _loadedData = true; ... }` guard, the page hammers the server on each re-render. *Fix:* keep the load-once latch; only `LoadData`/`LoadTag` should re-fetch deliberately.

- **Using a raw `HttpClient` instead of `Helpers.GetOrPost`.** A raw call goes out without the tenant id and auth token, so the server rejects it — or, worse, serves data with no tenant scope. *Fix:* always go through `GetOrPost<T>`; pass a body to POST, omit it to GET.

- **Hard-coding visible text.** Typing `"Save"` as a literal breaks localization the moment a non-English user opens the screen. *Fix:* use `<Language Tag="Save" />` (and add the tag to the language data) for every label, button, and message.

- **Showing a Delete button on a brand-new record.** Deleting something that was never saved makes no sense and can error. *Fix:* guard Delete behind `!_newTag`, exactly as the template does, and keep the `<DeleteConfirmation>` wrapper so deletes are intentional.

- **Editing a property that is not on the record type, or forgetting to send the whole `_tag`.** `Save` POSTs the entire `_tag` object; if your new field is not a property on `DataObjects.Tag`, your edits silently vanish. *Fix:* add the field to the data object first, then bind to it.

- **Ignoring the SignalR handler.** If you strip out `SignalRUpdate`, two users editing the same record will overwrite each other with no warning. *Fix:* keep the handler; it is what makes multi-user editing safe.

---

<a id="related-docs"></a>
## 8. Related Docs

- [032 — Building From the Shared Component Shelf](032_shared-components.md) — shared pieces used in pages
- [033 — Charts, Editors, Signatures, and Graphs](033_rich-components.md) — richer components for pages
- [035 — Validated, Translated, and Reachable](035_validation-localization-a11y.md) — the validation and a11y baseline
- [005 — Shipping Your First Record Screen](005_first-feature.md) — the first-feature exercise

---
*GuidesV2 · 031 · drafted from source (`CRM.Client/Pages/Settings/Tags/Tags.razor`, `EditTag.razor`, `CRM.Client/Helpers.cs`).*

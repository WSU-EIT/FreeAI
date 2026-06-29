# 034 — Leading Users Through a Multi-Step Wizard

> **Document ID:** 034  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Explain the multi-step wizard component, state hand-off between steps, and per-step validation gates.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 03x (Core Craft: Everyday Screens and Components) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Wizards Matter](#why-wizards) | Plain-language overview, key terms, and the honest fact that FreeCRM has no wizard component |
| 2 | [When to Reach for a Wizard](#when-to-use) | Good fits versus a single long form |
| 3 | [Anatomy of the Wizard Pattern](#anatomy) | The `view` state machine, step body, and footer buttons — from real code |
| 4 | [State Hand-Off Between Steps](#state-handoff) | How data survives a step change via local fields and the shared `Model` |
| 5 | [Per-Step Validation Gates](#validation-gates) | Blocking the next step until the current one is valid |
| 6 | [Navigation, Back, and Skips](#navigation) | Moving forward, backward, and skipping steps conditionally |
| 7 | [Submitting and Handling Failure](#submit) | The final commit, error recovery, and staying on the step |
| 8 | [Common Pitfalls and Checklist](#pitfalls) | Mistakes to avoid and a ship checklist |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-wizards"></a>
## 1. Why Wizards Matter

**Why it matters:** A long form full of fields scares people. A wizard takes that same form and breaks it into a few small, friendly screens, so the user only thinks about one decision at a time. That is the whole point — reduce the feeling of "this is too much" and catch mistakes early, before the user has typed twenty fields and lost them all to one error at the end.

Let us define the words first, because the rest of this doc leans on them:

- **Wizard** — a single feature that walks the user through a task as a sequence of screens, with Back/Next buttons, instead of showing everything at once.
- **Step** — one of those screens. A step usually shows a handful of related inputs (for example, "pick your database type" or "enter your email and password").
- **Gate** — a check that runs when the user tries to move forward. If the current step is not filled in correctly, the gate blocks the move and shows what is wrong. Think of a turnstile that will not turn until you have a valid ticket.
- **State hand-off** — keeping the user's answers alive as they move from step to step. If they type their name on step 1 and you forget it by step 3, the hand-off failed.

**The honest, important fact:** FreeCRM core ships **no dedicated wizard or stepper component.** We checked the shared component library at `FreeCRM/CRM.Client/Shared` (and its `AppComponents` subfolder) — there is no `Wizard.razor`, no `Stepper.razor`, no `Step.razor`, and no reusable multi-step control of any kind. There is also no such component anywhere else in `CRM.Client`.

So this doc is **not** "here is the component to drop in." It is "here is how FreeCRM actually builds multi-step flows, written as a pattern you can copy." And FreeCRM does build them — two real, shipping examples:

- `FreeCRM/CRM.Client/Pages/Authorization/Login.razor` — the login flow walks through provider selection, tenant selection, the local email/password screen, forgot-password, sign-up, and code validation. That is a wizard in everything but name.
- `FreeCRM/CRM.Client/Pages/Settings/Misc/Setup.razor` — the first-run database setup, whose own on-screen text literally calls itself a "wizard."

Everything below is grounded in those two files.

<a id="when-to-use"></a>
## 2. When to Reach for a Wizard

**Why it matters:** Reaching for a wizard when a single form would do just adds clicks. Reaching for a single form when a wizard is needed overwhelms people and buries errors. Picking right is most of the battle.

Reach for the multi-step pattern when **any** of these are true:

- **The path branches.** Later inputs depend on an earlier choice. In `Setup.razor`, choosing "SQL Server" reveals server/database/credential fields; choosing "SQLite" reveals a single file-path field. Showing all of them at once would be confusing — most are irrelevant to the user's choice.
- **The flow has natural phases.** `Login.razor` has genuinely separate moments: *which tenant?*, *which login method?*, *enter credentials*, *recover password*. Each is its own screen.
- **A step must succeed before the next makes sense.** In sign-up, the account must be created on the server *before* the "enter your validation code" screen can mean anything.

Stick with **one single form** (the everyday Edit page from [031](031_crud-templates.md)) when the fields are few, all required at once, and none of them reveal or hide the others. A plain Edit page with a Save button is simpler to build and simpler to use. Do not split a five-field form into five steps just because wizards look fancy — that is the most common over-engineering mistake here.

Rule of thumb: **branching or phases → wizard pattern; flat and short → single form.**

<a id="anatomy"></a>
## 3. Anatomy of the Wizard Pattern

**Why it matters:** Because there is no component to lean on, you assemble the wizard yourself from three plain parts. Once you see the three parts, every FreeCRM multi-step flow reads the same way.

### Part 1 — The "current step" variable

A single string field holds which step is showing. In both real files it is literally called `view`:

```csharp
protected string view = String.Empty;     // Login.razor
protected string view = "loading";         // Setup.razor
```

That one variable *is* the wizard's memory of where the user is. Nothing fancier than a string.

### Part 2 — The step body: one big switch

The markup is a `@switch` on that variable. Each `case` is one step. Only the matching case renders, so the user sees exactly one step at a time:

```razor
@switch (view) {
    case "selectloginprovider":
        // ... step 1 markup ...
        break;
    case "selecttenant":
        // ... step 2 markup ...
        break;
    case "local":
        // ... credentials step ...
        break;
    // ... forgotpassword, signup, signupvalidate, ...
}
```

(`@switch` is just C#'s `switch` statement used inside Razor markup — the `case` whose label equals `view` is the only block that draws.)

`Setup.razor` does the same thing with a smaller set of steps — `loading`, `setup`, `saving`, `alreadyconfigured` — and then **nests a second switch inside the `setup` step** to reveal the right database fields:

```razor
@switch (databaseType) {
    case "SQLServer":
        // server / database / credentials inputs
        break;
    case "SQLite":
        // single database-path input
        break;
}
```

That nested switch is the "branching path" from Section 2, made real. The progressive reveal (fields appear only after a choice) uses an `@if` guard, e.g. `@if (!String.IsNullOrWhiteSpace(databaseType)) { ... }`.

### Part 3 — The footer: the buttons that move you

Each step renders its own action buttons. There is no shared footer bar; the buttons live inside the step's `case`. From the credentials step in `Login.razor`:

```razor
<button type="button" class="btn btn-dark" @onclick="Back">
    <Language Tag="Back" IncludeIcon="true" />
</button>
<button type="button" class="btn btn-primary" @onclick="ProcessLogin" disabled="@LoginDisabled">
    <Language Tag="Log-In" IncludeIcon="true" />
</button>
```

Two takeaways an intern should notice: the button **text comes from `<Language Tag="..." />`** (so it translates — see [035](035_validation-localization-a11y.md)), and the primary button is **disabled by a computed property** (`LoginDisabled`) until the step is fillable. That `disabled` binding is your first, gentlest gate — covered properly in Section 5.

<a id="state-handoff"></a>
## 4. State Hand-Off Between Steps

**Why it matters:** A wizard is useless if it forgets the user's answers between screens. The good news: because all steps live inside **one component**, hand-off is mostly automatic — but there is one subtlety worth understanding so you do not lose data by accident.

### The two places data lives

**1. Local component fields** — for data used only inside this flow.

In `Login.razor`, the user's typed email and password are plain fields on the component:

```csharp
// email and password are component fields shared by every step
<input type="text" class="form-control" id="login-email" @bind="email" ... />
<input type="password" class="form-control" id="login-password" @bind="password" ... />
```

Because switching steps only changes the `view` string — it does **not** create a new component — these fields keep their values across steps automatically. Step 2 can read what step 1 wrote. The same is true in `Setup.razor`, where one object carries the answers across the whole flow:

```csharp
protected DataObjects.ConnectionStringConfig csConfig = new DataObjects.ConnectionStringConfig();
```

Every database step binds into that single `csConfig` object, and the final submit sends it.

**2. The shared `Model`** — for data the rest of the app also needs.

FreeCRM has one app-wide state container, `BlazorDataModel`, injected into pages as `Model` (it is a `partial class` defined in `FreeCRM/CRM.Client/DataModel.cs`). Steps write into it when a result must outlive the wizard. For example, once a tenant is chosen, `Login.razor` stores it on the shared model so the whole app knows:

```csharp
Model.TenantId = TenantId;
Model.Tenant = tenant;
```

`Model` also carries the notion of "which page is active" via `Model.View`, and it raises an `OnChange` event so any component can redraw when shared state moves:

```csharp
protected override void OnInitialized()
{
    Model.OnChange += OnDataModelUpdated;
    Model.View = _pageName;
}
```

### The rule of thumb

- Answer only matters **inside this wizard** → keep it in a **local field/object** (like `email` or `csConfig`).
- Answer must be **visible to the rest of the app** afterwards → write it to **`Model`** (like `Model.Tenant`).

### The one gotcha

Do **not** re-initialize a local field when you change steps unless you mean to wipe it. `Setup.razor` does this *deliberately* when the user picks a different database type — it throws away the half-typed config so stale values from the old choice cannot leak through:

```csharp
protected async void UpdateDatabaseType(string type)
{
    csConfig = new DataObjects.ConnectionStringConfig();   // intentional reset on branch change
    databaseType = type;
    // ...
}
```

That is the right move when a branch change makes old data meaningless. Doing it accidentally on a plain Back/Next is the bug.

<a id="validation-gates"></a>
## 5. Per-Step Validation Gates

**Why it matters:** The gate is what makes a wizard trustworthy. It catches a problem on *this* step, points the user straight at the bad field, and refuses to advance — so nobody reaches the final submit with broken data. FreeCRM uses one consistent gate recipe; learn it once and reuse it everywhere.

There are two layers of gating, weakest to strongest.

### Layer 1 — Disable the button (soft gate)

A computed property keeps the primary button greyed out until the obvious basics are present. From `Login.razor`'s sign-up step:

```csharp
protected bool SignupSaveDisabled {
    get {
        bool output = false;
        if (String.IsNullOrWhiteSpace(_user.FirstName) ||
            String.IsNullOrWhiteSpace(_user.LastName) ||
            String.IsNullOrWhiteSpace(_user.Email) ||
            String.IsNullOrWhiteSpace(_user.Password)) {
            output = true;
        }
        return output;
    }
}
```

Bind it with `disabled="@SignupSaveDisabled"`. This is friendly but weak — it only checks "not empty," and you should never rely on it alone for anything that matters.

### Layer 2 — Validate on click, then block (hard gate)

The real gate runs inside the step's action method. The recipe is always the same four moves:

1. Start an empty `errors` list and an empty `focus` string.
2. Check each field; on a failure, add a message **and** remember which field to jump to.
3. If there are any errors, show them, move the cursor to the first bad field, and **`return`** so you never advance.
4. Only past the gate do you do the real work.

Straight from the sign-up handler in `Login.razor`:

```csharp
if (String.IsNullOrWhiteSpace(_user.Email)) {
    errors.Add(Helpers.MissingRequiredField("Email"));
    if (focus == String.Empty) { focus = "signup-email"; }
}
if (String.IsNullOrWhiteSpace(_user.Password)) {
    errors.Add(Helpers.MissingRequiredField("Password"));
    if (focus == String.Empty) { focus = "signup-password"; }
}
// ... password-confirm must match, etc. ...

if (errors.Any()) {
    Model.ErrorMessages(errors);
    if (focus != String.Empty) {
        await Helpers.DelayedFocus(focus);
    }
    return;                          // <-- the gate: do not advance
}

Model.Message_Saving();             // only reached when the step is valid
```

What the helpers do, in plain terms:

- `Helpers.MissingRequiredField("Email")` — builds a translated "Email is required"-style message, so you do not hand-write English error strings.
- `Model.ErrorMessages(errors)` — shows the whole list to the user through the app's standard message banner (it lives on `Model`, so the display is consistent app-wide).
- `Helpers.DelayedFocus("signup-email")` — moves the cursor into the offending input (the string is the input's `id`). "Delayed" because it waits a tick for the DOM to settle before focusing.
- The bare `return` — the single most important line. It is the turnstile that stays locked.

`Setup.razor` uses the same shape with a tiny variation: it only tracks `focus` (no message list, because the required fields are marked with a `*` in the UI), and bails the same way:

```csharp
if (focus != String.Empty) {
    await jsRuntime.InvokeVoidAsync("DelayedFocus", focus);
    return;
}
```

For the deeper rules of *what* counts as valid and how those messages get translated, see [035 — Validated, Translated, and Reachable](035_validation-localization-a11y.md).

<a id="navigation"></a>
## 6. Navigation, Back, and Skips

**Why it matters:** Moving between steps is the spine of the wizard. FreeCRM keeps it almost embarrassingly simple — moving is just changing the `view` string — but there are a couple of details (clearing stale messages, skipping steps) that separate a polished flow from a janky one.

### The one method that moves you forward

`Login.razor` centralizes every step change in one tiny helper:

```csharp
protected void SetView(string newView)
{
    Model.ClearMessages();      // wipe the previous step's errors/notices
    view = newView;             // change which case renders
    StateHasChanged();          // ask Blazor to redraw
}
```

Three things, every time, in order:

1. **`Model.ClearMessages()`** — so an error from the step you are leaving does not haunt the step you are entering. Forgetting this is a classic bug: the user fixes step 1, advances, and still sees step 1's red banner.
2. **`view = newView`** — the actual move. Set it to the `case` label of the step you want.
3. **`StateHasChanged()`** — tells Blazor "state moved, please re-render." (Blazor only redraws when it knows something changed; here we change a plain field, so we say so explicitly.)

Higher-level steps just call it with a label, e.g. `SetView("selecttenant")` or `SetView("signupvalidate")`. Often they also move the cursor right after, e.g. `await Helpers.DelayedFocus("signup-validate")`.

### Going Back

Back is the same idea — set `view` to an earlier step. In the login flow, Back simply re-runs the "choose how to log in" entry step:

```csharp
protected async Task Back()
{
    await ShowLoginProviders();
}
```

And notice the markup only *shows* the Back button when going back is meaningful:

```razor
@if (AvailableLoginOptions.Count() > 1) {
    <button type="button" class="btn btn-dark" @onclick="Back">
        <Language Tag="Back" IncludeIcon="true" />
    </button>
}
```

If there is only one login option there is nowhere to go back to, so no button. Good wizards hide controls that cannot do anything.

### Skipping steps conditionally

Skipping is just *deciding the next `view` based on data instead of always picking the same one.* The login flow does exactly this — if there is only one tenant, it never shows the tenant-selection step:

```csharp
if (_tenantListing.Tenants.Count() == 1) {
    Model.Tenant = _tenantListing.Tenants.First();
    Model.TenantId = Model.Tenant.TenantId;
    await ShowLoginProviders();      // skip "selecttenant" entirely
} else {
    SelectTenants();                 // show the "selecttenant" step
}
```

The lesson: a "skipped" step is not a special feature — it is just a branch in your navigation logic that lands on a later `view`. Compute the destination, then `SetView` to it.

<a id="submit"></a>
## 7. Submitting and Handling Failure

**Why it matters:** The last step is where the user's effort either pays off or evaporates. The cardinal rule of wizard submit: **if it fails, do not lose the user's data and do not lose the user's place.** Keep them on the step, show what went wrong, let them fix and retry.

### The shape of a final submit

After the gate passes, you tell the user something is happening, call the server, and branch on the result. From the sign-up submit in `Login.razor`:

```csharp
Model.Message_Saving();

var saved = await Helpers.GetOrPost<DataObjects.User>("api/Data/UserSignUp", _user);

Model.ClearMessages();

if (saved != null) {
    if (saved.ActionResponse.Result) {
        _user = saved;
        SetView("signupvalidate");               // success: advance to next step
        await Helpers.DelayedFocus("signup-validate");
    } else {
        Model.ErrorMessages(saved.ActionResponse.Messages);   // server said no
    }
} else {
    Model.UnknownError();                        // call itself failed
}
```

Read the three outcomes carefully, because they are the whole pattern:

- **Success (`saved.ActionResponse.Result` is true).** Only now do we move forward with `SetView`. The server's response (`saved`) is captured back into `_user`, so the next step works with fresh, server-blessed data.
- **Business failure (server reachable but unhappy).** We surface the server's own messages via `Model.ErrorMessages(...)` and — crucially — we **do not change `view`.** The user stays on the step with everything they typed intact.
- **Hard failure (call did not even return).** `Model.UnknownError()` shows a generic "something went wrong." Again, no step change.

(`Helpers.GetOrPost<T>(url, payload)` is FreeCRM's standard wrapper for calling an API endpoint and getting back a typed object — here a `DataObjects.User`. `ActionResponse` is the response envelope every saved object carries: `Result` is the success boolean and `Messages` is the list of things to tell the user.)

### Partial recovery — the local-login example

The login submit shows the gentler recovery move: when a step both fails *and* the failure should reset the inputs, it clears the sensitive fields and re-shows the same step so the user can try again cleanly:

```csharp
} else {
    if (!errors.Any()) {
        errors.Add("Invalid Login");
    }
    email = String.Empty;
    password = String.Empty;
    await ShowLocalLogin();          // re-render the same step, fields cleared
}

if (errors.Any()) {
    Model.ErrorMessages(errors);
}
```

The pattern to internalize: **success advances; failure stays put, explains, and lets them retry.** Never advance on a failed submit, and never silently discard what the user typed unless re-entry is the safer choice (like wiping a wrong password).

<a id="pitfalls"></a>
## 8. Common Pitfalls and Checklist

**Why it matters:** Almost every wizard bug in this codebase pattern comes from the same short list. Knowing them up front saves you the debugging.

### Anti-patterns to avoid

- **Forgetting `Model.ClearMessages()` on a step change.** The previous step's red banner sticks around and confuses the user. `SetView` does this for you — so route *every* step change through one helper rather than setting `view` by hand in ten places.
- **Forgetting `StateHasChanged()`.** You set `view` and... nothing visibly happens, because Blazor was not told to redraw. The screen looks frozen.
- **Advancing on a failed submit.** Always `return` after `Model.ErrorMessages(...)` in a gate, and only `SetView(...)` inside the success branch.
- **Re-initializing local state on a normal Back/Next.** Resetting `csConfig`/`_user` on a plain navigation wipes the user's answers. Only reset on a *branch change* where the old data is genuinely meaningless (as `UpdateDatabaseType` does on purpose).
- **Hand-writing English error and button text.** Use `Helpers.MissingRequiredField(...)` for errors and `<Language Tag="..." />` for buttons so the flow translates. See [035](035_validation-localization-a11y.md).
- **Not focusing the bad field.** Collect a `focus` (the input's `id`) during validation and call `Helpers.DelayedFocus(focus)` so the cursor lands where the user must fix something.
- **Over-splitting.** Turning a short, flat form into a multi-step flow just adds clicks. If there is no branch and no phase, use a single Edit page ([031](031_crud-templates.md)).
- **Building a "reusable Wizard component" speculatively.** FreeCRM deliberately does not have one. Match the existing `view`-switch pattern so the next person recognizes it; do not invent a parallel framework for a single screen.

### Pre-ship checklist

- [ ] One `view` (or equivalent) string field holds the current step.
- [ ] Markup is a `@switch (view)` with one `case` per step; only one step renders at a time.
- [ ] Every step change goes through a single helper that does `ClearMessages()` → set `view` → `StateHasChanged()`.
- [ ] Data the user types lives in local fields/objects that survive step changes; data the app needs later is written to `Model`.
- [ ] Each forward move has a validation gate that collects `errors`, sets `focus`, shows `Model.ErrorMessages(errors)`, focuses the bad field, and `return`s without advancing on failure.
- [ ] Back/skip buttons only appear when they can actually do something; skipped steps are just a computed destination.
- [ ] Final submit advances **only** on `ActionResponse.Result == true`; business and hard failures stay on the step, explain, and keep the user's data.
- [ ] All visible text (buttons, errors, labels) comes through `<Language Tag="..." />` / translation helpers.

---

<a id="related-docs"></a>
## 9. Related Docs

- [033 — Charts, Editors, Signatures, and Graphs](033_rich-components.md) — the rich-component family
- [031 — List and Edit, the House Pattern](031_crud-templates.md) — the page templates it lives in
- [035 — Validated, Translated, and Reachable](035_validation-localization-a11y.md) — per-step validation

---
*GuidesV2 034 · drafted from source (`Login.razor`, `Setup.razor`, `DataModel.cs`, `Helpers.cs`) · 2026-06-05 — FreeCRM core ships no wizard component; documented as the established `view`-switch pattern.*

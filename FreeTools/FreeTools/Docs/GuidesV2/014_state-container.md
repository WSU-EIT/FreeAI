# 014 — The Living State Container

> **Document ID:** 014  ·  **Category:** Concept  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Teach the client-side container holding the current user, tenant, and cached lists as the app heartbeat.
> **Audience:** Newcomers gaining competence  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 01x (Mental Models: How This Differs From Stock .NET) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why the State Container Matters](#why-it-matters) | Plain-language intro and key terms (container, state, tenant, cache) defined |
| 2 | [The Mental Model: One Heartbeat](#mental-model) | The single shared `BlazorDataModel` every page reads from |
| 3 | [An Analogy You Already Know](#analogy) | A shared whiteboard the whole room watches |
| 4 | [What Lives Inside: User, Tenant, Lists](#whats-inside) | `User`, `Tenant`/`TenantId`, and cached lists like `Tags` |
| 5 | [How Components Read and React](#read-react) | `@inject`, the `OnChange` event, and `StateHasChanged` |
| 6 | [Lifecycle: Birth, Updates, Disposal](#lifecycle) | Loading via the model loader, `TriggerUpdate`, tenant switches |
| 7 | [Common Pitfalls and Gotchas](#pitfalls) | The `ObjectsAreEqual` guard, leaked subscriptions, stale reads |
| 8 | [Where to Go Next](#next-steps) | Related docs and hands-on follow-ups |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why the State Container Matters

When you open the app, dozens of separate screen pieces appear at once: the top menu, the toast notifications in the corner, the page body, the tenant switcher. Every one of them needs to know the same facts — *who is logged in, which customer account we are looking at, what the current language is.* If each piece fetched those facts on its own, they would disagree with each other and hammer the server with duplicate requests. The **state container** exists to prevent exactly that.

Let us define the key terms in plain language, because the rest of this doc leans on them:

- **State** — the current facts the app is holding in memory right now: the logged-in user, the active account, the loaded lists. State is just "what's true at this moment," nothing more mysterious than that.
- **Container** — a single object whose job is to *hold* that state in one place so everyone reads from the same copy. In this codebase the container is a C# class named **`BlazorDataModel`**, defined in `FreeCRM/CRM.Client/DataModel.cs` (namespace `CRM.Client`).
- **Tenant** — one isolated customer account inside the system. This is a **multi-tenant** app, meaning one running copy of the software serves many separate organizations, and each is walled off from the others. The container always knows which tenant you are currently working in.
- **Cache** — a kept-in-memory copy of data so you don't have to re-fetch it from the server on every click. The container caches lists like tags, departments, and languages.

**Why it matters:** the container is the difference between an app that feels instant and consistent versus one that flickers, disagrees with itself, and re-loads constantly. It is registered once and shared by the whole browser session, so there is exactly one source of truth. The class comment in the source says it plainly:

```csharp
/// <summary>
/// The Model used on every page in the Blazor application to share database in the interface.
/// </summary>
public partial class BlazorDataModel
```

(The word *Blazor* here just means Microsoft's framework for writing interactive web UIs in C# instead of JavaScript. You don't need to know Blazor deeply to follow this doc — treat it as "the web UI layer.")

---

<a id="mental-model"></a>
## 2. The Mental Model: One Heartbeat

Picture the container as the app's single **heartbeat**. There is exactly one of it, it beats whenever something changes, and every part of the UI listens for that beat and refreshes itself.

Two facts make this work, and both are visible in the real code.

**Fact one: there is only ever one container.** It is registered as a **singleton** — a service the framework creates once and hands the *same instance* to everyone who asks. From `FreeCRM/CRM.Client/Program.cs`:

```csharp
builder.Services.AddSingleton<BlazorDataModel>();
```

`AddSingleton` is the load-bearing word. If it said `AddScoped` or `AddTransient`, different pages might get different copies and the "single source of truth" promise would break. Because it is a singleton, when the menu updates the user's name, the page body sees the very same updated object.

**Fact two: the heartbeat is a C# event named `OnChange`.** An **event** is just a notification channel: code can *subscribe* to be told when something happens, and the container *fires* the event when state changes. From `DataModel.cs`:

```csharp
/// <summary>
/// The OnChange event that can be subscribed to in a view or component to be notified when this model changes.
/// </summary>
public event Action? OnChange;

private void NotifyDataChanged() => OnChange?.Invoke();
```

Every property setter in the container ends by calling `NotifyDataChanged()`, which invokes `OnChange`. So the rhythm is always the same:

> **something writes to a property → the property fires `NotifyDataChanged()` → `OnChange` invokes → every subscribed component re-renders.**

That single rhythm — write, notify, re-render — is the whole heartbeat. Sections 5 and 6 show each step in real code.

---

<a id="analogy"></a>
## 3. An Analogy You Already Know

Think of a busy operations room with one big **shared whiteboard** on the wall.

- The whiteboard is the **container** (`BlazorDataModel`). It holds the facts everyone needs: "Current user: Dana," "Account: Acme Corp," "Language: English."
- The people in the room are the **components** — the menu, the toasts, the page body. None of them keeps a private notepad of these facts; they all just glance at the whiteboard.
- When a fact changes, whoever changes it **rings a bell** so the whole room looks up. That bell is the `OnChange` event, and ringing it is `NotifyDataChanged()`.
- Each person has agreed to "look at the board whenever the bell rings." In code, that agreement is `Model.OnChange += StateHasChanged` — subscribing their re-render to the bell.

The analogy also explains one important refinement. Imagine someone walks to the board, reads "Account: Acme Corp," and rewrites it as... "Account: Acme Corp." Nothing actually changed, so ringing the bell would just waste everyone's attention. The real container guards against this: it only updates and rings the bell when the new value is genuinely different. That guard is the `ObjectsAreEqual` check you'll meet in Section 7. The whiteboard is shared, but the bell only rings when there is real news.

---

<a id="whats-inside"></a>
## 4. What Lives Inside: User, Tenant, Lists

The container holds three broad kinds of state. Understanding these three buckets is enough to read most of the codebase.

### 4.1 The current user

`User` is the logged-in person. Its real declaration:

```csharp
/// <summary>
/// The User object for the current user, or an empty User object if no user is logged in.
/// </summary>
public DataObjects.User User {
    get { return _User; }
    set {
        if (!ObjectsAreEqual(_User, value)) {
            _User = value;
            _ModelUpdated = DateTime.UtcNow;
            NotifyDataChanged();
        }
    }
}
```

Note the design: when nobody is signed in, `User` is **not null** — it is an *empty* `User` object. That spares the rest of the app from constant null checks. Alongside it sit `LoggedIn` (a simple `bool`) and `Users` — the list of *all* accounts this person owns, since one human may have a login in more than one tenant.

### 4.2 The current tenant

The active account is split across a few related members:

- `Tenant` — the full `DataObjects.Tenant` object (its name, settings, branding, and so on).
- `TenantId` — a `Guid` (a globally-unique 128-bit identifier) that pins down *which* tenant. Most lookups key off this id.
- `Tenants` and `AllTenants` — the accounts available to switch into. `AllTenants` is only populated for app-wide administrators.

```csharp
/// <summary>
/// The current TenantId.
/// </summary>
public Guid TenantId {
    get { return _TenantId; }
    set {
        if (_TenantId != value) {
            _TenantId = value;
            _ModelUpdated = DateTime.UtcNow;
            NotifyDataChanged();
        }
    }
}
```

The tenant is the spine of the multi-tenant design: nearly every cached list below belongs to *this* tenant, and switching tenants reloads them (Section 6).

### 4.3 The cached lists

These are the kept-in-memory collections that make the UI feel instant. A representative example is `Tags`:

```csharp
/// <summary>
/// The list of Tag objects.
/// </summary>
public List<DataObjects.Tag> Tags {
    get { return _Tags; }
    set {
        if (!ObjectsAreEqual(_Tags, value)) {
            _Tags = value;
            _ModelUpdated = DateTime.UtcNow;
            NotifyDataChanged();
        }
    }
}
```

Every cached list follows this identical shape — compare value, store, stamp `_ModelUpdated`, then notify. Other lists in the same container include `Departments`, `DepartmentGroups`, `Languages`, `UserGroups`, `Locations`, `Services`, and `EmailTemplates`. (Several are wrapped in `// {{ModuleItemStart:...}}` markers, meaning they only exist when that optional feature module is compiled in — but the pattern is always the same.)

There is also a fourth, lighter bucket worth knowing: **ephemeral UI state** like `View` (which screen is showing), `NavigationId`, `Loaded` (has the initial load finished), and `Messages` (the toast notifications). These are not data from the database — they are the app's moment-to-moment mood — but they live in the same container and use the same notify-on-change machinery.

---

<a id="read-react"></a>
## 5. How Components Read and React

A component is one reusable piece of UI — a `.razor` file. To use the shared state it does two things: **ask for the container**, then **subscribe to the heartbeat**. The real `ToastMessages.razor` component (in `FreeCRM/CRM.Client/Shared/`) is a compact, complete example.

**Step 1 — ask for the container.** The `@inject` line means "framework, hand me the shared `BlazorDataModel`." Because of the singleton registration from Section 2, every component that injects it gets the exact same object:

```razor
@implements IDisposable
@inject BlazorDataModel Model
```

**Step 2 — read state directly.** The markup just reads from `Model` as if it were a normal object:

```razor
@if (Model.Messages.Any()) {
    @foreach (var message in Model.Messages.OrderByDescending(x => x.Shown)) {
        ...
    }
}
```

**Step 3 — subscribe to the heartbeat.** `StateHasChanged` is the framework method that says "re-draw me." By wiring it to `OnChange`, the component re-draws whenever *anything* in the container changes:

```razor
@code {
    protected override void OnInitialized()
    {
        Model.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        Model.OnChange -= StateHasChanged;
    }
}
```

Read that pairing carefully, because it is the single most important pattern in this whole doc:

- `OnInitialized` runs once when the component appears. The `+=` **subscribes** the component's redraw to the bell.
- `Dispose` runs once when the component goes away. The `-=` **unsubscribes** it. This is why the file declares `@implements IDisposable` at the top — it is promising the framework "call my `Dispose` on the way out."

The why-it-matters: subscribe on the way in, unsubscribe on the way out, *every single time*. Forgetting the `-=` is the classic memory leak, covered in Section 7. Get this pair right and a component automatically stays in sync with the rest of the app for free — it never has to ask "did something change?", it simply redraws when the bell rings.

---

<a id="lifecycle"></a>
## 6. Lifecycle: Birth, Updates, Disposal

The container has a life story with three chapters: it is born and filled, it is updated over and over, and individual subscribers come and go.

### 6.1 Birth — the initial load

The singleton object itself is created empty when the app starts (every field has a sensible default — empty lists, an empty `User`, `Loaded = false`). It is then *populated* from the server. A purpose-built payload class, **`DataObjects.BlazorDataModelLoader`**, is fetched over HTTP and poured into the container. From `Helpers.cs`:

```csharp
blazorDataModelLoader = await GetOrPost<DataObjects.BlazorDataModelLoader>("api/Data/GetBlazorDataModel/");
...
Model.BlazorDataModelLoader = blazorDataModelLoader != null
    ? blazorDataModelLoader
    : new DataObjects.BlazorDataModelLoader();
```

Once the load finishes, the container's `Loaded` flag flips to `true`. Components commonly gate their first render on `Model.Loaded` so they don't try to draw before the data has arrived.

### 6.2 Updates — three ways state changes

1. **A property setter is assigned.** Any code writing `Model.Tags = newTags;` triggers the compare → store → notify cycle automatically. This is by far the most common path.

2. **An explicit nudge with `TriggerUpdate`.** Sometimes you mutate the *insides* of an object without replacing it, so no setter fires. For those cases the container exposes a manual bell-ringer:

   ```csharp
   /// <summary>
   /// The method used to notify pages of data model updates.
   /// </summary>
   public void TriggerUpdate()
   {
       _ModelUpdated = DateTime.UtcNow;
       NotifyDataChanged();
   }
   ```

3. **A real-time push from the server.** The app can receive live updates pushed from the backend, and the container surfaces them through a dedicated `OnSignalRUpdate` event (separate from `OnChange`). The flow of those live pushes is its own topic — see doc 046 in Related Docs.

### 6.3 Tenant switches — a special, signposted update

Changing tenants is a big enough event that the container fires *two extra* events bracketing it — one as the switch begins, one when it completes — so components can react to the change of account specifically:

```csharp
/// <summary>
/// An event that can be subscribed to when the tenant is being changed.
/// </summary>
public event Action? OnTenantChanging;

/// <summary>
/// An event that can be subscribed to for when the tenant change has completed.
/// </summary>
public event Action? OnTenantChanged;
```

### 6.4 Disposal — subscribers leave, the container stays

Here is the subtlety that trips people up: the **container is never disposed** during a normal session — it is the singleton, it lives as long as the browser tab. What gets disposed are the **components** subscribing to it. When a page navigates away, each of its components runs `Dispose` and detaches with `Model.OnChange -= StateHasChanged`. The whiteboard remains on the wall; only the people leaving the room stop watching it.

---

<a id="pitfalls"></a>
## 7. Common Pitfalls and Gotchas

Four mistakes account for almost all state-container bugs newcomers hit.

### 7.1 Forgetting to unsubscribe (the leak)

If a component does `Model.OnChange += StateHasChanged` in `OnInitialized` but omits the matching `-=` in `Dispose`, the container keeps a reference to a component that should be gone. The component never gets garbage-collected, and worse, its `StateHasChanged` keeps firing on a dead object. **Rule:** every `+=` needs a `-=`, and the component must declare `@implements IDisposable`. This is exactly the symmetric pair shown in Section 5 — treat it as one inseparable unit.

### 7.2 Mutating innards without ringing the bell

Because notifications fire from *setters*, changing the contents of an already-stored object does not notify anyone:

```csharp
Model.Tags.Add(newTag);   // mutates the list in place — NO setter runs, NO OnChange fires
```

The UI will look stale until something else happens to trigger a redraw. Fix it either by reassigning the whole property (`Model.Tags = updatedList;`) or by manually calling `Model.TriggerUpdate()` after the in-place change.

### 7.3 Misreading the "no-op" guard as a bug

Every setter wraps its work in an equality check and *skips everything* if the value hasn't really changed:

```csharp
set {
    if (!ObjectsAreEqual(_Tags, value)) {
        _Tags = value;
        _ModelUpdated = DateTime.UtcNow;
        NotifyDataChanged();
    }
}
```

`ObjectsAreEqual` compares the two values by serializing both to JSON and comparing the text. This is a deliberate optimization — it prevents pointless re-renders when you assign an identical value. Newcomers sometimes "fix" a perceived problem by assigning the same data and are surprised nothing redraws. That is the guard working as designed, not a bug. If you genuinely need a forced refresh, call `TriggerUpdate()`.

### 7.4 Reading state before it has loaded

At the very start, the container is empty and `Loaded` is `false`. Reading, say, `Model.User` before the initial load returns the *empty* placeholder user, not the real one. Always gate first-paint logic on `Model.Loaded` (and `Model.LoggedIn` where sign-in matters) rather than assuming the data is already there.

---

<a id="next-steps"></a>
## 8. Where to Go Next

You now know what the container is, what it holds, how components read and react to it, and how it lives and dies. Good follow-ups:

- **See the heartbeat without leaks.** Doc 015 drills into the `OnChange` subscribe/dispose pattern and how to listen safely — the natural next read after this one.
- **Watch state arrive live.** Doc 046 covers how real-time pushes from the server flow into the container via the SignalR path mentioned in Section 6.2.
- **Diagnose a misbehaving container.** When state looks stale, wrong, or out of sync in a running app, doc 085 (the diagnostics playbook) walks through tracking it down.
- **Keep the container fast at scale.** As tenants and cached lists grow, the equality guard and re-render volume start to matter; doc 086 covers performance considerations that touch this state model.
- **Hands-on practice.** Open `FreeCRM/CRM.Client/Shared/ToastMessages.razor`, find the `OnInitialized`/`Dispose` pair, then trace one cached list (try `Tags`) from its setter in `DataModel.cs` to a component that displays it. Seeing the full write → notify → render path once makes the whole model click.

---

<a id="related-docs"></a>
## 9. Related Docs

- [015 — Listening for Change Without Leaking](015_listening-for-change.md) — how its change events fire
- [046 — Pushing State Live Over SignalR](046_realtime-signalr.md) — real-time updates flow into it
- [006 — Speaking the Local Dialect](006_local-dialect.md) — glossary entry
- [085 — Diagnostics Playbook](085_diagnostics-playbook.md) — tracking down stale or wrong state in a running app
- [086 — Performance at Scale](086_performance-at-scale.md) — keeping the container and its re-renders fast as data grows

---
*GuidesV2 014 · drafted from source (`FreeCRM/CRM.Client/DataModel.cs`, `Program.cs`, `Shared/ToastMessages.razor`) · 2026-06-05.*

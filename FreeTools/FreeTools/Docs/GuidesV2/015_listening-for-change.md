# 015 — Listening for Change Without Leaking

> **Document ID:** 015  ·  **Category:** Concept  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Explain the container change events and how components subscribe and unsubscribe cleanly to re-render.
> **Audience:** Newcomers gaining competence  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 01x (Mental Models: How This Differs From Stock .NET) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | What change events are, and the leak they prevent — in plain language |
| 2 | [The Mental Model: A Newsroom](#mental-model) | The container as publisher, your pages as subscribers |
| 3 | [What Triggers a Change Event](#change-triggers) | The `NotifyDataChanged()` call inside every guarded setter |
| 4 | [Subscribing to Re-Render](#subscribing) | The real `OnInitialized` + `OnChange +=` pattern from the source |
| 5 | [Unsubscribing Cleanly](#unsubscribing) | `IDisposable`, `Dispose()`, and `OnChange -=` to release the handler |
| 6 | [Common Pitfalls and Leaks](#pitfalls) | Forgotten unsubscribes, render storms, and stale closures |
| 7 | [Putting It Together](#putting-it-together) | One full subscribe → render → unsubscribe lifecycle from `Tags.razor` |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

The whole app shares one in-memory object that holds the current state — the logged-in user, the list of tags, which page is showing, and so on. That object is `BlazorDataModel` (it lives in `CRM.Client/DataModel.cs`, in the `CRM.Client` namespace). Doc 014 calls it the "living state container." This doc answers the next obvious question: **when that shared state changes, how does every page on screen find out so it can redraw itself?**

The answer is a **change event**. A change event is a small announcement the container broadcasts that says "something I hold just changed." It carries no data — it is just a signal. Any page that wants to stay current **subscribes** to that signal (registers a function to be called when it fires), and when the signal arrives it asks Blazor to **re-render** (redraw the part of the screen it owns).

Two terms you will see throughout:

- **Subscription** — attaching your function to the event so it runs each time the event fires. In C# this is the `+=` operator on an event.
- **Leak** — forgetting to detach that function when your page goes away. The container keeps a reference to a page that no longer exists, so the page can never be cleaned up from memory, and worse, its handler keeps running and trying to redraw a dead page. That is a **memory leak**, and it is the single biggest hazard in this pattern.

Why it matters to you as a non-engineer steering this code: this is the one place where "it works on my screen" and "it slowly eats memory and throws errors after an hour" look identical at first. The pattern below is small and mechanical, but you must do **both halves** — subscribe *and* unsubscribe. The codebase already does this consistently; your job is to recognize the pattern and never break the second half.

---

<a id="mental-model"></a>
## 2. The Mental Model: A Newsroom

Think of a newswire service.

- **The container (`BlazorDataModel`) is the newsroom.** It holds the current facts and is the only one allowed to declare "the news just changed."
- **`OnChange` is the wire.** It is the channel the announcement travels on. In the code it is literally an event named `OnChange`.
- **`NotifyDataChanged()` is the editor hitting "publish."** Every time a fact in the newsroom is updated, the editor pushes a bulletin onto the wire. The bulletin has no headline text — it just says "refresh."
- **Each page or component is a subscriber** with a standing order: "whenever a bulletin comes over the wire, re-read the facts and redraw my section of the paper."
- **Unsubscribing is canceling your standing order** when you leave. If you walk out of the building but never cancel, the newsroom keeps printing your section forever, even though no one is reading it.

The critical insight from this analogy: the newsroom never knows or cares who is listening. It just publishes. That is what makes the design clean — the container has no idea which pages exist. But it is also why the responsibility to *stop* listening falls entirely on each subscriber. The newsroom will never remove you from its list; you must remove yourself.

---

<a id="change-triggers"></a>
## 3. What Triggers a Change Event

A change event fires from exactly one private method on the container:

```csharp
private void NotifyDataChanged() => OnChange?.Invoke();
```

`OnChange?.Invoke()` means "if anyone is subscribed, call all their handlers." The `?` guards against the case where nobody is listening (then it does nothing). The event itself is declared like this:

```csharp
/// <summary>
/// The OnChange event that can be subscribed to in a view or component to be notified when this model changes.
/// </summary>
public event Action? OnChange;
```

`Action` is just .NET's name for "a function that takes no arguments and returns nothing" — which fits, because the event carries no payload, only the "refresh" signal.

**So what calls `NotifyDataChanged()`?** Almost every property that holds shared state. The container follows one consistent recipe in its setters: only change the value if it is actually different, then announce. For example, the `Loaded` flag:

```csharp
public bool Loaded {
    get { return _Loaded; }
    set {
        if (_Loaded != value) {
            _Loaded = value;
            _ModelUpdated = DateTime.UtcNow;
            NotifyDataChanged();
        }
    }
}
```

And a collection property like `ActiveUsers`:

```csharp
public List<DataObjects.ActiveUser> ActiveUsers {
    get { return _ActiveUsers; }
    set {
        if (!ObjectsAreEqual(_ActiveUsers, value)) {
            _ActiveUsers = value;
            _ModelUpdated = DateTime.UtcNow;
            NotifyDataChanged();
        }
    }
}
```

Two things to notice, because they matter to performance:

1. **The equality guard comes first.** Simple values use `!=`; objects and lists use a helper, `ObjectsAreEqual(...)`. If the new value equals the old one, the setter does nothing — no mutation, no notification, no re-render. This is what stops the app from redrawing itself over identical data.
2. **`NotifyDataChanged()` is the last step**, after the value and the `_ModelUpdated` timestamp are set. So by the time subscribers wake up and re-read the container, the new value is already in place.

In the current source, `NotifyDataChanged()` is called from roughly five dozen setters and methods — `Loaded`, `View`, `User`, the message/toast helpers, and most of the cached lists. You do not need to memorize them. The takeaway is: **any time you assign to a public property on the container, assume it can fire a change event**, and assume every subscribed page will redraw.

There are also a few **specialized** events alongside `OnChange` for cases that need to carry data or signal a specific moment:

```csharp
public event Action<List<string>>? OnDotNetHelperHandler;   // messages from JavaScript
public event Action<DataObjects.SignalRUpdate>? OnSignalRUpdate; // live server pushes (see Doc 046)
public event Action? OnTenantChanged;                       // tenant switch finished
public event Action? OnTenantChanging;                      // tenant switch starting
```

`OnChange` is the workhorse for ordinary re-renders. The others are opt-in and used by pages that care about those specific situations.

---

<a id="subscribing"></a>
## 4. Subscribing to Re-Render

A page subscribes in its **`OnInitialized`** method — the Blazor lifecycle hook that runs once, right after the component is created and before its first render. That is the correct, earliest place to start listening. Here is the real pattern, lifted from `Pages/Settings/Tags/Tags.razor`:

```csharp
protected override void OnInitialized()
{
    Model.OnChange += OnDataModelUpdated;
    Model.OnSignalRUpdate += SignalRUpdate;
    Model.View = _pageName;
}
```

`Model` is the container, injected into the page with `@inject BlazorDataModel Model` at the top of the `.razor` file. The line `Model.OnChange += OnDataModelUpdated;` is the subscription: "call my `OnDataModelUpdated` method every time the container publishes a change."

The handler it points to is small and, importantly, **guarded**:

```csharp
protected void OnDataModelUpdated()
{
    if (Model.View == _pageName) {
        StateHasChanged();
    }
}
```

`StateHasChanged()` is Blazor's way of saying "mark me dirty and redraw me." The guard `if (Model.View == _pageName)` is a deliberate optimization: `OnChange` fires for *every* state change anywhere in the app, but this page only needs to redraw when **it** is the page currently on screen. `_pageName` is a constant the page sets for itself (for Tags it is `"tags"`), and `Model.View` is the container's record of which page is active. So a tag page ignores change events that arrive while the user is looking at, say, the invoices screen.

A simpler variant appears in `Layout/MainLayout.razor`, which always wants to redraw and so subscribes `StateHasChanged` directly with no wrapper:

```csharp
Model.OnChange += StateHasChanged;
```

Both forms are valid. Use a named wrapper (`OnDataModelUpdated`) when you want to guard the redraw; subscribe `StateHasChanged` directly only when the component genuinely should react to every change.

---

<a id="unsubscribing"></a>
## 5. Unsubscribing Cleanly

This is the half you can never skip. A page declares that it can clean itself up by implementing the `IDisposable` interface — a standard .NET contract meaning "I hold something that must be released; call my `Dispose()` when you are done with me." In a `.razor` file that declaration is one line at the top:

```razor
@implements IDisposable
```

Blazor then automatically calls your `Dispose()` method when the page is torn down — for example when the user navigates away. Inside `Dispose()`, you detach every handler you attached, using `-=`, the mirror image of the `+=` you used to subscribe. From the same `Tags.razor`:

```csharp
public void Dispose()
{
    Model.OnChange -= OnDataModelUpdated;
    Model.OnSignalRUpdate -= SignalRUpdate;
}
```

Why this is non-negotiable: the container holds the subscriber list. As long as your handler is on that list, the container holds a reference to your page object, so the .NET garbage collector cannot reclaim it — that is the memory leak. And every future `NotifyDataChanged()` would still call your dead page's handler. `-=` removes you from the list, severing the reference. The page becomes eligible for collection, and it stops being called.

The rule is symmetric and easy to audit: **for every `+=` in `OnInitialized`, there must be a matching `-=` in `Dispose`.** Same event, same handler. `MainLayout.razor` shows the same discipline:

```csharp
public void Dispose()
{
    dotNetHelper?.Dispose();
    Model.OnChange -= StateHasChanged;
    NavManager.LocationChanged -= LocationChanged;
    DialogService.OnClose -= ClearModelMessages;
}
```

Notice it unsubscribes from several different sources, not just `OnChange`. The principle generalizes: anything you `+=` to, you `-=` from here.

---

<a id="pitfalls"></a>
## 6. Common Pitfalls and Leaks

**Forgetting `@implements IDisposable`.** Without it, Blazor never calls your `Dispose()`, so your unsubscribe code never runs even if you wrote it. The page leaks. If you subscribe to `OnChange`, the declaration at the top of the file is mandatory.

**Subscribing but not unsubscribing.** The classic leak: a `+=` in `OnInitialized` with no matching `-=` in `Dispose`. Every time the user visits and leaves the page, another dead copy is pinned in memory, each still firing on every change event. Symptoms are gradual: slowdowns and console errors that appear only after navigating around for a while. Audit fix: count the `+=` and `-=` lines for the page — they must match one-for-one.

**Mismatched handler in `-=`.** `-=` only removes the *exact* handler you passed. If you subscribe with `OnDataModelUpdated` but try to unsubscribe with a different method (or an anonymous lambda you cannot reference again), the removal silently does nothing and you leak. This is one reason the codebase uses **named methods**, not inline lambdas, for these handlers — a named method can be detached by name.

**Removing the equality guard in a setter.** If you add a container property and call `NotifyDataChanged()` unconditionally — without the `if (!ObjectsAreEqual(...))` or `if (_x != value)` check shown in Section 3 — you can trigger a **render storm**: redraws firing for assignments that did not actually change anything, sometimes in a loop. Always follow the existing setter recipe: guard, mutate, timestamp, notify.

**Forgetting the `Model.View` guard.** Subscribing `StateHasChanged` directly (or an unguarded handler) on a deep content page means it redraws on every change anywhere in the app, even while it is off screen. It is not a correctness bug, but it is wasted work. Prefer the guarded `if (Model.View == _pageName)` wrapper for ordinary pages, as `Tags.razor` does.

**Stale closures.** A *closure* is a function that "captures" variables from where it was defined. If you subscribe with a lambda that captures a local value, that lambda keeps using the captured value even after it goes out of date — and, as above, you usually cannot unsubscribe it. Stick to instance methods that read fresh state from `Model` at call time.

---

<a id="putting-it-together"></a>
## 7. Putting It Together

Here is the complete lifecycle for one real page, `Pages/Settings/Tags/Tags.razor`, in the order it happens. Read it as a single round trip: declare → subscribe → react → unsubscribe.

**1. Declare the cleanup contract and inject the container** (top of the `.razor` file):

```razor
@page "/Settings/Tags"
@implements IDisposable
@inject BlazorDataModel Model
```

**2. Subscribe on creation** (`OnInitialized`, runs once):

```csharp
protected override void OnInitialized()
{
    Model.OnChange += OnDataModelUpdated;
    Model.OnSignalRUpdate += SignalRUpdate;
    Model.View = _pageName;
}
```

**3. React to each change, but only when this page is on screen:**

```csharp
protected void OnDataModelUpdated()
{
    if (Model.View == _pageName) {
        StateHasChanged();
    }
}
```

**4. Somewhere else entirely, a change happens.** Anyone — this page's own load code, another component, or a live server push over SignalR — assigns to a guarded container property. That setter runs `NotifyDataChanged()`, which does `OnChange?.Invoke()`, which calls every subscriber's handler. The tag page's `OnDataModelUpdated` runs, sees it is the active view, and redraws with the latest data.

**5. Unsubscribe on teardown** (`Dispose`, called automatically by Blazor when the user leaves):

```csharp
public void Dispose()
{
    Model.OnChange -= OnDataModelUpdated;
    Model.OnSignalRUpdate -= SignalRUpdate;
}
```

That is the entire contract. The container never tracks who is listening or stops calling anyone — it only publishes. Each page is fully responsible for both ends: starting to listen in `OnInitialized`, and stopping in `Dispose`. Get both halves right, match every `+=` with a `-=`, and your UI stays live without leaking a single page.

---

<a id="related-docs"></a>
## 8. Related Docs

- [014 — The Living State Container](014_state-container.md) — the container that emits the events
- [046 — Pushing State Live Over SignalR](046_realtime-signalr.md) — where most live changes originate
- [031 — List and Edit, the House Pattern](031_crud-templates.md) — how pages subscribe and clean up

---
*GuidesV2 · 015 · drafted from source (`CRM.Client/DataModel.cs`, `Pages/Settings/Tags/Tags.razor`, `Layout/MainLayout.razor`) · 2026-06-04.*

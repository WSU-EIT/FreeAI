# 047 — Growing the Shared Library

> **Document ID:** 047  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Teach how to author new reusable components that respect the wrapper, state, and style conventions.
> **Audience:** Advanced builders and extenders  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 04x (Extending Without Breaking: The Live Runtime) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Shared Components Matter](#why-it-matters) | What a component is, and why a shared shelf beats copy-paste |
| 2 | [When to Build vs. Reuse](#when-to-build) | A short test for whether a new component is justified |
| 3 | [Anatomy of a Compliant Component](#anatomy) | The wrapper, state, and style contracts every component honors |
| 4 | [Honoring the Wrapper Convention](#wrapper) | Where files live, how they are named, and how JavaScript is wired in |
| 5 | [Managing State the Shared Way](#state) | Parameters, two-way binding, the shared model, and disposing cleanly |
| 6 | [Styling Without Breaking the System](#styling) | Using the `Language` tag, icons, and CSS without fighting the app |
| 7 | [Worked Example and Checklist](#example) | A small JS-backed component built end to end, plus a ship checklist |
| 8 | [Common Pitfalls and Fixes](#pitfalls) | The mistakes that leak memory, break themes, or skip translation |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Shared Components Matter

**Why this matters first:** every screen in this product is assembled from small, reusable pieces. If each developer rebuilds the same upload box, code editor, or tooltip from scratch, the app slowly drifts — five subtly different upload boxes, five sets of bugs, five places to fix when the design changes. A shared library means you build the upload box *once*, get it right, and everyone reuses it. Growing that library correctly is what this doc is about.

A few terms, defined in plain language before we lean on them:

- **Component** — a self-contained chunk of user interface plus the logic that drives it. In this codebase a component is a Blazor *Razor* file (a `.razor` file), which mixes HTML-like markup with C# code. Think of it as a custom HTML tag you invent, like `<UploadFile />`, that you can drop into any page.
- **Blazor** — Microsoft's framework for building web UI in C# instead of JavaScript. The `.razor` files are compiled to C# that runs in the browser (WebAssembly).
- **Parameter** — an input you pass into a component, written as an HTML-like attribute: `<MonacoEditor Language="csharp" ReadOnly="true" />`. In C# these are properties marked `[Parameter]`.
- **The shared library / "the shelf"** — the folder `CRM.Client/Shared`. Every `.razor` file in there is a vetted, reusable component available to the whole app.
- **State** — the data a component holds while it is on screen (the current value in an editor, whether an upload is in progress). "Managing state" means changing that data and getting the screen to re-draw.
- **Style** — the visual appearance (colors, spacing, fonts). The app has one design system, and components must inherit it rather than hard-code their own look.

The shared shelf is the *what* (doc [032](032_shared-components.md) catalogs the pieces already on it). This doc is the *how*: how to add a new piece without breaking the conventions the existing pieces all follow. Those conventions are not bureaucracy — they are the reason a component written two years ago still themes correctly, still translates into every language, and still cleans up after itself today.

---

<a id="when-to-build"></a>
## 2. When to Build vs. Reuse

**Why this matters:** the most expensive component is the one you didn't need to write. Before adding to the shelf, make sure the shelf doesn't already hold what you want, and that what you want is genuinely reusable.

Run this quick test:

1. **Does it already exist?** Search `CRM.Client/Shared` first. The library already covers a lot: file upload (`UploadFile.razor`), a Monaco code editor (`MonacoEditor.razor`), charts (`Highcharts.razor`), PDF viewing (`PDF_Viewer.razor`), tooltips (`Tooltip.razor`), loading and toast messages, file pickers, tag selectors, and more. If a close match exists, prefer configuring it through its parameters over writing a new one (see [032 — Building From the Shared Component Shelf](032_shared-components.md)).

2. **Is it used in more than one place — or will it be?** A shared component earns its keep through reuse. If a piece of UI is only ever shown on a single page and you cannot imagine a second caller, leave it inline on that page. Promote it to `Shared` when the second caller appears.

3. **Is it a self-contained concept?** Good shared components have one clear job and a clean set of inputs: "edit code," "show a chart," "upload a file." If the thing you are describing needs to reach into a specific page's private logic to work, it is not really shared — it is part of that page.

4. **Can its inputs and outputs be expressed as parameters?** A reusable component talks to the outside world only through `[Parameter]` inputs and callbacks. If you cannot list the handful of parameters it would take, the boundary isn't clear yet.

If the answer to 2, 3, and 4 is "yes" and the answer to 1 is "no," build it — and follow the contracts in the rest of this doc so it fits the shelf.

---

<a id="anatomy"></a>
## 3. Anatomy of a Compliant Component

**Why this matters:** every component on the shelf follows the same three implicit contracts. Honor them and your component will feel native, theme correctly, and not leak resources. Break them and it will be the odd one out that causes support tickets.

The three contracts, at a glance:

| Contract | The promise it makes | Where it shows up in code |
|----------|----------------------|---------------------------|
| **Wrapper** | "I live where the app expects, am named consistently, and load any JavaScript the supported way." | File location in `CRM.Client/Shared`, the `.razor` / `.razor.js` pairing, the module-import pattern |
| **State** | "I take inputs as parameters, support two-way binding where it makes sense, react to the shared model, and tear myself down cleanly." | `[Parameter]`, `EventCallback<T>`, `@inject BlazorDataModel Model`, `@implements IDisposable` |
| **Style** | "I use the app's translation and theming machinery instead of hard-coded text and colors." | `<Language Tag="..." />`, `<Icon />`, CSS classes / `styledMode` rather than inline colors |

Here is the smallest component on the shelf that touches all the moving parts you will use — `Tooltip.razor`, adapted and condensed (the real source uses Allman braces and multi-line `[Parameter]` attributes):

```razor
@implements IDisposable

@if (!String.IsNullOrWhiteSpace(TipText)) {
    <i @ref="_element" @onclick="ShowTooltip" class="tooltip-item">@((MarkupString)_icon)</i>
}

@code {
    protected string _icon = String.Empty;
    protected ElementReference _element;

    [Parameter] public string? Icon { get; set; }
    [Parameter] public string? TipText { get; set; }
    [Parameter] public TooltipOptions? Options { get; set; }

    public void Dispose() { }

    protected override void OnInitialized() {
        if (!String.IsNullOrWhiteSpace(Icon)) {
            _icon = Icon;
        } else {
            _icon = "<i class=\"icon fa-solid fa-circle-info\"></i>";
        }
    }

    protected void ShowTooltip() {
        if (!String.IsNullOrWhiteSpace(TipText)) {
            Helpers.Tooltip(_element, TipText, Options);
        }
    }
}
```

Notice the shape already: a small markup block at the top, then an `@code { }` block holding `[Parameter]` inputs, protected `_`-prefixed backing fields, lifecycle methods (`OnInitialized`), and helper calls (`Helpers.Tooltip`). The next three sections unpack the three contracts one at a time.

---

<a id="wrapper"></a>
## 4. Honoring the Wrapper Convention

**Why this matters:** "the wrapper convention" is the set of unwritten rules about *where a component lives, how it is named, and how it reaches JavaScript.* Get these right and the build system, the auto-import list, and the browser all cooperate. Get them wrong and your component either won't be visible to other files or will load stale JavaScript.

### 4.1 Where the file lives, and what it's called

Put the component in `CRM.Client/Shared`. Anything in that folder is automatically available everywhere, because `CRM.Client/_Imports.razor` already brings the namespace into scope for the whole project:

```razor
@using CRM.Client.Shared
```

That single line is why a page can write `<UploadFile />` or `<MonacoEditor />` with no per-file `@using`. The namespace of your component is `CRM.Client.Shared`, and the tag name is simply the file name without the extension. So `Tooltip.razor` becomes `<Tooltip />`. Name the file after the job it does, in PascalCase.

### 4.2 When you need JavaScript: the `.razor.js` pairing

Blazor runs C# in the browser, but some libraries (a code editor, a charting engine, the clipboard API) are JavaScript. The convention here is a **colocated JavaScript module** — a file with the *same name* as the component plus `.razor.js`. The pairs already on the shelf:

```
MonacoEditor.razor   +  MonacoEditor.razor.js
Highcharts.razor     +  Highcharts.razor.js
UploadFile.razor     +  UploadFile.razor.js
UserDefinedFields.razor + UserDefinedFields.razor.js
```

A `.razor.js` file is an **ES module**: it `export`s named functions that C# can call. The simplest possible one, from `UploadFile.razor.js`:

```javascript
export function CopyPasswordToClipboard(password) {
    navigator.clipboard.writeText(password);
    console.log("Password Copied to Clipboard");
}
```

### 4.3 Loading the module — the exact pattern, and why the `?v=` is there

You load the module once, on the component's first render, and keep the reference. This is adapted from the pattern in `MonacoEditor.razor` — copy its shape:

```razor
@inject IJSRuntime jsRuntime

@code {
    protected IJSObjectReference? jsModule;

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Shared/MonacoEditor.Razor.js?v=" + Guid.NewGuid().ToString().Replace("-", ""));
        }
    }
}
```

Three things to copy exactly:

- **`@inject IJSRuntime jsRuntime`** — `IJSRuntime` is Blazor's bridge to the browser's JavaScript. Injecting it gives you `jsRuntime`, your handle for calling into JS.
- **`if (firstRender)`** — `OnAfterRenderAsync` runs after *every* re-draw; you only want to import the module the first time, so guard with `firstRender`.
- **`?v=` + a fresh GUID** — this is a **cache-buster**. A *GUID* is a random unique string; appending it as a query parameter makes the browser treat every load as a new URL so it never serves a stale, cached copy of your JavaScript after a deploy. Every component on the shelf does this.

Once you hold `jsModule`, you call exported functions by name. To call JS and ignore the result, use `InvokeVoidAsync`; to get a value back, use `InvokeAsync<T>`:

```razor
await jsModule.InvokeVoidAsync("SetAriaLabel", EditorId, AriaLabel);
```

### 4.4 When JavaScript needs to call *back* into C#

Some components need the reverse direction — JavaScript firing an event back into your C# (a chart slice was clicked, a library finished loading). The convention is a **`DotNetObjectReference`**: a wrapper that hands JavaScript a safe handle to your component instance. Section 7 builds one end to end; the key point here is that it is the *only* sanctioned way for JS to reach back, and it must be disposed (covered next).

---

<a id="state"></a>
## 5. Managing State the Shared Way

**Why this matters:** "state" is everything your component remembers while it is on screen, and "managing it the shared way" means using the same four mechanisms every other component uses, so callers get a predictable experience and nothing leaks memory.

### 5.1 Inputs are `[Parameter]`s — the only front door

A reusable component receives all its inputs as parameters. Mark each one `[Parameter]`, give it a sensible default, and document whether it is required or optional. From `MonacoEditor.razor`:

```razor
/// <summary>
/// REQUIRED: The language for the editor (defaults to 'plaintext'.)
/// </summary>
[Parameter] public string Language { get; set; } = MonacoLanguage.plaintext;

/// <summary>
/// OPTIONAL: Set to true to make the editor read-only.
/// </summary>
[Parameter] public bool ReadOnly { get; set; }
```

That XML-doc comment marked `REQUIRED` / `OPTIONAL` is the house habit — it tells the next developer, in IntelliSense, exactly what each input does.

### 5.2 Two-way binding: the `Value` / `ValueChanged` pair

**Why this matters:** a *bind* lets a parent page both set a value and be told when the component changes it, with one tidy attribute. To support `@bind-Value`, expose a `Value` parameter and a matching `ValueChanged` callback. An `EventCallback<T>` is Blazor's way of saying "call back to my parent with a value of type T." From `MonacoEditor.razor`:

```razor
/// <summary>
/// REQUIRED: The value to display in the editor.
/// </summary>
[Parameter] public string Value { get; set; } = String.Empty;

/// <summary>
/// The internal method allowing for 2-way binding with the @bind-Value option instead of @bind.
/// </summary>
[Parameter] public EventCallback<string> ValueChanged { get; set; }
```

When your value changes internally, raise the callback and ask Blazor to re-draw — again straight from `MonacoEditor.razor`:

```razor
protected void ValueHasChanged() {
    ValueChanged.InvokeAsync(Value);
    StateHasChanged();
}
```

`StateHasChanged()` is Blazor's "re-render me now" call. You only need it when state changes outside the normal flow (in a timer callback, a JS callback, or after manual work); event handlers triggered by the user re-render automatically.

For components that hand back something other than a simple value — like `UploadFile.razor`, which returns uploaded files — the house pattern is a `Delegate` callback parameter the component invokes when its work is done:

```razor
[Parameter] public Delegate? OnUploadComplete { get; set; }
```

### 5.3 The shared model: `BlazorDataModel`

**Why this matters:** the app keeps app-wide state — the current tenant, the logged-in user, the active language, loading messages — in one shared object called `BlazorDataModel`, registered once for the whole app (`AddSingleton<BlazorDataModel>()` in `Program.cs`). Inject it whenever your component needs that context:

```razor
@inject BlazorDataModel Model
```

`LoadingMessage.razor` is the whole story in two lines — it reads a value straight off the model:

```razor
@inject BlazorDataModel Model
<h2 class="loading">@((MarkupString)Model.LoadingMessage)</h2>
```

The model raises an event when anything on it changes. If your component must re-draw when shared state shifts (for example, when the user switches language), subscribe to that event in `OnInitialized` and — critically — *unsubscribe* in `Dispose`. From `Language.razor`:

```razor
@implements IDisposable
@inject BlazorDataModel Model

@code {
    public void Dispose() {
        Model.OnChange -= StateHasChanged;
    }

    protected override void OnInitialized() {
        Model.OnChange += StateHasChanged;
    }
}
```

`Model.OnChange` is declared as `public event Action? OnChange;` and fires whenever the shared model is updated.

### 5.4 Tear down cleanly — `@implements IDisposable`

**Why this matters:** anything you *attach* — an event subscription, a timer, a `DotNetObjectReference`, a JS module — keeps your component alive in memory until you let it go. Failing to release it is a **memory leak**: the component lingers invisibly, and over a long session the app slows down. The rule is simple and universal on this shelf: declare `@implements IDisposable` and release everything in `Dispose()`.

`MonacoEditor.razor` disposes its timer:

```razor
@implements IDisposable

@code {
    protected System.Timers.Timer _timer = new System.Timers.Timer();

    public void Dispose() {
        _timer.Dispose();
    }
}
```

`Highcharts.razor` disposes its `DotNetObjectReference`:

```razor
protected DotNetObjectReference<Highcharts>? dotNetHelper;

public void Dispose() {
    dotNetHelper?.Dispose();
}
```

If your `Dispose` is empty today (like `Tooltip.razor`'s), keep the `@implements IDisposable` in place anyway — it documents the contract and gives you the hook the moment you add a subscription.

---

<a id="styling"></a>
## 6. Styling Without Breaking the System

**Why this matters:** the app has one design system and ships in multiple languages. A component that hard-codes English text or a hex color looks fine in isolation and wrong everywhere else — untranslated for half the users, off-theme in dark mode. The conventions below keep your component in step with the rest of the app for free.

### 6.1 Never hard-code user-facing text — use `<Language>`

Any string a person reads goes through the `Language` component, which looks the text up by a *tag* (a short key) and returns the translation for the current language. From `RequiredIndicator.razor`:

```razor
<div class="required-indicator">
    <i class="required-flag"></i>
    <Language Tag="IndicatesRequiredField" />
</div>
```

`UploadFile.razor` uses the same approach for its instructions and even passes an icon flag:

```razor
<Language Tag="SupportedFileTypes" />:
@((MarkupString)SupportedFileTypesList.ToUpper())
...
<Language Tag="UploadingWait" IncludeIcon="true" />
```

Behind the scenes `Language` calls `Helpers.Text(tag, ...)` to resolve the string, and it can transform case, add a required-field flag, or prepend an icon via its parameters. The takeaway: if you find yourself typing literal sentences into markup, stop and add a tag instead.

### 6.2 Use the shared `Icon` and `Helpers` instead of bespoke markup

The app has an `<Icon Name="..." />` component and a static `Helpers` class full of vetted utilities — `Helpers.Tooltip(...)`, `Helpers.ConsoleLog(...)`, `Helpers.SerializeObject(...)`, `Helpers.SetTimeout(...)`. Reach for those before inventing your own. They are how the existing components stay consistent, and they are auto-available without extra `@using` lines.

### 6.3 Let the app theme you — prefer classes over inline colors

Style through CSS classes, not inline colors, so your component inherits the active theme. Two patterns appear on the shelf:

- **Class-driven markup.** `Tooltip.razor` renders `class="tooltip-item"`; `UploadFile.razor` toggles a drag class (`drag-and-drop-upload` plus a `drag-highlight` modifier). The colors live in the app's stylesheets, so the theme controls them.
- **Styled-mode for embedded libraries.** Where a third-party library would otherwise paint its own colors, the convention is to switch it into the app's styling. Every Highcharts render passes `styledMode: true`, which tells Highcharts to drop its built-in palette and take colors from CSS instead:

```javascript
Highcharts.chart(elementId, {
    chart: { type: 'column', styledMode: true },
    credits: { enabled: false },
    ...
});
```

If you must inject component-scoped CSS, follow `MonacoEditor.razor`, which builds a small stylesheet from its parameters and emits it through a `<style>` block — but reach for that only when a plain class won't do.

---

<a id="example"></a>
## 7. Worked Example and Checklist

**Why this matters:** the contracts click into place once you see them assembled. Here is a compact, faithful walk-through of the round-trip that the chart component uses — JavaScript calling *back* into C# — because that is the trickiest convention and the one most worth seeing whole. The snippets below are adapted and condensed from `Highcharts.razor` and `Highcharts.razor.js` (the real source uses Allman braces and multi-line `[Parameter]` attributes).

**Step 1 — Hold the references you will need to dispose.** On the C# side, keep both the JS module and a `DotNetObjectReference` to yourself:

```razor
@inject Microsoft.JSInterop.IJSRuntime jsRuntime

@code {
    protected IJSObjectReference? jsModule;
    protected DotNetObjectReference<Highcharts>? dotNetHelper;
}
```

**Step 2 — On first render, import the module, hand JS a reference to yourself, and kick things off.** `DotNetObjectReference.Create(this)` wraps the component so JavaScript can call its methods; you pass that wrapper to the module so it can keep it:

```razor
protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
        dotNetHelper = DotNetObjectReference.Create(this);
        jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./Shared/Highcharts.Razor.js?v=" + Guid.NewGuid().ToString().Replace("-", ""));
        await jsModule.InvokeVoidAsync("SetDotNetHelper", dotNetHelper);
        await jsModule.InvokeVoidAsync("LoadHighchartsResources");
    }
}
```

**Step 3 — Expose a `[JSInvokable]` method for JS to call.** The `[JSInvokable]` attribute is what makes a C# method reachable from JavaScript. Here the chart tells C# which slice was clicked, and C# fans it out to whatever callback the parent page supplied:

```razor
[Parameter] public Delegate? OnItemClicked { get; set; }

[JSInvokable]
public void ChartItemClicked(int index) {
    if (OnItemClicked != null) {
        OnItemClicked.DynamicInvoke(index);
    }
}
```

**Step 4 — On the JavaScript side, store the reference and call back through it.** The module saves the handle from `SetDotNetHelper` and later invokes the C# method by name with `invokeMethodAsync`:

```javascript
var dotNetHelper;

export function SetDotNetHelper(value) {
    dotNetHelper = value;
}

// ...inside a chart click handler:
click: function (event) {
    dotNetHelper.invokeMethodAsync("ChartItemClicked", this.index);
}
```

**Step 5 — Dispose the reference.** Without this, the `DotNetObjectReference` keeps the component pinned in memory:

```razor
public void Dispose() {
    dotNetHelper?.Dispose();
}
```

That is the entire JS-to-C# round trip the shelf uses. A component without JavaScript (most of them) skips steps 1–4 and is just parameters, the shared model, and a clean `Dispose`.

### Ship Checklist

Before you add a component to `Shared`, confirm:

- [ ] **Lives in `CRM.Client/Shared`** and is named in PascalCase after its job.
- [ ] **All inputs are `[Parameter]`s**, each with an XML-doc comment noting `REQUIRED` or `OPTIONAL` and a sensible default.
- [ ] **Supports `@bind-Value`** via a `Value` parameter + `ValueChanged` `EventCallback<T>` *if* it edits a value; or exposes a clear callback (`Delegate`) for other outputs.
- [ ] **No hard-coded user-facing text** — every readable string goes through `<Language Tag="..." />`.
- [ ] **No hard-coded colors** — style via CSS classes (and `styledMode` / equivalent for embedded libraries) so the theme applies.
- [ ] **Uses shared `Helpers` / `Icon`** instead of re-implementing utilities.
- [ ] **JavaScript, if any, is a colocated `.razor.js` module**, imported on `firstRender` with the `?v=` cache-buster, stored in an `IJSObjectReference`.
- [ ] **Any JS-to-C# callback uses `DotNetObjectReference` + `[JSInvokable]`.**
- [ ] **`@implements IDisposable`** is present, and `Dispose()` releases every subscription, timer, JS reference, and `DotNetObjectReference`.
- [ ] **Subscribes to `Model.OnChange` only if it must re-draw on shared-state changes** — and unsubscribes in `Dispose`.

---

<a id="pitfalls"></a>
## 8. Common Pitfalls and Fixes

**Why this matters:** these are the mistakes that pass code review by looking fine and then cause slow leaks, untranslated screens, or stale-after-deploy bugs in production.

| Pitfall | Why it bites | The fix |
|---------|--------------|---------|
| **Subscribing to `Model.OnChange` but not unsubscribing** | The model holds a reference to your disposed component forever — a classic memory leak that compounds over a session. | Mirror every `Model.OnChange += StateHasChanged;` in `OnInitialized` with `Model.OnChange -= StateHasChanged;` in `Dispose`, exactly as `Language.razor` does. |
| **Forgetting to dispose a `DotNetObjectReference` or timer** | Same leak, different source — JavaScript or a timer keeps the component pinned. | Implement `IDisposable` and call `dotNetHelper?.Dispose()` / `_timer.Dispose()`, as in `Highcharts.razor` and `MonacoEditor.razor`. |
| **Importing the JS module on every render** | `OnAfterRenderAsync` fires on every re-draw; re-importing each time is wasteful and can double-register handlers. | Guard the import with `if (firstRender)`. |
| **Dropping the `?v=` cache-buster** | After a deploy, browsers serve a stale cached copy of your JavaScript, so users hit old bugs you already fixed. | Always append `"?v=" + Guid.NewGuid().ToString().Replace("-", "")` to the module path. |
| **Hard-coding English text in markup** | The string never translates; non-English users see raw English. | Route every readable string through `<Language Tag="..." />`. |
| **Inline colors / a library's default palette** | The component ignores the active theme and looks wrong in dark mode or a re-skin. | Style via CSS classes; pass `styledMode: true` (or the equivalent) to embedded libraries. |
| **Calling `StateHasChanged()` everywhere "just in case"** | Redundant re-renders hurt performance and can mask the real flow. | Only call it after out-of-band changes — timer callbacks, JS callbacks, manual updates. User-triggered events re-render on their own. |
| **Reaching into a page's private logic from a "shared" component** | It isn't really shared; it is coupled to one page and will break the next caller. | If it can't be driven purely through `[Parameter]`s and callbacks, leave it inline on the page (see §2). |
| **Re-inventing an existing component** | Duplicate upload boxes / editors multiply bugs and drift visually. | Search `Shared` first and configure the existing component via its parameters ([032](032_shared-components.md)). |

---

<a id="related-docs"></a>
## 9. Related Docs

- [032 — Building From the Shared Component Shelf](032_shared-components.md) — the shelf you are growing
- [033 — Charts, Editors, Signatures, and Graphs](033_rich-components.md) — examples of rich components
- [051 — The Author House Style](051_house-code-style.md) — style conventions to respect

---
*GuidesV2 · 047 · drafted from source on 2026-06-05.*

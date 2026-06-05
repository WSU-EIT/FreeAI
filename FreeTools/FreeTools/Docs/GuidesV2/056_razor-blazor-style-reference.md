# 056 — The Razor / Blazor / HTML Style Reference

> **Document ID:** 056  ·  **Category:** Style  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Codify the verified, citation-backed conventions for writing `.razor` component files: file shape, the `.App.razor` override layer, HTML/markup, component usage and binding, and the `@code` block.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 05x (The House Style: Code Conventions) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [How to Read This Doc (and What Razor/Blazor/HTML Are)](#orientation) | One-paragraph orientation: what the three words mean, and how to use this reference |
| 2 | [Anatomy of a `.razor` File and Directive Order](#anatomy) | The fixed three-section shape and the order the `@`-directives go in |
| 3 | [Pages vs Shared Components, and Multiple Routes](#pages-vs-shared) | What makes a file a routable page, and why one file lists four `@page` lines |
| 4 | [The `.App.razor` Override Convention](#app-convention) | FreeCRM's signature upgrade-safe customization seam — base vs `.App.` |
| 5 | [HTML / Markup Conventions](#markup) | Casing, indentation, Bootstrap-first classes, conditionals/loops, comments |
| 6 | [Components, Parameters, `@bind`, and Events](#components) | Calling components, declaring parameters, two-way binding, event callbacks |
| 7 | [The `@code` Block](#code-block) | Member order, lifecycle methods, the DataModel subscription pattern, localization |
| 8 | [Quick Reference Cheat Sheet](#quick-reference) | Side-by-side ✗/✓ snippets you can copy |
| 9 | [FAQ — "Why Do We Do That?"](#faq) | The real questions interns ask, answered from source |
| 10 | [Related Docs](#related-docs) | Sibling style docs and the component guides |

---

<a id="orientation"></a>
## 1. How to Read This Doc (and What Razor/Blazor/HTML Are)

**Three words, one file.** **HTML** is the markup language that describes a web page's structure — `<div>`, `<input>`, `<button>`. **Razor** is a file format that lets you mix HTML with C# code in the same file, marking the C# parts with an `@` symbol; a Razor file ends in `.razor`. **Blazor** is the framework that takes those `.razor` files, turns each one into a reusable piece of UI called a **component**, and runs them — C# and all — directly in the browser. So when you open a `.razor` file you are looking at HTML, C#, and a handful of `@`-prefixed *directives* (configuration lines) all living together. This document is the rulebook for how FreeCRM writes those files.

**Why a separate doc for this.** Doc [051](051_house-code-style.md) covers C# style in general (braces, casing, `String.Empty`). This doc covers everything specific to `.razor` files: the file's overall shape, the FreeCRM-only `.App.razor` override trick, markup conventions, how components talk to each other, and the highly templated `@code` block at the bottom of every page. For the fine-grained C# *syntax* rules that apply *inside* `@code` (brace placement, casing, `String.Empty`), this doc points you to **doc 055 (the C# reference)** rather than repeating them.

**How every rule is justified.** Every rule below was independently verified against the live source at `c:\Users\pepkad\source\repo2\FreeCRM`. Each rule carries a clickable proof link to the exact file and line(s) it was confirmed against. When you doubt a rule, click the link and read the source — that is the final authority. Paths are written relative to the repo root and keep the `FreeCRM/...` prefix.

**A note on scope.** Everything here is drawn from hand-written code under `FreeCRM/CRM.Client/`. No rule cites vendored/third-party files (Bootstrap, FontAwesome, jQuery, `*.min.*`). When this doc says "the team does X," it means the actual, observable pattern in real components — not an aspiration.

**How these rules are enforced (important).** Unlike C#, `.razor` files are **not** formatted by the `dotnet format` command line — that tool only processes `.cs` files. The `[*.razor]` rules in our `.editorconfig` (e.g. same-line braces) take effect only when you format a `.razor` file **inside the IDE** (Visual Studio's Razor editor reads `.editorconfig`). In practice that means: there is no command-line / CI auto-formatter for Razor, so these conventions are kept by **formatting-in-the-editor and code review**, not by a build gate. See [053 — The Machine Referee: editorconfig and What It Enforces](053_editorconfig-enforcement.md) for the C# side, which *is* CLI-enforceable.

---

<a id="anatomy"></a>
## 2. Anatomy of a `.razor` File and Directive Order

**Why it matters:** every `.razor` file in `CRM.Client` has the *same* top-to-bottom shape. Once you internalize it, any file in the project is predictable — you always know where to look for the route, the markup, and the logic.

A **directive** is an `@`-prefixed line at the top of a Razor file that configures the component — for example, `@page` declares a URL, `@inject` asks for a service. They are not C# statements; they are instructions to the Blazor compiler.

**Rule 2.1 — Three sections, fixed order: directives → markup → one `@code` block.** Write every component top-to-bottom as the `@`-directive block first, then your HTML/markup, then exactly **one** `@code { }` block at the very bottom. This is the conventional Blazor shape, applied uniformly so any file reads the same way.

```razor
@inject BlazorDataModel Model

<span class="@IconClass" @onclick="ToggleStickyMenus" title="@Helpers.Text("ToggleStickyMenus")">
@if (Model.User.UserPreferences.StickyMenus) {
    <i class="fa-solid fa-thumbtack"></i>
} else {
    <i class="fa-solid fa-thumbtack"></i>
}
</span>

@code {
    protected string IconClass {
```

Proof: [StickyMenuIcon.razor:1-12](FreeCRM/CRM.Client/Shared/StickyMenuIcon.razor#L1-L12). The single `@code` block is always last when present — confirmed across [Tags.razor:121](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L121), [EditTag.razor:141](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L141), [Icon.razor:27](FreeCRM/CRM.Client/Shared/Icon.razor#L27), [MainLayout.razor:123](FreeCRM/CRM.Client/Layout/MainLayout.razor#L123). No file has more than one `@code` block.

**Rule 2.2 — Directive order: `@page` → `@implements`/`@inherits` → `@using` → `@inject`.** Put all `@page` route lines first, then lifecycle/base directives, then `@using` imports, then all `@inject` service lines grouped together.

```razor
@page "/Settings/Tags"
@page "/{TenantCode}/Settings/Tags"
@implements IDisposable
@using Blazored.LocalStorage
@inject IJSRuntime jsRuntime
@inject HttpClient Http
@inject ILocalStorageService LocalStorage
@inject BlazorDataModel Model
```

Proof: [Tags.razor:1-8](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L1-L8). The two reliable invariants are: **`@page` is always first**, and **`@inject` lines are grouped together** (usually last). The `@using`-vs-`@implements` ordering between those is not universal — [MainLayout.razor:1-13](FreeCRM/CRM.Client/Layout/MainLayout.razor#L1-L13) puts `@using` first, and [Language.razor:1-3](FreeCRM/CRM.Client/Shared/Language.razor#L1-L3) runs `@implements` → `@using` → `@inject`.

**What the directives mean (define-on-first-use):**

- **`@page "..."`** registers a URL route so the Blazor router can navigate to this component (Rule 3.1).
- **`@inject ServiceType Name`** is *dependency injection* — Blazor hands the component a ready-made service (an HTTP client, the shared data model) instead of the component creating one. The injected `BlazorDataModel Model` is the single most important injection in the app (Section 7).
- **`@using Namespace`** imports a C# namespace, exactly like a `using` at the top of a `.cs` file.
- **`@implements IDisposable`** declares that the component implements a cleanup interface — see Rule 2.3.
- **`@inherits BaseClass`** sets a custom base class — see Rule 2.4.

**Rule 2.3 — `@implements` pairs with a `Dispose()` that unsubscribes.** When a component subscribes to shared events, declare `@implements IDisposable` (or `@implements IAsyncDisposable`) near the top and implement the matching cleanup method in `@code`. The mechanism: components hook `Model.OnChange` / `Model.OnSignalRUpdate` in `OnInitialized`, and **must** unsubscribe on disposal to avoid memory leaks and stale handlers (covered in depth in Section 7).

```razor
    public void Dispose()
    {
        Model.OnChange -= OnDataModelUpdated;
        Model.OnSignalRUpdate -= SignalRUpdate;
    }
```

Proof: directive at [Tags.razor:3](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L3), implementation at [Tags.razor:130-134](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L130-L134). Some components declare `@implements IDisposable` but ship an **empty** `Dispose()` body (e.g. [Icon.razor:1](FreeCRM/CRM.Client/Shared/Icon.razor#L1) + [Icon.razor:38-40](FreeCRM/CRM.Client/Shared/Icon.razor#L38-L40); [Tooltip.razor:1](FreeCRM/CRM.Client/Shared/Tooltip.razor#L1) + [Tooltip.razor:20-22](FreeCRM/CRM.Client/Shared/Tooltip.razor#L20-L22)) — the interface is applied as a uniform convention even when there is nothing to clean up. The layout uses `@implements IAsyncDisposable` because it disposes an async SignalR connection ([MainLayout.razor:4](FreeCRM/CRM.Client/Layout/MainLayout.razor#L4), `DisposeAsync()` at [MainLayout.razor:373-378](FreeCRM/CRM.Client/Layout/MainLayout.razor#L373-L378)).

**Rule 2.4 — `@inherits` is for the layout only; `@layout` and `@attribute` are never used.** Do not add `@inherits` to ordinary pages or components. A Blazor *layout* (the outer shell a page renders inside) must derive from `LayoutComponentBase` so it can expose `@Body` — and that is the *only* place `@inherits` appears.

```razor
@using Blazored.LocalStorage
@using Microsoft.AspNetCore.SignalR.Client
@using FluentValidation
@implements IAsyncDisposable
@inherits LayoutComponentBase
@inject IJSRuntime jsRuntime
```

Proof: [MainLayout.razor:1-6](FreeCRM/CRM.Client/Layout/MainLayout.razor#L1-L6). A repo-wide search found exactly **one** `@inherits` ([MainLayout.razor:5](FreeCRM/CRM.Client/Layout/MainLayout.razor#L5)), **zero** uses of `@layout`, and **zero** uses of `@attribute` — so neither `@layout` nor `@attribute` is part of our standard.

**Rule 2.5 — `_Imports.razor` holds the project-wide `@using` list.** There is one special file, `_Imports.razor`, that holds the shared namespace imports for the whole project so individual components only add file-specific `@using` lines. It has no markup and no `@code` and is the one intentional exception to the "directives → markup → code" shape.

Proof: [_Imports.razor:1-14](FreeCRM/CRM.Client/_Imports.razor#L1-L14).

---

<a id="pages-vs-shared"></a>
## 3. Pages vs Shared Components, and Multiple Routes

**Why it matters:** the difference between a "page" and a "component" is just one directive, but it determines where the file lives and whether a user can reach it by typing a URL.

**Rule 3.1 — A routable *Page* has one or more `@page` directives and lives under `Pages\`; a *Shared* component never has `@page` and lives under `Shared\`.** `@page` is what registers a component with the Blazor router, making it reachable by URL. A component without `@page` can only be used as a nested tag inside other markup.

```razor
@page "/Settings/EditTag/{id}"
@page "/{TenantCode}/Settings/EditTag/{id}"
@page "/Settings/AddTag"
@page "/{TenantCode}/Settings/AddTag"
@implements IDisposable
```

Proof (Page): [EditTag.razor:1-5](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L1-L5). Contrast [Icon.razor:1](FreeCRM/CRM.Client/Shared/Icon.razor#L1), a Shared component that starts with `@implements` and has no `@page`. A repo-wide search returned **54** files with `@page` — every one under `Pages\`, and **no** file under `Shared\` has a `@page`.

**Rule 3.2 — A single Page commonly declares *four* `@page` routes.** Stack several `@page` lines to bind one component to several URLs. The two common reasons:

1. **Tenant code in the URL.** FreeCRM is multi-tenant, so most routes have a plain form *and* a `/{TenantCode}/...` variant. (`{TenantCode}` is a *route parameter* — a slot the router fills from the URL and hands the component as a property.)
2. **One editor handles both "edit" and "add."** The same component serves an edit URL (with an `{id}`) and an add URL (no id) — so an editor commonly has four lines: edit + tenant-edit + add + tenant-add.

```razor
@page "/Settings/EditUser/{userid}"
@page "/{TenantCode}/Settings/EditUser/{userid}"
@page "/Settings/AddUser"
@page "/{TenantCode}/Settings/AddUser"
```

Proof: [EditUser.razor:1-4](FreeCRM/CRM.Client/Pages/Settings/Users/EditUser.razor#L1-L4). Simple list pages have just two `@page` lines (one plain, one tenant-coded — [Tags.razor:1-2](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L1-L2)); a few utility pages have only one ([NotFound.razor:1](FreeCRM/CRM.Client/Pages/NotFound.razor#L1), [DatabaseOffline.razor:1](FreeCRM/CRM.Client/Pages/DatabaseOffline.razor#L1), [Setup.razor:1](FreeCRM/CRM.Client/Pages/Settings/Misc/Setup.razor#L1)).

**Rule 3.3 — The smallest Shared components are markup-only: no directives, no `@code`.** If a component just renders static or trivially-bound markup, write only the markup — skip the directive block and the `@code` block entirely.

```razor
<div class="required-indicator">
    <i class="required-flag"></i>
    <Language Tag="IndicatesRequiredField" />
</div>
```

Proof: [RequiredIndicator.razor:1-4](FreeCRM/CRM.Client/Shared/RequiredIndicator.razor#L1-L4) (the complete file). Even tiny components add an `@inject` when they need model data — [LoadingMessage.razor:1-2](FreeCRM/CRM.Client/Shared/LoadingMessage.razor#L1-L2) is a two-line component with one `@inject` and a single markup line.

---

<a id="app-convention"></a>
## 4. The `.App.razor` Override Convention

**Why it matters:** this is FreeCRM's signature pattern and the single most important thing in this doc. FreeCRM is a template you build *on top of*, and it periodically pulls new code from its original author (see [054](054_fork-sync-discipline.md)). If you edit the upstream files directly, every upgrade fights your changes. The `.App.razor` convention is the seam that makes customization **upgrade-safe**: the base file is upstream code you do not touch, and a matching `.App.` file is where *your* changes go. An upgrade can overwrite the base file without ever clobbering your customizations. (This is the same upgrade-safe philosophy that [041](041_upgrade-safe-model.md) describes for C# partial classes — `.App.razor` is its Razor counterpart.)

There are exactly **16** `.App.razor` files in `CRM.Client`: 15 under `Shared\AppComponents\` plus `MainLayout.App.razor` next to its base in `Layout\`. (A 17th, `CRM\Components\Modules.App.razor`, lives in the separate server-side `CRM` project and is out of scope here.)

**Rule 4.1 — Every customization seam is a file named `<BaseName>.App.razor`, paired with the base `<BaseName>.razor`.** The pairing is by *name*, not by folder. The base file's own comment states the intent.

```razor
@implements IDisposable
@inject BlazorDataModel Model

@{
    // This component is used as an alternate to the MainLayout.App.razor page if enabled.
    <div class="container-fluid page-view">
        <h1 class="page-title">
            Layout from MainLayout.App
        </h1>
```

Proof: [MainLayout.App.razor:1-9](FreeCRM/CRM.Client/Layout/MainLayout.App.razor#L1-L9).

**Rule 4.2 — A `.App.razor` file compiles to a class named `<BaseName>_App`; the dot becomes an underscore.** Razor turns the `.` in the filename into `_` for the generated C# class. So `EditTag.App.razor` becomes the component `EditTag_App`. The base file uses that underscore form both as a tag and as a C# type.

```razor
            <EditTag_App Area="edit" Value="_tag" />
```

and in the same base file's `@code`:

```razor
    protected EditTag_App AppModule = new EditTag_App();
```

Proof: tag at [EditTag.razor:130](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L130), field at [EditTag.razor:154](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L154). Same pattern: [NavigationMenu.razor:5](FreeCRM/CRM.Client/Shared/NavigationMenu.razor#L5) + [:191](FreeCRM/CRM.Client/Shared/NavigationMenu.razor#L191), [MainLayout.razor:29](FreeCRM/CRM.Client/Layout/MainLayout.razor#L29) + [:143](FreeCRM/CRM.Client/Layout/MainLayout.razor#L143).

**Rule 4.3 — The base creates a `new <BaseName>_App()` field and reads boolean toggles off it to decide what to render.** Before committing to render, the base needs to ask the customization "are you switched on?" Creating a throwaway instance lets the base read the `.App.` file's default property values (`Enabled`, `OverridePage`, `OverrideCompleteLayout`, `RequireLogin`) as configuration.

```razor
@if (navigationMenuApp.Enabled) {
    <NavigationMenu_App Loading="Loading" />
} else {
    <header>
```

with the field:

```razor
    protected NavigationMenu_App navigationMenuApp = new NavigationMenu_App();
```

Proof: [NavigationMenu.razor:4-7](FreeCRM/CRM.Client/Shared/NavigationMenu.razor#L4-L7) and [:191](FreeCRM/CRM.Client/Shared/NavigationMenu.razor#L191). The same trick is used outside of rendering — [MainLayout.razor:884](FreeCRM/CRM.Client/Layout/MainLayout.razor#L884) does `var appIndexPage = new Index_App();` purely to read `appIndexPage.RequireLogin` ([:885](FreeCRM/CRM.Client/Layout/MainLayout.razor#L885)) for anonymous-access logic.

**Rule 4.4 — Wholesale UI replacements expose an `Enabled` toggle (default `false`); the base swaps to the custom version only when `Enabled` is true.** Ship disabled-by-default so the stock UI renders out of the box; flipping one boolean activates your version with **no base edits**.

```razor
@code {
    // Set this to true to use this navigation menu instead of the default.
    [Parameter] public bool Enabled { get; set; } = false;

    [Parameter] public bool Loading { get; set; }
```

Proof: [NavigationMenu.App.razor:6-10](FreeCRM/CRM.Client/Shared/AppComponents/NavigationMenu.App.razor#L6-L10). The layout uses **two** toggles — `Enabled` ([:17](FreeCRM/CRM.Client/Layout/MainLayout.App.razor#L17)) plus `OverrideCompleteLayout` ([:26](FreeCRM/CRM.Client/Layout/MainLayout.App.razor#L26)) — so it can distinguish "take over everything" from a minimal override that keeps the offcanvas menu and banners ([MainLayout.razor:28-31](FreeCRM/CRM.Client/Layout/MainLayout.razor#L28-L31)).

**Rule 4.5 — Page-level replacements use `OverridePage` instead (default `false`).** Same opt-in idea, but the semantics are "replace this whole page," so the property is named `OverridePage`.

```razor
        if (AppModule.OverridePage) {
            <EditUser_App Value="_user" />
        } else {
```

and the declaration:

```razor
    /// <summary>
    /// Set this to true to use this page to override the entire built-in page.
    /// </summary>
    [Parameter] public bool OverridePage { get; set; } = false;
```

Proof: base [EditUser.razor:22-24](FreeCRM/CRM.Client/Pages/Settings/Users/EditUser.razor#L22-L24), declaration [EditUser.App.razor:25-28](FreeCRM/CRM.Client/Shared/AppComponents/EditUser.App.razor#L25-L28). Same pattern in `Users.App.razor`, `UserGroups.App.razor`, `EditUserGroup.App.razor`.

**Rule 4.6 — To *add* fields into an existing page (not replace it), the base renders the `.App.` inline and passes an `Area` string plus `Value`/`@bind-Value`.** One `.App.` component is reused at several insertion points of one base page; the `Area` parameter tells it *where* it is rendering ("th", "td", "edit", "Top", "Style", ...) so it can emit the right markup for that slot.

Base passes a slot name and the bound record:

```razor
            <EditTag_App Area="edit" Value="_tag" />
```

The `.App.` switches on the slot:

```razor
    switch (Helpers.StringLower(Area)) {
        case "th":
            // Add any additional header cells here for the tag listing on the Tags page.
            break;

        case "td":
            // Add any additional cells here for each tag row on the Tags page.
            break;

        case "edit":
            // Add any additional fields here for the Edit Tag page.
            break;
    }
```

Proof: base [EditTag.razor:130](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L130), slot switch [EditTag.App.razor:8-20](FreeCRM/CRM.Client/Shared/AppComponents/EditTag.App.razor#L8-L20). Multi-slot reuse: [Settings.razor:218](FreeCRM/CRM.Client/Pages/Settings/Misc/Settings.razor#L218) and friends (`Area="General"/"Theme"/"Authentication"`), and the appointment editor at [EditAppointment.razor:62](FreeCRM/CRM.Client/Pages/Scheduling/EditAppointment.razor#L62) (`Area="Top"/"Style"/"Attendees"`). Two binding styles coexist: simple cases pass `Value="..."` one-way ([Tags.razor:102](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L102) → `Value="tag" Area="td"`; note [Tags.razor:69](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L69) is `Area="th"` with **no** `Value`), editable cases use `@bind-Value` for two-way binding. The `.App.` declares the matching `Value` + `EventCallback<T> ValueChanged` pair to support `@bind-Value` ([EditTag.App.razor:31,36](FreeCRM/CRM.Client/Shared/AppComponents/EditTag.App.razor#L31-L36)).

**Rule 4.7 — Inline-additions `.App.` components gate their markup behind `if (Enabled)` and/or `switch (Area)` so they render nothing until you opt in.** The shipped file must be invisible until someone fills in an `Area` case.

```razor
    if (Enabled) {
        switch (Helpers.StringLower(Area)) {
            case "top":
                // Anything to show at the top of the form before other content and just after the title field
                break;
```

Proof: [EditAppointment.App.razor:14-18](FreeCRM/CRM.Client/Shared/AppComponents/EditAppointment.App.razor#L14-L18). The base often *double-gates* by also checking `AppModule.Enabled` before even emitting the tag ([EditAppointment.razor:61-62](FreeCRM/CRM.Client/Pages/Scheduling/EditAppointment.razor#L61-L62)).

**Rule 4.8 — Replacement `.App.` components ship with a visible placeholder body.** Because these render only when explicitly turned on, the placeholder is a developer-facing marker proving the override is active and showing where to put real content.

```razor
@implements IDisposable
@inject BlazorDataModel Model

<div>Custom Navigation Menu</div>
```

Proof: [NavigationMenu.App.razor:1-4](FreeCRM/CRM.Client/Shared/AppComponents/NavigationMenu.App.razor#L1-L4). The one exception is `About.App.razor`, shipped with *real, intended* content (the actual "About" copy at [About.App.razor:8-20](FreeCRM/CRM.Client/Shared/AppComponents/About.App.razor#L8-L20)) and consumed unconditionally as `<About_App />` ([About.razor:23](FreeCRM/CRM.Client/Pages/About.razor#L23)), because the About text is meant to be customized per deployment.

**Rule 4.9 — Every `.App.razor` follows the same boilerplate skeleton.** `@implements IDisposable`, `@inject BlazorDataModel Model`, subscribe to `Model.OnChange` in `OnInitialized`, unsubscribe in `Dispose`. These components must re-render when the shared model changes.

```razor
    public void Dispose()
    {
        Model.OnChange -= StateHasChanged;
    }

    protected override void OnInitialized()
    {
        Model.OnChange += StateHasChanged;
    }
```

Proof: [NavigationMenu.App.razor:12-20](FreeCRM/CRM.Client/Shared/AppComponents/NavigationMenu.App.razor#L12-L20). Identical in `EditUser.App.razor`, `Users.App.razor`, `About.App.razor`, `MainLayout.App.razor`. Components that load their own page-scoped data instead subscribe a local `OnDataModelUpdated` handler that guards on `Model.View == _pageName` — verified in [Index.App.razor:53-55](FreeCRM/CRM.Client/Shared/AppComponents/Index.App.razor#L53-L55) and [:68-73](FreeCRM/CRM.Client/Shared/AppComponents/Index.App.razor#L68-L73).

**Rule 4.10 — The base calls back into the `.App.` instance's `Save(...)` so local validation runs as part of the stock workflow.** Beyond rendering, the seam lets local code veto a save. The base's `Save` invokes `AppModule.Save(...)`, merges any returned error messages and focus field, and aborts if the customization vetoes — without the base knowing what the customization checks.

```razor
        var saveApp = AppModule.Save(_tag);
        if (!saveApp.Result) {
            if (saveApp.Messages.Any()) {
                errors.AddRange(saveApp.Messages);
            }

            if (focus == String.Empty && !String.IsNullOrWhiteSpace(saveApp.Focus)) {
                focus = saveApp.Focus;
            }
        }
```

matched by the `.App.` stub it calls:

```razor
    public DataObjects.ModuleAction Save(DataObjects.Tag tag)
    {
        var output = new DataObjects.ModuleAction { Result = true };

        // Perform and pre-save checks here. Return false if there is an error and include any error messages.
        // Optionally set the focus to a specific field by setting output.FocusField to the field element id.

        return output;
    }
```

Proof: base call [EditTag.razor:313-322](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L313-L322), stub [EditTag.App.razor:50-58](FreeCRM/CRM.Client/Shared/AppComponents/EditTag.App.razor#L50-L58). Only the inline-edit / `OverridePage` components carry a `Save` stub; pure replacements (`NavigationMenu.App.razor`, `MainLayout.App.razor`, `About.App.razor`) do not.

**Rule 4.11 — Markup inside an inline-additions `.App.` lives in a top-level `@{ ... }` block; pure replacements place markup directly.** Inline-additions components must run C# (an `if`/`switch` on `Enabled`/`Area`) and conditionally emit markup, so the whole body is wrapped in one `@{ ... }` statement block. Pure replacements that always render fixed markup skip the block.

Block form (inline-additions / override pages):

```razor
@{
    // {{ModuleItemStart:Tags}}
    // This component is used to display application-specific settings on the Tags and Edit Tag page.
    switch (Helpers.StringLower(Area)) {
```

Direct-markup form (pure replacement):

```razor
<div>Custom Navigation Menu</div>
```

Proof: block form [EditTag.App.razor:5-8](FreeCRM/CRM.Client/Shared/AppComponents/EditTag.App.razor#L5-L8); direct-markup form [NavigationMenu.App.razor:4](FreeCRM/CRM.Client/Shared/AppComponents/NavigationMenu.App.razor#L4).

**The whole convention in one sentence:** never edit the upstream `<X>.razor`; put your changes in `<X>.App.razor`, which ships disabled/placeholder, follows the `IDisposable` + `Model.OnChange` skeleton, and is reached by the base through a `new X_App()` field and a boolean toggle.

---

<a id="markup"></a>
## 5. HTML / Markup Conventions

**Why it matters:** consistent markup makes a page diffable and predictable. The rules here are about casing, whitespace, which styling tool to reach for, and how to switch between HTML and C# inside markup. For C# *syntax* details that surface in markup (brace style, `String.Empty`), see doc 055.

**Rule 5.1 — HTML elements and attributes are lowercase; our Blazor components are PascalCase.** Write all real HTML tags and attributes lowercase (`div`, `input`, `class`, `href`); write our own components and their parameters PascalCase (`<Language Tag="..." />`). A lowercase first letter is exactly how Razor tells a plain HTML element apart from a component.

```razor
        <div class="btn-group mb-2" role="group">
            <a href="@(Helpers.BuildUrl("Settings/AddTag"))" role="button" class="btn btn-success">
                <Language Tag="AddNewTag" IncludeIcon="true" />
            </a>
```

Proof: [Tags.razor:22-25](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L22-L25). (One genuine inconsistency: the native attribute `selected` is occasionally written uppercase `SELECTED` ([EditTag.razor:89](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L89)) while elsewhere it is lowercase ([EditDepartment.razor:75](FreeCRM/CRM.Client/Pages/Settings/Departments/EditDepartment.razor#L75)). Lowercase is the dominant form.)

**Rule 5.2 — Indent four spaces per nesting level.** Each level of nesting — HTML element, `@if`/`@foreach` block, or `@code` — adds four spaces.

```razor
        <div class="mb-2">
            <div class="form-check form-switch">
                <input type="checkbox" id="tags-IncludeDeletedRecords" class="form-check-input" @bind="Model.User.UserPreferences.IncludeDeletedItems" />
                <label for="tags-IncludeDeletedRecords" class="form-check-label"><Language Tag="IncludeDeletedRecords" /></label>
            </div>
        </div>
```

Proof: [Tags.razor:39-44](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L39-L44). When a single start-tag is split across two lines, continuation indentation is *not* consistent: [EditTag.razor:69-70](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L69-L70) indents the continuation one extra level (+4), while [EditDepartment.razor:59-60](FreeCRM/CRM.Client/Pages/Settings/Departments/EditDepartment.razor#L59-L60) aligns it at the same level (+0). Either is acceptable.

**Rule 5.3 — Self-close void and empty elements, with a space before `/>`.** Void HTML elements (`input`, `br`, `hr`) and childless components are written self-closing, with a leading space before the slash. Razor *requires* void/empty elements to be explicitly closed; the leading space is house style.

```razor
        <RequiredIndicator />

        <div class="mb-2 form-check form-switch">
            <input type="checkbox" role="switch" class="form-check-input" id="edit-tag-Enabled" @bind="_tag.Enabled" />
            <label for="edit-tag-Enabled" class="form-check-label"><Language Tag="Enabled" /></label>
        </div>
```

Proof: [EditTag.razor:58-63](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L58-L63).

**Rule 5.4 — Lean on Bootstrap utility/component classes instead of custom CSS.** Style layout and controls with Bootstrap classes (`mb-2`, `btn btn-success`, `btn-group`, `form-check form-switch`, `form-control`, `form-select`, `table table-sm`, `alert alert-danger`, `d-flex`, `container-fluid`) rather than writing custom CSS. This keeps pages visually consistent without bespoke stylesheets. (CSS specifics are doc [057](057_css-style-reference.md).)

```razor
                <div class="btn-group mb-2" role="group">
                    <a href="@(Helpers.BuildUrl("Settings/Tags"))" class="btn btn-dark">
                        <Language Tag="Back" IncludeIcon="true" />
                    </a>

                    @if (!_tag.Deleted) {
                        <button type="button" class="btn btn-success" @onclick="Save">
                            <Language Tag="Save" IncludeIcon="true" />
                        </button>
```

Proof: [EditTag.razor:31-39](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L31-L39). A small set of hand-written *project* CSS classes appear alongside Bootstrap for app-specific semantics Bootstrap doesn't cover — `page-title` ([Tags.razor:12](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L12)), `nowrap` ([Tags.razor:85](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L85)), `item-deleted` ([Tags.razor:78](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L78)), `required-indicator` ([RequiredIndicator.razor:1](FreeCRM/CRM.Client/Shared/RequiredIndicator.razor#L1)).

**Rule 5.5 — `@`-prefixed blocks transition from markup into C#; once already inside C#, drop the `@`.** Use `@if` / `@foreach` / `@switch` when moving *from* markup into a control block. Once you are already inside a Razor code region (e.g. inside an outer `@if` body), a nested statement is plain C# and must **not** be re-prefixed with `@`.

```razor
@if (Model.Loaded && Model.View == _pageName) {
    @if (_loading) {
        <h1 class="page-title">
            <Language Tag="Tags" IncludeIcon="true" />
        </h1>
        <LoadingMessage />
    } else {
```

```razor
        if (Helpers.HaveBlazorPlugins(_pageName, "Top")) {
            <div class="mb-2">
                <BlazorPlugins Module="@_pageName" Position="Top" TValue="System.String" />
            </div>
        }
```

Proof: the `@if` at [Tags.razor:10-16](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L10-L16) and the bare `if` at [Tags.razor:33-37](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L33-L37). The deciding factor is purely whether the surrounding context is already a C# code region.

**Rule 5.6 — Open-brace on the same line; `} else {` cuddled on one line.** Put the `{` on the same line as the `@if`/`if`/`foreach`/`switch`, and write `else` cuddled as `} else {`. (This matches the C# control-flow brace style in doc 051 / 055.)

```razor
                    @if (_tag.Style == color) {
                        <option class="@("tag tag-" + color.ToLower())" value="@color" SELECTED>@color</option>
                    } else {
                        <option class="@("tag tag-" + color.ToLower())" value="@color">@color</option>
                    }
```

Proof: [EditTag.razor:88-92](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L88-L92).

**Rule 5.7 — Output a simple value with bare `@member`; wrap any real expression in `@(...)`.** Use `@something` for a single variable or member chain; use `@(...)` when the expression is a method call combined with a cast, string concatenation, or anything more complex. Bare `@` only reliably parses a simple member; parentheses tell Razor where the expression ends.

```razor
                                <td>@tag.Name</td>
                                <td>
                                    @((MarkupString)Helpers.RenderTag(tag))
                                </td>
```

Proof: [Tags.razor:89-92](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L89-L92). Attribute values follow the same split: `href="@(Helpers.BuildUrl("Settings/AddTag"))"` ([Tags.razor:23](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L23)). Wrapping a single helper call is preferred but not universal — [Index.razor:27](FreeCRM/CRM.Client/Pages/Index.razor#L27) uses the unwrapped form for the same helper.

**Rule 5.8 — Render trusted HTML via `@((MarkupString)expr)`.** Blazor HTML-*encodes* string output by default (so `<b>` shows up as literal text). When a string already contains HTML that should render as HTML (icons, formatted dates, server markup), cast it to `MarkupString` inside `@(...)`.

```razor
                                <!-- {{ModuleItemStart:Appointments}} -->
                                <td class="center">@((MarkupString)Helpers.BooleanToIcon(tag.UseInAppointments))</td>
                                <!-- {{ModuleItemEnd:Appointments}} -->
```

Proof: [Tags.razor:93-95](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L93-L95). This always travels through a `Helpers` call or a model property, never an inline literal, and is reserved for Helper-produced or server-supplied HTML — never unfiltered user input.

**Rule 5.9 — Compute local C# helper variables at the top of a loop/conditional body, before the markup.** Inside a `@foreach` or block, compute class strings and flags as plain C# first, then emit the markup that uses them. This keeps conditional class logic out of the element start-tag.

```razor
                    @foreach (var tag in Model.Tags.Where(x => x.Enabled == true || !Model.User.UserPreferences.EnabledItemsOnly).OrderBy(x => x.Name)) {
                        @if (!tag.Deleted || Model.User.UserPreferences.IncludeDeletedItems) {
                            string itemClass = String.Empty;
                            if (tag.Deleted) {
                                itemClass = "item-deleted";
                            } else if (!tag.Enabled) {
                                itemClass = "disabled";
                            }

                            <tr class="@itemClass">
```

Proof: [Tags.razor:74-83](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L74-L83). A blank line typically separates the variable computation from the emitted markup.

**Rule 5.10 — Use `switch` for multi-way branches, one blank line between `case` arms.** For more than two branches use a `switch` (with `@` when transitioning from markup); separate each `case`...`break;` arm with one blank line so the arms are scannable.

```razor
                switch (message.MessageType) {
                    case MessageType.Primary:
                        toastClass += "text-bg-primary";
                        toastCloseButtonClass += " btn-close-white";
                        break;

                    case MessageType.Secondary:
                        toastClass += "text-bg-secondary";
                        toastCloseButtonClass += " btn-close-white";
                        break;
```

Proof: [ToastMessages.razor:13-22](FreeCRM/CRM.Client/Shared/ToastMessages.razor#L13-L22).

**Rule 5.11 — One blank line between sibling blocks; none between a label and its tightly-coupled input.** Put a single blank line between logically distinct sibling blocks (each form group, each toolbar section); keep a `<label>` and its `<input>` together with no blank line.

```razor
        <div class="mb-2">
            <div class="form-check form-switch">
                <input type="checkbox" id="tags-IncludeDeletedRecords" class="form-check-input" @bind="Model.User.UserPreferences.IncludeDeletedItems" />
                <label for="tags-IncludeDeletedRecords" class="form-check-label"><Language Tag="IncludeDeletedRecords" /></label>
            </div>
        </div>

        <div class="mb-2">
            <div class="form-check form-switch">
                <input type="checkbox" id="tags-EnabledItemsOnly" class="form-check-input" @bind="Model.User.UserPreferences.EnabledItemsOnly" />
                <label for="tags-EnabledItemsOnly" class="form-check-label"><Language Tag="EnabledItemsOnly" /></label>
            </div>
        </div>
```

Proof: [Tags.razor:39-51](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L39-L51).

**Rule 5.12 — Link a `<label>`/`<input>` pair with a matching, scope-prefixed `id`/`for`.** Give each input an `id` and point its `<label for="...">` at the same value, prefixed by the page/component scope (`edit-tag-Name`, `tags-IncludeDeletedRecords`). This makes the label clickable (accessibility) and lets code call `Helpers.DelayedFocus("edit-tag-Name")` by that id; the scope prefix keeps ids unique across components.

```razor
            <div class="mb-2">
                <label for="edit-tag-Name">
                    <Language Tag="TagName" Required="true" />
                </label>
                <input type="text" id="edit-tag-Name" @bind="_tag.Name" @bind:event="oninput"
                    class="@Helpers.MissingValue(_tag.Name, "form-control")" />
            </div>
```

Proof: [EditTag.razor:65-71](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L65-L71) (the id is later focused at [EditTag.razor:264](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L264)). The scope segment matches the component; the trailing segment's casing is not strictly enforced (`edit-tag-Name` PascalCase vs `edit-department-departmentName` camelCase).

**Rule 5.13 — Two comment styles: `@* ... *@` for Razor, `<!-- -->` for HTML and module markers.** Use `@* ... *@` to comment out Razor markup or notes — it is stripped at compile time and never reaches the browser. Reserve `<!-- -->` for genuine HTML comments and the `{{ModuleItem...}}` build markers (next rule), because those must survive into the DOM where tooling scans for them.

```razor
                                    <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="themeDropdown">
                                        @*
                                        <li>
                                            <a class="dropdown-item disabled" href="#">
                                                <Language Tag="Theme" IncludeIcon="true" />
                                            </a>
                                        </li>
                                        <li><hr class="dropdown-divider"></li>
                                        *@
```

Proof: multi-line `@*...*@` at [NavigationMenu.razor:140-148](FreeCRM/CRM.Client/Shared/NavigationMenu.razor#L140-L148); single-line `@* </form> *@` at [Login.razor:419](FreeCRM/CRM.Client/Pages/Authorization/Login.razor#L419). `@*...*@` is used sparingly (exactly 11 files contain it).

**Rule 5.14 — Module-conditional markup is fenced by `<!-- {{ModuleItemStart:X}} -->` / `<!-- {{ModuleItemEnd:X}} -->` comments.** Markup belonging to an optional module is bracketed by these literal HTML comments (and the C# equivalent `// {{ModuleItemStart:X}}` inside code blocks). These are **not decorative** — a build/strip utility removes the fenced region when a module is disabled.

```razor
                        <!-- {{ModuleItemStart:Appointments}} -->
                        <td class="center" style="width:1%;"><Icon Name="Schedule" Title="TagUseInAppointments" /></td>
                        <!-- {{ModuleItemEnd:Appointments}} -->
                        <!-- {{ModuleItemStart:EmailTemplates}} -->
                        <td class="center" style="width:1%;"><Icon Name="EmailTemplates" Title="TagUseInEmailTemplates" /></td>
                        <!-- {{ModuleItemEnd:EmailTemplates}} -->
```

Proof: [Tags.razor:60-65](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L60-L65). Inside `@code`/C# regions the same fences use `//` line-comment form ([EditTag.razor:283-285](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L283-L285)). See [043](043_plugin-model.md) for the module system.

**Rule 5.15 — Reserve inline `style="..."` for tiny, layout-only one-offs.** Almost all styling goes through classes; an inline `style` is used only for the couple of trivial layout values that have no utility class (forcing a 1%-width column, hiding an element). No multi-property visual styling is done inline.

```razor
                    <tr class="table-dark">
                        <td style="width:1%;"></td>
                        <th><Language Tag="TagName" ReplaceSpaces="true" /></th>
                        <th><Language Tag="TagPreview" ReplaceSpaces="true" /></th>
```

Proof: [Tags.razor:56-59](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L56-L59). Other instances: `style="display:none;"` ([MainLayout.razor:113](FreeCRM/CRM.Client/Layout/MainLayout.razor#L113)) and the `<style>` element that injects tenant theme CSS ([MainLayout.razor:17-25](FreeCRM/CRM.Client/Layout/MainLayout.razor#L17-L25)).

---

<a id="components"></a>
## 6. Components, Parameters, `@bind`, and Events

**Why it matters:** components are how you avoid copy-pasting UI. This section is the contract for *using* a component (passing it data) and for *defining* the data it accepts. The shared-component catalog itself is doc [032](032_shared-components.md); the rich ones (charts, editors) are [033](033_rich-components.md); authoring new ones is [047](047_custom-components.md).

**A note on what's *absent*:** FreeCRM does **not** use Blazor's "cascading values/parameters" (a feature for passing data implicitly down a component tree). A repo-wide search for `CascadingValue`/`CascadingParameter` returned zero matches. Shared state instead flows through the injected `BlazorDataModel Model` singleton (Rule 6.8, and Section 7).

**Rule 6.1 — Invoke a shared component as a PascalCase tag with attribute parameters; self-close when there's no inner content.** Call a component by writing its file name as a tag and pass each parameter as an HTML-style attribute.

```razor
<h1 class="page-title">
    <Language Tag="Tags" IncludeIcon="true" />
</h1>
<LoadingMessage />
```

Proof: [Tags.razor:12-15](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L12-L15). `<LoadingMessage />` takes no parameters at all — it pulls its text from the injected `Model` ([LoadingMessage.razor:1-2](FreeCRM/CRM.Client/Shared/LoadingMessage.razor#L1-L2)). `<Language>` is by far the most-invoked component (768 invocations across 66 files); `<Icon>` appears ~42 times across 19 files.

**Rule 6.2 — Render icons by logical `Name`, never by raw CSS class.** Use `<Icon Name="SomeName" />` (optionally `Title="..."`) and let the component resolve the name to the right glyph — do not hand-write `<i class="fa-...">` in pages. The component looks `Name` up in `Helpers.AppIcons`/`Helpers.Icons`, so icons stay swappable in one place.

```razor
<td class="center" style="width:1%;"><Icon Name="Schedule" Title="TagUseInAppointments" /></td>
```

Proof: [Tags.razor:61](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L61); resolution logic at [Icon.razor:65-86](FreeCRM/CRM.Client/Shared/Icon.razor#L65-L86). `Title` is treated as a language tag (run through `Helpers.Text` at [Icon.razor:57-62](FreeCRM/CRM.Client/Shared/Icon.razor#L57-L62)), so pass a tag name, not literal prose.

**Rule 6.3 — All user-visible text goes through `<Language Tag="..." />`.** Never type a hard-coded English label in markup; emit it with `<Language Tag="LabelKey" />` so it can be translated. `Language` resolves the tag via `Helpers.Text(...)` against the language file. It is the single most-used component in the codebase.

```razor
<label for="edit-tag-Name">
    <Language Tag="TagName" Required="true" />
</label>
```

Proof: [EditTag.razor:66-68](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L66-L68); component render line at [Language.razor:4](FreeCRM/CRM.Client/Shared/Language.razor#L4). `Language` exposes optional flags — `IncludeIcon`, `Class`, `Required`, `ReplaceSpaces`, `TransformCase`, `MarkUndefinedStrings` — documented with XML summaries at [Language.razor:6-39](FreeCRM/CRM.Client/Shared/Language.razor#L6-L39). Localization is also doc [035](035_validation-localization-a11y.md).

**Rule 6.4 — Compose components by nesting; the small shared widgets have no generic `ChildContent` slot.** Build a component by placing other components and HTML directly in its body; do not expect a generic child-content slot on the small shared widgets — they are fixed-purpose and hard-code their internal layout.

```razor
<div class="required-indicator">
    <i class="required-flag"></i>
    <Language Tag="IndicatesRequiredField" />
</div>
```

Proof: [RequiredIndicator.razor:1-4](FreeCRM/CRM.Client/Shared/RequiredIndicator.razor#L1-L4). A `RenderFragment` (a slot that accepts arbitrary child markup) exists in only two places project-wide — a `[Parameter]` at [MainLayout.App.razor:19](FreeCRM/CRM.Client/Layout/MainLayout.App.razor#L19) and a protected field at [DynamicComponent.razor:50](FreeCRM/CRM.Client/Pages/TestPages/DynamicComponent.razor#L50) — and `ChildContent` is never a parameter anywhere. So child content is a rare, layout-level tool, not an everyday pattern.

**Rule 6.5 — Declare parameters as `[Parameter] public T Name { get; set; }`, one per line.** Every public input is a property attributed with `[Parameter]`; give nullable optionals a `?`, and give value-type optionals a default with `= ...`. The compact one-line form is most common.

```razor
[Parameter] public string? Id { get; set; }
[Parameter] public string? Name { get; set; }
[Parameter] public string? Title { get; set; }
```

Proof: [Icon.razor:28-30](FreeCRM/CRM.Client/Shared/Icon.razor#L28-L30). A few richer components break the attribute onto its own line and add XML `/// <summary>` docs ([Language.razor:6-39](FreeCRM/CRM.Client/Shared/Language.razor#L6-L39), [UndeleteMessage.razor:32-45](FreeCRM/CRM.Client/Shared/UndeleteMessage.razor#L32-L45)); the tiny ones (`Icon.razor`) skip the docs.

**Rule 6.6 — Two-way bind native HTML inputs with `@bind`, adding `@bind:event="oninput"` for live updates.** `@bind` is Blazor's two-way binding directive: it reads the property into the field and writes the field back into the property. By default a text input updates the property only when it loses focus; adding `@bind:event="oninput"` updates on every keystroke, which is what drives live validation styling.

```razor
<input type="text" id="edit-tag-Name" @bind="_tag.Name" @bind:event="oninput"
    class="@Helpers.MissingValue(_tag.Name, "form-control")" />
```

Proof: [EditTag.razor:69-70](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L69-L70). Checkboxes and `<select>` use plain `@bind` (no `:event`) since their change is discrete — `@bind="_tag.Enabled"` ([EditTag.razor:61](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L61)), `@bind="_tag.Style"` on a select ([EditTag.razor:83](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L83)).

**Rule 6.7 — Two-way bind a whole object into a child component with `@bind-Value`, backed by a `Value` + `ValueChanged` pair.** To push edits back and forth between a parent and a child component, the child exposes a `Value` parameter plus an `EventCallback<T> ValueChanged`, and the parent consumes it as `@bind-Value="_obj"`. The `ValueChanged` callback is what lets the child push edits back up.

Child side:

```razor
/// <summary>
/// The Tenant object bound with the @bind-Value directive.
/// </summary>
[Parameter] public DataObjects.Tag Value { get; set; } = new DataObjects.Tag();

/// <summary>
/// The internal method allowing for 2-way binding with the @bind-Value option instead of @bind.
/// </summary>
[Parameter] public EventCallback<DataObjects.Tag> ValueChanged { get; set; }
```

Parent side:

```razor
<EditAppointment_App Area="Top" AllowEdit="AllowEdit" @bind-Value="_appointment" />
```

Proof: child [EditTag.App.razor:28-36](FreeCRM/CRM.Client/Shared/AppComponents/EditTag.App.razor#L28-L36) (note the doc-comment literally says "Tenant object" though the type is `Tag` — a copy-paste artifact preserved in source); parent [EditAppointment.razor:62](FreeCRM/CRM.Client/Pages/Scheduling/EditAppointment.razor#L62). The desugared long form `Value="_tag" ValueChanged="@(v => _tag = v)"` is also in active use ([EditTag.razor:47](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L47)) — both spellings are valid.

**Rule 6.8 — `EventCallback` / `EventCallback<T>` is the standard "notify the parent" mechanism.** An **`EventCallback`** is Blazor's event contract: a parameter the parent supplies a handler for, that the child raises with `await Callback.InvokeAsync(...)`. Raising it also triggers the parent's re-render automatically. Guard optional callbacks with `.HasDelegate`.

Declare + invoke:

```razor
[Parameter] public EventCallback OnUndelete { get; set; }
...
protected async Task DoUndelete()
{
    await OnUndelete.InvokeAsync();
}
```

Consume from parent:

```razor
<UndeleteMessage DeletedAt="_tag.DeletedAt" LastModifiedBy="@_tag.LastModifiedBy" OnUndelete="(() => _tag.Deleted = false)" />
```

Proof: declaration/invoke [UndeleteMessage.razor:35,63-66](FreeCRM/CRM.Client/Shared/UndeleteMessage.razor#L63-L66); parent [EditTag.razor:24](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L24). `EventCallback` appears as a same-line `[Parameter]` in 15 files (16 declarations). The `.HasDelegate` guard lets a component skip drawing UI when no handler was supplied ([PluginPrompts.razor:44](FreeCRM/CRM.Client/Shared/PluginPrompts.razor#L44)).

> **Legacy nuance:** an older parallel convention uses a raw `Delegate?` parameter invoked via `DynamicInvoke(...)` — e.g. `OnComplete` declared across [TagSelector.razor:106-107](FreeCRM/CRM.Client/Shared/TagSelector.razor#L106-L107) and raised at [TagSelector.razor:264](FreeCRM/CRM.Client/Shared/TagSelector.razor#L264). This `Delegate?` form is widespread in older code. **New code should prefer `EventCallback`.**

**Rule 6.9 — Wire events with a bare method group for no-arg handlers, and `@(() => ...)` lambda when you need to pass a value.** Use `@onclick="Save"` (a method group) when the handler takes no event-specific args; use a parenthesized lambda when you must pass a captured value such as the current loop item.

Method group:

```razor
<button type="button" class="btn btn-success" @onclick="Save">
    <Language Tag="Save" IncludeIcon="true" />
</button>
```

Lambda with captured loop value:

```razor
<button type="button" class="btn btn-xs btn-primary nowrap" @onclick="@(() => EditTag(tag.TagId))">
    <Language Tag="Edit" IncludeIcon="true" />
</button>
```

Proof: method group [EditTag.razor:37-39](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L37-L39); lambda [Tags.razor:85-87](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L85-L87). A typed explicit-cast lambda is used for change events — `onchange="@((ChangeEventArgs e) => ValueChanged(e, prompt))"` ([PluginPrompts.razor:111](FreeCRM/CRM.Client/Shared/PluginPrompts.razor#L111)). When a loop value must be stable inside a lambda, copy it to a local first — `int currentCounter = counter;` ([PluginPrompts.razor:142](FreeCRM/CRM.Client/Shared/PluginPrompts.razor#L142)).

**Rule 6.10 — Capture an element handle with `@ref="_field"`, typed `ElementReference`.** Use `@ref` to grab a handle to a rendered element (declaring the field as `ElementReference`), then pass it to JS-interop helpers that must act on the actual DOM node (positioning a tooltip, focusing an input).

```razor
<i @ref="_element" @onclick="ShowTooltip" class="tooltip-item">@((MarkupString)_icon)</i>
...
protected ElementReference _element;
```

Proof: [Tooltip.razor:4,9](FreeCRM/CRM.Client/Shared/Tooltip.razor#L4-L9). Note `@key=` is **not used anywhere** in hand-written Razor — the team relies on default list diffing in `@foreach` loops rather than explicit keys.

**Rule 6.11 — Generic components declare `@typeparam` and are invoked with an explicit `TValue`.** A component that works over an arbitrary type declares `@typeparam TValue` and exposes `TValue Value` / `EventCallback<TValue> ValueChanged`; callers must specify the type via `TValue="..."`. This lets one component (plugins, date pickers) serve many data types.

Definition:

```razor
@typeparam TValue
...
[Parameter] public TValue? Value { get; set; }
[Parameter] public EventCallback<TValue> ValueChanged { get; set; }
```

Invocation:

```razor
<BlazorPlugins Module="@_pageName" Button="true" TValue="System.String" />
```

Proof: definition [BlazorPlugins.razor:3,16-17](FreeCRM/CRM.Client/Shared/BlazorPlugins.razor#L16-L17); invocation [Tags.razor:28](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L28). Callback naming is per-component — the generic `DateTimePicker` pairs `Value` with an `OnUpdate` callback instead of `ValueChanged` ([EditAppointment.razor:81](FreeCRM/CRM.Client/Pages/Scheduling/EditAppointment.razor#L81)).

---

<a id="code-block"></a>
## 7. The `@code` Block

**Why it matters:** the `@code` block is the C# brain of a page, and in FreeCRM it is almost a **fill-in-the-blanks template** — nearly every page in `CRM.Client` follows a byte-for-byte member order. Learn the template once and every page's logic is in the same place. (For the C# *syntax* inside this block — brace placement, casing, `String.Empty`, underscore fields — defer to doc 055; this section is about *structure and lifecycle*, not syntax.)

**The member order (top to bottom):**

> `[Parameter]`s → `protected` state fields → `_pageName` → `Dispose()` → `OnAfterRenderAsync` → `OnInitialized` → `OnDataModelUpdated` → UI handlers → data methods (`Delete`/`Load`/`Save`, grouped) → `SignalRUpdate` (last)

**Rule 7.1 — `[Parameter]` properties go first, one per line.** Parameters (route values and inputs) are the component's public contract, so they sit at the top where you look first.

```csharp
@code {
    [Parameter] public string? id { get; set; }
    [Parameter] public string? TenantCode { get; set; }
```

Proof: [EditTag.razor:141-143](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L141-L143).

**Rule 7.2 — `protected` underscore state fields come second.** After the parameters, declare component-local view-state as `protected` fields — booleans (`_loading`, `_loadedData`, `_newTag`, `_validatedUrl`), the title, the data object — all underscore-prefixed. `protected` (not `private`) is used uniformly even though these are never inherited.

```csharp
    protected bool _loading = true;
    protected bool _loadedData = false;
    protected bool _newTag = false;
    protected string _title = String.Empty;
    protected DataObjects.Tag _tag = new DataObjects.Tag();
    protected bool _validatedUrl = false;
```

Proof: [EditTag.razor:145-150](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L145-L150). The field set is per-page (a list page like [Tags.razor:124-126](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L124-L126) only needs a few). (For *why* it's `String.Empty` and `_PascalCase`, see doc 055.)

**Rule 7.3 — `_pageName` is its own field, just after the state fields.** Give every page a `protected string _pageName = "...";` set to the lowercase view key. This one string drives the render guard (`Model.View == _pageName`), the SignalR filter, and plugin lookups (`Helpers.HaveBlazorPlugins(_pageName, ...)`), so it is centralized rather than repeated as a literal.

```csharp
    protected bool _validatedUrl = false;

    protected string _pageName = "edittag";
```

Proof: [EditTag.razor:150-152](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L150-L152) (compare [Tags.razor:128](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L128) → `"tags"`). Reusable Shared components (e.g. `Language.razor`) have no `_pageName` because they are not routed views.

**Rule 7.4 — `Dispose()` comes next and unsubscribes every DataModel event.** Declare `public void Dispose()` right after the fields and detach every model event you subscribed to. `BlazorDataModel` is an injected **singleton** (one shared instance for the whole app); if a destroyed component's handler stays attached, the singleton keeps it alive — a memory leak and stale-callback bug. The page's `@implements IDisposable` (Rule 2.3) is what makes the framework call this.

```csharp
    public void Dispose()
    {
        Model.OnChange -= OnDataModelUpdated;
        Model.OnSignalRUpdate -= SignalRUpdate;
    }
```

Proof: [EditTag.razor:156-160](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L156-L160). Redraw-only components unsubscribe just `OnChange`, bound straight to the framework's `StateHasChanged` ([Language.razor:46-49](FreeCRM/CRM.Client/Shared/Language.razor#L46-L49)).

**Rule 7.5 — The DataModel subscription API is plain `+=` / `-=` on its events — there is no `Subscribe()`/`Unsubscribe()` method.** `BlazorDataModel` publishes ordinary C# `event`s. A setter on any tracked property calls `NotifyDataChanged()` → `OnChange?.Invoke()`, which fans out to every subscribed component's handler. So **subscribing is literally `+=`** in `OnInitialized`, and unsubscribing is `-=` in `Dispose`.

```csharp
    /// <summary>
    /// The OnChange event that can be subscribed to in a view or component to be notified when this model changes.
    /// </summary>
    public event Action? OnChange;
    ...
    public event Action<DataObjects.SignalRUpdate>? OnSignalRUpdate;
    ...
    private void NotifyDataChanged() => OnChange?.Invoke();
```

Proof: [DataModel.cs:2095-2120](FreeCRM/CRM.Client/DataModel.cs#L2095-L2120) (`OnChange` at 2098, `OnSignalRUpdate` at 2108, `NotifyDataChanged` at 2120); the subscribe side at [EditTag.razor:188-189](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L188-L189) and teardown at [EditTag.razor:158-159](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L158-L159). The model also exposes `OnDotNetHelperHandler`, `OnTenantChanged`, `OnTenantChanging` for components that need them ([DataModel.cs:2103-2118](FreeCRM/CRM.Client/DataModel.cs#L2103-L2118)), but most pages only use `OnChange` + `OnSignalRUpdate`. The state-container philosophy behind this model is doc [014](014_state-container.md) and [015](015_listening-for-change.md).

**Rule 7.6 — Lifecycle methods are ordered by purpose, not by Blazor's call order: `Dispose` → `OnAfterRenderAsync` → `OnInitialized` → `OnDataModelUpdated`.** The same physical layout appears in every page so you always find each method in the same place — even though this deliberately does *not* match runtime order.

```csharp
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            Model.TenantCodeFromUrl = TenantCode;
        }
        ...
    }

    protected override void OnInitialized()
    {
        Model.OnChange += OnDataModelUpdated;
        Model.OnSignalRUpdate += SignalRUpdate;
        Model.View = _pageName;
    }
```

Proof: [EditTag.razor:156-198](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L156-L198); identical layout in [EditDepartment.razor:117-159](FreeCRM/CRM.Client/Pages/Settings/Departments/EditDepartment.razor#L117-L159) and [Tags.razor:130-172](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L130-L172).

**Rule 7.7 — `OnInitialized` (synchronous) does setup; data loading is deferred to `OnAfterRenderAsync`, gated by `_loadedData`.** Use the sync `OnInitialized` to subscribe to events and set `Model.View = _pageName`. Do the actual async data load from `OnAfterRenderAsync`, because loading needs the model `Loaded` and the user `LoggedIn`, which is only guaranteed after first render. Gating on `_loadedData` (plus a navigation-id comparison on edit pages) prevents reloading on every re-render.

```csharp
        if (Model.Loaded && Model.LoggedIn) {
            if (!Model.FeatureEnabledTags || !Model.User.Admin) {
                Helpers.NavigateToRoot();
                return;
            }
            ...
            if (!_loadedData || Helpers.StringValue(Model.NavigationId) != Helpers.StringValue(id)) {
                _loadedData = true;
                await LoadTag();
            }
        }
```

Proof: [EditTag.razor:168-183](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L168-L183); setup in `OnInitialized` at [:186-191](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L186-L191). List pages gate only on `!_loadedData` ([Tags.razor:153-156](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L153-L156)). The async `OnInitializedAsync` is reserved for the four infrastructure files that must `await` during startup (`MainLayout`, `SelectFile`, `BlazorPlugin`, `ServerUpdated`) — regular Settings pages never use it. (The click-to-database round-trip this load performs is doc [017](017_click-to-database.md).)

**Rule 7.8 — `OnDataModelUpdated` re-renders only when this page is the active view.** The `OnChange` handler is named `OnDataModelUpdated` and calls `StateHasChanged()` only if `Model.View == _pageName`. `OnChange` fires on *every* subscribed component for *any* model mutation; the guard means a backgrounded page does not waste a render cycle when another view changes the shared model.

```csharp
    protected void OnDataModelUpdated()
    {
        if (Model.View == _pageName) {
            StateHasChanged();
        }
    }
```

Proof: [EditTag.razor:193-198](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L193-L198); identical in [Tags.razor:167-172](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L167-L172) and [EditDepartment.razor:154-159](FreeCRM/CRM.Client/Pages/Settings/Departments/EditDepartment.razor#L154-L159). Redraw-only components skip the wrapper and bind `OnChange` directly to `StateHasChanged` ([Language.razor:53](FreeCRM/CRM.Client/Shared/Language.razor#L53)), because they have no `_pageName` to guard on.

**Rule 7.9 — UI handlers come after the change handler, before the data methods.** Place small UI/navigation handlers (`Back`, `EditTag`, formatting helpers) between `OnDataModelUpdated` and the heavier CRUD/data methods.

```csharp
    protected void EditTag(Guid TagId)
    {
        Helpers.NavigateTo("Settings/EditTag/" + TagId.ToString());
    }
```

Proof: [Tags.razor:174-177](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L174-L177); `Back()` on the editor at [EditTag.razor:200-203](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L200-L203). Pages with no extra UI handlers simply omit the section.

**Rule 7.10 — CRUD/data methods are grouped together: the loader, `Save`, and `Delete`.** Group the data methods, each wrapping the server call in `Model.Message_*()` / `Model.ClearMessages()` and handling the null + failure cases. Every round-trip goes through `Helpers.GetOrPost<T>(...)`, which returns `null` on transport failure and a typed result otherwise — so the convention is: clear/show status messages around the call, then branch `null` → `Model.UnknownError()`, failure → `Model.ErrorMessages(...)`, success → navigate.

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

Proof: [EditTag.razor:330-344](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L330-L344) (`LoadTag` 227-265, `Delete` 205-225); same shape in [EditDepartment.razor:166-222](FreeCRM/CRM.Client/Pages/Settings/Departments/EditDepartment.razor#L166-L222). **Order note:** the methods are *grouped*, but the actual physical sequence in both edit pages is `Delete` → `Load` → `Save`, not a strict Load→Save→Delete. Treat the rule as "loader + Save + Delete clustered together," sequence varies. The `GetOrPost` wrapper and standard result shape are docs [026](026_standard-result.md) and [012](012_wrapped-plumbing.md).

**Rule 7.11 — A loader ends with `StateHasChanged()`, then moves keyboard focus.** A loader flips `_loading = false`, calls `this.StateHasChanged()`, then awaits `Helpers.DelayedFocus("<element-id>")` to move focus into the form's first editable field. This is accessibility: after the spinner is replaced by the form, keyboard/screen-reader users land on the input. `DelayedFocus` waits for the DOM to render first.

```csharp
        _loading = false;
        this.StateHasChanged();

        await Helpers.DelayedFocus("edit-tag-Name");
```

Proof: [EditTag.razor:261-264](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L261-L264); same at [EditDepartment.razor:218-221](FreeCRM/CRM.Client/Pages/Settings/Departments/EditDepartment.razor#L218-L221). List pages have nothing to focus, so the loader ends with just `StateHasChanged()` ([Tags.razor:197](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L197)). (Both `this.StateHasChanged()` and bare `StateHasChanged()` appear — no consistent preference.)

**Rule 7.12 — `SignalRUpdate` is the LAST method in the block.** Put the `SignalRUpdate(DataObjects.SignalRUpdate update)` handler at the very bottom. SignalR pushes real-time changes from *other* users; the handler filters on `Model.View == _pageName`, the matching `UpdateType`, the matching record id, and `update.UserId != Model.User.UserId` (to ignore echoes of your own changes), then reacts — so an edit by another user live-updates `_tag` (or shows a "record deleted" message) without a manual refresh.

```csharp
    protected void SignalRUpdate(DataObjects.SignalRUpdate update)
    {
        if (Model.View == _pageName && update.UpdateType == DataObjects.SignalRUpdateType.Tag && update.ItemId == _tag.TagId && update.UserId != Model.User.UserId) {
            switch (update.Message.ToLower()) {
                case "deleted":
                    Helpers.NavigateTo("Settings/Tags");
                    Model.Message_RecordDeleted("", update.UserDisplayName);
                    break;
```

Proof: [EditTag.razor:347-366](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L347-L366); list-page variant (just `StateHasChanged()`) at [Tags.razor:200-205](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L200-L205). The full SignalR mechanism is doc [046](046_realtime-signalr.md).

**Rule 7.13 — Call `StateHasChanged()` manually after any state change outside a normal UI event.** Blazor auto-renders after UI event handlers, but changes made inside `await`-continuations or external callbacks (SignalR, `OnChange`) are *not* auto-detected — so the component must request the re-render explicitly.

```csharp
                case "saved":
                    var tag = Helpers.DeserializeObject<DataObjects.Tag>(update.ObjectAsString);
                    if (tag != null) {
                        _tag = tag;
                        StateHasChanged();
                        Model.Message_RecordUpdated("", update.UserDisplayName);
                    }
                    break;
```

Proof: [EditTag.razor:356-363](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L356-L363).

**Rule 7.14 — All user-visible text goes through localization: `<Language Tag="..." />` in markup, `Helpers.Text("...")` in code.** Never hard-code display strings. `Helpers.Text` looks the key up in `Model.Language.Phrases`, falls back to `Model.DefaultLanguage.Phrases`, and only then humanizes the raw key — so every label is translatable, and missing keys are auto-flagged (rendered ALL-CAPS) when `MarkUndefinedStrings` is true.

```csharp
        if (missingModule) {
            errors.Add(Helpers.Text("TagMustBeEnabledForAtLeastOneModule"));
        }
```

Proof (helper): [Helpers.cs:6307-6366](FreeCRM/CRM.Client/Helpers.cs#L6307-L6366) (signature 6307-6314; primary lookup 6320-6329; fallback 6331-6338; not-found humanize with `AllCaps` 6359-6366). Code use: [EditTag.razor:310](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L310); markup use: [Tags.razor:13](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L13). The `<Language>` component itself calls `Helpers.Text(Tag.Trim(), false, null, MarkUndefinedStrings)` at [Language.razor:72](FreeCRM/CRM.Client/Shared/Language.razor#L72). See [035](035_validation-localization-a11y.md) for the localization model.

**Rule 7.15 — Validation collects errors into a list, sets a focus target, and shows them via the model.** In `Save`, accumulate `List<string> errors`, track the id of the first invalid field in `string focus`, push field errors via `Helpers.MissingRequiredField("Key")`, and if any errors exist call `Model.ErrorMessages(errors)` + `await Helpers.DelayedFocus(focus)` and `return` *before* saving. All errors surface at once, and focus jumps to the first offending field.

```csharp
        if (String.IsNullOrWhiteSpace(_tag.Name)) {
            errors.Add(Helpers.MissingRequiredField("TagName"));
            if (focus == String.Empty) { focus = "edit-tag-Name"; }
        }
        ...
        if (errors.Any()) {
            Model.ErrorMessages(errors);
            await Helpers.DelayedFocus(focus);
            return;
        }
```

Proof: [EditTag.razor:274-328](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L274-L328); `MissingRequiredField` at [Helpers.cs:4664-4668](FreeCRM/CRM.Client/Helpers.cs#L4664-L4668). Sub-modules contribute errors too — `EditTag.Save` merges `AppModule.Save(_tag)`'s messages and focus ([EditTag.razor:313-322](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L313-L322)).

**Rule 7.16 — Flag required inputs with `Helpers.MissingValue(...)`, a `<RequiredIndicator />`, `Required="true"` labels, and stable ids.** Wrap a required input's CSS class in `Helpers.MissingValue(value, "form-control")`, label it `<Language Tag="Key" Required="true" />`, show a `<RequiredIndicator />` near the top of the form, and give every input a stable `id` matching its `<label for=...>` and the `DelayedFocus` target. `MissingValue` appends the `m-r` marker class when the bound value is empty, so empty required fields flag visually as you type (the input uses `@bind:event="oninput"`).

```razor
            <div class="mb-2">
                <label for="edit-tag-Name">
                    <Language Tag="TagName" Required="true" />
                </label>
                <input type="text" id="edit-tag-Name" @bind="_tag.Name" @bind:event="oninput"
                    class="@Helpers.MissingValue(_tag.Name, "form-control")" />
            </div>
```

Proof: [EditTag.razor:65-71](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L65-L71); `<RequiredIndicator />` at [EditTag.razor:58](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L58); `MissingValue(string?, ...)` at [Helpers.cs:4762-4769](FreeCRM/CRM.Client/Helpers.cs#L4762-L4769); the `m-r` marker class returned by `MissingValueClass` at [Helpers.cs:4774-4778](FreeCRM/CRM.Client/Helpers.cs#L4774-L4778) (literal on line 4776). `MissingValue` is overloaded for `DateTime?`, `decimal?`, `Guid?`, `int?`, `object[]?`, and `string?` so any field type can be flagged. Accessibility is doc [035](035_validation-localization-a11y.md).

---

<a id="quick-reference"></a>
## 8. Quick Reference Cheat Sheet

The left column is wrong for this codebase; the right column matches verified source.

**File shape & directives**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `@code` block in the middle, or two `@code` blocks | one `@code` block, always at the very bottom |
| `@page` placed after `@inject` | `@page` first; `@inject` grouped (usually last) |
| `@layout` / `@attribute` directives | never used — not part of the standard |
| `@inherits` on a regular page | `@inherits` only on `MainLayout` (`LayoutComponentBase`) |
| subscribe to `Model.OnChange` with no `Dispose` | `@implements IDisposable` + `-=` in `Dispose()` |

**The `.App.razor` seam**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| editing the upstream `<X>.razor` to customize | put changes in `<X>.App.razor` (upgrade-safe) |
| referencing it as `<X.App>` | `<X_App />` (dot becomes underscore) |
| shipping it enabled by default | `Enabled` / `OverridePage` default `false` |

**Markup**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `<DIV>`, `<Input>` (HTML uppercased) | `<div>`, `<input>` lowercase; components PascalCase |
| `<input>` (unclosed void element) | `<input ... />` self-closed, space before `/>` |
| custom CSS for spacing/buttons/forms | Bootstrap utility classes (`mb-2`, `btn btn-success`) |
| `@Helpers.BuildUrl("x")` for a complex expr | `@(Helpers.BuildUrl("x"))` — wrap expressions |
| rendering HTML as a plain string (gets escaped) | `@((MarkupString)expr)` for trusted HTML |
| `//` to disable Razor markup | `@* ... *@` (stripped at compile time) |
| `<!-- -->` repurposed for Razor notes | `<!-- -->` reserved for HTML + `{{ModuleItem...}}` |

**Components, binding, events**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `<i class="fa-...">` in a page | `<Icon Name="..." />` |
| hard-coded English label | `<Language Tag="Key" />` |
| `<CascadingValue>` for shared state | `@inject BlazorDataModel Model` + `Model.OnChange` |
| new code using `Delegate?` + `DynamicInvoke` | `EventCallback` / `EventCallback<T>` |
| `@onclick="@(() => Save())"` (no args needed) | `@onclick="Save"` (bare method group) |

**`@code` member order**

```text
[Parameter]s → protected _state fields → _pageName →
Dispose() → OnAfterRenderAsync → OnInitialized → OnDataModelUpdated →
UI handlers → data methods (Delete/Load/Save) → SignalRUpdate (last)
```

**The DataModel subscription pattern (memorize this pair)**

```csharp
// in OnInitialized:
Model.OnChange += OnDataModelUpdated;
Model.OnSignalRUpdate += SignalRUpdate;
Model.View = _pageName;

// in Dispose:
Model.OnChange -= OnDataModelUpdated;
Model.OnSignalRUpdate -= SignalRUpdate;

// the guarded handler:
protected void OnDataModelUpdated()
{
    if (Model.View == _pageName) {
        StateHasChanged();
    }
}
```

---

<a id="faq"></a>
## 9. FAQ — "Why Do We Do That?"

**Q1. What is a `.App.razor` file, and when do I edit it vs the base?**
The base `<X>.razor` is upstream framework code; an upgrade can overwrite it. The matching `<X>.App.razor` is *your* seam — your customizations survive upgrades because they live there. **Always edit the `.App.` file, never the base.** The base reaches your `.App.` through a `new X_App()` field and a boolean toggle (Section 4).

**Q2. Why does one page have four `@page` lines?**
Two reasons multiply: FreeCRM is multi-tenant, so every route has a plain form *and* a `/{TenantCode}/...` form; and one editor component handles both "edit" (with an `{id}`) and "add" (no id). Edit + tenant-edit + add + tenant-add = four ([EditUser.razor:1-4](FreeCRM/CRM.Client/Pages/Settings/Users/EditUser.razor#L1-L4)).

**Q3. Why no `@layout` or `@attribute` anywhere?**
The codebase simply doesn't use them — a repo-wide search returns zero of each. The layout relationship is handled through `MainLayout` (the only file with `@inherits`), and there are no `@attribute`-based route constraints. If you find yourself reaching for either, you are off the beaten path; check with a maintainer.

**Q4. The dot in `EditTag.App.razor` — why does the tag say `<EditTag_App>`?**
Razor turns the `.` in a filename into `_` for the generated C# class. So the file compiles to a class `EditTag_App`, and that underscore form is what you use both as a tag and as a C# type ([EditTag.razor:130,154](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L130-L154)).

**Q5. How do I make a component show live updates when data changes?**
Inject `BlazorDataModel Model`, subscribe `Model.OnChange += OnDataModelUpdated;` in `OnInitialized`, unsubscribe in `Dispose`, and have `OnDataModelUpdated` call `StateHasChanged()` (guarded by `Model.View == _pageName`). For cross-user real-time updates, also subscribe `Model.OnSignalRUpdate += SignalRUpdate;` (Rules 7.5, 7.8, 7.12).

**Q6. Where do I put a new method in `@code`?**
By role: a UI/navigation handler goes after `OnDataModelUpdated` and before the data methods; a data method (load/save/delete) goes in the CRUD cluster; the `SignalRUpdate` handler always stays last (Section 7's member order).

**Q7. Why subscribe with `+=` and unsubscribe with `-=` — is there a `Subscribe()` method?**
No. `BlazorDataModel` exposes plain C# `event`s ([DataModel.cs:2095-2120](FreeCRM/CRM.Client/DataModel.cs#L2095-L2120)), and `+=`/`-=` is how you attach/detach a handler to an event. There is no wrapper method; the events *are* the API.

**Q8. Why must I unsubscribe in `Dispose()`?**
`Model` is a singleton that outlives any single page. If a destroyed component leaves its handler attached, the singleton keeps the dead component alive (a memory leak) and keeps calling a stale handler. `@implements IDisposable` lets Blazor call your `Dispose()` to detach (Rules 2.3, 7.4).

**Q9. Why Bootstrap classes instead of custom CSS?**
Bootstrap gives consistent spacing/buttons/forms/grids out of the box, so pages look uniform without bespoke stylesheets and without per-page CSS to maintain. Custom project classes exist only for app-specific semantics Bootstrap can't express (`page-title`, `item-deleted`). See Rule 5.4 and doc [057](057_css-style-reference.md).

**Q10. When do I write `@(...)` vs bare `@`?**
Bare `@member` for a single variable or member chain; `@(...)` for anything more complex — a method call, a cast, string concatenation. Parentheses tell Razor where the expression ends (Rule 5.7).

**Q11. My HTML string is showing up as literal text with the tags visible. Why?**
Blazor HTML-encodes string output by default to prevent injection. When the string is *trusted* HTML (icons, server markup), opt out with `@((MarkupString)expr)`. Use it only for Helper-produced or server-supplied HTML, never raw user input (Rule 5.8).

**Q12. Why is `_pageName` a field instead of just a literal string?**
That one string drives the render guard, the SignalR filter, and plugin lookups. Centralizing it in a field means there's one place to set the view key, and the three call sites stay in sync (Rule 7.3).

**Q13. Why does data load in `OnAfterRenderAsync` instead of `OnInitialized`?**
Loading needs `Model.Loaded` and the user `LoggedIn`, which is only guaranteed after first render. `OnInitialized` (synchronous) does the cheap setup — subscribe to events, set `Model.View`; the async load is deferred and gated by `_loadedData` so it doesn't re-fire on every render (Rule 7.7).

**Q14. `EventCallback` vs `Delegate?` — which do I use for a new component event?**
`EventCallback` / `EventCallback<T>`. It's the modern Blazor contract and auto-triggers the parent's re-render. The `Delegate?` + `DynamicInvoke` form you'll see in older files is legacy; don't add new code in that style (Rule 6.8).

**Q15. Why two ways to bind — `@bind` and `@bind-Value`?**
`@bind` two-way-binds a native HTML input to a field (`@bind="_tag.Name"`). `@bind-Value` two-way-binds a whole object into a *child component*, which exposes a `Value` + `EventCallback<T> ValueChanged` pair to push edits back up. Different targets, same idea (Rules 6.6, 6.7).

**Q16. Why is `@code` ordered `Dispose → OnAfterRenderAsync → OnInitialized` when that's not the order Blazor calls them?**
Deliberately. The order is by *purpose* (teardown, then data gate, then setup, then change handler) so every page's `@code` is laid out identically and you always know where to look. Predictability beats matching runtime order (Rule 7.6).

---

<a id="related-docs"></a>
## 10. Related Docs

- [051 — The Author House Style](051_house-code-style.md) — the general C# brace/casing/`String.Empty` rules referenced throughout this doc.
- [055 — The C# Style Reference](055_csharp-style-reference.md) — deep C# syntax rules that apply *inside* the `@code` block; defer to it rather than re-deriving syntax here.
- [057 — The CSS Style Reference](057_css-style-reference.md) — the Bootstrap-first / custom-class rules behind Section 5's markup.
- [058 — The JavaScript Style Reference](058_javascript-style-reference.md) — JS-interop conventions reached from `@code` (e.g. `DelayedFocus`).
- [032 — Building From the Shared Component Shelf](032_shared-components.md) — the catalog of shared components you invoke in Section 6.
- [033 — Charts, Code Editors, Rich Text, and PDFs](033_rich-components.md) — the rich components and their binding shapes.
- [035 — Validated, Translated, and Reachable](035_validation-localization-a11y.md) — the validation/localization/accessibility model behind Rules 7.14–7.16.
- [047 — Growing the Shared Library](047_custom-components.md) — authoring your own reusable components.
- [041 — Code the Framework Can Update Underneath](041_upgrade-safe-model.md) — the C# partial-class counterpart to the `.App.razor` seam.
- [046 — Real-Time With SignalR](046_realtime-signalr.md) — the SignalR mechanism behind `SignalRUpdate`.

---
*GuidesV2 056 · The Razor / Blazor / HTML Style Reference · drafted 2026-06-05 from citation-verified source under `FreeCRM/CRM.Client/` (Tags.razor, EditTag.razor, EditDepartment.razor, MainLayout.razor + MainLayout.App.razor, NavigationMenu.razor + .App, the AppComponents seam files, Icon/Language/Tooltip/UndeleteMessage/RequiredIndicator/LoadingMessage/StickyMenuIcon/ToastMessages/BlazorPlugins/PluginPrompts/TagSelector, plus DataModel.cs and Helpers.cs). Every rule carries a line-level proof link; no vendored/third-party code is cited.*

# 032 — Building From the Shared Component Shelf

> **Document ID:** 032  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Catalog the shared-component library and how to compose vetted UI pieces instead of reinventing them.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 03x (Core Craft: Everyday Screens and Components) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Reuse the Shelf](#why-reuse) | Plain-language overview and key terms |
| 2 | [Component Catalog at a Glance](#catalog) | Inventory of the real `Shared/` components |
| 3 | [Finding the Right Component](#finding) | Where they live and how to match one to a need |
| 4 | [Composing Components Together](#composing) | Nesting pieces and launching dialog components |
| 5 | [Props, Slots, and Customization](#customizing) | Configuring components via `[Parameter]` and callbacks |
| 6 | [When to Build New Instead](#build-new) | Deciding to extend versus create fresh |
| 7 | [Common Pitfalls and Anti-Patterns](#pitfalls) | Mistakes that break reuse and consistency |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-reuse"></a>
## 1. Why Reuse the Shelf

**Why it matters:** Almost every screen in the app needs the same small jobs done — show a translated label, draw an icon, show a "loading…" spinner, warn that a record is deleted, confirm a dangerous action. If each page solved those jobs its own way, you would get a thousand slightly-different buttons and a translation that updates in nine places but not the tenth. The "shelf" is a folder of pre-built, already-debugged pieces that do those jobs the *one* correct way. Reaching for a shelf piece is faster than writing your own and keeps the whole app consistent.

**Plain-language terms (defined on first use):**

- **Component** — In Blazor (Microsoft's framework for building web UIs in C# instead of JavaScript), a component is a reusable chunk of UI saved as a `.razor` file. You drop it into a page with an HTML-like tag, e.g. `<LoadingMessage />`. The tag name is just the file name.
- **Shared component** — A component that lives in the `Shared/` folder and is meant to be used by *many* pages, not owned by one. These are the "shelf."
- **Parameter** — An input you pass to a component, written like an HTML attribute. In the component's C# it is a property marked `[Parameter]`. Example: in `<Language Tag="Cancel" />`, `Tag` is a parameter.
- **Markup vs. code** — A `.razor` file has two halves: the top is the visual markup (what gets drawn), the bottom is an `@code { }` block of C# (the logic). When you read a component, look at `@code` to learn its parameters.

The shelf exists because the alternative — copy-pasting the same `<div class="loading">…</div>` into every page — means a fix or a styling change has to be made everywhere at once. With a shared component, you fix it in one file and every page inherits the fix on the next build. That single-source-of-truth property is the entire reason to reuse instead of rebuild.

---

<a id="catalog"></a>
## 2. Component Catalog at a Glance

These are the real components in `FreeCRM/CRM.Client/Shared/`. They fall into a few natural groups. (The heavyweight ones — charts, code editors, PDF viewer, signature pad — get their own deep dive in [033](033_rich-components.md); they are listed here only so you know where they sit.)

**Everyday display pieces (drop them inline, no setup):**

| Component | What it does |
|---|---|
| `Language` | Renders a translated text label from a language tag. The workhorse of the app. |
| `Icon` | Draws an icon by friendly name, resolving to a Google Material, Font Awesome, or inline SVG glyph. |
| `LoadingMessage` | Shows the standard "loading…" heading while data is being fetched. |
| `RequiredIndicator` | The little "* indicates a required field" legend for forms. |
| `Tooltip` | A clickable info icon that pops a tooltip with help text. |
| `LastModifiedMessage` | An "Added / Last Modified by …" footer line, read straight off any record object. |
| `UndeleteMessage` | The red banner shown on a soft-deleted record, with an Undelete button. |
| `ModalMessage` | A bare wrapper that renders an HTML message inside a dialog. |
| `StickyMenuIcon` | The thumbtack toggle that pins/unpins the menus. |
| `SwitchTenants` | The "switch account" list shown when a user belongs to more than one tenant. |

**Pickers and input helpers (often shown in a dialog):**

| Component | What it does |
|---|---|
| `SelectFile` | A grid of stored files for the user to pick from. |
| `UploadFile` | A drag-and-drop file upload card (generic over the upload type). |
| `TagSelector` | A checklist for adding/removing tags on a record. |
| `GetInputDialog` | A prompt that asks the user for a value and returns it via a callback. |
| `GeneratePasswordDialog` | A password generator with copy-to-clipboard. |
| `RenderFiles` | Renders a list of attached files with view/delete actions. |
| `UserDefinedFields` | Renders the tenant's custom (admin-defined) fields. |

**Heavyweight / specialized (covered in [033](033_rich-components.md)):**

`Highcharts` (graphs), `MonacoEditor` and `HtmlEditorDialog` (rich text / code editors), `PDF_Viewer`, plus the plugin host components (`BlazorPlugin`, `BlazorPlugins`, `PluginPrompts`) and navigation shells (`NavigationMenu`, `OffcanvasPopoutMenu`). There is also an `AppComponents/` subfolder holding the editable, per-app screens (the home `Index`, `EditUser`, `Settings`, etc.) — those are *pages*, not generic shelf pieces.

> **A key distinction:** some tags you will use — `DeleteConfirmation`, `GetInput`, `GeneratePassword`, the rich `Editor` — are **not** in `Shared/`. They come from the external **FreeBlazor** NuGet package (referenced as `FreeBlazor` version `2.0.6` in `CRM.Client.csproj`, imported via `@using FreeBlazor` in `_Imports.razor`). The thin `Shared/` files like `GetInputDialog` and `GeneratePasswordDialog` are *wrappers* that wire the FreeBlazor component up to the app's translations and dialog service. So "the shelf" really has two tiers: local `Shared/` pieces, and the FreeBlazor package pieces underneath them.

---

<a id="finding"></a>
## 3. Finding the Right Component

**Why it matters:** You cannot reuse what you cannot find. The good news is the naming is literal and the location is fixed, so finding the right piece is mostly a folder listing away.

**Where they live:** `FreeCRM/CRM.Client/Shared/*.razor`. The file name *is* the tag name — `LoadingMessage.razor` is used as `<LoadingMessage />`. No `@using` is needed for these because `_Imports.razor` already imports `CRM.Client.Shared`.

**How to match a need to a component, in order:**

1. **Name it in plain words, then look for that word.** Need a "loading" message? `LoadingMessage`. Need to "select a file"? `SelectFile`. The names describe the job.
2. **Read the `@code` block to learn the contract.** Every parameter is a property tagged `[Parameter]`, and most carry an XML `/// <summary>` comment explaining them. For example, `LastModifiedMessage` documents its single input right in the source:

   ```csharp
   /// <summary>
   /// The object containing the Added, AddedBy, LastModified, and LastModifiedBy
   /// properties (or UploadDate and UploadedBy for files.)
   /// </summary>
   [Parameter] public Object? DataObject { get; set; }
   ```

   That tells you exactly how to call it: `<LastModifiedMessage DataObject="@myRecord" />`.
3. **Check whether it is "inline" or "dialog."** Inline pieces (`Language`, `Icon`, `LoadingMessage`, `LastModifiedMessage`, `UndeleteMessage`) you place directly in your markup. Dialog pieces (`SelectFile`, `TagSelector`, `GetInputDialog`, `GeneratePasswordDialog`, `UploadFile`) are usually *opened* through a `Helpers` method rather than placed by hand — see the next section.
4. **Search for an existing caller as a worked example.** Grep the codebase for the tag name; an existing page using it correctly is the best documentation. For instance, many edit pages already use `<DeleteConfirmation OnConfirmed="Delete" … />`, so you can copy that shape.

---

<a id="composing"></a>
## 4. Composing Components Together

**Why it matters:** Real features are not one component; they are several nested together. "Compose" just means *put components inside other components* so the small jobs add up to a working screen. The shelf is designed for this — pieces are deliberately tiny so they combine cleanly.

**Pattern A — nesting inline pieces.** Components freely contain other components. `RequiredIndicator`, a four-line file, is literally just a `required-flag` icon element and a `Language` tag:

```razor
<div class="required-indicator">
    <i class="required-flag"></i>
    <Language Tag="IndicatesRequiredField" />
</div>
```

The same nesting shows up everywhere. `UndeleteMessage` builds its banner out of several `<Language />` tags plus a button whose label is itself a `<Language Tag="UndeleteRecord" IncludeIcon="true" />`. You compose by stacking — there is no special "slot" syntax to learn for these; you just write the child tags where you want them drawn.

**Pattern B — the standard "loading then content" wrapper.** Almost every data screen follows the same shape: show `LoadingMessage` until the data arrives, then swap to the real UI. This is the most copied composition in the app. From `TagSelector`:

```razor
@if (_loading) {
    <LoadingMessage />
} else {
    // ...the real content once data is ready...
}
```

**Pattern C — launching a dialog component.** Dialog pieces are not placed in markup; they are opened on demand and hand their result back through a **callback** (a method you supply that the component calls when it is done — usually a `Delegate` or an `Action<T>`). The app's `Helpers` class wraps Radzen's `DialogService` so callers do not have to assemble the dialog by hand. For example, `Helpers.SelectFile` opens the `SelectFile` component and routes the chosen file's `Guid` to your method:

```csharp
public static async Task SelectFile(Action<Guid> OnFileSelected, string Title = "",
    bool ImagesOnly = false, bool ShowCancelButton = true, bool ShowRefreshButton = true)
{
    if (String.IsNullOrWhiteSpace(Title)) {
        Title = Text("SelectFile");
    }

    Dictionary<string, object?> parameters = new Dictionary<string, object?>();
    parameters.Add("OnFileSelected", OnFileSelected);
    parameters.Add("ImagesOnly", ImagesOnly);
    // ...
    await DialogService.OpenAsync<SelectFile>(Title, parameters, new DialogOptions() { ... });
}
```

So in a page you write `await Helpers.SelectFile(OnFileSelected: id => { _logoId = id; });` and the dialog, the file grid, and the close-on-pick behaviour are all handled for you. The same pattern — a Helpers method wrapping Radzen DialogService.OpenAsync<T> — powers `GetInputDialog`, `GeneratePasswordDialog`, `TagSelector`, `UploadFile`, `ModalMessage`, and the rest. **Takeaway:** for dialogs, call the matching `Helpers` method and pass a callback; do not hand-roll the dialog plumbing.

---

<a id="customizing"></a>
## 5. Props, Slots, and Customization

**Why it matters:** The whole point of reuse is to change behaviour *without copying the file*. You bend a shelf component to your need by setting its **parameters** (props) and handing it **callbacks** — never by editing the shared file for a one-screen tweak (that change would hit every other caller).

**Parameters = props.** Every input is a `[Parameter]` property. `Language` is the richest example — a handful of options change how the same label renders:

```csharp
[Parameter] public string Tag { get; set; } = String.Empty;          // which phrase to show (required)
[Parameter] public string? Class { get; set; }                        // extra CSS class around the text
[Parameter] public bool? IncludeIcon { get; set; }                    // draw the tag's icon before the text
[Parameter] public bool? ReplaceSpaces { get; set; }                  // turn spaces into &nbsp;
[Parameter] public bool? Required { get; set; }                       // append a required-field marker
[Parameter] public TextCase? TransformCase { get; set; }              // Normal / Lowercase / Uppercase / Sentence / Title
```

So `<Language Tag="Cancel" />` gives a plain translated label, while `<Language Tag="Cancel" IncludeIcon="true" />` adds the matching icon, and `<Language Tag="Welcome" ReplaceSpaces="true" />` keeps the phrase from wrapping. The `TransformCase` parameter takes the `TextCase` enum (`Normal, Lowercase, Uppercase, Sentence, Title`) defined in `DataModel.cs` — that is the kind of detail you confirm by reading the source rather than guessing.

**Optional parameters have sensible defaults.** Note the `?` on the types above (`bool?`, `TextCase?`): a nullable parameter you can omit. The component supplies the fallback. For instance `Tooltip` defaults its icon when you do not pass one:

```csharp
protected override void OnInitialized()
{
    if (!String.IsNullOrWhiteSpace(Icon)) {
        _icon = Icon;
    } else {
        _icon = "<i class=\"icon fa-solid fa-circle-info\"></i>";
    }
}
```

So `<Tooltip TipText="Help here" />` just works; `Icon` is there only when you want to override the default glyph.

**Callbacks = the "result" channel.** Components that produce a value or an event hand it back through a delegate parameter, not a return value. `UndeleteMessage` exposes `OnUndelete`; `SelectFile` exposes `OnFileSelected`; `TagSelector` exposes `OnComplete` and `OnValueChanged`. You supply the method and the component invokes it at the right moment:

```csharp
[Parameter] public EventCallback OnUndelete { get; set; }   // UndeleteMessage
```

```razor
<UndeleteMessage DeletedAt="@record.Deleted" OnUndelete="UndeleteRecord" />
```

**About "slots."** Blazor's version of a slot — markup you nest *inside* a component's tags — is called a `RenderFragment` / child content, and the named-region form (like `<CustomContent>` inside `UploadFile`, which itself is a MudBlazor feature) is how the framework supports it. Most of the simple `Shared/` pieces in this doc are configured purely through parameters and callbacks and do not expose a child-content slot; reach for child content only when a component is explicitly built to accept it.

**The rule of thumb:** customize through parameters and callbacks first. If the component genuinely cannot express what you need, that is the signal to read the next section — not to fork the file.

---

<a id="build-new"></a>
## 6. When to Build New Instead

**Why it matters:** Reuse is the default, but forcing a wrong-fit component into a job (or copying its file to tweak it) creates more mess than building fresh. Knowing where the line is keeps the shelf trustworthy.

**Reuse (or extend by parameter) when:**

- A shelf component does the job and only differs by a value you can pass as a parameter or callback — that is exactly what parameters are for.
- You need consistency with the rest of the app (labels, icons, loading states, delete banners). Rolling your own here actively *hurts* — you would drift from the standard look and lose translation coverage.

**Build something new when:**

- **No existing component matches the shape of the data or interaction.** If you are bending parameters in unnatural ways or ignoring half of them, the fit is wrong.
- **The behaviour is genuinely page-specific** and will never be reused. A one-off layout belongs in the page itself, not in `Shared/`.
- **You would otherwise edit a shared file to satisfy one caller.** Changing a shelf component for a single screen risks every other screen that uses it. Either add a *new optional parameter* (with a default that preserves current behaviour, the way `Language` and `Tooltip` are written) or build a separate component.

**Where new shared work goes:** if the new piece really is reusable, author it as its own `.razor` file following the same conventions — small surface, `[Parameter]` inputs with `/// <summary>` docs, callbacks for results, `LoadingMessage` for the loading state. The full how-to for adding to the library lives in [047 — Growing the Shared Library](047_custom-components.md). If it is reusable *and* low-level enough, it may even belong down in the FreeBlazor package rather than `Shared/`.

---

<a id="pitfalls"></a>
## 7. Common Pitfalls and Anti-Patterns

**Why it matters:** The shelf only stays valuable if people use it as intended. A few recurring mistakes quietly erode it.

- **Copy-pasting markup instead of using the component.** Writing your own `<h2 class="loading">…</h2>` instead of `<LoadingMessage />`, or a raw `<i class="fa-…">` instead of `<Icon Name="…" />`, means the next styling change skips your copy. Use the tag.

- **Hard-coding visible text instead of `Language`.** Every user-facing string should go through `<Language Tag="…" />` (or `Helpers.Text(...)` in code). Typing literal English defeats translation and the per-tenant phrase overrides. The shelf is built so you almost never write a bare string in markup.

- **Forking a shared file for a one-screen tweak.** Copying `SelectFile.razor` to `SelectFileMyPage.razor` and editing it forks the maintenance forever. Add a parameter or compose around it instead.

- **Hand-rolling dialog plumbing.** Manually calling `DialogService.OpenAsync<…>` with your own parameter dictionary, when a `Helpers.SelectFile` / `Helpers.SelectTags` / `Helpers.GetInput` wrapper already exists, duplicates logic and skips the standard dialog options (size, focus, title defaults). Call the `Helpers` method.

- **Forgetting the result comes via callback, not return value.** Dialog and picker components do not "return" anything to the caller; they invoke your `OnFileSelected` / `OnComplete` / `OnUndelete` delegate. If you await the open call expecting a value back, you will get nothing. Pass a callback that captures the result.

- **Confusing the two tiers.** Reaching for `<DeleteConfirmation>` and assuming it is in `Shared/` (it is a FreeBlazor package component), or editing a thin `Shared/` wrapper when the real behaviour lives in the package underneath. Know whether the piece is local or from FreeBlazor before you try to change it.

- **Ignoring `IDisposable` / `Model.OnChange`.** Components that react to live data (e.g. `Language`, `ToastMessages`, `SwitchTenants`) subscribe to `Model.OnChange` in `OnInitialized` and unsubscribe in `Dispose`. If you author a new shared component that listens to the model, follow the same subscribe/unsubscribe pair or you will leak handlers.

---

<a id="related-docs"></a>
## 8. Related Docs

- [031 — List and Edit, the House Pattern](031_crud-templates.md) — pages built from these pieces
- [033 — Charts, Editors, Signatures, and Graphs](033_rich-components.md) — the heavyweight components
- [047 — Growing the Shared Library](047_custom-components.md) — authoring new shared components

---
*GuidesV2 · 032 · drafted from source (`FreeCRM/CRM.Client/Shared/*.razor`, `Helpers.cs`, `_Imports.razor`, `CRM.Client.csproj`).*

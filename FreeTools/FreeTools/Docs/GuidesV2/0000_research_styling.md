# 0000 — Research: Our Current Styling Recommendations (Consolidated)

> **Document ID:** 0000
> **Category:** Research
> **Purpose:** A single consolidated snapshot of every style/convention recommendation our existing guide docs currently make for FreeCRM-based projects.
> **Audience:** Devs, AI agents, contributors.
> **Outcome:** 📖 One-stop reference for "what our docs say good FreeCRM code looks like," sourced doc-by-doc.

**Scope:** This doc summarizes the *current state of our written guidance* in `Docs/Guides/`. It is the **baseline**. A companion doc, [0001_freecrm_styling_latest_research.md](0001_freecrm_styling_latest_research.md), records what the **original author (Brad Wickett / `wicketbr`)** has *explicitly* changed in the upstream FreeCRM repo during his recent style-unification work — and flags where our inferred guidance below now needs to be confirmed, tightened, or corrected against his actual cleanup.

---

## ⚠️ Important Caveat: Inferred vs. Authoritative

Most of our style guidance below was **reverse-engineered from reading code** across the FreeCRM ecosystem, not handed to us as an explicit spec. The guides say so themselves:

- `004_styleguide.md`: *"consolidated from analysis of the authoritative projects … plus 30+ additional FreeCRM-based projects."*
- `005_style.comments.md`: *"Patterns derived from analysis of 500+ comments across multiple production projects."*
- `006_architecture.unique_features.md`: *"Analysis of 25+ FreeCRM-based repositories."*

So this document is best read as **"our best understanding of the author's preferences, as inferred from his code."** Where the upstream author has now made those preferences explicit (via his recent cleanup commits), **doc 0001 is authoritative and supersedes anything here that conflicts.**

### Authoritative source ordering (per `004_styleguide.md`)

When two projects disagree, our docs say to trust them in this order:

1. **FreeCRM-main** — the public base template all projects derive from
2. **nForm** — built from scratch in 2025, cleanest implementation (private)
3. **TrusselBuilder** — large project, migrated across versions (private)
4. **Helpdesk4** — large project, migrated across versions (private)

> **Note for 0001:** "FreeCRM-main" here = the upstream `wicketbr/FreeCRM` that our `WSU-EIT/FreeCRM` fork merges from. Brad Wickett is the original author; his explicit style is the real top of this list.

---

## Source Map — Where Each Recommendation Lives

| Topic | Primary doc |
|-------|-------------|
| What's custom vs stock .NET | `006_architecture.freecrm_overview.md` |
| File naming (`.App.`), C#/Razor style | `004_styleguide.md` (the big one) |
| Style-guide index + file-naming reminder | `005_style.md` |
| Comment voice & patterns | `005_style.comments.md` |
| Architecture principles | `006_architecture.md`, `006_architecture.unique_features.md` |
| Helpers / utilities | `007_patterns.helpers.md` |
| SignalR real-time | `007_patterns.signalr.md` |
| Playwright automation | `007_patterns.playwright.md` |
| Razor page templates (CRUD) | `008_components.razor_templates.md` |
| Components (wizard, monaco, highcharts, signature, network) | `008_components.*.md` |
| Quickstart, roleplay, docs standards, templates | `000`–`003` |
| Doc format & numbering | `002_docsguide.md`, `003_templates.md` |

---

## 1. The Core Principle — The Wrapper Rule

FreeCRM wraps many .NET features with tenant-aware helpers. The single most important rule:

> **If it starts with `Helpers.`, `Model.`, `DataObjects.`, or `DataAccess.`, it is FreeCRM custom code. Everything else is (probably) stock .NET.**

| Don't (breaks tenant routing / auth) | Do (FreeCRM wrapper) |
|--------------------------------------|----------------------|
| `NavManager.NavigateTo("/x")` | `Helpers.NavigateTo("x")` |
| `Http.GetFromJsonAsync<T>(...)` | `Helpers.GetOrPost<T>(...)` |
| `IStringLocalizer["Save"]` | `<Language Tag="Save" />` / `Helpers.Text("Save")` |
| `AuthenticationStateProvider` | `Model.LoggedIn` / `Model.User` |
| `JsonSerializer.Serialize(...)` | `Helpers.SerializeObject(...)` |
| `new User()` (flat DTO) | `DataObjects.User` (nested partial) |

`BlazorDataModel` (injected as `Model`) is the central state container: holds the logged-in user, cached lists (Tags/Users/Departments), SignalR state, current view, and fires `OnChange` / `OnSignalRUpdate` events. (`006_architecture.freecrm_overview.md`)

---

## 2. File Naming & Organization — the `.App.` Convention (MANDATORY)

**Every file that adds or modifies functionality beyond stock FreeCRM MUST use the `.App.` naming convention.** This is called out as mandatory in `000`, `001`, `004`, and `005`.

```
{ProjectName}.App.{Feature}.{OptionalSubFeature}.{Extension}
```

| Category | Pattern | Example |
|----------|---------|---------|
| Base framework (stock) | `{ClassName}.cs` | `DataAccess.cs` |
| Base customization | `{ClassName}.App.cs` | `DataAccess.App.cs` |
| Project extension of base | `{ClassName}.App.{Project}.cs` | `DataAccess.App.FreeManager.cs` |
| Project-specific NEW | `{Project}.App.{Feature}.cs` | `FreeManager.App.EntityWizard.cs` |

Applies to **all** extensions: `.cs`, `.razor`, `.js`, `.css`, `.json`. Multi-level nesting is encouraged for big features (`FreeManager.App.EntityWizard.State.cs`, `…Handlers.cs`, `…Generation.cs`).

**Blazor gotcha:** the compiler turns filename dots into underscores for the class name.

| File | Class | Reference |
|------|-------|-----------|
| `FreeManager.App.EntityWizard.razor` | `FreeManager_App_EntityWizard` | `<FreeManager_App_EntityWizard />` |

**Why:** during a FreeCRM framework update you can instantly tell your code from stock (`find . -name "FreeManager.App.*"`). Drop short prefixes like `FM` when moving to the `.App.` pattern (keep `FM` only inside entity/table names for DB clarity). `ConfigurationHelper.App.cs` should live **only** in the DataObjects project, never also in Server (partial-class conflict).

**The Coordinator pattern:** when splitting a feature across many partials, add a coordinator file with a `#region` header that lists every related partial file.

**File-size limits** (docs *and* code; `001`, `002`): target ≤300 lines, soft max 500, hard max 600 ("must split or justify").

---

## 3. C# Conventions (`004_styleguide.md`)

### Braces
- **Types, methods, namespaces → opening brace on a NEW line.**
- **Control statements (`if`/`for`/`while`/`foreach`/`switch`/`try`) → opening brace on the SAME line.**
- `else`/`catch`/`finally` → same line as the closing brace (`} else {`).
- Always use braces, **except** single-line early guard clauses.

```csharp
public string GetDisplayName(User user)   // method: brace next line
{
    if (user == null) throw new ArgumentNullException(nameof(user));  // guard: no braces
    if (!user.IsAuthorized) return String.Empty;

    if (user.HasNickname) {               // control: brace same line
        return user.Nickname;
    }
    return user.FullName;
}
```

> **Watch for 0001:** the docs cover types/methods/control flow but are **silent on property-body brace placement**. The author's recent cleanup makes a specific rule there. See 0001.

### Namespaces & usings
- File-scoped namespaces: `namespace HelpDesk;` (no braces).
- Usings at top, ordered System → third-party → project; remove unused.

### Types
- **New code:** explicit types + target-typed `new()` — `List<string> names = new();`
- **Legacy `var`:** acceptable, don't refactor working code. `var` always fine in `foreach`.

### Naming (the full table)

| Element | Convention | Example |
|---------|------------|---------|
| Namespaces | file-scoped PascalCase | `namespace nForm.Server.Controllers;` |
| Classes / Methods / Properties | PascalCase | `DataController`, `GetUser()`, `TenantId` |
| Interfaces | `I` + PascalCase | `IDataAccess` |
| **Private fields** | `_camelCase` | `_connectionString`, `_fingerprint` |
| **DI-injected service fields** | `camelCase`, **no underscore** | `da`, `context`, `configurationHelper` |
| Protected fields | `_camelCase` | `_loading`, `_loadedData` |
| Local variables | `camelCase` | `output`, `tenantId` |
| **Constructor DI params** | `camelCase` | `daInjection`, `httpContextAccessor` |
| **Regular method params** | **PascalCase** (non-standard, deliberate) | `GetUser(Guid UserId, User? CurrentUser = null)` |
| Hub classes | `camelCase` (exception) | `crmHub`, `nFormhub` |
| `CurrentUser` field | PascalCase (mirrors `HttpContext.User`) | `private DataObjects.User CurrentUser;` |

The underscore-or-not split is the subtle one: **DI services get no underscore; everything else private/protected gets `_`.**

### Properties & methods
- Auto-properties for simple get/set; expression-bodied (`=>`) for computed one-liners.
- Methods: expression-bodied if it fits one line, block body if it would wrap.

### Strings
- Interpolation by default: `$"Hello, {name}!"`.
- Use `String.IsNullOrEmpty` / `String.IsNullOrWhiteSpace` (not `x == null || x == ""`).
- Examples consistently return `String.Empty` (not `""`).

> **Watch for 0001:** the docs *show* `String.Empty` in examples but never state a hard rule. The author's cleanup makes `String.Empty`-over-`""` systematic. See 0001.

### Null handling
- Prefer explicit `if (x == null)` checks; null-coalescing assignment for init (`_cache ??= new();`).
- Null-safe helper conversions: `BooleanValue`, `GuidValue`, `StringValue`, `IntValue`, `DecimalValue`.

### Other
- **LINQ:** fluent method syntax, one operation per line; avoid query syntax.
- **Async:** `async`/`await`, **no `Async` suffix** on method names.
- **Exceptions:** catch general `Exception`, type-check inside; use `RecurseException(ex)` to collect full details into `output.Messages`.
- **Switch expressions** for value mappings; traditional switch for side-effects.

---

## 4. DataObjects Conventions

All DTOs live in one `public partial class DataObjects`, split across files, classes alphabetical, enums first.

| Property type | Convention |
|---------------|------------|
| Required string | `= string.Empty;` |
| Optional string | `string?` |
| Required Guid | no initializer |
| Optional Guid | `Guid?` |
| Boolean | default value (`= true;`) |
| Collections | `= new();` |
| Complex objects | `= new();` |

**Standard soft-delete fields** on every deletable entity:
```csharp
public bool Deleted { get; set; }
public DateTime? DeletedAt { get; set; }
public string? LastModifiedBy { get; set; }
```

**Response objects** inherit `ActionResponseObject`; use `BooleanResponse` (`{ List<string> Messages; bool Result; }`) for all success/failure returns. List responses use `List<T> X { get; set; } = new();`.

---

## 5. DataAccess Conventions

- Split by domain via partials: `DataAccess.Categories.cs`, `DataAccess.Users.cs`, etc.; `DataAccess.App.cs` for app-specific.
- **Interface method declared in the same file as its implementation** (`partial interface IDataAccess` + `partial class DataAccess`).
- Standard method signatures:

| Operation | Signature |
|-----------|-----------|
| Delete | `Task<BooleanResponse> Delete{X}(Guid Id, User? CurrentUser = null, bool ForceDeleteImmediately = false)` |
| Get one | `Task<{X}> Get{X}(Guid Id, User? CurrentUser = null)` |
| Get list | `Task<List<{X}>> Get{Xs}(Guid TenantId, User? CurrentUser = null)` |
| Save | `Task<{X}> Save{X}({X} item, User? CurrentUser = null)` |

- CRUD pattern: start with `BooleanResponse output = new();` → `FirstOrDefaultAsync` → null guard → soft-delete vs immediate (per tenant `DeletePreference`) → `SaveChangesAsync` in try/catch → `SignalRUpdate(...)` → `ClearTenantCache(tenantId)`.
- Private fields `_camelCase`; the EF context field is `data` (no underscore — it's effectively the DI'd context); helper accessors `CurrentUserIdString(CurrentUser)`, `CurrentUserId(CurrentUser)`.

---

## 6. Controller (DataController) Conventions

- `[ApiController]`, `partial class DataController : ControllerBase`, split by domain.
- DI fields without underscore (`da`), `CurrentUser` PascalCase, `_returnCodeAccessDenied = "{{AccessDenied}}";`.
- Routes: `[Route("~/api/Data/Get{X}/{id}")]`. GET for reads + deletes, POST for saves.

| Scenario | Attribute |
|----------|-----------|
| Public | `[AllowAnonymous]` |
| Any logged-in user | `[Authorize]` |
| Admin only | `[Authorize(Policy = Policies.Admin)]` |
| Custom | check in method body, return `Unauthorized(_returnCodeAccessDenied)` |

## 7. EFModels

Scaffolded; only customize in `EFModelOverrides.cs` (`ConfigureConventions`). GUIDs convert to strings for MySQL/Postgres/SQLite; left native for SQL Server / InMemory.

---

## 8. Razor / Blazor Conventions (`004`, `008_components.razor_templates.md`)

### Directive order
1. `@page` route(s) → 2. `@using` → 3. `@inject` → 4. `@implements IDisposable`

### Routing (dual route for multi-tenant)
```razor
@page "/Settings/Categories"
@page "/{TenantCode}/Settings/Categories"
```
Single-tenant apps drop the `{TenantCode}` routes, the `TenantCode` param, `Model.TenantCodeFromUrl`, and `ValidateUrl` calls.

### Standard page skeleton
- Render guard: `@if (Model.Loaded && Model.View == _pageName) { @if (_loading) { … <LoadingMessage /> } else { … } }`
- Protected state fields: `_loading = true`, `_loadedData = false`, `_pageName`, `_title`, `_newItem`, `_entity = new();`
- Lifecycle: `OnInitialized` (subscribe + `Model.View = _pageName`), `OnAfterRenderAsync` (set tenant, permission check, `ValidateUrl`, load), `Dispose` (unsubscribe + remove from `Subscribers_*`).
- Method order: parameters → state fields → lifecycle → event handlers → navigation → data (`LoadData`/`Save`/`Delete`) → helpers.

### CRUD method shapes
- **Load:** edit vs new branch; set `Model.NavigationId`, `Model.ViewIsEditPage`; `Helpers.GetOrPost<T>("api/Data/Get…")`; end with `Helpers.DelayedFocus("edit-x-Field")`.
- **Save:** `Model.ClearMessages()` → build `List<string> errors` + `string focus` → `Helpers.MissingRequiredField` → `Model.Message_Saving()` → `GetOrPost` → handle `ActionResponse.Result` → `Model.ErrorMessages` / `Model.UnknownError()`.
- **Delete:** `Model.Message_Deleting()` → `GetOrPost<BooleanResponse>` → update local cache → navigate back.

### Shared components (custom)
`<Language>`, `<Icon>`, `<LoadingMessage>`, `<DeleteConfirmation>`, `<RequiredIndicator>`, `<LastModifiedMessage>`, `<UndeleteMessage>`, `<StickyMenuIcon>`.

### Markup/CSS conventions
- Bootstrap-based: `page-title`, `mb-2`, `btn-toolbar`, `btn-group`, `form-check form-switch`, `form-control`, `form-select`, `table table-sm`, `table-dark`, `item-deleted`, `disabled`, `center`, `nowrap`, `note`.
- **Buttons grouped in `btn-group`** when more than one (a recurring UI-consistency rule).
- Form-element id convention: `{context}-{entity}-{field}` → `edit-category-Description`.
- Validation styling via `Helpers.MissingValue(value, "form-control")`.

> **Watch for 0001:** `.razor` files have their own brace rule in `.editorconfig` (`csharp_new_line_before_open_brace = none`). The list/template docs also show `table-dark` on headers, while a recent author commit *removes* `table-dark` from some headers for consistency. See 0001.

### Component-authoring patterns (`008_components.*`)
- **Colocated JS:** `Component.razor` + `Component.razor.js`, imported via `./Shared/Component.razor.js` (often with `?v=<guid>` cache-bust).
- **JS→C# callbacks:** `DotNetObjectReference.Create(this)` + `[JSInvokable]`.
- **Two-way binding:** `[Parameter] Value` + `[Parameter] EventCallback<T> ValueChanged` + `@bind-Value`.
- Always `@implements IDisposable` and dispose the `DotNetObjectReference`.
- *Caveat from `008_components.wizard.md`:* FreeCICD/wizard examples are community-contributed — "functional but not authoritative style."

---

## 9. Comment Style & Voice (`005_style.comments.md`)

**Voice:** procedural, direct, present-tense, instructional, impersonal (no "I/we/you"). "A calm, experienced developer thinking out loud."

**The 10 patterns:**

| Pattern | Lead-in |
|---------|---------|
| Sequencing | `// First,` `// Now,` `// Next,` `// Then,` `// Finally,` |
| Conditional check | `// See if …` |
| Validation | `// Make sure …` |
| Branch logic | `// If the …, then …` |
| Context | `// This is a …` |
| File header | `// Use this file as a place to …` |
| Constraint | `// Only …` |
| State transition | `// At this point …` |
| Result state | `// Valid …` / `// Still …` |
| Action | `// Remove …` / `// Delete …` |

**Formatting:** sentence case (acronyms stay caps), one space after `//`, blank line *before* a comment block but none between comment and its code, period optional on single-line / used on multi-line. **No TODOs, no humor, don't state the obvious, don't explain language features.** XML docs on interfaces/public APIs/plugins: lowercase-start param/returns, no trailing period on single-line summaries.

**Commented-out code:** acceptable only for template examples and module markers (`// {{ModuleItemStart:Appointments}}`); never dead/"just in case" code.

> **Watch for 0001:** a recent author commit *removes* old commented-out code and "extra linebreaks." Our "blank line before a comment block" guidance should be reconciled with his whitespace cleanup. See 0001.

---

## 10. Helpers — Use Them, Don't Reinvent (`007_patterns.helpers.md`)

`Helpers` is a static partial class initialized once in `MainLayout` (`Helpers.Init(...)`). **If a helper exists, use it.** Most-used: `Text()`, `GetOrPost<T>()`, `DelayedFocus()`, `NavigateTo()`, `MissingRequiredField()`, `MissingValue()`, `StringValue()`, `NavigateToRoot()`, `ValidateUrl()`, `BuildUrl()`. Extend via `Helpers.App.cs` (never edit `Helpers.cs`). `<Language>`/`<Icon>` components in markup; `Helpers.Text()`/`Helpers.Icon()` in C#. `BuildUrl()` returns a string for `href`; `NavigateTo()` performs navigation.

> **Watch for 0001:** an author commit removes redundant `Helpers.` qualifiers on calls made from *inside* the Helpers class itself. See 0001.

---

## 11. SignalR (`007_patterns.signalr.md`)
Tenant-scoped hub (`crmHub : Hub<IsrHub>`), `SignalRUpdate` DTO + `SignalRUpdateType` enum (app-specific types numbered high, e.g. `100+`). Pages: subscribe in `OnInitialized`, handle only updates from *other* users on the *current* view, unsubscribe in `Dispose`.

## 12. Doc Conventions (`002_docsguide.md`, `003_templates.md`)
`{NUM}_{category}_{topic}.md`, 3-digit number, next available. Header block (Document ID / Category / Purpose / Audience / Outcome — or Predicted/Actual/Resolution for meetings), `---` rules, footer (`*Created:* / *Maintained by:*`). Outcome emoji set: ✅ ⚠️ ❌ 📋 🔄 📖. Archive (don't delete) into `archive/`, keep numbers.

---

## 13. One-Screen Quick Reference

```
BRACES        types/methods → new line · if/for/while → same line · } else {
TYPES         new code: explicit + new() · var ok in foreach/legacy
FIELDS        DI services: da (no _) · other private/protected: _camelCase · CurrentUser PascalCase
PARAMS        ctor DI: camelCase · method params: PascalCase (UserId, CurrentUser)
STRINGS       interpolation · String.IsNullOrWhiteSpace · String.Empty in examples
LINQ          fluent, one op per line       ASYNC  await, no Async suffix
FILES         {Project}.App.{Feature}.{ext} · ≤300 ln ideal / 600 max · dots→underscores in Blazor
WRAPPERS      Helpers./Model./DataObjects./DataAccess. = custom; rest = stock .NET
DTO           soft-delete trio · BooleanResponse · required string = string.Empty
DA            partial by domain · interface beside impl · Get/Save/Delete signatures fixed
RAZOR         _loading/_loadedData/_pageName · OnInit/OnAfterRender/Dispose · GetOrPost CRUD
COMMENTS      "See if" / "Make sure" / "First, Now, Next" · sentence case · no TODOs
```

---

## 14. Open Questions This Baseline Leaves (resolved in 0001)

These are the spots where our guidance is **silent or only-by-example**, which the author's explicit cleanup now answers:

1. **Property-body brace placement** — types/methods/control-flow are covered; properties are not.
2. **`""` vs `String.Empty`** — shown in examples, never mandated.
3. **Blank-line / whitespace policy** — "blank line before comment blocks" vs. a repo-wide "remove extra linebreaks" pass.
4. **Redundant `Helpers.` qualifier** inside the Helpers class.
5. **`table-dark` on table headers** — templates show it; a consistency commit removes it in places.
6. **Multi-line method-parameter formatting** — not specified here.
7. **Member/param ordering & sorting** (alphabetical fields? param order?) — only loosely implied.
8. **`.editorconfig` as the formal source of truth** — referenced but not reproduced.

➡️ **See [0001_freecrm_styling_latest_research.md](0001_freecrm_styling_latest_research.md) for the author's explicit answers, pulled from his actual commits.**

---

*Created: 2026-06-04*
*Maintained by: [Quality]*
*Sources: `Docs/Guides/000`–`300`, read in full.*

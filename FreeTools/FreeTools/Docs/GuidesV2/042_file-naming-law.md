# 042 — The Naming Law That Keeps Your Code Yours

> **Document ID:** 042  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Detail the strict file-naming convention that separates app-specific files from framework files.
> **Audience:** Advanced builders and extenders  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 04x (Extending Without Breaking: The Live Runtime) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Naming Is a Law](#why-it-matters) | The `.App.` convention in plain language, and why getting it wrong costs you your code |
| 2 | [App Files vs Framework Files](#two-classes) | The two file classes, who owns each, and the partial-class trick that joins them |
| 3 | [The Naming Rules](#naming-rules) | The exact `.App.` segment rule, where it sits in the filename, and where the files live |
| 4 | [Worked Examples](#examples) | Real framework/app pairs from the tree, sorted into each class |
| 5 | [How Separation Protects Upgrades](#upgrade-safety) | Why the boundary lets the framework be replaced wholesale on upgrade |
| 6 | [Pitfalls and Anti-Patterns](#pitfalls) | The naming mistakes that get your work overwritten |
| 7 | [Quick Reference Checklist](#checklist) | At-a-glance rules before you commit |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Naming Is a Law

Most codebases let you put your code anywhere. FreeCRM does not — and that single restriction is the most valuable promise the framework makes you.

Here is the situation it solves. FreeCRM is a **template**: you start from the original author's code (Brad Wickett's `wicketbr/FreeCRM`) and build your own product on top of it. But the author keeps improving the base — fixing bugs, upgrading from .NET 9 to .NET 10, adding features. You *want* those improvements. The problem: if your customizations are tangled into the same files the author ships, every upgrade becomes a merge fight, and a careless "take theirs" wipes out your work.

The fix is a naming **law** (not a guideline — the upgrade tooling literally depends on it). Every file in the codebase belongs to exactly one of two camps, and you can tell which camp a file is in *just by reading its name*:

- A **framework file** — e.g. `DataAccess.cs`. The author owns it. On upgrade it can be **replaced wholesale**. You never edit it.
- An **app file** — e.g. `DataAccess.App.cs`. *You* own it. The author ships an empty-ish starter version and never touches it again. This is where 100% of your customizations go.

That extra `.App.` segment dropped into the middle of the filename is the whole law. **"App" here means "your application's specific code"** — the stuff that makes *your* product different from the vanilla template, as opposed to the framework plumbing everyone shares.

Why it matters in one sentence: the `.App.` segment is the line between "code the framework can overwrite" and "code that is yours forever," and the framework's own upgrade utility (the `Upgrade FreeCRM.exe` tool, added in commit `863e16a`) only works if your code is already on the right side of that line.

Get this right and upgrades become a non-event. Get it wrong — put your logic in a framework file — and the next upgrade silently deletes it.

<a id="two-classes"></a>
## 2. App Files vs Framework Files

The two classes differ in **ownership**, **edit rights**, and **upgrade behavior**:

| | Framework file | App file |
|---|---|---|
| Example name | `DataAccess.cs`, `Program.cs`, `MainLayout.razor` | `DataAccess.App.cs`, `Program.App.cs`, `MainLayout.App.razor` |
| Who owns it | The upstream author | **You** |
| Do you edit it? | **Never** | Always — this is your workspace |
| What happens on upgrade | Replaced wholesale with the new version | Left untouched |
| What it contains | Built-in implementation, plumbing, the calls *into* your hooks | Your overrides, your settings, your business logic |

**How can two files cooperate without one editing the other?** Through a C# feature called a **partial class**. "Partial" means a single class is *split across multiple files* — the compiler stitches them back into one type at build time. So `DataAccess.cs` and `DataAccess.App.cs` are not two classes; they are one `DataAccess` class written in two files:

```csharp
// DataAccess.App.cs
namespace CRM;

// Use this file as a place to put any application-specific data access methods.

public partial class DataAccess
{
    // The name of the application.
    private string _appName = "freeCRM";

    // The version of your application.
    private string _version = "2.0.0";
    // ...
}
```

The framework half (`DataAccess.cs`) holds the constructor, the EF Core database wiring, and the real machinery. Your half (`DataAccess.App.cs`) holds the knobs (`_appName`, `_version`, `_tokenDays`) and a set of pre-named **hook methods** the framework promises to call — methods like `GetApplicationSettingsApp(...)`, `SaveDataApp(...)`, and `ProcessBackgroundTasksApp(...)`. The framework calls them; you fill them in.

The boundary is even visible in C# **interfaces**. The contract `IDataAccess` is *also* partial, and the app file adds your own method signatures to it:

```csharp
// DataAccess.App.cs
public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> ProcessBackgroundTasksApp(Guid TenantId, long Iteration);
    DataObjects.BooleanResponse YourMethod();
}
```

So the mental model is: **framework file = the engine, app file = your seat at the controls.** Same class, two files, one clean line of ownership between them.

<a id="naming-rules"></a>
## 3. The Naming Rules

The law is small enough to state exactly. A file is an **app file** if and only if its name carries a literal `.App.` segment immediately before the final extension.

**Rule 1 — The `.App.` segment.** Insert `.App` as a second-to-last dotted segment, keeping the original base name and extension:

| Framework file | App file |
|---|---|
| `DataAccess.cs` | `DataAccess.App.cs` |
| `Program.cs` | `Program.App.cs` |
| `Helpers.cs` | `Helpers.App.cs` |
| `MainLayout.razor` | `MainLayout.App.razor` |
| `site.css` | `site.App.css` |

The pattern is `BaseName` + `.App` + `.extension`. It is **not** `App.DataAccess.cs` (prefix), **not** `DataAccessApp.cs` (no dot), and **not** a separate `AppDataAccess` class. The dot-delimited `.App.` in the middle is the entire signal.

**Rule 2 — Casing is exact.** It is `.App.` — capital `A`, lowercase `pp`. This is not cosmetic: the upgrade tooling matches on this literal string. `.app.` or `.APP.` will not be recognized as an app file, and will be treated as framework code (i.e. overwritten).

**Rule 3 — The base name and folder must mirror the framework file.** An app file sits **next to** its framework partner in the same project and folder, and shares its base name so the partial class lines up:

- `CRM.DataAccess/DataAccess.cs` ↔ `CRM.DataAccess/DataAccess.App.cs`
- `CRM/Program.cs` ↔ `CRM/Program.App.cs`
- `CRM.Client/Layout/MainLayout.razor` ↔ `CRM.Client/Layout/MainLayout.App.razor`

**Rule 4 — Self-contained app components live under `Shared/AppComponents/`.** Some app files have no framework twin because they *are* entirely yours — full Blazor components you own end to end. These are gathered in `CRM.Client/Shared/AppComponents/` and still carry the `.App.razor` suffix (e.g. `Index.App.razor`, `Settings.App.razor`, `Users.App.razor`). The suffix marks them as yours-to-keep even though there is no `Index.razor` beside them.

**Rule 5 — The extension is preserved.** `.App.` works for any file type: `.cs` (C#), `.razor` (Blazor components), and `.css` (styles all live as `site.App.css`). The framework's `.editorconfig` style rules apply to app files exactly as they do to framework files — the `.App.` segment changes ownership, not formatting.

<a id="examples"></a>
## 4. Worked Examples

These are real files from the current tree, sorted into the two classes. (Verified against `c:\Users\pepkad\source\repo2\FreeCRM`.)

**Paired files — framework file on the left, the app file you edit on the right:**

| Framework file (do not touch) | App file (yours) | Lives in |
|---|---|---|
| `DataModel.cs` | `DataModel.App.cs` | `CRM.Client` |
| `Helpers.cs` | `Helpers.App.cs` | `CRM.Client` |
| `MainLayout.razor` | `MainLayout.App.razor` | `CRM.Client/Layout` |
| `DataAccess.cs` | `DataAccess.App.cs` | `CRM.DataAccess` |
| `GraphAPI.cs` | `GraphAPI.App.cs` | `CRM.DataAccess` |
| `DataController.cs` | `DataController.App.cs` | `CRM/Controllers` |
| `Program.cs` | `Program.App.cs` | `CRM` |
| `site.css` | `site.App.css` | `CRM.Client/wwwroot/css` |

**Self-contained app components** — yours end to end, no framework twin, all under `CRM.Client/Shared/AppComponents/`:

`About.App.razor`, `AppSettings.App.razor`, `EditUser.App.razor`, `Index.App.razor`, `NavigationMenu.App.razor`, `Settings.App.razor`, `Users.App.razor`, and siblings.

**What an app file actually looks like in practice.** `Program.App.cs` is pure hooks — the framework's `Program.cs` calls each one at the right moment, and you fill in the body:

```csharp
// Program.App.cs
public partial class Program
{
    public static WebApplicationBuilder AppModifyBuilderStart(WebApplicationBuilder builder)
    {
        var output = builder;
        // Add any app-specific modifications to the builder here.
        return output;
    }
}
```

And on the framework side, `Program.cs` is where those hooks get invoked — you can see it reach into your file by name:

```csharp
// Program.cs (framework — you never edit this)
var builder = AppModifyBuilderStart(WebApplication.CreateBuilder(args));
// ...
policies.AddRange(AuthenticationPoliciesApp);
// ...
var app = AppModifyStart(AppModifyBuilderEnd(builder).Build());
AppModifyEnd(app).Run();
```

The framework file *calls into* the app file. That is the whole collaboration model: framework drives, app customizes — and because they are partials of one class, no `using`, no registration, and no wiring is needed. You just fill in the method body.

Even the CSS follows the law. `site.App.css` ships as nothing but an invitation:

```css
/*
    Add any app-specific CSS here.
*/
```

<a id="upgrade-safety"></a>
## 5. How Separation Protects Upgrades

This separation exists to make one specific operation safe: **replacing every framework file wholesale** when a new version of FreeCRM arrives, without losing a line of your work.

Here is why it holds. An upgrade is, mechanically, "overwrite all the framework files with the author's new versions." Because your code is *never* in a framework file — it is always in a `.App.` file the author doesn't ship over — there is nothing of yours to overwrite. The author's `Upgrade FreeCRM.exe` utility (commit `863e16a`) relies on exactly this: it requires your app code to already be in `.App.` form so it can swap framework files freely.

The hook pattern is what makes this lossless rather than merely non-destructive. The framework doesn't just *avoid* your files — it *reaches into them by name*. When `DataAccess.cs` is replaced with a newer version, that new version still calls `GetApplicationSettingsApp(...)`, `SaveDataApp(...)`, and `ProcessBackgroundTasksApp(...)` — the same hook names, living in *your* untouched `DataAccess.App.cs`. So your customizations keep running against the upgraded engine automatically. The contract is the method name; the implementation on each side can evolve independently.

The durability of this is observable in the repo's own history. The `.App.` files have survived major structural churn — the .NET 9 → 10 upgrade, a folder restructure, and even `ConfigurationHelper.App.cs` moving from the `CRM` project into `CRM.DataObjects` — riding through each as `R100` renames (Git's term for a 100%-identical-content rename: the file moved or was tracked across a framework change, but **not one byte of your code changed**).

In short: the naming law turns "merge the framework's changes into mine" — the slow, error-prone operation — into "drop in the new framework files; mine are already separate." That is why two things both work at once: *"find all of my code"* (search for `.App.`) and *"upgrade the framework underneath me"* (replace everything that isn't `.App.`).

<a id="pitfalls"></a>
## 6. Pitfalls and Anti-Patterns

Every pitfall here ends the same way: your code lands in a framework file and the next upgrade deletes it. Watch for these.

- **Editing a framework file directly.** The single most damaging mistake. If you add a method or change a line in `DataAccess.cs` instead of `DataAccess.App.cs`, it works *today* and is gone after the next upgrade. Rule of thumb: if the filename has no `.App.` segment, treat it as read-only.

- **Misspelling or mis-casing the segment.** `.app.`, `.APP.`, `DataAccessApp.cs`, or `App.DataAccess.cs` are all **not** recognized as app files. The literal middle segment must be `.App.` (capital A, lowercase pp). A near-miss is worse than an obvious error because the file looks customized but is treated as framework code — and overwritten.

- **Putting the segment in the wrong place.** It must be the second-to-last dotted segment, e.g. `Helpers.App.cs`. Prefix forms (`App.Helpers.cs`) and suffix-without-dot forms (`HelpersApp.cs`) break the partial-class pairing — the base names no longer match, so the two halves don't compile into one class.

- **Creating a new class instead of a partial.** Writing a separate `class AppDataAccess` defeats the purpose: the framework's hook calls target `partial class DataAccess`, not your standalone class, so your code never gets invoked. Always extend the existing type with `public partial class DataAccess`, matching the name exactly.

- **Renaming or "tidying" framework files.** Renaming `DataAccess.cs` (or moving it out of its folder) breaks the partial pairing *and* desyncs you from the author's tree, so future upgrades can no longer line up. Leave framework names and locations alone.

- **Putting custom Blazor components outside `Shared/AppComponents/` without the `.App.` suffix.** A custom component named `MyThing.razor` dropped into a framework folder looks like framework code. Self-contained app components belong in `CRM.Client/Shared/AppComponents/` and keep the `.App.razor` suffix.

- **Mixing your logic into a framework hook *body* in the framework file.** The hooks exist so you *don't* have to edit the framework. If you find yourself opening `Program.cs` to change what `AppModifyBuilderStart` does, stop — edit the body in `Program.App.cs` instead.

<a id="checklist"></a>
## 7. Quick Reference Checklist

Run this before you commit:

- [ ] **Is my new code in a `.App.` file?** If the filename has no `.App.` segment, move the code.
- [ ] **Spelling/casing exact?** Literal `.App.` — capital `A`, lowercase `pp`, dots on both sides.
- [ ] **Segment in the right spot?** `BaseName` + `.App` + `.ext` (e.g. `DataAccess.App.cs`), never a prefix and never without the dot.
- [ ] **Base name matches its framework twin?** `DataAccess.App.cs` pairs with `DataAccess.cs`; same folder, same base name.
- [ ] **Using `partial`, not a new class?** Your additions read `public partial class X { ... }` / `public partial interface IX { ... }`, matching the framework type name.
- [ ] **Did I leave every framework file untouched?** No edits, no renames, no moves to files lacking `.App.`.
- [ ] **Self-contained component?** It lives in `CRM.Client/Shared/AppComponents/` and ends in `.App.razor`.
- [ ] **Filling a hook?** I implemented the body of the pre-named `…App` method (e.g. `SaveDataApp`) rather than editing where the framework calls it.

If all eight are checked, the next framework upgrade cannot touch your work.

---

<a id="related-docs"></a>
## 8. Related Docs

- [041 — Code the Framework Can Update Underneath](041_upgrade-safe-model.md) — the upgrade-safe model it serves
- [004 — Reading the Repository Map](004_reading-the-tree.md) — how the tree reflects the split
- [022 — Shaping Records With Nested Partial DTOs](022_nested-partial-dtos.md) — App naming applied to DTOs

---
*GuidesV2 042 · drafted from source (`c:\Users\pepkad\source\repo2\FreeCRM`) on 2026-06-05.*

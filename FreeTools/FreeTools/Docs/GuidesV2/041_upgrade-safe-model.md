# 041 — Code the Framework Can Update Underneath

> **Document ID:** 041  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Explain the partial-class segregation contract that lets the framework update beneath your app code.
> **Audience:** Advanced builders and extenders  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 04x (Extending Without Breaking: The Live Runtime) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-it-matters) | The upgrade problem, and what "partial class" and "segregation" mean |
| 2 | [The Mental Model](#mental-model) | The framework half and your half, living in one class |
| 3 | [The Segregation Contract](#the-contract) | The rules that keep an upgrade from erasing your work |
| 4 | [Anatomy of a Split Class](#split-anatomy) | The real `.App.` files and how they wire together |
| 5 | [Extension Hooks](#extension-hooks) | The `...App` seam methods the framework calls into |
| 6 | [Pitfalls and Anti-Patterns](#pitfalls) | The edits that quietly break a future upgrade |
| 7 | [Verifying an Upgrade](#verifying) | How to confirm your code survived regeneration |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why This Matters

**The problem in one sentence:** you want to build on top of FreeCRM, but you also want to keep getting the framework's bug fixes and new features — and normally those two goals fight each other.

Here is why they fight. FreeCRM is an open-source starter platform: a CRM built in C# with Blazor WebAssembly on .NET 10. (Blazor WebAssembly just means the user interface is written in C# and runs inside the browser instead of JavaScript.) When the FreeCRM team ships a new version, they hand you a fresh copy of hundreds of files. If your custom code lived *inside* those files, taking the new version would overwrite everything you wrote. So most teams freeze on an old version, fall behind, and slowly drift into a fork they can never update again. That is the trap this whole document is about avoiding.

FreeCRM's answer is a discipline called **segregation**: the framework's code and your code are kept in physically separate files, even when they describe the *same* class. The framework's files are considered "owned" by the framework — an upgrade is allowed to overwrite them. Your files are "owned" by you — an upgrade must never touch them. Because the two never share a file, an upgrade can replace the framework's half wholesale and your half rides along untouched.

The mechanism that makes "one class, two files" legal is a C# language feature called a **partial class**. "Partial" here means *split across files*: you mark a class with the `partial` keyword in two (or more) files, and the compiler stitches them back into one class at build time. The framework writes one part; you write the other. Same class, same name, same namespace — two files with two different owners.

You can see the pattern at a glance from the file names. In the FreeCRM source, the framework's data-access code lives in many files like `DataAccess.Users.cs` and `DataAccess.Appointments.cs`, while *your* code lives in exactly one file with `.App.` in its name:

```
CRM.DataAccess\DataAccess.cs            <- framework-owned
CRM.DataAccess\DataAccess.Users.cs      <- framework-owned
CRM.DataAccess\DataAccess.App.cs        <- YOURS
```

That `.App.` marker is the load-bearing convention of this entire model. The deeper rules behind the name are covered in [042 — The Naming Law That Keeps Your Code Yours](042_file-naming-law.md); this doc explains *why* the split exists and *how* to live inside it safely.

**Why it matters to you as a builder:** if you respect the contract, upgrading is a tool you run, not a project you dread. FreeCRM even ships a console app, `Upgrade FreeCRM.exe`, whose entire job is to migrate an existing app onto a newer framework — and its README is blunt about the precondition:

> "If you are running an older version of an application based on the FreeCRM framework and you have already migrated to use .app. files for all of your app-specific code, then you can use the new 'Upgrade FreeCRM.exe' console application to upgrade your existing application."

Read that twice. The upgrade tool only works *if* you put your app-specific code in `.App.` files. The segregation contract is not a style preference — it is the entry ticket to ever upgrading at all.

---

<a id="mental-model"></a>
## 2. The Mental Model

Picture each major framework class as a single object made from two stacked transparency sheets. The bottom sheet is the framework's; the top sheet is yours. The compiler presses them together into one class, but on disk they are two files with two owners.

```
        ┌─────────────────────────────────────┐
        │  YOUR sheet:  DataAccess.App.cs      │   <- you edit this
        ├─────────────────────────────────────┤
        │  FRAMEWORK sheet: DataAccess.cs,     │   <- upgrade overwrites these
        │  DataAccess.Users.cs, ...Appointments│
        └─────────────────────────────────────┘
              both compile into ONE  class DataAccess
```

Concretely, the framework declares the class like this (from `DataAccess.cs`):

```csharp
namespace CRM;

public partial class DataAccess: IDisposable, IDataAccess
{
    // ... hundreds of framework methods across many files ...
}
```

And your sheet declares the *same* class, in `DataAccess.App.cs`, with the same `partial` keyword and the same namespace:

```csharp
namespace CRM;

// Use this file as a place to put any application-specific data access methods.

public partial class DataAccess
{
    // ... your fields, your methods, your hooks ...
}
```

Two files. One `class DataAccess`. The keyword that makes this safe is `partial` on both halves — without it, the compiler would reject "two classes with the same name."

The key intuition: **an upgrade is a swap of the bottom sheet only.** When you run `Upgrade FreeCRM.exe`, the framework's files (`DataAccess.cs`, `DataAccess.Users.cs`, and so on) are replaced with their newer versions. Your `DataAccess.App.cs` is carried forward as-is. Because the two halves only ever met at the class boundary — never inside the same file — the swap is clean.

This is not limited to data access. The same two-sheet idea repeats across every layer of the app, each with its own `.App.` sheet:

| Class / area | Framework sheet(s) | Your sheet |
|---|---|---|
| Server-side data access | `DataAccess.cs`, `DataAccess.*.cs` | `DataAccess.App.cs` |
| Shared data objects (DTOs) | `DataObjects.cs`, `DataObjects.*.cs` | `DataObjects.App.cs` |
| Browser data model | (generated) `BlazorDataModel` | `DataModel.App.cs` |
| App startup / hosting | `Program.cs` | `Program.App.cs` |
| Configuration | `ConfigurationHelper.cs` | `ConfigurationHelper.App.cs` |
| Global settings | `GlobalSettings.cs` | `GlobalSettings.App.cs` |
| Per-page UI | e.g. `Pages\About.razor` | `Shared\AppComponents\About.App.razor` |

Everywhere you see a `.App.` file, you are looking at *your* sheet of a two-sheet class.

---

<a id="the-contract"></a>
## 3. The Segregation Contract

Think of this as a treaty between you and the framework. Each side promises something, and as long as both keep their promises, upgrades stay non-destructive.

**The framework promises:**

1. **It will only ever overwrite non-`.App.` files.** Every file *without* `.App.` in its name is framework territory and may be replaced on upgrade.
2. **It will never overwrite an `.App.` file.** Files with `.App.` in the name are yours; the upgrade carries them forward unchanged.
3. **It will keep calling your hooks.** The framework half contains the calls into your `...App` methods (covered in §5). Those call sites are part of the framework's published surface, so they remain stable across versions.

**You promise:**

1. **Put all app-specific code in `.App.` files.** New fields, new methods, new properties, custom DTOs — they go in your sheet, never in a framework file. The README states this as the literal precondition for the upgrade tool to run.
2. **Do not edit framework files.** The moment you change a non-`.App.` file, you have written code on the bottom sheet — and the next upgrade swaps that sheet out, taking your change with it.
3. **Extend the class, do not redefine it.** Your sheet adds to the same `partial class`; it must keep the same namespace (`CRM` for the server, `CRM.Client` for the browser) and the same `partial` keyword so the two halves fuse.
4. **Fill in the hooks rather than rewiring the framework.** Where the framework offers you a seam (a `...App` method, an `AppModify...` method, an `<About_App />` component), use it instead of reaching into framework code.

**Why the contract holds:** because the two halves never share a physical file, "replace the framework's files" and "preserve the user's files" are two non-overlapping operations. The partial-class feature is what lets them still compile into one coherent class afterward. Break promise #1 or #2 — put your logic in a framework file — and you have glued the sheets together; the next upgrade tears your work in half.

The framework also leaves the data layer honest about migrations. In `DataAccess.App.cs` you decide whether the framework manages your schema:

```csharp
// Indicates if the app uses data migrations. If false, you will manage your own database schema updates.
private bool _useMigrations = false;
```

That flag lives in *your* sheet precisely because it is a per-app decision the framework must not overwrite.

---

<a id="split-anatomy"></a>
## 4. Anatomy of a Split Class

Let's open a real split and see how the wiring works end to end. We'll use `DataAccess`, the server-side class that talks to the database, because it shows every moving part.

**The framework half** declares the class and the interface it implements (`DataAccess.cs`):

```csharp
namespace CRM;

public partial class DataAccess: IDisposable, IDataAccess
{
    private string _connectionString;
    private EFDataModel data;
    // ...

    public DataAccess(/* ... */)
    {
        // ...
        DataAccessAppInit();   // <- the framework calls into YOUR sheet
        // ...
    }
}
```

Notice the constructor calls `DataAccessAppInit()`. That method does not exist in any framework file — it lives in *your* sheet. The framework is reaching up to your transparency on purpose.

**Your half** (`DataAccess.App.cs`) supplies that method, plus your own fields and any custom methods:

```csharp
namespace CRM;

// Use this file as a place to put any application-specific data access methods.

public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> ProcessBackgroundTasksApp(Guid TenantId, long Iteration);
    DataObjects.BooleanResponse YourMethod();
}

public partial class DataAccess
{
    private string _appName = "freeCRM";
    private string _version = "2.0.0";

    private void DataAccessAppInit()
    {
        // Add any app-specific initialization logic here.
    }

    public DataObjects.BooleanResponse YourMethod()
    {
        return new DataObjects.BooleanResponse {
            Result = true,
            Messages = new List<string> { "Your Messages" }
        };
    }
}
```

Two things worth calling out:

- **The interface is partial too.** `IDataAccess` is split exactly like the class. The framework declares the bulk of it; your sheet adds `partial interface IDataAccess { ... }` to publish your own methods (like `YourMethod()`) on the same contract. So your custom methods are first-class members of the type, callable through the interface, yet they live entirely on your sheet.
- **App identity lives on your sheet.** `_appName`, `_version`, the release date, token lifetimes, lockout policy — all the per-application constants sit in `DataAccess.App.cs`, so an upgrade never resets your app's name or version back to the framework defaults.

**The UI follows the same anatomy, in Razor.** Razor is Blazor's HTML-plus-C# template format. The framework owns the page file `Pages\About.razor`, and inside it the framework renders *your* component by tag name:

```razor
@* in framework-owned Pages\About.razor *@
<About_App />
```

`<About_App />` is the framework calling into `Shared\AppComponents\About.App.razor` — your sheet. (The `.App.razor` file name becomes the `_App` suffix in the component tag.) Your component is plain, editable Razor:

```razor
@* your Shared\AppComponents\About.App.razor *@
@implements IDisposable
@inject BlazorDataModel Model

@{
    <h1 class="page-title">
        <Icon Name="About" />
        About freeCRM
    </h1>
    <div class="mb-2">
        A free, open-source CRM (Customer Relationship Management) system.
    </div>
}
```

Same treaty, different layer: the framework's page is replaceable, your `.App.razor` component is yours, and the page reaches your component through a stable tag.

---

<a id="extension-hooks"></a>
## 5. Extension Hooks

A **hook** (also called a *seam*) is a spot where the framework deliberately stops and asks, "does the app want to do anything here?" It does that by *calling a method that lives on your sheet*. You fill the method in; the framework calls it at the right moment. You never have to edit framework code to be invited into the flow.

These hooks all follow one naming habit: they end in `App`. When you see a method whose name ends in `App`, it is a doorway the framework opens into your code.

**Data-layer hooks (in `DataAccess.App.cs`).** The framework's own methods call these at well-defined points. A few real ones, with the moment they fire:

- `GetDataApp(object Rec, object DataObject, ...)` — fires every time the framework maps a database row into a data object, so you can copy across columns you added. It is called from many framework files, e.g. `DataAccess.Appointments.cs` and `DataAccess.Departments.cs`.
- `SaveDataApp(object Rec, object DataObject, ...)` — the mirror image: fires on save, so you can copy your custom fields back down to the database row.
- `DeleteRecordsApp(object Rec, ...)` — fires before a record is deleted, so you can clean up related rows you own.
- `ProcessBackgroundTasksApp(Guid TenantId, long Iteration)` — fires on each tick of the background service, so your periodic jobs run without touching the framework's scheduler. The README points you straight here: *"you can modify the ProcessBackgroundTasksApp method in the DataAccess.App.cs file to run methods in there."* (See [045 — The Background Service](045_background-service.md).)
- `GetApplicationSettingsApp`, `SaveApplicationSettingsApp`, `GetDeletedRecordCountsApp`, `GetFilterColumnsApp`, `SortUsersApp` — the same pattern for settings, deleted-record reporting, list filtering, and sorting.

Here is `GetDataApp` as the framework ships it — a hook you flesh out, not one you have to invent:

```csharp
private void GetDataApp(object Rec, object DataObject, DataObjects.User? CurrentUser = null)
{
    try {
        if (Rec is EFModels.EFModels.User && DataObject is DataObjects.User) {
            var rec  = Rec as EFModels.EFModels.User;
            var user = DataObject as DataObjects.User;

            if (rec != null && user != null) {
                //user.Property = rec.Property;   // <- you uncomment and fill this in
            }
            return;
        }
    } catch { }
}
```

And the framework half calls it for you, e.g. inside `DataAccess.Appointments.cs`:

```csharp
GetDataApp(rec, output, CurrentUser);
```

**Startup hooks (in `Program.App.cs`).** Hosting and dependency wiring have their own seams, called from the framework's `Program.cs`:

```csharp
public partial class Program
{
    public static WebApplicationBuilder AppModifyBuilderStart(WebApplicationBuilder builder) { /* yours */ return builder; }
    public static WebApplicationBuilder AppModifyBuilderEnd(WebApplicationBuilder builder)   { /* yours */ return builder; }
    public static WebApplication        AppModifyStart(WebApplication app)                   { /* yours */ return app; }
    public static WebApplication        AppModifyEnd(WebApplication app)                     { /* yours */ return app; }
}
```

The framework's `Program.cs` threads them through the real startup sequence:

```csharp
var builder = AppModifyBuilderStart(WebApplication.CreateBuilder(args));
// ...
var app = AppModifyStart(AppModifyBuilderEnd(builder).Build());
// ...
AppModifyEnd(app).Run();
```

So you can add services, middleware, or configuration at four precise points in startup — all from your sheet.

**Data-shape hooks (in `DataObjects.App.cs`).** The shared data objects (the DTOs sent between server and browser) are partial classes too, so you can graft new fields onto existing types without editing the framework's definition:

```csharp
public partial class DataObjects
{
    public partial class User
    {
        //public string? MyCustomUserProperty { get; set; }
    }
}
```

**Browser hooks (in `DataModel.App.cs`).** The in-browser `BlazorDataModel` exposes the same kind of seam — for example `HaveDeletedRecordsApp` and `PrecompileBlazorPlugins` — so your UI state extensions ride along on your sheet, in the `CRM.Client` namespace.

A note on terminology: C# also has a formal "partial method" language feature (a method whose declaration and body can live in different parts of a partial class). FreeCRM's hooks are mostly *ordinary* methods placed on your partial sheet and called from the framework's partial sheet — same effect, simpler to read. The point is not the exact C# feature; it is that the call site is stable and the body is yours.

---

<a id="pitfalls"></a>
## 6. Pitfalls and Anti-Patterns

Each of these *looks* like it works today and *fails* at the next upgrade. They all reduce to one root cause: putting your intent somewhere the framework is allowed to overwrite.

- **Editing a framework file "just this once."** Adding a field to `DataAccess.cs`, tweaking a query in `DataAccess.Users.cs`, or changing markup in `Pages\About.razor` writes your change onto the bottom sheet. The next upgrade swaps that sheet and your change is gone. **Fix:** move it to the matching `.App.` file.

- **Renaming `.App.` away, or hiding logic under a different name.** The upgrade tool and the segregation contract key off the literal `.App.` token in the file name. A file named `DataAccessCustom.cs` is *not* protected. **Fix:** keep the `.App.` marker exactly as the framework ships it (see [042](042_file-naming-law.md)).

- **Forgetting the `partial` keyword or changing the namespace on your half.** If your sheet says `class DataAccess` (no `partial`) or `namespace MyApp` instead of `CRM`, the two halves no longer fuse into one class and the build breaks — or worse, you silently shadow the framework type. **Fix:** match the framework's `partial` + namespace exactly (`CRM` server-side, `CRM.Client` in the browser).

- **Bypassing a hook instead of filling it in.** If you copy a whole framework method into your sheet and edit the copy "to be safe," you now own a stale fork of that method that will drift from the framework's real one. **Fix:** use the provided `...App` / `AppModify...` seam, which the framework keeps calling across versions.

- **Putting custom DTO fields in framework `DataObjects.*.cs` files.** They compile today and vanish on upgrade. **Fix:** extend the partial type in `DataObjects.App.cs`.

- **Skipping the migration prerequisite.** Running `Upgrade FreeCRM.exe` against an app whose custom code is *not* yet in `.App.` files is exactly the case the README warns is unsupported — the tool can only carry forward what lives on your sheet.

- **Assuming the tool handles everything.** The README is explicit about edge cases: *"There are edge cases that cannot be updated with this tool, such as having additional projects in your solution. The tool will copy those projects, but any references in other projects must be added manually."* Treat the contract as the happy path and the tool's printed report as your checklist for the rest.

---

<a id="verifying"></a>
## 7. Verifying an Upgrade

After you run `Upgrade FreeCRM.exe` (its usage is `"Upgrade FreeCRM.exe" C:\MyExistingApp`), you want fast, concrete confirmation that your half came through intact. Work down this list:

1. **Read the tool's report first.** The upgrade tool produces a report by design — the README calls it out: *"The tool will produce a report that will help with any additional steps that will be required for the migration."* It flags the edge cases (extra projects, manual references) it could not finish. Treat any item there as required follow-up, not optional.

2. **Confirm your `.App.` files are present and unchanged.** Every file with `.App.` in the name should still hold your code, not framework defaults. The fastest tell: your app identity. Open `DataAccess.App.cs` and check that `_appName` and `_version` are *yours*, not back to `"freeCRM"` / `"2.0.0"`. If those reverted, the wrong sheet won — investigate before going further.

3. **Confirm you have not stranded edits in framework files.** Diff your old framework files against the new ones. Any place the diff shows *your* logic disappearing is a spot where you had (wrongly) edited a framework file; re-home that logic into the matching `.App.` file now.

4. **Build the solution.** A clean compile proves the two halves still fuse: same `partial`, same namespace, no duplicate or missing members. If the build complains about a missing method like `DataAccessAppInit` or about an interface member, your sheet drifted from the framework's expected seam — reconcile the signature against the new framework half.

5. **Exercise each hook you implemented.** Run the app and trigger the paths that call your seams: load and save a record (`GetDataApp` / `SaveDataApp`), delete one (`DeleteRecordsApp`), let the background service tick (`ProcessBackgroundTasksApp`), open a page whose `.App.razor` component you customized (e.g. About → `<About_App />`). If your customizations still appear and behave, the contract held.

6. **Smoke-test startup customizations.** If you added services or middleware via `AppModifyBuilderStart/End` or `AppModifyStart/End`, confirm those still register — a healthy startup and your feature working end-to-end is the proof.

Green across all six means the framework's sheet was swapped, your sheet rode along, and you are back on the upgrade path for next time.

---

<a id="related-docs"></a>
## 8. Related Docs

- [042 — The Naming Law That Keeps Your Code Yours](042_file-naming-law.md) — the naming law that implements this
- [084 — Riding the Framework Forward](084_performing-upgrades.md) — taking an upgrade in practice
- [023 — Inside the Partial Data-Access Layer](023_partial-data-access.md) — partials in the data layer

---
*GuidesV2 · 041 · drafted from source 2026-06-05 · grounded in FreeCRM `DataAccess.App.cs`, `Program.App.cs`, `DataObjects.App.cs`, `About.App.razor`, and the README "Upgrade" section.*

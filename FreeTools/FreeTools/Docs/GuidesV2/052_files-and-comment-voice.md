# 052 — Where Code Lives and How Comments Sound

> **Document ID:** 052  ·  **Category:** Style  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Define file-organization conventions and the consistent voice our code comments use.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 05x (The House Style: Code Conventions) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | Why predictable file layout and a steady comment voice lower friction |
| 2 | [File and Folder Layout](#folder-layout) | The six projects, the partial-class split, and where each kind of file lives |
| 3 | [Naming Conventions](#naming) | How files, partials, and the `.App.` pairs are named |
| 4 | [The Comment Voice](#comment-voice) | Tone, person, and tense the real codebase actually uses |
| 5 | [What to Comment (and What Not To)](#what-to-comment) | When a comment earns its place — and the special markers you must not delete |
| 6 | [Worked Examples](#examples) | Real comments copied from the source, good patterns and anti-patterns |
| 7 | [Quick Checklist](#checklist) | Fast pre-commit review reminders |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

**The short version: when every file is where you expect it and every comment sounds the same, you stop spending brainpower on "where is this?" and "what tone do I use?" and spend it on the actual work.**

Two ideas are bundled in this doc, and they share one goal — predictability.

- **File organization** is the question of *which file a given piece of code lives in*. In FreeCRM the answer is rarely "one giant file." Instead, a single logical class (say, all the server-side data logic) is **split across dozens of small files**, one per topic. If you know the topic, you know the file.
- **Comment voice** is the *tone and grammar of the notes developers leave in the code*. A "comment" is any line the compiler ignores — in C# it starts with `//` (or sits between `/* */`); the documentation form starts with `///`. FreeCRM's comments have a recognizable, plain-spoken voice, and matching it makes your additions invisible in the best way: nobody can tell where the framework's notes end and yours begin.

Why care, as a non-engineer reviewing or directing this work? Because both conventions are **load-bearing for the framework's headline feature: upgradeability.** FreeCRM is a template you fork and customize, then periodically pull the upstream framework's improvements back in. That only works if your code is cleanly separated from the framework's code — and the file layout (especially the `.App.` convention in [§2](#folder-layout)) is exactly that separation made physical. A sloppy layout means painful merges; a clean one means upgrades are almost free.

The rest of this doc is descriptive, not aspirational: every rule below was read out of the actual repository at `c:\Users\pepkad\source\repo2\FreeCRM`, not invented.

---

<a id="folder-layout"></a>
## 2. File and Folder Layout

### 2.1 The solution is six projects

FreeCRM is one **solution** (`CRM.slnx` — the file that ties the projects together) made of six **projects** (each project is a separately compiled unit with its own `.csproj` file). Knowing which project owns a concern tells you which folder to open.

| Project | What lives here |
|---------|-----------------|
| `CRM` | The ASP.NET Core host: controllers (the HTTP API), SignalR hubs, server startup. |
| `CRM.Client` | The Blazor WebAssembly client — everything that runs in the browser: pages, shared components, the shared data model. |
| `CRM.DataAccess` | The server-side data layer: business logic, database operations, encryption, file storage, Graph/LDAP integrations. |
| `CRM.DataObjects` | The plain data shapes (DTOs) passed between client and server, plus configuration helpers. |
| `CRM.EFModels` | The Entity Framework Core models — the C# classes that mirror database tables. |
| `CRM.Plugins` | The plugin contracts and host. |

("DTO" = *data transfer object*, a class that just holds fields with no behavior; it exists to carry data across the wire. "EF Core" = Entity Framework Core, Microsoft's library for mapping C# objects to database rows.)

### 2.2 The big classes are split into many small files — by topic

The most distinctive layout choice in FreeCRM is the **partial class**. A `partial class` is one class whose source is spread across multiple files; the compiler stitches them together. FreeCRM uses this to keep enormous classes navigable: instead of a 10,000-line `DataAccess.cs`, there is a `DataAccess.cs` plus ~30 sibling files, each named for the slice of work it holds.

```
CRM.DataAccess/
  DataAccess.cs              ← constructor, EF context setup, core init
  DataAccess.Authenticate.cs ← login flows
  DataAccess.Encryption.cs   ← AES encrypt/decrypt helpers
  DataAccess.FileStorage.cs  ← upload / retrieve / delete files
  DataAccess.Users.cs        ← user CRUD, password hashing, lockout
  DataAccess.App.cs          ← YOUR application-specific overrides
  ... ~25 more, one per domain
```

The same pattern repeats in the other projects:

- **`DataController.*`** in `CRM/Controllers/` — the HTTP API split by topic (`DataController.Users.cs`, `DataController.Tenants.cs`, `DataController.FileStorage.cs`, …).
- **`DataObjects.*`** in `CRM.DataObjects/` — the DTOs split by domain (`DataObjects.Appointments.cs`, `DataObjects.Invoices.cs`, `DataObjects.Tags.cs`, …).

The interface is partial too. `public partial interface IDataAccess` is declared in *each* domain file alongside the methods that implement it — so the contract for file storage lives in `DataAccess.FileStorage.cs`, right next to its implementation:

```csharp
public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> DeleteFileStorage(Guid FileId, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false);
    Task<DataObjects.FileStorage> GetFileStorage(Guid FileId, DataObjects.User? CurrentUser = null);
    ...
}

public partial class DataAccess
{
    public async Task<DataObjects.BooleanResponse> DeleteFileStorage(Guid FileId, ...)
    {
        ...
    }
}
```

**Rule of thumb:** when you add a method, put it in the file whose name matches its topic. If no file matches, the topic probably belongs in a new partial file named the same way, or in the `.App.` partial (below).

> The `CRM.DataAccess/README.md` in the repo carries a full table mapping every `DataAccess.*.cs` file to its responsibility — treat it as the authoritative map when you're hunting for where something lives.

### 2.3 The `.App.` convention: your code, kept apart from the framework's

This is the layout rule that makes upgrades work, so it deserves its own paragraph.

Every customizable area ships as a **pair of partial files**: the framework file (e.g. `DataAccess.cs`) holds the built-in implementation, and a **`.App.` partner** (e.g. `DataAccess.App.cs`) is the empty-by-default place where *your* customizations go. Because they're partials, code in `DataAccess.App.cs` is part of the very same `DataAccess` class — it can see and extend everything — but it lives in a file the framework promises never to overwrite.

Representative pairs that exist in the tree today:

```
DataModel.cs        / DataModel.App.cs          (CRM.Client)
Helpers.cs          / Helpers.App.cs            (CRM.Client)
MainLayout.razor    / MainLayout.App.razor      (CRM.Client/Layout)
DataAccess.cs       / DataAccess.App.cs         (CRM.DataAccess)
GraphAPI.cs         / GraphAPI.App.cs           (CRM.DataAccess)
DataController.cs   / DataController.App.cs      (CRM/Controllers)
Program.cs          / Program.App.cs            (CRM)
ConfigurationHelper.cs / ConfigurationHelper.App.cs (CRM.DataObjects)
GlobalSettings.cs   / GlobalSettings.App.cs     (CRM.DataObjects)
```

Plus a whole folder of UI overrides: `CRM.Client/Shared/AppComponents/*.App.razor` (About, Settings, Users, EditUser, EditTenant, and more), and even a CSS pair, `wwwroot/css/site.App.css`.

The payoff: when you pull a new version of the framework, the framework's `.cs`/`.razor` files can be replaced wholesale, and your `.App.` files survive untouched. The repo even ships an `Upgrade FreeCRM.exe` utility that depends on your app already being in `.App.` form.

> **Why this matters in one sentence:** the `.App.` split is what lets "find all the code I wrote" and "swap the framework underneath me" both be true at the same time.

A freshly cloned `.App.` file is mostly empty stubs and guidance comments. For example, `GlobalSettings.App.cs` in full:

```csharp
namespace CRM;

public static partial class GlobalSettings
{
    // Add any app-specific global settings here.
}
```

That single comment *is* the file's contract: this is your sandbox.

### 2.4 The `CRM.Client` browser layout

Inside `CRM.Client` the front-end follows the usual Blazor shape:

- `Pages/` — routable pages (`Index.razor`, plus subfolders `Authorization/`, `Settings/`, `Invoices/`, `Scheduling/`, …).
- `Layout/` — the page shell (`MainLayout.razor` + its `.App.` partner).
- `Shared/AppComponents/` — reusable UI pieces and their `.App.` overrides.
- `DynamicBlazorSupport/` — the in-browser Roslyn compiler that renders plugin-supplied components at runtime.
- `wwwroot/` — static assets (CSS, JS, images).

The `CRM.Client/README.md` carries the full route-to-component table if you need to find which file backs a given URL.

---

<a id="naming"></a>
## 3. Naming Conventions

File names in FreeCRM are not decorative — they encode *what's inside* and *who owns it*. There are three patterns, and that's the whole vocabulary.

**1. `BaseName.cs` — the base partial.** The first file of a partial class carries the bare class name and the "core" content (constructor, setup, the most central members). Examples: `DataAccess.cs`, `DataController.cs`, `DataObjects.cs`, `DataModel.cs`.

**2. `BaseName.Topic.cs` — a domain slice.** Additional partials append the topic with a dot, in PascalCase (each word capitalized, no spaces). The topic names line up across the three families so the same concept is findable everywhere:

| Topic | DataAccess file | Controller file | DataObjects file |
|-------|-----------------|-----------------|------------------|
| Users | `DataAccess.Users.cs` | `DataController.Users.cs` | (in `DataObjects.cs`) |
| Tags | `DataAccess.Tags.cs` | `DataController.Tags.cs` | `DataObjects.Tags.cs` |
| Invoices | `DataAccess.Invoices.cs` | `DataController.Invoices.cs` | `DataObjects.Invoices.cs` |
| File storage | `DataAccess.FileStorage.cs` | `DataController.FileStorage.cs` | (in `DataObjects.cs`) |

**3. `BaseName.App.cs` (or `.App.razor`) — the customization partner.** The literal token `App` between the base name and the extension marks the file as *yours to edit, framework-hands-off*. This is the most important naming rule in the codebase; [§2.3](#folder-layout) covers the why. The full law on this token lives in [042 — The Naming Law That Keeps Your Code Yours](042_file-naming-law.md).

A few smaller conventions worth knowing:

- **`GlobalUsings.cs`** holds the `global using` directives for a project — namespaces imported once for every file. (A `global using` is a `using` statement that applies project-wide so individual files don't have to repeat it.)
- **`_Imports.razor`** is the Blazor equivalent for `.razor` files.
- **Per-provider files use a `.Provider.cs` suffix**, e.g. `DataMigrations.SQLServer.cs`, `DataMigrations.MySQL.cs`, `DataMigrations.PostgreSQL.cs`, `DataMigrations.SQLite.cs`.
- **Namespaces follow the project, not the folder depth.** Server-side data files use `namespace CRM;`, client files use `namespace CRM.Client;`. These are **file-scoped namespaces** — the `namespace CRM;` form with a semicolon and no braces, which indents the whole file one level less. You'll see it at the top of nearly every `.cs` file:

```csharp
namespace CRM;

public partial class DataAccess
{
    ...
}
```

---

<a id="comment-voice"></a>
## 4. The Comment Voice

A "comment voice" is the consistent *style of writing* used in the code's notes — the same way a brand has a consistent tone. FreeCRM's voice is easy to match once you've read a few files, because it is unusually disciplined. Here's the profile, drawn straight from the source.

**Person and mood: second-person, imperative, addressed to the next developer.** Comments talk *to you*, the person about to edit this file, and they tell you what to do. They say "Use this file as a place to…", "Add any app-specific…", "Remove this line once you implement your logic." This is the dominant voice in every `.App.` file:

```csharp
// Use this file as a place to put any application-specific data access methods.
```
```csharp
// Add any app-specific initialization logic here.
```

**Tense: present, describing what the code *is* or what the reader *should* do.** Not "this will return" — just plain present statements of fact or instruction.

**Tone: plain, calm, complete sentences, ending in a period.** No jokes, no shorthand, no all-caps shouting, no "TODO: fix this later" rot. Field comments read like a glossary entry:

```csharp
// The number of bad login attempts before an account is locked out.
private int _accountLockoutMaxAttempts = 5;

// The number of days a JWT token is valid for. This is used when encoding JWT tokens.
private int _tokenDays = 7;
```

**Placement: a field's explanatory comment goes on the full line *above* it, not trailing on the same line.** (The one common exception is a short throwaway note trailing a line of code, like the async-stub note below.) This above-the-field placement is deliberate — when the original author moved tunable fields into `DataAccess.App.cs`, inline trailing comments were promoted to full lines above the field.

**XML doc comments (`///`) for methods, plain `//` for everything else.** A `///` comment is a *documentation comment*: the IDE reads its `<summary>` and `<param>` tags and shows them as tooltips. FreeCRM uses `///` on methods that a developer is expected to fill in or call, and ordinary `//` for inline notes:

```csharp
/// <summary>
/// Called from various delete methods to delete any app-specific records before continuing with the delete process.
/// </summary>
/// <param name="Rec">The EF record object.</param>
/// <param name="CurrentUser">The User object for the current user, if loaded.</param>
/// <returns>A BooleanResponse object.</returns>
private async Task<DataObjects.BooleanResponse> DeleteRecordsApp(object Rec, DataObjects.User? CurrentUser = null)
{
    ...
}
```

**Razor markup comments live inside the C# block.** In `.razor` files, explanatory notes sit inside an `@{ }` or `@code { }` block using the same `//` style:

```razor
@{
    // This component is used to display application-specific items on the about page.
    ...
}
```

The throughline of the whole voice: **write the comment as a clear instruction or a one-sentence definition, for a competent developer who has never seen this file before.** That is the same reader model this very doc set is written for.

---

<a id="what-to-comment"></a>
## 5. What to Comment (and What Not To)

A comment earns its place when it tells the reader something the code itself cannot. FreeCRM's actual comments fall into a few honest categories — and there are a couple of special markers you must never delete.

**Comment the *why* and the *what-to-do*, not the *what*.** Good FreeCRM comments explain intent, document a contract, or guide a future edit. They rarely restate the obvious.

- **Field meaning / units.** `// The number of minutes an account is locked out after reaching the maximum number of bad login attempts.`
- **An instruction to the customizing developer.** `// Add any app-specific filter columns here.`
- **A scaffolding note that flags temporary code.** The async stubs throughout the `.App.` files carry an honest, self-deprecating trailing note:

```csharp
await Task.Delay(0); // Simulate a delay since this method has to be async. This can be removed once you implement your await logic.
```
```csharp
output.Result = true; // Remove this line once you implement your logic.
```

**Use commented-out code only as a worked example, clearly labeled.** Normally dead code is deleted (see below). The exception in FreeCRM is *example* code — a commented template showing the developer exactly how to wire something up, often introduced with `// Example:` or `// Sample`:

```csharp
// Add any app-specific filter columns here.
// Example:
// if (Type.ToLower() == "users" && Position.ToLower() == "username") {
//     output.Add(new DataObjects.FilterColumn { Name = "MyColumn", Type = "string", ... });
// }
```

**Do NOT leave genuinely dead or commented-out *implementation* code.** The house style deletes old commented-out logic and unused code outright. If it's not an intentional `// Example:` template, it should be removed, not left to rot.

**Do NOT delete the special template markers.** Two kinds of comment look like notes but are actually *machine instructions* — tooling reads them:

- **Module markers** `// {{ModuleItemStart:Name}}` … `// {{ModuleItemEnd:Name}}` wrap optional feature blocks (Appointments, Invoices, Payments, Services, …). The `Remove Modules from FreeCRM.exe` tool uses these markers to strip whole features cleanly. Deleting a marker breaks that tool.

```csharp
// {{ModuleItemStart:Invoices}}
if (Rec is EFModels.EFModels.Invoice) {
    ...
}
// {{ModuleItemEnd:Invoices}}
```

**Don't comment to apologize or to track work.** No `// TODO`, no `// HACK`, no `// not sure why this works`. If something needs doing, do it or open an issue; if logic is subtle, explain the reason plainly.

---

<a id="examples"></a>
## 6. Worked Examples

All of the "good" examples below are copied faithfully from the real FreeCRM source. The "avoid" examples are the anti-patterns the house style exists to prevent.

**Example 1 — A field comment.**

✅ Good (from `DataAccess.App.cs`): full sentence, on the line above, states purpose and units.
```csharp
// The number of days a JWT token is valid for. This is used when encoding JWT tokens.
private int _tokenDays = 7;
```
🚫 Avoid: trailing, fragmentary, restates the name.
```csharp
private int _tokenDays = 7; // days
```

**Example 2 — A method note.**

✅ Good (from `Program.App.cs`): says where to act, in the imperative.
```csharp
public static WebApplication AppModifyStart(WebApplication app)
{
    var output = app;
    // Add any app-specific modifications to the app here.
    return output;
}
```
🚫 Avoid: narrates the obvious instead of guiding the reader.
```csharp
// set output to app and return it
```

**Example 3 — A method with an XML doc comment.**

✅ Good (from `Helpers.App.cs`): `<summary>`/`<param>`/`<returns>` document the contract for tooltips.
```csharp
/// <summary>
/// Gets the deleted records for a specific app type.
/// </summary>
/// <param name="deletedRecords">The DeletedRecords object.</param>
/// <param name="type">The item type.</param>
/// <returns>A nullable list of DeletedRecordItem objects.</returns>
public static List<DataObjects.DeletedRecordItem>? GetDeletedRecordsForAppType(DataObjects.DeletedRecords deletedRecords, string type)
{
    ...
}
```

**Example 4 — A worked-example block left in on purpose.**

✅ Good (from `Helpers.App.cs`): commented template under a clear `// Sample` label, showing exactly how to add a menu item.
```csharp
public static List<DataObjects.MenuItem> MenuItemsApp {
    get {
        // Add any app-specific top-level menu items here.
        var output = new List<DataObjects.MenuItem>();

        // Sample
        //if (Model.User.Admin) {
        //    output.Add(new DataObjects.MenuItem {
        //        Title = "My Custom Menu Item",
        //        Icon = "Home",
        //        ...
        //    });
        //}

        return output;
    }
}
```

**Example 5 — A Razor component note.**

✅ Good (from `About.App.razor`): one-line `//` inside the markup block explaining the component's job.
```razor
@{
    // This component is used to display application-specific items on the about page.

    <h1 class="page-title">
        <Icon Name="About" />
        About freeCRM
    </h1>
}
```

**Example 6 — Module markers (do not touch).**

✅ Correct: the markers stay exactly as-is so the module-removal tool can find them.
```csharp
// {{ModuleItemStart:Payments}}
if (Rec is EFModels.EFModels.Payment) {
    var rec = Rec as EFModels.EFModels.Payment;
    if (rec != null) {
        // Remove any related records.
    }
}
// {{ModuleItemEnd:Payments}}
```
🚫 Avoid: "tidying up" by deleting the marker comments — this silently breaks `Remove Modules from FreeCRM.exe`.

---

<a id="checklist"></a>
## 7. Quick Checklist

Run this before you commit. It's fast.

**Layout**
- [ ] New code is in the project that owns the concern (host / client / data access / data objects / EF models / plugins).
- [ ] It's in the topic partial whose name matches (`*.Users.cs`, `*.Tags.cs`, …), or a sensibly named new partial.
- [ ] Customizations went into the `.App.` partner — not into a framework file the next upgrade will overwrite.
- [ ] The file uses the right file-scoped namespace (`namespace CRM;` for server, `namespace CRM.Client;` for client).

**Comment voice**
- [ ] Comments are full sentences in the imperative/present, ending with a period.
- [ ] Field comments sit on the line *above* the field, not trailing it.
- [ ] Methods meant to be filled in or called have a `///` summary; inline notes use plain `//`.
- [ ] Comments explain *why* or *what to do*, not *what the code obviously is*.

**Don't-break-this**
- [ ] No `// TODO` / `// HACK` / apology comments left behind.
- [ ] No dead or commented-out *implementation* code (only labeled `// Example:` / `// Sample` templates are kept).
- [ ] Every `// {{ModuleItemStart:X}}` / `// {{ModuleItemEnd:X}}` marker is intact.

---

<a id="related-docs"></a>
## 8. Related Docs

- [051 — The Author House Style](051_house-code-style.md) — the parent style charter
- [042 — The Naming Law That Keeps Your Code Yours](042_file-naming-law.md) — the file-naming law
- [053 — The Machine Referee: editorconfig and What It Enforces](053_editorconfig-enforcement.md) — machine enforcement

---
*GuidesV2 · 052 · drafted 2026-06-05 from the FreeCRM source at `c:\Users\pepkad\source\repo2\FreeCRM`.*

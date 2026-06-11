# 004 — Reading the Repository Map

> **Document ID:** 004  ·  **Category:** Onboarding  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** A guided tour of the folder layout so newcomers can predict where any kind of code or asset lives.
> **Audience:** Brand-new adopters  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 00x (Landing Zone: From Clone to Login) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why the Map Matters](#why-it-matters) | What a repo map is and why a mental map saves you hours |
| 2 | [The Organizing Principle](#mental-model) | The one rule — "split by layer" — that shapes every folder |
| 3 | [Top-Level Tour](#top-level-tour) | The six projects in the solution and what each one does |
| 4 | [Where Code Lives](#where-code-lives) | Mapping each kind of code to the project that holds it |
| 5 | [Where Assets Live](#where-assets-live) | Images, configs, plugin files, and static web assets |
| 6 | [Naming Signals](#naming-signals) | How a filename's prefix, `.App.` tag, and suffix reveal its job |
| 7 | [Predicting New Locations](#predicting-locations) | A repeatable method for deciding where a new file belongs |
| 8 | [When You Can't Find It](#troubleshooting) | Common wrong turns and fast search shortcuts |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why the Map Matters

**A "repo map" (repository map) is just a mental picture of where things live** in the codebase — which folder holds the database code, which holds the web pages, which holds shared definitions. The repository (or "repo") is the whole collection of source files you cloned. The map is the model you carry in your head that lets you guess, correctly, where a file lives before you go looking for it.

**Why care?** Because most of the time you spend in an unfamiliar codebase is not spent writing — it is spent *finding*. If you cannot predict where something lives, every small task starts with a frustrating scavenger hunt. A good mental map turns "where on earth is the login logic?" into a one-second answer: *server-side data layer, in the authentication file.*

FreeCRM is laid out so deliberately that the map is learnable in an afternoon. There are exactly **six projects** (a "project" is one compilable unit of code — in C# it has a `.csproj` file), and each one has a single, clear job. Once you know the six jobs, you can place almost any file. This document gives you that map and the small set of rules that make it predictable.

The payoff compounds: the same layout repeats whether you are reading the code, adding a feature, or upgrading to a newer framework release. Learn it once, reuse it forever.

---

<a id="mental-model"></a>
## 2. The Organizing Principle

**There is one rule behind the whole tree: split the code by *layer*, and within each layer split files by *feature*.**

A "layer" is a horizontal slice of responsibility. Picture the application as a stack:

- The **browser layer** (what the user sees and clicks)
- The **server layer** (what receives requests and enforces rules)
- The **data-access layer** (what reads and writes the database)
- The **database-model layer** (the shape of the tables)
- The **shared-contract layer** (definitions both the browser and server agree on)
- The **plugin layer** (a runtime extension system)

Each layer is its own project. That separation is not decoration — it is what lets the browser code (which runs on the user's machine) and the server code (which runs on your machine) share the same definitions without dragging server-only libraries into the browser. The browser cannot run database code, so the database code lives in a separate project the browser never references.

Inside each project, the **second rule** kicks in: one file per feature. Instead of a single 10,000-line `DataAccess.cs`, the data layer is broken into `DataAccess.Users.cs`, `DataAccess.Invoices.cs`, `DataAccess.Appointments.cs`, and so on — roughly thirty files, each owning one slice of behavior. They are all the *same class* (`DataAccess`), stitched together using a C# feature called a **partial class** (a class whose definition is spread across several files; the compiler merges them into one). The benefit: you open exactly the file whose name matches your feature, and nothing else.

So the entire map reduces to two questions you ask in order:

1. **Which layer?** → picks the project.
2. **Which feature?** → picks the file inside it.

Everything below is just the detail that makes those two questions easy to answer.

---

<a id="top-level-tour"></a>
## 3. Top-Level Tour

The solution file is `FreeCRM/CRM.slnx` (a "solution" is the container that groups related projects). It pulls together these six projects. Here is each one, top to bottom, with its one-line job:

| Project (folder) | Layer | What it is | One-line job |
|------------------|-------|-----------|--------------|
| **CRM.Client** | Browser | Blazor WebAssembly client | Everything the user sees and clicks; runs *inside the browser*. |
| **CRM** | Server | ASP.NET Core host | Receives web/API requests, serves the client, enforces auth, runs background jobs. |
| **CRM.DataAccess** | Data access | Class library | All business logic and database reads/writes; server-only integrations (email, PDF, Graph, LDAP). |
| **CRM.EFModels** | Database model | Class library | The Entity Framework `DbContext` and one class per database table. |
| **CRM.DataObjects** | Shared contract | Class library | The DTOs (Data Transfer Objects) both browser and server agree on. |
| **CRM.Plugins** | Plugin runtime | Class library | Loads, compiles, and runs dynamic C#/Blazor plugin files at runtime. |

A few terms unpacked, since this is where the jargon clusters:

- **Blazor WebAssembly** — a way to write web front-ends in C# instead of JavaScript; the C# is compiled to WebAssembly and runs in the browser. That is why `CRM.Client` is "the browser layer."
- **ASP.NET Core host** — the web server program. `CRM/Program.cs` is the entry point that boots everything. This project is the only *executable*; the four class libraries are loaded by it.
- **DTO (Data Transfer Object)** — a plain data shape (just properties, no logic) used to ship information between the browser and the server. Because both sides must understand it, DTOs live in their own shared project, `CRM.DataObjects`.
- **Entity Framework (EF) Core** — the library that maps C# classes to database tables, so you write C# instead of raw SQL. Its model lives in `CRM.EFModels`.

**How they depend on each other** (verified from the project README references):

- `CRM` (server) references `CRM.DataAccess`, `CRM.Plugins`, and `CRM.Client`.
- `CRM.DataAccess` references `CRM.DataObjects`, `CRM.EFModels`, and `CRM.Plugins`.
- `CRM.DataObjects` references `CRM.Plugins`.
- `CRM.Client` references only `CRM.DataObjects`.
- `CRM.EFModels` and `CRM.Plugins` reference nothing else — they are the foundation everyone builds on.

Notice the deliberate gap: **the browser project (`CRM.Client`) references only `CRM.DataObjects`.** It has no line of sight to the database or data-access projects. That is the "split by layer" rule enforced by the compiler itself — the browser physically cannot reach server-only code.

---

<a id="where-code-lives"></a>
## 4. Where Code Lives

Now we map *kinds* of code to their homes. When you are looking for a specific behavior, this table tells you which project, and the notes tell you which folder inside it.

| You're looking for… | Project | Folder / file pattern |
|---------------------|---------|-----------------------|
| A page the user navigates to | `CRM.Client` | `Pages/` (e.g. `Pages/Settings/Users/Users.razor`) |
| A reusable UI widget (button, dialog, menu) | `CRM.Client` | `Shared/` (e.g. `Shared/ModalMessage.razor`) |
| The shared client-side state object | `CRM.Client` | `DataModel.cs` (`BlazorDataModel`) |
| Client-side helper methods | `CRM.Client` | `Helpers.cs` |
| A REST API endpoint | `CRM` | `Controllers/DataController.*.cs` (e.g. `DataController.Invoices.cs`) |
| App startup / service wiring | `CRM` | `Program.cs` |
| The real-time SignalR hub | `CRM` | `Hubs/signalrHub.cs` |
| Authentication middleware | `CRM` | `Classes/CustomAuthenticationHandler.cs` |
| The background job runner | `CRM` | `Classes/BackgroundProcessor.cs` |
| Business logic / database CRUD | `CRM.DataAccess` | `DataAccess.*.cs` (e.g. `DataAccess.Users.cs`) |
| Login / token logic | `CRM.DataAccess` | `DataAccess.Authenticate.cs`, `DataAccess.JWT.cs` |
| PDF, email, encryption, file storage | `CRM.DataAccess` | `DataAccess.PDF.cs`, `DataAccess.EmailTemplates.cs`, `DataAccess.Encryption.cs`, `DataAccess.FileStorage.cs` |
| A database table definition | `CRM.EFModels` | `EFModels/<Entity>.cs` (e.g. `EFModels/User.cs`) |
| The `DbContext` itself | `CRM.EFModels` | `EFModels/EFDataModel.cs` |
| A DTO shared across the API boundary | `CRM.DataObjects` | `DataObjects.*.cs` (e.g. `DataObjects.Invoices.cs`) |
| Plugin loading / execution | `CRM.Plugins` | `Plugins.cs` |
| A sample / custom plugin | `CRM` | `PluginFiles/` (e.g. `Example1.cs`, `HelloWorld.plugin`) |

**The feature-name trick.** Notice the same feature names repeat across layers. A "Users" feature has, in parallel:

- `CRM.Client/Pages/Settings/Users/Users.razor` — the page
- `CRM/Controllers/DataController.Users.cs` — the API endpoint
- `CRM.DataAccess/DataAccess.Users.cs` — the business logic
- `CRM.DataObjects/` — the `User` DTO (in `DataObjects.cs`)
- `CRM.EFModels/EFModels/User.cs` — the table

So if you know the feature, you can predict all five files by sliding the same word across the layers. This is the single most useful pattern in the whole repo. Want invoices? Swap "Users" for "Invoices" in every path above.

**The partial-class detail.** In `CRM.DataAccess` and in `CRM/Controllers`, the many `.cs` files are all one class each — `DataAccess` and `DataController` respectively — declared `partial` so the compiler can spread one class across dozens of feature files. You do not need to register a new file anywhere; naming it `DataAccess.Whatever.cs` with `public partial class DataAccess` inside is enough for it to join the class.

---

<a id="where-assets-live"></a>
## 5. Where Assets Live

"Assets" are the non-C# files an app needs: images, stylesheets, fonts, configuration, and the like. They cluster in a few predictable spots.

**Static web assets (served to the browser)** live in `CRM.Client/wwwroot/`. The name `wwwroot` is an ASP.NET Core convention meaning "the public web root — anything here is downloadable by a browser." Inside it:

- `wwwroot/css/` — stylesheets
- `wwwroot/js/` — JavaScript helpers
- `wwwroot/images/` — image files
- `wwwroot/fontawesome/`, `wwwroot/lib/` — third-party icon and library bundles
- `wwwroot/favicon.ico`, `apple-touch-icon.png`, `site.webmanifest`, `web-app-manifest-*.png` — browser tab icon and progressive-web-app manifest files

**Configuration** uses JSON files named `appsettings.json` (an ASP.NET Core convention — the file the app reads its settings from at startup):

- `CRM/appsettings.json` and `CRM/appsettings.Development.json` — the *server's* settings: database type and connection string, SignalR mode, authentication providers, the background-service schedule. The `.Development.json` variant overrides values when you run locally.
- `CRM.Client/wwwroot/appsettings.json` — settings the *browser* is allowed to see (never put secrets here; the browser downloads it).
- `CRM/web.config` — IIS hosting configuration, used when deploying under Windows IIS.

**Plugin files** live in `CRM/PluginFiles/`. These are not normal compiled assets — they are source files the running app discovers, compiles, and executes on the fly:

- `*.cs` files (e.g. `Example1.cs`, `UserUpdate.cs`) — plugins included as plain source code.
- `*.plugin` files (e.g. `HelloWorld.plugin`) — plugins kept out of the normal build because they would cause build conflicts (for example, the `HelloWorld` sample references an external DLL listed in a companion `HelloWorld.assemblies` file).
- `BlazorComponents/` — runtime-loadable Blazor UI plugins, each a `.razor` file plus an optional `.json` metadata sidecar (e.g. `Button_Testing_TestButton.razor` + `Button_Testing_TestButton.json`).
- `Plugins.md` — documentation on writing plugins.

**Database migration scripts** are C# files but worth flagging here because they are data, not logic: `CRM.DataAccess/DataMigrations.SQLServer.cs`, `DataMigrations.SQLite.cs`, `DataMigrations.MySQL.cs`, and `DataMigrations.PostgreSQL.cs` hold the per-provider SQL that builds and updates the schema.

---

<a id="naming-signals"></a>
## 6. Naming Signals

Filenames in this codebase are not arbitrary — they are signposts. Read the name and you already know the file's layer, feature, and ownership. There are three signals to learn.

**Signal 1 — the prefix tells you the class.** A file named `DataAccess.Invoices.cs` is, before you open it, part of the `DataAccess` class and about invoices. `DataController.Users.cs` is part of `DataController` and about users. `DataObjects.Appointments.cs` holds appointment DTOs. The text before the first dot is the class; the text after is the feature.

**Signal 2 — the `.App.` tag means "this file is yours, not the framework's."** This is the single most important naming convention in FreeCRM. A file with `.App.` in the middle of its name — for example `DataAccess.App.cs`, `Program.App.cs`, `DataObjects.App.cs`, or `NavigationMenu.App.razor` — is reserved for *your* application-specific code. Files *without* `.App.` are framework files that a future framework upgrade may overwrite. Files *with* `.App.` are never overwritten; the upgrade tooling preserves them.

You can see the intent stated in the code itself. An excerpt from `CRM.DataObjects/DataObjects.App.cs`:

```csharp
namespace CRM;

// Use this file as a place to put any application-specific data objects.

public partial class DataObjects
{
    public partial class User
    {
        //public string? MyCustomUserProperty { get; set; }
    }
}
```

That commented-out `MyCustomUserProperty` is an invitation: add your own field here, in the `.App.` partial, and it survives every upgrade. There are around 30 such `.App.` files across the solution today — one alongside most core framework files (`Program.App.cs`, `Helpers.App.cs`, `DataController.App.cs`, `GraphAPI.App.cs`, and so on) — each a safe pocket for your customizations. The full rules live in **[042 — The Naming Law That Keeps Your Code Yours](042_file-naming-law.md)**; the only thing to remember here is *the map has a built-in "this is mine" marker, and it is the word `App`.*

**Signal 3 — the suffix tells you the file type's role.** `.razor` is a Blazor UI component (markup plus C#). `.razor.js` and `.razor.css` are the JavaScript and scoped-style companions for the component of the same name (e.g. `UploadFile.razor`, `UploadFile.razor.js`, `UploadFile.razor.css`). `.cshtml` is an older-style server-rendered Razor page (used for a handful of authentication pages in `CRM/Pages/`). `.csproj` marks a project root. `.plugin` and `.assemblies` are plugin-system files.

There is also a folder-level signal: a folder named `App` (such as `CRM.Client/Pages/App/`) is the designated home for your own pages. Its `info.txt` says so directly: *"Place your app-specific pages in this folder for easier upgrading to future versions of the app."*

---

<a id="predicting-locations"></a>
## 7. Predicting New Locations

When you are about to add something new, do not guess — run it through this short decision procedure. It is the same two questions from Section 2, expanded.

**Step 1 — Which layer does it belong to?** Ask what the code *does*:

- Users see or click it → `CRM.Client` (a page in `Pages/`, a widget in `Shared/`).
- It receives an HTTP/API request → `CRM` (`Controllers/`).
- It reads or writes the database, or runs server-only logic (email, PDF, encryption) → `CRM.DataAccess`.
- It is a new database table → `CRM.EFModels/EFModels/` (and wire it into `EFDataModel.cs`).
- It is a data shape both browser and server must understand → `CRM.DataObjects`.
- It extends the plugin runtime → `CRM.Plugins`; an actual plugin file → `CRM/PluginFiles/`.

**Step 2 — Is it a brand-new feature, or an addition to an existing one?**

- *Existing feature* (say, you are adding an invoice operation): open the matching `*.Invoices.cs` partial file in the right layer and add to it. No new file needed.
- *Brand-new feature* (say, "Reports"): create the parallel set of files using the feature name — `DataObjects.Reports.cs`, `DataAccess.Reports.cs`, `DataController.Reports.cs`, a `Pages/Reports/` page, and an `EFModels/Report.cs` if it needs a table. Follow the exact prefix-plus-feature pattern you saw in Section 4.

**Step 3 — Is this *your* customization or a framework change?** If it is your own application-specific code, prefer an `.App.` file or the `App/` folder so an upgrade never clobbers it:

- A custom DTO field → add it in `DataObjects.App.cs`.
- Custom startup wiring → `Program.App.cs`.
- A custom page → `CRM.Client/Pages/App/`.
- A custom data-layer method → `DataAccess.App.cs`.

**Step 4 — Sanity-check against a sibling.** Before creating the file, find an existing feature that resembles yours and copy its placement and naming exactly. The repo is internally consistent, so "where does the Users equivalent live?" is almost always the right question. If your new file's path does not mirror an existing one, you are probably putting it in the wrong place.

---

<a id="troubleshooting"></a>
## 8. When You Can't Find It

Even with the map, files hide. Here are the common wrong turns and the fast ways out.

**Wrong turn 1: "The login logic isn't in the client."** Correct — anything touching the database, passwords, or tokens lives server-side in `CRM.DataAccess` (`DataAccess.Authenticate.cs`), never in the browser project. If your search for business logic comes up empty in `CRM.Client`, you are in the wrong layer. The browser only *calls* the server; it never holds the rules.

**Wrong turn 2: searching for a giant single file.** There is no monolithic `DataAccess.cs` holding everything — it is split into ~30 partial files by feature, and likewise `DataController` is split across `Controllers/`. Search by *feature word*, not by class name. Looking for tag handling? The word "Tags" appears in `DataObjects.Tags.cs`, `DataAccess.Tags.cs`, `DataController.Tags.cs`, and `Pages/Settings/Tags/`.

**Wrong turn 3: editing a framework file and losing it on upgrade.** If your change is in a file *without* `.App.` in its name, a framework upgrade can overwrite it. If you cannot find a customization you made earlier, check whether it was in a framework file that got regenerated — and move it into the matching `.App.` file next time.

**Wrong turn 4: looking for a page route in the code-behind.** Page URLs (`@page` routes) are declared at the top of each `.razor` file, not in a central router. The full route table is documented in the `CRM.Client` project's `README.md` if you need the bird's-eye view.

**Fast search shortcuts** (use your editor's project-wide search, or the search tools):

- **To find a feature's files**, search filenames for the feature word, e.g. `*Invoices*` — it surfaces the matching file in every layer at once.
- **To find a DTO definition**, search the `CRM.DataObjects` project for `class <Name>` (e.g. `class Invoice`).
- **To find a table**, look directly at `CRM.EFModels/EFModels/<Name>.cs`.
- **To find where a method is called**, search the text of the method name across the whole solution; the call site is usually in the layer one step "up" (client calls controller, controller calls data access).
- **To confirm a file is yours and upgrade-safe**, check for `.App.` in the name. No `.App.` means framework-owned.

When in doubt, return to the two questions — *which layer? which feature?* — and the parallel-naming pattern from Section 4 will point you to the file.

---

<a id="related-docs"></a>
## 9. Related Docs

- [042 — The Naming Law That Keeps Your Code Yours](042_file-naming-law.md) — the naming law explains the file split
- [041 — Code the Framework Can Update Underneath](041_upgrade-safe-model.md) — why app and framework files are separated

---
*GuidesV2 · 004 · Reading the Repository Map · drafted from source 2026-06-05.*

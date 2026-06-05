# 001 — Three Pillars: CRM, Conventions, and CLI

> **Document ID:** 001  ·  **Category:** Onboarding  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** How FreeCRM, the house conventions, and FreeTools interlock so a newcomer knows what they are looking at.
> **Audience:** Brand-new adopters  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 00x (Landing Zone: From Clone to Login) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | Plain-language definitions of CRM, conventions, and CLI, and why a newcomer needs all three |
| 2 | [The Three Pillars at a Glance](#pillars-glance) | One-line summary and analogy for each pillar |
| 3 | [FreeCRM: The Application](#freecrm-app) | The .NET 10 Blazor product, its six projects, and what it does |
| 4 | [House Conventions: The Rulebook](#house-conventions) | The `.App.cs` partial-class pattern and the rename/remove/upgrade tools |
| 5 | [FreeTools: The CLI](#freetools-cli) | The Aspire-orchestrated analysis and documentation suite |
| 6 | [How They Interlock](#how-interlock) | How the framework, the rulebook, and the CLI reinforce one another |
| 7 | [Common Confusions and Pitfalls](#pitfalls) | Mistakes newcomers make and how to avoid them |
| 8 | [Where to Go Next](#next-steps) | Pointers to follow-on guides and resources |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

If you just cloned this repository, you are looking at three different things that share a name and a purpose but do very different jobs. Knowing which is which saves you from the most common beginner frustration: editing the wrong file, in the wrong project, in a way that gets erased later. This whole guide set exists to prevent that, and this first doc is the map.

Let's define the three words you'll see constantly, in plain language:

- **CRM** stands for *Customer Relationship Management*. It is a category of business software for keeping track of people, companies, appointments, invoices, and the work you do for them. **FreeCRM** is a ready-made, open-source CRM you can run as-is or, more often, use as a *starting skeleton* for your own application. It is the actual running product.
- **Conventions** are the agreed-upon rules and patterns the codebase follows — where things go, how files are named, and which files are "yours" versus "the framework's." They are not enforced by a compiler; they are habits the project relies on. The most important one here is a naming pattern called `.App.cs`, explained in Section 4.
- **CLI** stands for *Command-Line Interface* — a program you drive by typing commands in a terminal instead of clicking buttons. **FreeTools** is the CLI in this repo. It does not run your CRM for customers; it *inspects* a Blazor app and writes documentation, screenshots, and reports about it.

Why do you need all three at once? Because they answer three different questions. FreeCRM answers *"what am I building on?"* The conventions answer *"where do I put my own code so an upgrade doesn't delete it?"* FreeTools answers *"what does this app actually contain, and is it healthy?"* Skip any one and you will eventually get burned — by upgrading and losing work, or by not understanding the codebase you inherited.

<a id="pillars-glance"></a>
## 2. The Three Pillars at a Glance

One sentence and one analogy each:

- **FreeCRM — the application (the house).** An open-source CRM built in C# Blazor WebAssembly on .NET 10 that you can run as-is, customize, or harvest for parts. Think of it as a pre-built house with the plumbing and wiring already in.
- **House Conventions — the rulebook (the building code).** A small set of patterns — chiefly the `.App.cs` file convention — that mark which parts of the house are "yours to remodel" and which are "structural, leave to the framework." Think of it as the building code that tells you which walls are load-bearing.
- **FreeTools — the CLI (the home inspector).** A separate command-line suite that walks through a Blazor app, lists every room (route), photographs each one (screenshots), and writes an inspection report. Think of it as the inspector who shows up after the build and tells you what's really there.

A useful mental note: pillars 1 and 2 live in the **FreeCRM** repository and ship together. Pillar 3, **FreeTools**, is a *separate* repository and toolset that you point *at* a Blazor app from the outside.

<a id="freecrm-app"></a>
## 3. FreeCRM: The Application

**Why it matters:** This is the thing that actually does the work — the product your users log into. Everything else in this guide exists to help you build on it safely.

**What it is.** Per its own README, FreeCRM is *"An open-source CRM solution built in C# Blazor WebAssembly using .NET 10."* Blazor WebAssembly means the user interface runs as compiled C# inside the browser (no JavaScript framework required); .NET 10 is the version of Microsoft's runtime it targets. You can *"use this project as-is, or customize it to fit your needs. Or, just grab code that you need to use in your project."* That last option matters: many adopters treat FreeCRM as a parts bin, not a finished product.

**The six projects.** The solution file (`CRM.slnx`) defines six projects. Knowing what each does is the single most useful orientation fact in the codebase:

| Project | Role in plain terms |
|---------|---------------------|
| `CRM` | The web host (an ASP.NET Core server, SDK `Microsoft.NET.Sdk.Web`). It serves the app, exposes the API, and wires up authentication. This is the project you run. |
| `CRM.Client` | The Blazor WebAssembly front end (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`) — the pages and components that run in the user's browser. |
| `CRM.DataObjects` | Plain data shapes (the "nouns" — `User`, `BooleanResponse`, etc.) shared by both the server and the client so they speak the same language. |
| `CRM.EFModels` | The database layer, built on Entity Framework Core. "EF" is Microsoft's tool for mapping C# classes to database tables. |
| `CRM.DataAccess` | The business logic — the methods that read and write data, authenticate users, send email, generate PDFs, and so on. |
| `CRM.Plugins` | The extension system that lets you add behavior without modifying the core (see plugins below). |

These reference each other in layers. For example, `CRM.DataAccess` references `CRM.DataObjects`, `CRM.EFModels`, and `CRM.Plugins`; the web `CRM` project references `CRM.DataAccess`, `CRM.Plugins`, and `CRM.Client`. The shared namespace across these projects is simply `CRM` — every core file begins with `namespace CRM;`.

**Databases it speaks.** The `CRM.EFModels` project ships drivers for SQL Server, SQLite, MySQL, and PostgreSQL, plus an **InMemory** option used for quick local runs and tests. The database type is chosen at startup; if no connection is configured, the app redirects you to a setup page rather than crashing.

**Optional modules.** A lot of the CRM is opt-out. The README lists modules you can strip with a tool: *Appointments, EmailTemplates, Invoices, Locations, Payments, SamplePages, Services, Tags* (and a few others like About, Logo, and SamplePlugins). You remove what you don't need rather than building up from nothing.

**Plugins and the background service.** Two built-in extension points are worth naming early:

- **Plugins** let you add functionality without editing the core. The README names the types that ship out of the box: *"Auth", "BackgroundProcess", "Example", and "UserUpdate".* A plugin can be a normal `.cs` source file, or a `.plugin` file when including it as source would cause build conflicts (for example, when it depends on an external DLL).
- **The background service** runs periodic tasks on a timer — by default *"enabled and configured to run every 60 seconds"* — handling jobs like permanently purging records that were only soft-deleted after their retention period.

The takeaway: FreeCRM is a full, layered application, but it is built to be reshaped. The conventions in the next section are what make reshaping it safe.

<a id="house-conventions"></a>
## 4. House Conventions: The Rulebook

**Why it matters:** This is the pillar that newcomers underestimate and later regret ignoring. FreeCRM is a *framework you upgrade over time.* If your custom code is tangled into the framework's files, a future upgrade will overwrite it. The conventions exist so that never happens.

**The `.App.cs` convention — the one rule to internalize first.** Across the codebase you will find paired files: a core file and an `.App.cs` sibling. The `.App.cs` file is *yours.* It is where application-specific code lives, kept separate from the framework so the upgrade tool can leave it alone. Real examples in the repo include:

- `CRM.DataObjects\DataObjects.App.cs` — your custom data shapes
- `CRM.DataAccess\DataAccess.App.cs` — your custom business logic and app settings (app name, version, token lifetimes)
- `CRM\Controllers\DataController.App.cs` — your custom API endpoints
- `CRM\Program.App.cs`, `CRM.Client\Helpers.App.cs`, and more

This works because of a C# feature called a **partial class** — a single class whose definition is split across multiple files and merged by the compiler. The framework declares the class in one file; your `.App.cs` file adds to the *same* class without touching the framework's copy. You can see the pattern directly in the source. The framework-side class is declared `partial`:

```csharp
// DataAccess.cs (framework)
namespace CRM;

public partial class DataAccess: IDisposable, IDataAccess
```

…and your additions sit in the matching `.App.cs` file, which the comment explicitly reserves for you:

```csharp
// DataObjects.App.cs (yours)
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

Notice the commented-out `MyCustomUserProperty` line: the framework is literally showing you where to add your own field to the `User` object. The same "Use this file as a place to put any application-specific…" comment appears at the top of `DataAccess.App.cs` and `DataController.App.cs`. When in doubt about where your code goes, look for the `.App.cs` file.

**The three maintenance tools.** The FreeCRM README documents three standalone console utilities (they ship as `.exe` files in the solution's Solution Items folder). These *are* the conventions made executable:

| Tool | What it does | Example |
|------|--------------|---------|
| `Rename FreeCRM.exe` | Renames every project, namespace, and GUID from "FreeCRM" to your project name. | `"Rename FreeCRM.exe" MyNewProjectName` |
| `Remove Modules from FreeCRM.exe` | Strips optional modules you don't need (or keeps only the ones you name). | `keep:Tags` or `remove:all` |
| `Upgrade FreeCRM.exe` | Migrates your existing app onto a fresh copy of the framework. | `"Upgrade FreeCRM.exe" C:\MyExistingApp` |

The upgrade tool is the *reason* the `.App.cs` convention exists. The README's upgrade section says it plainly: it works for apps that *"have already migrated to use .app. files for all of your app-specific code."* In other words: keep your code in `.App.cs` files, and upgrading to a newer FreeCRM is a supported, repeatable process. Don't, and you are on your own to merge by hand.

**Other conventions you'll meet quickly:** a single shared `CRM` namespace; partial classes split by topic (the `CRM.DataAccess` project has files like `DataAccess.Users.cs`, `DataAccess.Invoices.cs`, `DataAccess.Tags.cs` — all the *same* `DataAccess` class, sliced by feature); and a parallel `DataController.*.cs` set on the API side. These topic-sliced files keep each piece small and findable. The deeper vocabulary is catalogued in [006 — Speaking the Local Dialect](006_local-dialect.md).

<a id="freetools-cli"></a>
## 5. FreeTools: The CLI

**Why it matters:** When you inherit or build a large Blazor app, you need to know *what is actually in it* — every page, how big each file is, which routes require login, and whether each page even renders. FreeTools answers those questions automatically so you don't have to read every file by hand.

**What it is.** From its overview, FreeTools is a *"comprehensive CLI toolset for analyzing, testing, and documenting Blazor web applications. It discovers routes, captures screenshots with smart SPA timing, tests endpoints, and generates detailed markdown reports."* It is currently **version 2.1**, built on .NET 10 and orchestrated by **Microsoft Aspire** (a framework for coordinating multi-service .NET apps). It is a *separate* toolset from FreeCRM — you point it at a Blazor project rather than embedding it.

**How you run it.** A single command kicks off the whole pipeline:

```bash
# Run against your project (must be a sibling folder to FreeTools)
dotnet run --project FreeTools.AppHost -- --target YourProjectName
```

`FreeTools.AppHost` is the Aspire entry point that orchestrates everything; `--target` names the project folder to analyze (it defaults to the bundled `BlazorApp1` sample).

**The pipeline, phase by phase.** FreeTools is not one program but a coordinated set of small console tools that run in dependency order:

| Phase | Tool | Output (plain terms) |
|-------|------|----------------------|
| 1 (static) | `FreeTools.EndpointMapper` | Scans `@page` directives → `pages.csv` (every route, plus which need `[Authorize]`) |
| 1 (static) | `FreeTools.WorkspaceInventory` | File metrics (size, lines, type) → `workspace-inventory.csv` |
| 2 (HTTP) | `FreeTools.EndpointPoker` | Sends GET requests to each route → `*.html` snapshots |
| 3 (visual) | `FreeTools.BrowserSnapshot` | Drives a real browser via Playwright → `*.png` + `metadata.json` |
| 4 (report) | `FreeTools.WorkspaceReporter` | Aggregates everything → a Markdown `Report.md` |

A shared library, `FreeTools.Core`, holds the common helpers (CLI argument parsing, route parsing, path sanitizing) the tools reuse.

**What "v2.1" added** — worth knowing because the names show up in output: *smart SPA timing* (waits for the page to go network-idle instead of just "loaded," which matters for Blazor's single-page-app behavior), an *auto-retry* for blank screenshots under 10 KB, *console error capture* for JavaScript errors, per-screenshot `metadata.json` files, and a *Screenshot Health* section in the report that flags failures and errors.

**Where output lands.** Reports are filed by project and git branch, so runs don't clobber each other:

```
Docs/runs/{ProjectName}/{BranchName}/latest/
    ├── pages.csv
    ├── workspace-inventory.csv
    ├── {ProjectName}-Report.md
    └── snapshots/...
```

The generated report is the payoff: a route map, code-distribution charts, "largest files" lists, large-file warnings tuned to be LLM-friendly (it flags files over ~450 lines), a screenshot gallery, and the screenshot-health summary.

<a id="how-interlock"></a>
## 6. How They Interlock

**Why it matters:** The three pillars are most useful as a loop, not as isolated parts. Seeing the loop tells you the natural order of operations for any new project.

A typical lifecycle ties all three together:

1. **Start from the framework (Pillar 1).** Clone FreeCRM. It gives you a working, layered CRM with authentication, a database, plugins, and a background service already in place.
2. **Make it yours by following the conventions (Pillar 2).** Run `Rename FreeCRM.exe` to take on your own name and namespace, run `Remove Modules from FreeCRM.exe` to drop modules you don't need, then write your custom code exclusively in the `.App.cs` files. Now your work is cleanly separated from the framework.
3. **Inspect and document with the CLI (Pillar 3).** Point FreeTools at your app. It enumerates every route, screenshots each page, flags oversized files, and writes a report — giving you (and any AI assistant or teammate) an accurate map of what you actually built.
4. **Upgrade safely (back to Pillars 1 + 2).** When a new FreeCRM ships, run `Upgrade FreeCRM.exe`. Because step 2 kept your code in `.App.cs` files, the upgrade tool can lift it onto the fresh framework. Re-run FreeTools to confirm nothing regressed.

The reinforcing relationships in one breath: **the framework defines the structure; the conventions protect your additions to that structure across upgrades; the CLI verifies and documents the result.** Each pillar makes the next one safer to use. Skip the conventions and the upgrade path breaks; skip the CLI and you are upgrading blind.

<a id="pitfalls"></a>
## 7. Common Confusions and Pitfalls

**Why it matters:** Almost every early mistake here is preventable with one fact you already learned above. This section is the cheat sheet.

- **Confusing FreeCRM with FreeTools.** They share a "Free" prefix and a documentation style, but FreeCRM is the *application you ship* and FreeTools is a *separate inspection CLI.* They live in different repositories. You don't deploy FreeTools to customers.
- **Editing a framework file instead of its `.App.cs` sibling.** If you add a property to `DataObjects.cs` instead of `DataObjects.App.cs`, or an endpoint to a core `DataController.*.cs` instead of `DataController.App.cs`, the next `Upgrade FreeCRM.exe` can overwrite your change. Rule of thumb: *if it's your code, it belongs in an `.App.cs` file.*
- **Expecting `partial class` to mean "a separate class."** Files like `DataAccess.Users.cs` and `DataAccess.Invoices.cs` are all the *same* `DataAccess` class, split for readability. Don't go looking for many different classes — look for many slices of one.
- **Renaming by hand.** Trying to find-and-replace "FreeCRM" yourself will miss project GUIDs and break the solution. Use `Rename FreeCRM.exe`, which handles project files, GUIDs, and namespaces together.
- **Deleting modules by deleting files.** Use `Remove Modules from FreeCRM.exe` (`remove:` / `keep:` / `remove:all`). The README warns it *"may still leave remnants,"* but it is far safer than manual deletion — and remnants can be reported as issues.
- **Running FreeTools from the wrong place.** Your target project must be a *sibling folder* to FreeTools and named via `--target`. If you run it without that layout, it analyzes the bundled `BlazorApp1` sample instead of your app.
- **Ignoring large-file warnings.** When the report flags files over ~450 lines, that isn't just style — those files are harder for both humans and AI tools to work with. Treat the warning as a refactor hint.

<a id="next-steps"></a>
## 8. Where to Go Next

**Why it matters:** You now have the map. The natural next move is to make the framework build and log in, then learn the vocabulary so the rest of the guides read smoothly.

- **Get it running.** Head to [003 — Clone, Build, Seed, and Sign In](003_zero-to-login.md) to take FreeCRM from a fresh clone to a working login. The InMemory database option (Section 3) makes a first run quick.
- **Learn the vocabulary.** Skim [006 — Speaking the Local Dialect](006_local-dialect.md) for the precise meaning of `.App.cs`, partial classes, DataObjects, DataAccess, plugins, and the other terms you'll see everywhere.
- **Decide if it fits.** If you're still evaluating whether to adopt this framework at all, read [081 — The Fit Test: Is This Framework Right for Us?](081_is-it-for-us.md).
- **Keep this doc as your map.** When you feel lost about *which* pillar a problem belongs to, return to Section 6's lifecycle loop — it almost always tells you where to look.

---

<a id="related-docs"></a>
## 9. Related Docs

- [006 — Speaking the Local Dialect](006_local-dialect.md) — the vocabulary every term links from
- [003 — Clone, Build, Seed, and Sign In](003_zero-to-login.md) — next: get it building and logged in
- [081 — The Fit Test: Is This Framework Right for Us?](081_is-it-for-us.md) — is this framework even right for us

---
*GuidesV2 · 001 · drafted from source (FreeCRM `README.md` / `CRM.slnx` / project files and FreeTools `000_overview.md`).*

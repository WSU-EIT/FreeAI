# 084 — Riding the Framework Forward

> **Document ID:** 084  ·  **Category:** Operations  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Walk through taking a framework upgrade safely using the extension model and upstream-sync discipline.
> **Audience:** Operators and maintainers  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 08x (Operate, Deploy, and Steward) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Upgrades Matter](#why-it-matters) | What "framework," "extension model," and "upstream sync" mean, and why upgrading is a feature not a chore |
| 2 | [Should You Upgrade Now?](#decide-to-upgrade) | The triggers, timing, and the one hard precondition |
| 3 | [The Extension Model](#extension-model) | How `.App.` files keep your code out of the framework's way |
| 4 | [Pre-Upgrade Checklist](#pre-upgrade) | Backups, branches, and readiness checks before you start |
| 5 | [Running the Upstream Sync](#upstream-sync) | The exact download → Rename → Remove Modules → Upgrade procedure |
| 6 | [Verifying the Upgrade](#verify) | Reading the report, building, and exercising your hooks |
| 7 | [Rolling Back Safely](#rollback) | Reverting cleanly when an upgrade goes wrong |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Upgrades Matter

**Why it matters first:** the whole point of building on FreeCRM is that someone else maintains the engine for you — fixing bugs, closing security holes, and adding features. But you only get that benefit if you can actually *take* their new versions. An app that can never upgrade is an app slowly rotting on an old release. This document is about making "take the new version" a routine you run on purpose, not a crisis you survive.

Three terms you will see throughout, in plain language:

- **Framework.** FreeCRM is not just an app you run — it is a *framework*, meaning a base of code you build *on top of*. It is an open-source CRM written in C# with Blazor WebAssembly on .NET 10. ("Blazor WebAssembly" just means the user interface is written in C# and runs inside the browser.) The framework is the part the original authors own and keep improving.

- **Extension model.** The set of rules that lets your custom code and the framework's code live side by side without colliding. In FreeCRM, that model is built on **partial classes** (a C# feature that lets one class be split across several files) plus a file-naming convention: your code goes in files with `.App.` in the name, the framework's code goes in files without it. Because your half and the framework's half are always in *separate files*, the framework can replace its files wholesale and never touch yours. (The full theory is [041 — Code the Framework Can Update Underneath](041_upgrade-safe-model.md).)

- **Upstream sync.** "Upstream" is the original source of the framework — the repository you forked from. "Syncing upstream" means pulling that newer code down into your copy. An *upgrade* is what happens when you fold a newer framework version into your existing app. There are two ways to do it, and this doc covers both: a Git-level merge from the upstream repository (the day-to-day way), and a one-shot console tool for older apps that need a bigger jump (the heavy-lifting way).

The headline insight: **upgrading is safe by design, but only if you have held up your end of the extension model.** FreeCRM even ships a dedicated console application, `Upgrade FreeCRM.exe`, whose entire job is to migrate an existing app onto a newer framework version. Its README states the precondition in one breath:

> "If you are running an older version of an application based on the FreeCRM framework and you have already migrated to use .app. files for all of your app-specific code, then you can use the new 'Upgrade FreeCRM.exe' console application to upgrade your existing application."

Read that twice. The tool only works *if your app-specific code is already in `.App.` files*. That single sentence is the reason the extension model exists. The rest of this doc assumes you have followed it.

---

<a id="decide-to-upgrade"></a>
## 2. Should You Upgrade Now?

**Why it matters:** an upgrade is cheap when you do it often and expensive when you do it rarely. Skipping versions lets your copy *drift* — the gap between your code and upstream grows until a once-trivial merge becomes a week of conflict-resolution. The decision is less "should I ever upgrade" and more "am I keeping the gap small."

**Take an upgrade when any of these is true:**

- **A security or correctness fix lands upstream.** This is non-negotiable — a known vulnerability in the framework is your vulnerability too.
- **You want a feature the framework just added** rather than re-building it yourself.
- **Routine cadence.** The upstream repo gets cleaned up and improved continuously (the source research records a sweeping 187-file style-and-cleanup pass in a single commit), so small, frequent syncs keep your diff readable. Pulling monthly is far easier than pulling yearly.
- **Before a big platform jump,** like a .NET major-version move, you want to already be close to upstream so the jump is the only moving part.

**Hold off (briefly) when:**

- **You are mid-feature on framework-adjacent code.** Finish and commit your work first so a merge has a clean baseline to land on.
- **You cannot test right now.** Never upgrade something you cannot immediately build and smoke-test (see §6).

**The one hard precondition — not optional:** your app-specific code must already live in `.App.` files. If you have edits sitting inside framework files (anything *without* `.App.` in the name), stop and re-home them first. An upgrade is allowed to overwrite framework files, so any custom logic stranded there will simply vanish. The console upgrade tool's README warns that running it against an app that has *not* migrated to `.App.` form is exactly the unsupported case. Getting your code into `.App.` shape is the real work of "deciding to upgrade"; once that is true, the upgrade itself is mechanical.

---

<a id="extension-model"></a>
## 3. The Extension Model

**Why it matters:** this is the machinery that makes an upgrade non-destructive. If you understand nothing else before upgrading, understand that **every file with `.App.` in its name is yours and survives; every file without it is the framework's and may be replaced.** That single rule is what lets you swap the engine without losing your bodywork.

**How it works.** A C# `partial class` can be split across multiple files; the compiler stitches them back into one class at build time. FreeCRM uses this to give every customizable area two halves living in two files with two owners:

```
CRM.DataAccess\DataAccess.cs            <- framework-owned (upgrade may overwrite)
CRM.DataAccess\DataAccess.Users.cs      <- framework-owned (upgrade may overwrite)
CRM.DataAccess\DataAccess.App.cs        <- YOURS (upgrade never touches)
```

All three compile into one `class DataAccess`. The framework writes most of it; you write `DataAccess.App.cs`. Because your half is in a separate physical file, "replace the framework's files" and "keep the user's files" are two operations that never overlap.

**The pairs you will actually deal with.** In the current source tree, the `.App.` files are:

| Your file | What it holds |
|---|---|
| `CRM.DataAccess\DataAccess.App.cs` | App identity, app-specific data methods, the `...App` hooks |
| `CRM.DataObjects\DataObjects.App.cs` | Custom fields grafted onto shared data objects (DTOs) |
| `CRM.DataObjects\ConfigurationHelper.App.cs` | App configuration overrides |
| `CRM.DataObjects\GlobalSettings.App.cs` | App-wide settings |
| `CRM\Program.App.cs` | Startup/hosting hooks (`AppModifyBuilderStart/End`, `AppModifyStart/End`) |
| `CRM\Controllers\DataController.App.cs` | App-specific API endpoints |
| `CRM.Client\DataModel.App.cs` | Browser-side data-model extensions |
| `CRM.Client\Helpers.App.cs` | Client helper overrides |
| `CRM.DataAccess\GraphAPI.App.cs`, `Utilities.App.cs`, `RandomPasswordGenerator.App.cs` | Other app-specific server code |

**Your app's identity lives here on purpose.** Open `DataAccess.App.cs` and you find the per-app constants — name, version, release date, token policy — sitting in *your* sheet so an upgrade can never reset them:

```csharp
// The name of the application.
private string _appName = "freeCRM";

// Indicates if the app uses data migrations. If false, you will manage your own database schema updates.
private bool _useMigrations = false;

// The version of your application.
private string _version = "2.0.0";
```

Those defaults (`"freeCRM"`, `"2.0.0"`) are the values you change to make the app yours — and after an upgrade, the fact that they are still *your* values is your fastest proof the right sheet survived (see §6).

For the full contract — the promises each side makes, the extension hooks the framework calls into, and the anti-patterns that quietly break a future upgrade — read [041 — Code the Framework Can Update Underneath](041_upgrade-safe-model.md). This doc takes that contract as given and focuses on the act of upgrading.

---

<a id="pre-upgrade"></a>
## 4. Pre-Upgrade Checklist

**Why it matters:** an upgrade rewrites files. If something goes wrong, your only real safety net is a clean, recoverable starting point. Five minutes of preparation turns "disaster" into "discard and retry." Work this list top to bottom before you touch anything:

1. **Commit or stash everything.** Your working tree should be clean — no uncommitted edits. ("Stash" is Git's term for setting changes aside temporarily.) An upgrade landing on top of half-finished work makes it impossible to tell what changed.

2. **Branch first; never upgrade on `main`.** Create a dedicated branch (for example `upgrade/2.1.0`) and do the upgrade there. If it goes badly, you delete the branch and your `main` is untouched. This is the cheapest rollback there is.

3. **Tag or note your current good state.** Record the exact commit you are upgrading *from* so you can return to it precisely. A Git tag like `pre-upgrade-2.0.0` makes the rollback in §7 a one-liner.

4. **Back up the database — and decide who owns the schema.** Check `_useMigrations` in `DataAccess.App.cs`. If it is `false`, *you* manage your own database schema (the inline comment says exactly this), so a framework upgrade will not migrate your tables — plan any schema changes yourself. Either way, back up the database before proceeding.

5. **Confirm the precondition from §2:** every piece of your custom code is in a `.App.` file. Quickly scan the framework files you suspect you may have touched; anything custom found there must move to its matching `.App.` file *now*, before the upgrade overwrites it.

6. **Know your modules.** Note which optional modules your app keeps. The optional modules the framework can add or remove are: About, Appointments, EmailTemplates, Invoices, Locations, Logo, Payments, SamplePages, SamplePlugins, Services, and Tags. You will need this list in §5 to keep the upgraded copy shaped like your app.

7. **Make sure you can build and run today.** Establish a known-good baseline *before* changing anything, so that if the build breaks after the upgrade you know the upgrade caused it.

Green on all seven means you have a clean tree, a throwaway branch, a tagged escape hatch, a database backup, and confidence that your code is all in `.App.` files. Now you can upgrade.

---

<a id="upstream-sync"></a>
## 5. Running the Upstream Sync

There are two routes. Pick based on how far behind you are.

### Route A — The Git merge (routine, small gaps)

For day-to-day upgrades, you keep your fork synchronized with the upstream repository using Git. "Upstream" is the original FreeCRM repository you forked from; your fork periodically merges its `main` branch. The mechanics — adding the upstream remote, fetching, merging, and resolving conflicts — are a discipline in their own right, covered step by step in [054 — Living on a Fork: Staying in Sync Upstream](054_fork-sync-discipline.md). The short version:

1. Fetch the latest from the upstream remote.
2. Merge upstream's `main` into your upgrade branch.
3. Resolve conflicts — which, if you have honored the extension model, should only ever appear in framework files, never in your `.App.` files.
4. Build, then proceed to verification (§6).

Because your code is segregated into `.App.` files, conflicts land in framework code you do not own, so resolving them usually means "take upstream's version." This is why a small, frequent merge is almost painless.

### Route B — The Upgrade console tool (big jumps, older apps)

When you are far behind — for example, an older app jumping several versions — FreeCRM ships a console application, `Upgrade FreeCRM.exe`, that migrates your existing code into a fresh copy of the new framework. (It is a compiled tool shipped alongside `Rename FreeCRM.exe` and `Remove Modules from FreeCRM.exe` in the solution's root.) The procedure, straight from the README, runs in four ordered steps:

1. **Download a fresh copy** of the new FreeCRM files. This is the clean target you will migrate *into*.

2. **Rename it to match your app exactly.** Run `Rename FreeCRM.exe` so the fresh copy uses the *exact same namespace* as your existing application — this is what lets the tool line your code up with the framework's:

   ```
   "Rename FreeCRM.exe" MyNewProjectName
   ```

3. **Remove the modules you do not use,** so the fresh copy is shaped like your app. Use the module list you noted in §4:

   ```
   "Remove Modules from FreeCRM.exe" keep:Tags
   ```

   (`keep:` removes everything *not* named; `remove:Module1,Module2` removes the named ones; `remove:all` removes every optional module. No spaces in the options.)

4. **Run the upgrade,** pointing it at your existing application's folder. The tool takes a single command-line argument — the path to your existing app — and attempts to migrate your code into the new version:

   ```
   "Upgrade FreeCRM.exe" C:\MyExistingApp
   ```

**What it does and does not handle.** The tool migrates the code it can and then hands you a written report of what it could not finish. The README is explicit about its known limits:

> "There are edge cases that cannot be updated with this tool, such as having additional projects in your solution. The tool will copy those projects, but any references in other projects must be added manually."

So extra projects are copied but their cross-project references are *your* job to re-add. Treat the tool as doing the bulk move, and its report (covered next) as your checklist for the remainder.

---

<a id="verify"></a>
## 6. Verifying the Upgrade

**Why it matters:** an upgrade that builds is not the same as an upgrade that *works*. You need fast, concrete proof that the framework's half was swapped and *your* half rode along intact. Work down this list:

1. **Read the tool's report first (Route B).** The upgrade tool produces a report by design — the README: *"The tool will produce a report that will help with any additional steps that will be required for the migration."* Every item in it (extra-project references, edge cases) is required follow-up, not a suggestion.

2. **Confirm your app identity survived.** The single fastest tell that the right sheet won: open `DataAccess.App.cs` and check that `_appName` and `_version` are still *your* values, not back to the framework defaults `"freeCRM"` / `"2.0.0"`. If they reverted, your `.App.` file was overwritten — stop and investigate before anything else.

3. **Confirm no edits got stranded in framework files.** Diff your old framework files against the new ones. Any place the diff shows *your* logic disappearing is a spot where you had wrongly edited a framework file; re-home that logic into the matching `.App.` file now.

4. **Build the solution.** A clean compile proves the two halves still fuse — same `partial`, same namespace, no duplicate or missing members. A complaint about a missing seam method or interface member means your `.App.` sheet drifted from the new framework's expected shape; reconcile the signature against the new framework half.

5. **Exercise every hook you implemented.** Run the app and trigger the paths that call into your sheet: load and save a record, delete one, let the background service tick (the `ProcessBackgroundTasksApp` method in `DataAccess.App.cs`), and open any page whose `.App.razor` component you customized. If your customizations still appear and behave, the contract held.

6. **Smoke-test startup customizations.** If you added services or middleware via the `AppModifyBuilderStart/End` or `AppModifyStart/End` hooks in `Program.App.cs`, confirm they still register and the app starts cleanly.

Green across all six means the framework's sheet was swapped, your sheet rode along, and you are back on the upgrade path for next time. Now you can merge the upgrade branch into `main` and deploy ([083 — Shipping It to Production](083_deployment-shapes.md)).

---

<a id="rollback"></a>
## 7. Rolling Back Safely

**Why it matters:** the bravery to upgrade comes entirely from knowing you can undo it. If verification (§6) fails in a way you cannot quickly fix, do not try to hand-patch a broken upgrade under pressure — roll back to your known-good state and try again deliberately.

This is exactly why §4 told you to branch and tag. Rolling back is then trivial:

- **You upgraded on a throwaway branch.** Just switch back to `main` and delete the upgrade branch. Your production code never changed — there is nothing to undo.

  ```
  git switch main
  git branch -D upgrade/2.1.0
  ```

- **You tagged the pre-upgrade commit.** If you need to return your branch to the exact starting point, reset to the tag you made in §4 (`pre-upgrade-2.0.0`). Because you committed everything beforehand, nothing of yours is lost.

- **You ran the console tool (Route B) into a fresh copy.** The tool migrates *into a new directory*, so your original app under `C:\MyExistingApp` is still sitting there untouched — rolling back is as simple as continuing to run the original and discarding the new copy.

- **Database safety.** If the upgrade touched schema or data, restore from the backup you took in §4. Remember that with `_useMigrations = false` you own the schema, so an upgrade should not have altered your tables unless you changed them yourself — but the backup is your guarantee either way.

After a rollback, diagnose calmly: most failed upgrades trace back to custom logic that was *not* yet in `.App.` files. Re-home it (§3), then re-run the upgrade from a clean branch. The rollback cost you nothing because you prepared — which is the entire reason the checklist comes first.

---

<a id="related-docs"></a>
## 8. Related Docs

- [041 — Code the Framework Can Update Underneath](041_upgrade-safe-model.md) — the extension model that makes it safe
- [054 — Living on a Fork: Staying in Sync Upstream](054_fork-sync-discipline.md) — the upstream-sync discipline
- [083 — Shipping It to Production](083_deployment-shapes.md) — deploying the upgraded build

---
*GuidesV2 · 084 · drafted from source 2026-06-05 · grounded in FreeCRM README "Upgrade" section, `DataAccess.App.cs`, the `.App.` file inventory, and the upgrade-safe model in 041.*

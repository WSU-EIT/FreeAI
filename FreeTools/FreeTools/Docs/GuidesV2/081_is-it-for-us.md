# 081 — The Fit Test: Is This Framework Right for Us?

> **Document ID:** 081  ·  **Category:** Operations  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Plain-language account of what FreeCRM does and does not solve, with learning curve and lock-in risks named honestly.
> **Audience:** Evaluators and decision-makers  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 08x (Operate, Deploy, and Steward) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-it-matters) | What "fit," "lock-in," and "learning curve" mean, and why honesty beats a sales pitch |
| 2 | [What FreeCRM Solves Well](#good-fit) | The problems it removes and the teams it fits cleanly |
| 3 | [What It Does Not Solve](#poor-fit) | Gaps, anti-patterns, and the teams it will fight |
| 4 | [The Learning Curve, Honestly](#learning-curve) | The skills your team needs and how long the ramp really is |
| 5 | [Lock-In and Exit Risks](#lock-in) | What it costs to leave, and what you actually own |
| 6 | [The Fit Decision Checklist](#decision) | Yes/no questions that produce a clear go / no-go |
| 7 | [Verify and Pilot Before Committing](#verify-pilot) | A cheap one-afternoon test of fit before you commit |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why This Matters

Picking a software foundation is a one-way-ish door. Once your team has built features on top of something, ripping it out is expensive — so the cheapest moment to be honest about fit is *before* you start. This doc is that honesty check.

Three terms drive the whole decision, so let's define them in plain language first:

- **Fit** — does this tool match the shape of *your* problem? A great tool used for the wrong job is still the wrong tool. Fit is not "is it good," it's "is it good *for us*."
- **Learning curve** — how long, and how painful, is the gap between "we installed it" and "the team is productive with it." A short curve means you ship sooner; a long curve means real calendar time spent learning before any value appears.
- **Lock-in** — how hard it is to *leave* later. Lock-in has two flavors: your **data** being trapped in a format only this tool reads, and your **code** being so wedded to the framework's way of doing things that you can't extract it. Low lock-in means you keep your options.

FreeCRM is described by its own README as "an open-source CRM solution built in C# Blazor WebAssembly using .NET 10," with an explicit invitation to "use this project as-is, or customize it to fit your needs. Or, just grab code that you need to use in your project." That last sentence is the most honest thing in the README: this is a **starting codebase you own**, not a hosted product you rent. That single fact changes every answer below — it means *you* hold the source, *you* run it, and *you* are responsible for it. The rest of this doc tells you when that's a gift and when it's a burden.

---

<a id="good-fit"></a>
## 2. What FreeCRM Solves Well

FreeCRM is a strong fit when these things are true. Each item below is grounded in something that actually ships in the source, not a wish list.

**You want to own the source, not rent a SaaS.** This is a full application you compile and host yourself. There is no per-seat subscription, no vendor account, no "talk to sales." If owning the code and the database matters to you — for cost, control, or compliance reasons — this is the core reason the project exists.

**Your team lives in the Microsoft / .NET world.** The whole stack is C# on **.NET 10**. The server (`CRM.csproj`) is an ASP.NET Core web app; the browser UI (`CRM.Client.csproj`) is **Blazor WebAssembly** — meaning the user interface is written in C# and runs *inside the browser* as WebAssembly, so your team writes one language end to end instead of C# on the server and JavaScript on the front end. If your developers already know C#, the ramp is dramatically shorter.

**You haven't committed to one database yet — or you need to support several.** The framework supports five database engines out of the box, chosen by a single `DatabaseType` setting in `appsettings.json`. The real code that switches on it lives in `DataAccess.cs`:

```csharp
switch (_databaseType.ToLower()) {
    case "inmemory":   optionsBuilder.UseInMemoryDatabase("InMemory"); ...
    case "mysql":      optionsBuilder.UseMySQL(_connectionString, ...);
    case "postgresql": optionsBuilder.UseNpgsql(_connectionString, ...);
    case "sqlite":     optionsBuilder.UseSqlite(_connectionString);
    case "sqlserver":  optionsBuilder.UseSqlServer(_connectionString, ...);
}
```

That means you can develop against **InMemory** (a fake database that lives only in RAM — zero setup, perfect for a quick spin) or **SQLite** (a single file on disk) and later point production at **SQL Server**, **MySQL**, or **PostgreSQL** by changing one config value. Not many starter kits give you that flexibility for free.

**You serve multiple customers or business units from one app.** FreeCRM is **multi-tenant** — "tenant" being the industry word for a separate, walled-off customer whose data never bleeds into another customer's. The concept is baked into the data model (entities carry a `TenantId`) and into the API: the `DataController` reads the tenant from the request header or query string on every call. If you're building a product that several organizations will log into, this is a major head start.

**You want a modular CRM, not a monolith you can't trim.** The optional features — Appointments, Email Templates, Invoices, Locations, Payments, Services, Tags, and more — can be removed wholesale with the bundled "Remove Modules from FreeCRM.exe" tool (`keep:Tags`, `remove:all`, etc.) and can also be turned off at runtime via the `GloballyDisabledModules` list in `appsettings.json`. You start with a lot and subtract what you don't need.

**You need to extend it without forking it.** FreeCRM ships a **plugin architecture** — a way to add your own behavior in separate files instead of editing the core. The README lists the built-in plugin types: `"Auth"`, `"BackgroundProcess"`, `"Example"`, and `"UserUpdate"`. There's also a **background service** (a timer that runs housekeeping tasks every 60 seconds by default) you can hook into. So routine "run this on a schedule" and "react when a user logs in" needs have a designed home.

**You need authentication handled already.** Sign-in via Apple, Facebook, Google, Microsoft Account, and any generic **OpenID** provider is wired up in the server project and configured through the `AuthenticationProviders` section of `appsettings.json`. You are not building login from scratch.

In short: FreeCRM fits a small-to-mid .NET team that wants to *own* a multi-tenant, multi-database CRM, customize it heavily, and avoid a SaaS bill.

---

<a id="poor-fit"></a>
## 3. What It Does Not Solve

Honesty cuts both ways. Here is where FreeCRM is the wrong tool or will fight you.

**It is not a hosted, zero-ops product.** There is no managed cloud you sign up for. *You* deploy it, patch it, back up the database, secure the server, and watch the logs. If your organization has no one who can own a running .NET application, "free" becomes expensive fast — the cost just moves from a subscription line to your people's time.

**It is not for non-.NET shops.** Everything is C# and the .NET 10 SDK. A team that lives in Python, Node, Java, or PHP would be adopting an entire runtime and language alongside the app. That's a strategic commitment, not a quick win. Don't pick this to "save time" if your team would first have to learn C#.

**It is not a drop-in library or microservice.** This is a whole application with a server, a WebAssembly client, a data-access layer, EF Core models, and a plugins project all wired together. You can "grab code you need" as the README suggests, but you cannot `npm install` a slice of it. Adopting it means adopting the application's structure.

**It is not a no-code / admin-configures-everything tool.** Meaningful customization happens in source code — plugins, the `DataAccess.App.cs` extension points, Blazor components. A business analyst can toggle modules and settings, but reshaping behavior requires a developer. If you wanted Salesforce-style point-and-click admin configuration, this is not that.

**It is not a turnkey enterprise CRM with a vendor behind it.** There is no support contract, no SLA, no account manager, no certified-integrations marketplace. If your procurement or risk team requires "a vendor we can call at 2 a.m.," an open-source codebase you maintain yourself will not satisfy that box on its own.

**It is not pre-tuned for extreme scale.** The provider setup enables sensible retry-on-failure for the SQL databases, but performance at very high tenant counts or request volumes is your engineering problem to measure and tune, not a guarantee that ships in the box. Treat scale as something to *pilot* (see §7), not assume.

**Anti-pattern to avoid:** adopting FreeCRM and then editing the core files everywhere instead of using plugins and the `.app.` extension points. Teams that do this make every future upgrade painful — which leads straight into the lock-in discussion.

---

<a id="learning-curve"></a>
## 4. The Learning Curve, Honestly

"Learning curve" here means: from a standing start, how long until your team is genuinely productive, and what do they need to know to get there?

**The skills your team actually needs:**

- **C# and .NET 10** — non-negotiable. This is the floor.
- **Blazor (especially Blazor WebAssembly)** — the UI framework. If your developers know C# but not Blazor, expect a real but manageable ramp; Blazor is "C# instead of JavaScript in the browser," so the concepts are familiar even if the framework is new.
- **Entity Framework Core** — the library that maps C# objects to database tables, so developers rarely write raw SQL. Comfort here matters because the entire data layer is EF Core.
- **Basic web hosting** — how to deploy and run an ASP.NET Core app (IIS, a container, or a cloud host). The README even includes IIS guidance for keeping the background service alive, which tells you the project expects you to handle hosting.

**Roughly how long the ramp is:**

- *Already a Blazor + EF Core .NET team:* you can be running locally in well under an hour. The InMemory database needs no connection string at all — the code literally hard-codes a placeholder for it — so "clone, set `DatabaseType` to `InMemory`, press F5" is a real first experience. Productive customization in days.
- *A C# team new to Blazor:* plan for a couple of weeks of genuine learning before the team is fluent enough to extend the UI confidently.
- *A team new to C# entirely:* this is a months-long language adoption, not a learning curve. Reconsider (see §3).

**Where the friction actually is.** The honest friction is not the everyday code — it's the *first-run and rename steps*. To make the project your own, the README's intended path is: run "Rename FreeCRM.exe" to change the project name, namespaces, and GUIDs; run "Remove Modules from FreeCRM.exe" to trim features; configure your database and auth in `appsettings.json`; then build on top. None of these are hard, but they are unfamiliar one-time rituals, and skipping them (especially the rename) makes later upgrades messier. Budget an afternoon for a careful first setup rather than expecting an instant install.

**The good news on the curve:** because everything is one language and the project ships working examples (the `PluginFiles` folder includes `ExampleBackgroundProcess.cs`, `UserUpdate.cs`, a `HelloWorld` plugin, and several `Example*.cs` files), your team learns by copying patterns that already run, rather than from a blank page.

---

<a id="lock-in"></a>
## 5. Lock-In and Exit Risks

The fair question for any foundation is: "if this turns out wrong, how trapped are we?" FreeCRM scores well on the things you most worry about and asks for honesty on a couple of others.

**Your data is not trapped — this is the strongest exit story.** Because the data lives in a *standard* database engine of your choosing (SQL Server, MySQL, PostgreSQL, or SQLite) accessed through EF Core, your data is in an ordinary relational database that any tool on earth can read. There is no proprietary, undocumented data format. If you walk away tomorrow, you still have a normal database you can query, export, or migrate. That is roughly the opposite of SaaS data lock-in.

**Your code is genuinely yours.** It's open source and you hold the full source tree. No vendor can revoke access, change pricing, or sunset the product out from under you. You can fork it, freeze it, or rewrite it on your own schedule.

**The real lock-in is framework-shaped, and you control how deep it goes.** The honest risk isn't data — it's *coupling*. The more your custom logic edits FreeCRM's core files directly, the more your application *is* FreeCRM, and the harder it is to extract later or to absorb upstream updates. The project's design actively pushes you to avoid this: put custom behavior in **plugins**, put app-specific code in the **`.app.` extension points** (e.g., `ProcessBackgroundTasksApp` in `DataAccess.App.cs`), and you keep a clean seam between "the framework" and "your stuff."

**Upgrades are a managed risk, not a free lunch.** Because you own a copy, you don't get automatic updates — you pull a fresh version yourself. The project ships an "Upgrade FreeCRM.exe" tool to migrate an existing app onto a new release, and its instructions assume you used the rename tool and the `.app.` pattern. The README is candid that "there are edge cases that cannot be updated with this tool" (for example, extra projects you added) and that it "will produce a report" of manual steps. Translation: staying current is doable and tooling-assisted, but it is *your* periodic chore, and discipline now (plugins, `.app.`, a clean rename) directly lowers that future cost.

**Net assessment:** low data lock-in, low licensing lock-in, and *self-determined* framework lock-in. You largely decide how trapped you'll be by how cleanly you extend it.

---

<a id="decision"></a>
## 6. The Fit Decision Checklist

Work down this list. The pattern of answers gives you a clear go / no-go without needing an engineer to interpret it.

**Strong "go" signals — if most are yes, FreeCRM fits:**

- [ ] Our team knows C#/.NET, or is willing to commit to it as a platform.
- [ ] We want to **own and host** the code rather than rent a SaaS.
- [ ] We can staff someone to deploy, back up, and maintain a running .NET app.
- [ ] We need **multi-tenant** (many customers, isolated data) and/or flexibility across SQL Server / MySQL / PostgreSQL / SQLite.
- [ ] We expect to **customize** the CRM, not just configure it.
- [ ] Avoiding a per-seat subscription and keeping our data in a standard database matters to us.

**Strong "no-go" signals — if any are yes, stop and reconsider:**

- [ ] We have **no one** who can own a self-hosted .NET application.
- [ ] We need a **non-.NET** stack (Python/Node/Java/PHP) and won't change.
- [ ] We require a **vendor with support, SLAs, and a contract** to satisfy procurement or risk.
- [ ] We need **no-code admin configuration** with no developer involvement.
- [ ] We want a hosted product that **auto-updates** with zero maintenance on our side.

**How to read the result:** mostly "go" signals and no "no-go" signals → strong fit, proceed to the pilot in §7. Any single hard "no-go" → FreeCRM is likely the wrong foundation no matter how attractive the rest looks; the mismatch will dominate. A mixed picture → run the pilot specifically to test the items you're unsure about before committing budget.

---

<a id="verify-pilot"></a>
## 7. Verify and Pilot Before Committing

You can de-risk this entire decision in roughly an afternoon, for free, before anyone commits. The goal of a **pilot** is to convert opinions ("it looks fit") into evidence ("we ran it and saw it fit").

**Step 1 — Run it with zero infrastructure.** Pull the source, set `"DatabaseType": "InMemory"` in `appsettings.json` (no connection string needed — the code supplies its own placeholder for InMemory), and launch. This proves the project builds and runs on your machines and lets your developers click through the real UI. If this step is painful for your team, that *is* the fit signal.

**Step 2 — Make it yours.** Run "Rename FreeCRM.exe" with your project name and "Remove Modules from FreeCRM.exe" to drop modules you don't want (e.g., `keep:Tags`). This validates the customization rituals from §4 and surfaces any friction while the cost of finding out is zero.

**Step 3 — Point it at a real database.** Flip `DatabaseType` to `SQLite` (a single file, near-zero setup) or to the SQL Server / MySQL / PostgreSQL you'd actually use in production, and confirm it connects and creates its schema. This proves the data-portability story end to end on *your* infrastructure.

**Step 4 — Write one tiny plugin.** Copy `ExampleBackgroundProcess.cs` or `UserUpdate.cs` from the `PluginFiles` folder, change its message, and confirm your custom code runs without touching the core. This is the single most important test, because it proves the *low-lock-in extension path* (§5) works for your team. If your developers can add a plugin comfortably, the framework-coupling risk is under your control.

**Step 5 — Stress the one thing you're unsure about.** Pick your biggest open question from the §6 checklist — usually scale, a specific auth provider, or a particular database — and test *only that*. A pilot earns its keep by answering the question that would otherwise be an expensive surprise.

If all five steps pass, you have direct evidence of fit, learning curve, and exit options — and you've spent an afternoon, not a quarter.

---

<a id="related-docs"></a>
## 8. Related Docs

- [082 — One Tenant or Many](082_tenancy-topology.md) — the tenancy decision
- [066 — Explaining to the Intern-CTO](066_explain-to-the-cto.md) — explained for the decision-maker
- [087 — Trust and Trajectory](087_security-and-roadmap.md) — security and roadmap context

---
*GuidesV2 081 · drafted from source (FreeCRM README, `CRM.csproj`, `appsettings.json`, `DataAccess.cs`, `DataController.cs`, `EFDataModel.cs`) · 2026-06-05.*

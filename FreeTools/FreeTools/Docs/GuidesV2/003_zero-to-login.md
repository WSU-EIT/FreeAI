# 003 — Clone, Build, Seed, and Sign In

> **Document ID:** 003  ·  **Category:** Onboarding  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Restore, build, set up the database, seed a starter tenant, and land on a logged-in session.
> **Audience:** Brand-new adopters  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 00x (Landing Zone: From Clone to Login) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | The big picture and the key terms defined |
| 2 | [Before You Start](#before-you-start) | SDK, database engine, and the one config file that matters |
| 3 | [Clone and Restore](#clone-and-restore) | Getting the source and pulling its dependencies |
| 4 | [Build the Solution](#build-the-solution) | Compiling `CRM.slnx` for the first time |
| 5 | [Set Up the Database](#set-up-the-database) | Picking a provider and the connection string |
| 6 | [Seed a Starter Tenant](#seed-starter-tenant) | The auto-seed that creates tenants and the admin login |
| 7 | [Run and Sign In](#run-and-sign-in) | Launching the host and your first login |
| 8 | [Troubleshooting](#troubleshooting) | Fixing the common failures along the way |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

**Why it matters:** This is the doc that turns "I have a folder of code" into "I'm staring at a working app, logged in as an administrator." If you can get through these seven steps once, every later doc — building a feature, reading the tree, deploying — assumes you can get back here on demand. This is the heartbeat check.

The good news: FreeCRM does most of the heavy lifting for you. You do not hand-write a database schema, and you do not manually create a first user. The app creates its own database and seeds its own login data the first time it starts. Your job is mostly to point it at a database and press run.

A few terms, defined in plain language, because they show up everywhere from here on:

- **Clone** — making a local copy of the source code from its Git repository onto your machine. "Git" is the version-control system that tracks every change; "repository" (or "repo") is just the project's folder as Git stores it.
- **Restore** — downloading the third-party code libraries (called *NuGet packages*) that the project depends on, like Entity Framework Core. The project lists what it needs; restore goes and fetches them.
- **Build** — compiling the human-readable C# source into something the computer can actually run. If the build fails, nothing else works, so a clean build is your first milestone.
- **Tenant** — one isolated customer/organization inside the app. FreeCRM is *multi-tenant*, meaning a single running app can serve many separate organizations, each with its own users and data walled off from the others. Each tenant has a short **tenant code** (for example `admin` or `tenant1`) used in URLs and at login.
- **Seed** — pre-loading the database with the minimum starter data so the app is usable: at least one tenant and at least one administrator account you can log in with.
- **Session** — the logged-in state after you authenticate. Landing on a real session is the finish line for this doc.

The whole path is: clone → restore → build → point at a database → run → the app seeds itself → you log in.

<a id="before-you-start"></a>
## 2. Before You Start

**Why it matters:** Ninety percent of "it won't start" problems are a missing prerequisite, not a bug. Check these first.

You need three things on the machine:

1. **The .NET 10 SDK.** The *SDK* (Software Development Kit) is the toolbox that compiles and runs .NET code; it includes the `dotnet` command-line tool. FreeCRM targets `net10.0` across every project, so an older SDK will not build it. (Installing this is the entire job of doc 002 — do that first if you have not.)

2. **An editor (optional but recommended).** Visual Studio 2022/2026 or VS Code. You can do everything in this doc from a terminal with `dotnet`, but an editor makes step 5 and debugging easier.

3. **A database engine — or none at all.** FreeCRM supports five database back-ends, and you choose which one in config. They are, exactly as the code names them:
   - `SQLServer` — Microsoft SQL Server (the default in the shipped config; LocalDB or a local instance works fine for development).
   - `InMemory` — a throwaway database that lives only in RAM. **This is the zero-install option.** No database engine to set up; data vanishes when the app stops. Perfect for a first run.
   - `SQLite` — a single-file database, no server needed.
   - `MySQL`
   - `PostgreSQL`

The one file that controls all of this is **`FreeCRM/CRM/appsettings.json`** — the host project's configuration file. Two settings in it matter for this doc: `DatabaseType` (which provider) and `ConnectionStrings:AppData` (where the database lives). We come back to them in step 5.

If you want the absolute fastest path with nothing else installed, plan to use `InMemory`. If you want data that persists, use `SQLServer` or `SQLite`.

<a id="clone-and-restore"></a>
## 3. Clone and Restore

**Why it matters:** You cannot build code you do not have, and you cannot build it without its dependencies. This step gets both.

**Clone the repository.** From a terminal, in whatever parent folder you want the code to live:

```
git clone <your-FreeCRM-repo-url> FreeCRM
cd FreeCRM
```

After this you should see the solution file **`CRM.slnx`** in the folder, alongside project folders like `CRM`, `CRM.Client`, `CRM.DataAccess`, and `CRM.EFModels`. (`.slnx` is the newer XML-based Visual Studio solution format; it groups all the projects so they build together.)

**Restore the dependencies.** A *restore* pulls down every NuGet package the projects reference — Entity Framework Core, the database providers, the Blazor and authentication packages, and so on:

```
dotnet restore CRM.slnx
```

You do not strictly need to run this by hand — `dotnet build` and `dotnet run` restore automatically — but running it on its own is a clean way to confirm your SDK can reach the package feed before you try to compile. If restore fails here, it is almost always a network/feed problem, not a code problem.

<a id="build-the-solution"></a>
## 4. Build the Solution

**Why it matters:** A clean build proves the source and your toolchain agree. Everything downstream (running, seeding, logging in) assumes the binaries exist.

From the repository root:

```
dotnet build CRM.slnx
```

The build compiles every project in dependency order. A couple to know by name, because their roles come up later:

- **`CRM.EFModels`** holds the *Entity Framework Core* model — `EFDataModel` (a `DbContext`, which is EF's gateway object to the database) plus one C# class per table (`User`, `Tenant`, `Appointment`, and so on). This project defines the shape of the database.
- **`CRM.DataAccess`** is the data layer that opens the database, creates it if needed, and seeds it.
- **`CRM`** is the ASP.NET Core host — the web server you actually run. Its SDK is `Microsoft.NET.Sdk.Web`, so it produces a runnable web application.

You are looking for `Build succeeded` with zero errors. Warnings are fine for now. If the build fails on a missing SDK or wrong framework version, stop and fix that in doc 002 — do not try to work around it.

<a id="set-up-the-database"></a>
## 5. Set Up the Database

**Why it matters:** The app needs to know *which* database engine to talk to and *where* it is. This is the single most common place a first run goes sideways — and also the place where FreeCRM is friendliest, because it builds the schema for you.

Open **`FreeCRM/CRM/appsettings.json`**. Two keys do the work.

**`DatabaseType`** picks the provider. The shipped default is SQL Server; the other options are commented out right above it:

```json
//"DatabaseType": "InMemory",
//"DatabaseType": "MySQL",
//"DatabaseType": "PostgreSQL",
//"DatabaseType": "SQLite",
"DatabaseType": "SQLServer",
```

**`ConnectionStrings:AppData`** is the *connection string* — the address-and-credentials line the app uses to reach the database. The file ships with one example per provider and the SQL Server one active:

```json
"ConnectionStrings": {
  "AppData": "Data Source=(local);Initial Catalog=CRM;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
}
```

Pick your path:

- **Easiest first run — `InMemory`.** Set `"DatabaseType": "InMemory"`. With InMemory you do not need a connection string at all (the app supplies one internally), and you do not need any database engine installed. The trade-off: the data is wiped every time the app stops.
- **Persistent local — `SQLServer`.** Leave the default. `Data Source=(local)` points at a SQL Server instance on your machine; `Initial Catalog=CRM` is the database name. You do **not** need to create the `CRM` database first — see the next paragraph.
- **Persistent, no server — `SQLite`.** Set `"DatabaseType": "SQLite"` and a connection string like `"Data Source=C:\\Working\\CRM.db"`. That `.db` file is created for you.

Here is the part that saves you the most work. FreeCRM uses a **database-first** approach with the schema created automatically by the app, *not* EF migrations. In `CRM.DataAccess` the flag `_useMigrations` is set to `false` (in `DataAccess.App.cs`), and at startup the data layer calls Entity Framework's `EnsureCreated()`:

```csharp
// from DataAccess.cs — on first startup, if the database isn't there yet
data.Database.EnsureCreated();
```

`EnsureCreated()` means: if the database does not exist, build it now from the model. So you generally do **not** run any `dotnet ef` or `add-migration` / `update-database` commands to get started — you just point the app at an empty (or non-existent) database and run it. (The migration tooling described in `CRM.EFModels/Setup.txt` is only for maintainers shipping coded migrations to customers; new adopters skip it entirely.)

If you launch the app and it cannot reach the database at all, it does not crash blindly — it routes you to a built-in **setup wizard** (the `Setup` page, backed by `SetupController`) where you can fill in the provider and connection details from the browser, and it writes them back into `appsettings.json` for you.

<a id="seed-starter-tenant"></a>
## 6. Seed a Starter Tenant

**Why it matters:** A freshly created database is empty. Without a tenant and a user, there is nothing to log in to. FreeCRM handles this automatically — you do not write any seed scripts — but you should understand what gets created so you know which credentials to type.

Right after the database is created (or connected to), the data layer calls `SeedTestData()` once at startup. That method is in **`CRM.DataAccess/DataAccess.SeedTestData.cs`**, and it guarantees a known starting state. Here is what it creates, taken straight from the code:

**Two tenants:**

| Tenant code | Name | Notes |
|-------------|------|-------|
| `admin` | Admin | The administrative tenant (GUID `…0001`) |
| `tenant1` | Tenant 1 | A sample working tenant (GUID `…0002`) |

**An administrator account** in the `admin` tenant, with a default password set only if one is not already present:

```csharp
adminUser = new User {
    UserId = _guid1,
    TenantId = _guid1,
    Username = "admin",
};
// ...
if (String.IsNullOrEmpty(adminUser.Password)) {
    adminPassword = "admin";
    adminUser.Password = HashPassword(adminPassword);
}
```

So your first login is **username `admin`, password `admin`**. The same admin account (same password) is also created inside every other tenant, so you can administer `tenant1` too.

**Sample users** in `tenant1`, handy for poking around (created by `SeedTestData_CreateDefaultTenantData`):

| Username | Password | State |
|----------|----------|-------|
| `test` | `test` | Enabled, admin |
| `john.doe` | `test` | Enabled, regular user |
| `jane.doe` | `test` | **Disabled** (use it to see how a blocked account behaves) |

Two things worth knowing:

- The seed is **idempotent** — it checks for each record before adding it, so it runs safely on every startup and only fills in what is missing. It will not duplicate users or reset a password you have already changed.
- Passwords are stored **hashed**, never in plain text — note the `HashPassword(...)` call above. The strings `admin` and `test` are just the *inputs* you type at the login screen.

You do not run anything for this step. It happens the instant the app starts for the first time. Step 7 is where you see it.

<a id="run-and-sign-in"></a>
## 7. Run and Sign In

**Why it matters:** This is the payoff — proof that clone, build, database, and seed all came together.

**Launch the host project.** From the repository root:

```
dotnet run --project CRM/CRM.csproj
```

(Or open `CRM.slnx` in your IDE, set `CRM` as the startup project, and press run.) On this first run the app will create the database if needed and execute the seed from step 6, so the very first start can take a few seconds longer than later ones.

**Open the app in a browser.** The development URLs are defined in `CRM/Properties/launchSettings.json`:

- HTTPS: `https://localhost:7271`
- HTTP: `http://localhost:5201`

Your IDE may launch the browser for you. If you started from the terminal, watch the console — it prints the URL it is listening on.

**Sign in.** The login screen lives at the route `/Login`, or `/{TenantCode}/Login` if you want to target a specific tenant by its code (for example `/admin/Login`). Both routes are declared right at the top of `Login.razor`:

```razor
@page "/Login"
@page "/{TenantCode}/Login"
```

Choose the **local account** login option (the seeded accounts are local accounts — the social and OpenID options need provider keys you have not configured yet), then enter:

- **Username:** `admin`
- **Password:** `admin`

You should land on a logged-in session as the administrator. That is the finish line for this doc. From here you can change the admin password, explore `tenant1` with the `test` / `john.doe` accounts, and move on to learning the codebase.

<a id="troubleshooting"></a>
## 8. Troubleshooting

**Why it matters:** Knowing the *cause* turns a five-minute fix into a thirty-second one. Most failures here map to a single missing prerequisite or a typo in one config line.

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `dotnet build` fails with a framework/SDK error | Wrong or missing .NET SDK; FreeCRM needs the .NET 10 SDK (`net10.0`) | Install the .NET 10 SDK — see doc 002. Verify with `dotnet --info`. |
| `dotnet restore` fails | No network access to the NuGet feed | Check connectivity/proxy; retry `dotnet restore CRM.slnx`. |
| App opens but redirects you to a setup/configure page | The app cannot reach the configured database | Fill in the provider and connection details in the setup wizard, or fix `DatabaseType` / `ConnectionStrings:AppData` in `CRM/appsettings.json`. |
| "DatabaseOffline" startup error | SQL Server/MySQL/PostgreSQL is not running or the connection string is wrong | Start the engine, or switch `DatabaseType` to `InMemory` for a no-install run. |
| Just want it running, no database to set up | — | Set `"DatabaseType": "InMemory"` in `appsettings.json`. No connection string or engine needed (data is not persisted). |
| Login fails with `admin` / `admin` | Database was created but the seed has not run, or the password was already changed | Confirm the app reached the database (no startup error), restart so the idempotent seed can run; if you changed the password earlier, use the new one. |
| Logged in but no sample users to test with | You are looking at the `admin` tenant; the `test` / `john.doe` / `jane.doe` users live in `tenant1` | Switch to the `tenant1` tenant (for example via `/tenant1/Login`). |
| Data disappears every restart | You are on the `InMemory` provider | Switch to `SQLServer`, `SQLite`, MySQL, or PostgreSQL for persistence. |
| Browser cannot reach the URL | Wrong port | Use the URLs from `CRM/Properties/launchSettings.json` (`https://localhost:7271` or `http://localhost:5201`). |

---

<a id="related-docs"></a>
## 9. Related Docs

- [002 — Getting .NET 10 and the Toolchain to Cooperate](002_toolchain-prereqs.md) — do the toolchain prerequisites first
- [004 — Reading the Repository Map](004_reading-the-tree.md) — then learn the folder layout
- [005 — Shipping Your First Record Screen](005_first-feature.md) — then take a guided tour

---
*GuidesV2 003 · drafted from source 2026-06-05.*

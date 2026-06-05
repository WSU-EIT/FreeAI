# 002 — Getting .NET 10 and the Toolchain to Cooperate

> **Document ID:** 002  ·  **Category:** Onboarding  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Install the runtime, SDK, and editor tooling so the first build and live-reload work the first time.
> **Audience:** Anyone preparing a dev machine  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 00x (Landing Zone: From Clone to Login) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why the Toolchain Matters](#why-it-matters) | Runtime, SDK, and tooling defined in plain language, and why version 10 is non-negotiable |
| 2 | [Pick Your Platform and Editor](#choose-setup) | Choosing OS and IDE before you install anything |
| 3 | [Install the .NET 10 SDK](#install-sdk) | Getting the SDK onboard and proving it with `dotnet --version` |
| 4 | [Set Up Editor Tooling](#editor-tooling) | The extensions and settings that make the editor understand this solution |
| 5 | [Turn On Live-Reload](#live-reload) | Using `dotnet watch` and hot reload so changes show up without a restart |
| 6 | [Verify the First Build](#verify) | Restoring, building, running, and confirming live-reload actually works |
| 7 | [Fix Common Setup Failures](#troubleshooting) | Version mismatches, missing workloads, and tooling errors |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why the Toolchain Matters

**Why it matters:** before you can clone, build, or sign in (that is doc 003's job), your machine has to *speak the same language as the code*. If the language version is even slightly off, the project will not compile, and the error messages it produces will point you in the wrong direction. Getting the toolchain right is the single biggest predictor of whether your first day goes smoothly or turns into an afternoon of confusing red text. Spend the fifteen minutes here and the rest of onboarding gets much easier.

Three terms come up constantly. Here they are in plain language:

- **Runtime** — the engine that actually *runs* the compiled app on a machine. Think of it as the motor. FreeCRM runs on the .NET 10 runtime. End users (and production servers) need the runtime; developers get it bundled inside the SDK, so you usually do not install it separately.
- **SDK (Software Development Kit)** — the full workshop: the compiler that turns C# into something runnable, the `dotnet` command-line tool, and the runtime itself. The SDK is what you, as a developer, install. One install gives you everything needed to build and run.
- **Tooling** — your editor or IDE (Integrated Development Environment — a code editor with a built-in compiler, debugger, and helpers) plus the extensions that teach it to understand C#, Blazor, and Razor files. Tooling does not change *whether* the code builds; it changes how pleasant it is to work on. Good tooling gives you autocomplete, inline errors, and one-click debugging.

**Why setup order matters:** install the SDK first, then the editor tooling, then turn on live-reload. The editor extensions probe your machine for an installed SDK when they start up; if you install the editor first and the SDK second, the extension may cache "no SDK found" and need a restart. Doing it in order avoids that whole class of "it was working a minute ago" confusion.

**Why version 10 specifically:** every project in this solution declares `net10.0` as its target framework — the exact .NET version it is built against. You can see it at the top of each project file, for example in `CRM/CRM.csproj`:

```xml
<TargetFramework>net10.0</TargetFramework>
```

The SDK is allowed to be *newer* than the target (a .NET 10 SDK can build a `net10.0` project), but it cannot be *older*. A .NET 9 or .NET 8 SDK will refuse to build this code. That is the most common first-day failure, and section 7 covers the exact error.

---

<a id="choose-setup"></a>
## 2. Pick Your Platform and Editor

**Why it matters:** .NET 10 is cross-platform, so you have real choices here — but the choices affect which database providers and conveniences you get for free. Deciding up front saves you from reinstalling halfway through.

**Operating system.** The SDK and the application run on Windows, macOS, and Linux. The project's default settings lean slightly toward Windows — for example, the out-of-the-box database connection in `CRM/appsettings.json` points at a local SQL Server using Windows Integrated Security:

```jsonc
"AppData": "Data Source=(local);Initial Catalog=CRM;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

That default is convenient on Windows but is purely a configuration value, not a hard requirement. On macOS or Linux you simply pick a different database provider — the `CRM.EFModels` project ships support for SQL Server, PostgreSQL, MySQL, SQLite, and an in-memory option. (Choosing and configuring the database is doc 003's territory; here you only need to know the cross-platform door is open.)

**Editor / IDE.** Any of these work because they all consume the same SDK underneath:

- **Visual Studio (Windows)** — the heaviest, most batteries-included option. Understands the `.slnx` solution file, Blazor, and Razor with no extra setup, and has the strongest visual debugger. Best fit if you are on Windows and want zero configuration.
- **Visual Studio Code (any OS)** — lightweight and cross-platform. Needs the **C# Dev Kit** extension to understand the solution (covered in section 4). Best fit for macOS/Linux or if you prefer a light editor.
- **JetBrains Rider (any OS)** — cross-platform, strong out of the box for .NET, understands `.slnx`. A good middle ground.

This solution uses the newer **`.slnx`** solution format — an XML file named `CRM.slnx` at the repository root that simply lists the projects. (A "solution" is the file that groups related projects so the editor can open them all together.) Make sure whichever editor you choose is recent enough to open `.slnx`; older versions only understood the legacy `.sln` format. If your editor cannot open it, you can still build everything from the command line, which needs no editor at all.

> **Rule of thumb:** Windows + Visual Studio is the lowest-friction path. VS Code + C# Dev Kit is the lowest-friction *cross-platform* path. Pick one now; you can switch later without touching the code.

---

<a id="install-sdk"></a>
## 3. Install the .NET 10 SDK

**Why it matters:** this is the one install you genuinely cannot skip. No SDK, no build — no exceptions. The good news is it is a single download and a one-line check.

**Step 1 — Download.** Get the **.NET 10 SDK** (not just the runtime) from the official Microsoft .NET download page. Pick the installer for your OS and CPU architecture (on Apple Silicon Macs, choose the Arm64 build; on most Windows/Linux machines, x64). The SDK bundles the matching runtime, so you do not download that separately.

**Step 2 — Install.** Run the installer and accept the defaults. On macOS and Linux you can alternatively use your package manager or the official install script; either way the goal is the same — a `dotnet` command on your `PATH`.

**Step 3 — Prove it.** Open a fresh terminal (a new window, so it picks up the updated `PATH`) and run:

```bash
dotnet --version
```

You want to see a version that starts with `10.` (for example `10.0.100`). If you see a 9.x or 8.x number, that older SDK is winning on your `PATH` and the project will not build until you fix it — jump to section 7.

**Step 4 — See everything installed.** This lists every SDK and runtime on the machine, which is invaluable when something is off:

```bash
dotnet --info
```

**Do I need any extra "workloads"?** A *workload* is an optional add-on the SDK can install for specialized targets (mobile, WebAssembly tooling, and so on). For this solution the standard SDK install is sufficient — `CRM.Client` is a Blazor WebAssembly project, and the `Microsoft.NET.Sdk.BlazorWebAssembly` SDK it uses comes with the base install. The project files pull everything else they need as NuGet packages (downloadable code libraries) during restore, so there is nothing extra to install by hand. If you ever do hit a "missing workload" message, section 7 shows the one command that fixes it.

---

<a id="editor-tooling"></a>
## 4. Set Up Editor Tooling

**Why it matters:** the SDK lets you build; tooling lets you *work without going blind*. With it you get red squiggles under mistakes as you type, autocomplete that knows the project's own types, and a debugger that pauses the running app so you can inspect it. Skipping this still lets the code build, but you will be coding in the dark.

Follow the section for the editor you chose in section 2.

**Visual Studio (Windows).** During install, select the **ASP.NET and web development** workload. That single checkbox brings in everything FreeCRM needs: C#, Blazor, Razor, and the debugger. Then just open `CRM.slnx`. No extensions required.

**Visual Studio Code (any OS).** Install two extensions from the Marketplace:

1. **C# Dev Kit** — adds full solution awareness, so VS Code can open `CRM.slnx`, build it, and debug it. (It pulls in the base **C#** extension automatically.)
2. **C#** (the base language extension) — provides the language server that powers autocomplete and inline errors. Installed for you by Dev Kit, but worth confirming it is enabled.

Then open the `FreeCRM` folder. VS Code will detect the solution and offer to restore packages; let it.

**JetBrains Rider (any OS).** Open `CRM.slnx` directly. Rider's .NET support is built in — no extensions needed.

**A couple of repo settings that affect tooling.** The project intentionally turns off one Blazor behavior to make debugging quieter. In each web/WebAssembly project file you will find:

```xml
<BlazorDisableThrowNavigationException>true</BlazorDisableThrowNavigationException>
```

That tells Blazor not to throw an internal exception during page navigation. **Why you care:** without it, your debugger would pause on a harmless exception every time you navigate between pages. Leave this setting alone — it is what keeps step-through debugging usable.

You will also see a `.editorconfig` file listed in the solution. An **EditorConfig** file standardizes formatting (indentation, spacing) across every editor, so your saved files match the house style automatically. Most editors honor it with no setup; just keep "format on save" or your editor's EditorConfig support enabled.

---

<a id="live-reload"></a>
## 5. Turn On Live-Reload

**Why it matters:** without live-reload, every tiny change — fix a label, tweak some CSS — means stop the app, rebuild, restart, and click your way back to where you were. Live-reload collapses that loop to "save the file and watch the browser update." On a Blazor app you touch dozens of files an hour, so this is the difference between a smooth day and a tedious one.

Two related terms:

- **`dotnet watch`** — a mode of the SDK that keeps an eye on your source files and, when one changes, applies the change to the already-running app instead of restarting from scratch.
- **Hot reload** — the underlying capability that injects a code change into the *live* process. `dotnet watch` is the easy front door to it.

**The command.** From inside the web host project folder (`FreeCRM/CRM`), run:

```bash
dotnet watch
```

(You can be explicit with `dotnet watch run` — same result.) The app starts, opens a browser, and from then on most edits to `.razor`, `.cs`, and `.css` files apply within a second or two with no manual restart. The launch profile already sets the environment to `Development` and opens the browser for you (see `CRM/Properties/launchSettings.json`).

**Doing it from the editor instead.**

- **Visual Studio:** press the **Hot Reload** button on the toolbar (the flame icon), or just edit while debugging — Visual Studio applies changes live.
- **VS Code / Rider:** the `dotnet watch` command above is the reliable path and works identically across platforms.

**What hot reload can and cannot do.** Most everyday edits — markup, method bodies, styling — apply instantly. Some structural changes (renaming a type, editing the project file, changing method signatures in certain ways) need a real rebuild; `dotnet watch` will tell you it is doing a "full restart" and handle it for you. So if a change does not appear, you do not need to do anything special — watch usually restarts on its own.

---

<a id="verify"></a>
## 6. Verify the First Build

**Why it matters:** this is the moment of truth. Running these four steps proves the SDK, the projects, and live-reload all agree with each other — *before* you layer the database and login on top in doc 003. If something is wrong with the toolchain, you want to find out here, where the cause is obvious, not three steps later where it is buried.

Run these from the repository's `FreeCRM` folder (the one containing `CRM.slnx`).

**Step 1 — Restore.** *Restore* means downloading every NuGet package the projects depend on (Entity Framework, Blazor Bootstrap, MudBlazor, and the rest). It runs automatically before a build, but running it on its own gives a clean first signal:

```bash
dotnet restore
```

**Step 2 — Build.** This compiles all six projects:

```bash
dotnet build
```

Those six projects, and why they exist:

| Project | What it is |
|---------|-----------|
| `CRM` | The web host — the ASP.NET Core server that serves the app and the APIs (`Microsoft.NET.Sdk.Web`). |
| `CRM.Client` | The Blazor WebAssembly front end that runs in the browser (`Microsoft.NET.Sdk.BlazorWebAssembly`). |
| `CRM.DataAccess` | The layer that reads and writes the database. |
| `CRM.DataObjects` | Plain data shapes shared between server and client. |
| `CRM.EFModels` | The Entity Framework database models and provider support (SQL Server, PostgreSQL, MySQL, SQLite, in-memory). |
| `CRM.Plugins` | The plugin system that lets you extend the app without forking it. |

A clean build ends with `Build succeeded` and zero errors. (Warnings are fine for now.)

**Step 3 — Run.** From the `FreeCRM/CRM` folder:

```bash
dotnet run
```

The app starts and listens on the URLs from the launch profile — by default `http://localhost:5201` for plain HTTP and `https://localhost:7271` for HTTPS. Open one of those in a browser and you should see the app's landing page. (You will not log in yet — that needs the database and a seeded tenant, which is doc 003.)

**Step 4 — Prove live-reload.** Stop the app, then start it with `dotnet watch` (section 5). With the browser open, change a piece of visible text in a `.razor` file under `CRM.Client` and save. Within a second or two the browser should reflect the change with no restart on your part. **If that works, your toolchain is fully wired and you are ready for doc 003.**

---

<a id="troubleshooting"></a>
## 7. Fix Common Setup Failures

**Why it matters:** nearly every setup failure here has a one-line cause and a one-line fix. Knowing the four usual suspects turns a scary error wall into a thirty-second correction.

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| Build error mentioning the project needs a newer SDK, or `NETSDK1045` "current SDK does not support targeting .NET 10.0" | An older SDK (9.x/8.x) is first on your `PATH`. | Install the .NET 10 SDK (section 3). Confirm with `dotnet --version` showing `10.x`. If an old one still wins, fix `PATH` ordering or remove the stale SDK. |
| Editor cannot open `CRM.slnx`, or shows it as an unknown file | Editor too old to understand the new `.slnx` solution format. | Update the editor to a current version, or build from the command line (`dotnet build`), which does not need an editor. |
| `dotnet --version` reports a 9.x or 8.x number | Multiple SDKs installed; an older one is taking priority. | Run `dotnet --info` to see all SDKs. Ensure the 10.x SDK is on `PATH` ahead of the others. |
| A "missing workload" message during build | A required optional SDK component is not installed. | Run `dotnet workload restore` from the solution folder; it installs exactly what the projects ask for. |
| `dotnet restore` fails to download packages | Network/proxy issue, or a private NuGet feed is unreachable. | Check connectivity. Note `CRM.Client` adds the public dotnet-tools feed via `RestoreAdditionalProjectSources`; that source must be reachable. |
| No autocomplete or inline errors in VS Code | C# / C# Dev Kit extensions missing or disabled. | Install and enable both (section 4), then reload the window. |
| Edits do not appear in the browser while running | Not running under `dotnet watch`, or the change needs a full rebuild. | Start with `dotnet watch` (section 5). Structural changes trigger an automatic restart — wait for it. |
| HTTPS page warns about an untrusted certificate on first run | The local development certificate is not trusted yet. | Run `dotnet dev-certs https --trust` once, then restart the app. |

If none of these match, the fastest diagnostic is always `dotnet --info` (confirms SDK and runtime versions) followed by a clean `dotnet build` from the command line — that strips the editor out of the picture and tells you whether the problem is the toolchain or the tooling.

---

<a id="related-docs"></a>
## 8. Related Docs

- [003 — Clone, Build, Seed, and Sign In](003_zero-to-login.md) — next step once the toolchain works
- [083 — Shipping It to Production](083_deployment-shapes.md) — the same runtimes power production deploys

---
*GuidesV2 002 · drafted from source · replaces the scaffold TODOs with verified .NET 10 toolchain content.*

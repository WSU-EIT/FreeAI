# FreeTools

A .NET 10 workspace containing a suite of CLI analysis tools, a Roslyn code-reorganiser, and a
FreeCRM-based example application used as the tools' working target.

Developed by **[Enrollment Information Technology (EIT)](https://em.wsu.edu/eit/meet-our-staff/)**
at **Washington State University**.

---

## What is here

```
FreeTools/
├── FreeTools/          # The tool suite — analysis pipeline, FreeCodeMaid, standalone utilities
├── FreeExamples/       # FreeCRM-based Blazor app; the pipeline's default target
└── Docs/               # Workspace-level documentation
```

| Folder | Contents |
|--------|----------|
| **[FreeTools/](FreeTools/)** | The tools themselves. Start there — it has the pipeline diagram, the environment-variable reference, and per-tool notes. |
| **[FreeExamples/](FreeExamples/)** | A working FreeCRM application (~65k LOC). Serves as the default `--target` for the pipeline and as a reference for the FreeCRM extension pattern. |
| **[Docs/](Docs/)** | Overview, features, architecture, and the GuidesV2 library. |

### GuidesV2

[`FreeTools/Docs/GuidesV2/`](FreeTools/Docs/GuidesV2/) is a separate ~1.6 MB library of 71 long-form
guides covering FreeCRM development practice — architecture, data-stack anatomy, style references for
C#/Razor/CSS/JS/SQL, and a set of collaboration and planning workflows. It also retains the meeting
and briefing transcripts that produced it (the `000x_` series).

Those guides document *FreeCRM development*, not the FreeTools CLIs. They are reference material and
are maintained independently of this README.

---

## Quick start

```bash
# Run the analysis pipeline against FreeExamples
cd FreeTools/FreeTools.AppHost
dotnet run

# Results
ls FreeTools/Docs/runs/FreeExamples/main/latest/
```

See **[FreeTools/README.md](FreeTools/README.md)** for the full pipeline description, per-tool usage,
configuration, and known rough edges.

---

## Solution files

There are two, and only one of them works:

| Solution | Status |
|----------|--------|
| `FreeTools/FreeTools.slnx` | **Working.** All 11 project references resolve. This is the one to open. |
| `FreeTools.slnx` (this folder) | **Broken.** 29 of its 51 project references point into a `ReferenceProjects/` directory that does not exist in this repository. It will not load cleanly. |

The root solution was written when this workspace also vendored read-only copies of FreeCRM,
FreeCICD, and FreeGLBA under `ReferenceProjects/`. Those copies are gone; the solution file was never
updated. FreeGLBA now lives at [`../FreeGLBA`](../FreeGLBA) in this repository.

Additionally, `FreeCodeMaid/1.0` has its own solution
(`FreeTools/FreeCodeMaid/1.0/FreeCodeReorganizer.slnx`) which itself omits two of its five buildable
projects — see [FreeCodeMaid's README](FreeTools/FreeCodeMaid/1.0/README.md).

---

## The FreeCRM extension pattern

Both FreeExamples and FreeGLBA are built on FreeCRM, which uses a layered extension system so the
base framework can be upgraded without re-diffing every file.

1. **Framework files** (`Program.cs`, `DataController.cs`, …) — shipped by FreeCRM, never modified.
2. **`.App.` hook files** (`Program.App.cs`, `DataAccess.App.cs`, …) — shipped with empty methods
   called at defined lifecycle points. You add a single line here.
3. **`{ProjectName}.App.{Feature}` files** — your code, called from the hooks.

```
Program.cs                              ← framework, never touched
    └── Program.App.cs                  ← hook file, one line added
            └── FreeCICD.App.Program.cs ← your code
```

When FreeCRM updates: copy the framework files over wholesale, diff only the handful of `.App.` hook
files, and leave your `{ProjectName}.App.*` files completely alone.

### Hook points

| Hook file | Provides |
|-----------|----------|
| `Program.App.cs` | `AppModifyBuilderStart/End()`, `AppModifyStart/End()`, `ConfigurationHelpersLoadApp()` |
| `DataController.App.cs` | App-specific API endpoints |
| `DataAccess.App.cs` | App-specific data operations |
| `DataObjects.App.cs` | App-specific DTOs |
| `DataModel.App.cs` | App-specific client state |
| `Helpers.App.cs` | App-specific client helpers |
| `ConfigurationHelper.App.cs` | App-specific configuration properties |

### Naming

| Pattern | Example |
|---------|---------|
| `{ProjectName}.App.{Feature}.cs` | `FreeCICD.App.API.cs` |
| `{ProjectName}.App.{Feature}.razor` | `FreeCICD.App.UI.Wizard.razor` |
| `{Feature}.App.{SubFeature}.razor` | `About.App.razor` |

`FreeTools.AppExtractor` exists to pull exactly this `.App.*` layer out of a fork.

---

## Plain-English briefing

**What is it?** A set of command-line tools that automatically analyse and document a Blazor web
application. One command starts the app and then: discovers every page route, inventories the entire
codebase with Roslyn, sends an HTTP request to each route, screenshots each page with a real browser,
runs a four-engine accessibility audit, and assembles the results into a markdown report filed by git
branch.

**Why does it exist?** So documentation is *generated* rather than hand-maintained. Route lists, code
metrics, screenshots, and accessibility findings all rot the moment someone writes them down by hand;
producing them on demand keeps them true.

**What is genuinely distinctive?**

- The whole pipeline runs from one command, rather than as five disconnected tools.
- Screenshotting is SPA-aware — it waits for Blazor to render, where most tools capture a blank
  loading shell. It also does two passes, anonymous and authenticated, so login-gated pages are covered.
- The accessibility scanner merges four independent engines (axe-core, HTML_CodeSniffer, IBM ACE, and
  in-house rules) and de-duplicates their findings against a curated WCAG mapping.

**What it is not.** "FreeTools" is a folder name applied to three separate things. The analysis
pipeline is coherent and works. FreeCodeMaid is a distinct and higher-quality product that happens to
live in the same directory. ForkCRM and AppExtractor are one-off utilities that do not use the shared
core they sit next to. Sizing an investment in "FreeTools" means deciding which of the three you mean.

**Where to look first:** [FreeTools/README.md](FreeTools/README.md), then a generated report under
`FreeTools/Docs/runs/{Project}/{Branch}/latest/`.

---

## About

Developed and maintained by
**[Enrollment Information Technology (EIT)](https://em.wsu.edu/eit/meet-our-staff/)** at
**Washington State University**.

We build internal tools and automation to support enrollment management processes across WSU.

Questions or feedback? Visit our [team page](https://em.wsu.edu/eit/meet-our-staff/) or open an issue
on [GitHub](https://github.com/WSU-EIT/FreeTools/issues).

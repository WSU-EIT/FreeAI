# FreeCodeReorganizer (1.0) — the maintained CodeMaid-style cleanup

This is the **1.0 line** of our member-reorganizer tooling: a proper, installable **editor extension**
(the way you install CodeMaid), built and owned by us. The legacy console tool lives in `../0.0`
(`FreeCodeMaid`) and is frozen as-is.

> **Naming:** we dropped the "CodeMaid" name — that's [Steve Cadwallader's open-source extension](https://github.com/codecadwallader/codemaid),
> not ours. A read-only reference clone lives at `FreeAI/_codemaid-reference/` (git-ignored, local only).
> If we ever fork *their* code and republish with our fixes, we'd keep their name + license. We're not
> doing that — this is a fresh tool, so it's **FreeCodeReorganizer**.

---

## What I learned: how you turn C# into a Visual Studio extension

### 1. Visual Studio extensions and VS Code extensions are NOT the same thing
They share the `.vsix` file extension and nothing else — completely different APIs and runtimes
([VSIX anatomy](https://learn.microsoft.com/en-us/visualstudio/extensibility/anatomy-of-a-vsix-package?view=visualstudio),
[VS Code commands](https://code.visualstudio.com/api/extension-guides/command)):

| | Visual Studio | VS Code |
|---|---|---|
| Language | **C#** (the extension *is* .NET) | **TypeScript/JavaScript** |
| API | VSSDK / VisualStudio.Extensibility | VS Code Extension API |
| What CodeMaid is | ✅ this (VS-only) | ❌ CodeMaid does not run here |

So "works in both" means **two thin front-ends over one shared engine** — not one extension.

### 2. For Visual Studio there are two models ([choose-your-model](https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/extensibility-models?view=visualstudio))
- **Classic VSSDK (in-process):** .NET Framework, runs inside VS, most complete & best-documented. What
  CodeMaid and ~all marketplace extensions use. Easiest path; huge sample/template base. The modern,
  friendly wrapper over it is **[Community.VisualStudio.Toolkit](https://github.com/VsixCommunity/Community.VisualStudio.Toolkit)**.
- **VisualStudio.Extensibility (out-of-process):** modern .NET, runs in its own process (VS can't be
  crashed by it), but newer and **less feature-breadth** today. The strategic future, not yet the
  pragmatic choice for a small cleanup tool.

### 3. Why we rewrite instead of using CodeMaid
Two reasons, and the second is the real one:
1. **CodeMaid has known bugs under VS 2026** and is effectively unmaintained. (The good news for *us*:
   a *fresh* extension targeting `[17.0, 19.0)` works cleanly in VS 2026 — *"a VS 2022 extension
   doesn't need modifying for VS 2026, because VS 2026 supports API version 17.x."* So the platform
   isn't the blocker; CodeMaid's specific, unmaintained code is.)
2. **CodeMaid can't be tailored to the FreeCRM author's exact conventions.** Its reorganize is its own
   opinionated order; we need *our* rules — fields grouped by purpose (not alphabetized), properties +
   methods interleaved alphabetically ignoring visibility, the by-purpose-skip threshold, and the
   wrapped-parameter `){` restore. Owning the engine is the only way to match the author 1:1.

That's why this is a **clean rewrite over our own `FreeCodeReorganizer.Core` engine**, not a fork of
CodeMaid. (If we ever needed *their* feature breadth, forking + republishing under their name/license
would be the move — but we don't; we need precision, not breadth.)

### 4. The pieces of a VS extension
- **`source.extension.vsixmanifest`** — identity, version, which VS versions it installs into, prerequisites.
- **`*.vsct`** (XML command table) — declares **commands**, where they appear (menus, toolbars, context
  menus), and **default keyboard shortcuts** (`<KeyBindings>`).
  ([menus & commands](https://learn.microsoft.com/en-us/visualstudio/extensibility/vsix/recipes/menus-buttons-commands?view=vs-2022),
  [keybindings](https://learn.microsoft.com/en-us/visualstudio/extensibility/binding-keyboard-shortcuts-to-menu-items?view=vs-2022))
- **An `AsyncPackage`** — the entry point VS loads.
- **Command handlers** — what runs when the button/shortcut fires.
- **Options pages** — the configurable choices in *Tools → Options*; users can also rebind the shortcut
  in *Tools → Options → Environment → Keyboard*.

---

## Our decision & architecture

**Classic VSSDK + Community.VisualStudio.Toolkit, in-process, targeting `[17.0, 19.0)`.** Mature,
documented, the "install it like CodeMaid" experience, and it covers VS 2022 + 2026. One shared engine,
thin front-ends:

```
FreeCodeMaid/
├── 0.0/                        # frozen: the original console tool (net10), still in FreeTools.slnx
│   └── FreeCodeMaid.csproj
└── 1.0/                        # the new, maintained suite
    ├── FreeCodeReorganizer.Core/        # ✅ the Roslyn engine (netstandard2.0) — BUILDS, reused by the front-end
    └── FreeCodeReorganizer/             # ✅ the Visual Studio VSIX extension ("install like CodeMaid") — BUILDS to a .vsix
```

- **`FreeCodeReorganizer.Core`** — the reorganize logic lifted out of the console tool: member reordering
  (alphabetical, visibility-agnostic), the by-purpose-skip threshold, and the wrapped-parameter `){`
  restore. Pure Roslyn, `netstandard2.0`, so the .NET-Framework VSIX **and** modern .NET can both use it.
  API: `Reorganizer.Run(sourceText, ReorderConfig, eol) → ReorgResult`.
- **`FreeCodeReorganizer`** (VSIX) — commands (Reorganize Document, on the Edit menu + code context menu),
  a default shortcut (`Ctrl+K, Ctrl+R`), and a *Tools → Options* page mapping to `ReorderConfig`.
  Targets `net48`; builds to an installable `.vsix` for VS 2022 + 2026.

---

## Division of responsibility: `.editorconfig` owns formatting, we own the gaps

This is the core design rule, the same in the CLI and the extension:

1. **`.editorconfig` first.** Standard formatting — indentation, normal brace placement, spacing,
   `String.Empty`, line endings, etc. — is **owned by `.editorconfig`** and run *first*. In the CLI
   that's `dotnet format whitespace`; in the VS extension it's the editor's **Format Document**
   (toggle: *Run editorconfig format first*, default on). Whoever owns the repo changes their style by
   editing `.editorconfig` — locally, the normal way — and we respect it.
2. **Then FreeCodeReorganizer layers ONLY what `.editorconfig` can't express:** member **reordering**
   and the wrapped-parameter **`){`** brace. Anything `.editorconfig` *can* control stays in
   `.editorconfig` — we never duplicate it as an option.
3. **Our options cover only those gaps**, with sensible ranges/choices, defaulting to our (FreeCRM)
   choices: sort alphabetically / ignore leading `_` / group by visibility / static-first (member
   ordering), the `){` toggle, and the by-purpose threshold (`MaxFractionReordered`, 0–1). See
   *Tools → Options → FreeCodeReorganizer*.

> **FreeTools mirrors FreeCRM.** The whole FreeTools suite idealizes the FreeCRM layout, so FreeCRM's
> `.editorconfig` now governs FreeTools too (`FreeAI/FreeTools/FreeTools/.editorconfig`) and our own
> tool code is kept to it (including the `){` style). Running the reorganizer on our own source is a
> near no-op — that's the consistency check.

---

## Building & installing the VS extension

The VSIX needs the **VSSDK build tools** that ship with the *"Visual Studio extension development"*
workload. You do **not** need to open Visual Studio to build it, but you do need VS (with that workload)
installed, because the build uses VS's **Framework MSBuild** and the VSSDK packaging targets.

### Build it from the command line (what we actually do)
This is a verified, working path — it produces the `.vsix` without launching the IDE:

```powershell
# 1. Find VS + its MSBuild (any edition with the "extension development" workload):
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msb = & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"

# 2. Build (restore + pack the .vsix). Framework MSBuild, NOT `dotnet build`:
& $msb "FreeCodeReorganizer\FreeCodeReorganizer.csproj" /t:Rebuild /restore /p:Configuration=Release
```

Output: **`FreeCodeReorganizer\bin\Release\net48\FreeCodeReorganizer.vsix`** (~1 MB; a ZIP containing
`extension.vsixmanifest`, the `.pkgdef`, our two DLLs + the toolkit). Double-click it to install into a
normal VS, or publish it to the Marketplace / an internal feed.

Two things make a command-line build work (both already wired into the `.csproj`):
- **Use VS's MSBuild, not `dotnet build`.** A VSIX is a .NET *Framework* project and the VSSDK targets
  are Framework-only; `dotnet`'s .NET MSBuild can't run them.
- **The VSSDK packaging targets are imported by hand.** The `Microsoft.VSSDK.BuildTools` NuGet only
  auto-imports an env-var shim; the real packaging logic (`VSCTCompile`, `GeneratePkgDef`,
  `CreateVsixContainer`) lives in `tools\VSSDK\Microsoft.VsSDK.targets`. The project switches from the
  `Sdk="..."` attribute to **explicit `Sdk.props`/`Sdk.targets` imports** so it can import that targets
  file *after* the .NET SDK targets. A command-line build also sets `DeployExtension=false` (only VS's
  F5 deploys into the experimental hive), so it just packs the artifact.

> **Why `net48`?** `Community.VisualStudio.Toolkit.17` (17.0.507) ships only a `lib/net48` assembly, so
> the VSIX targets `net48` (a `net472` target can't consume it). net48 loads fine in VS 2022/2026 and
> still satisfies the manifest's `[4.7.2,)` .NET Framework dependency.

### Or build + debug inside Visual Studio
1. Open `FreeCodeReorganizer.slnx` in VS 2022/2026 (with the extension-development workload).
2. **F5** → launches the **Experimental Instance** with the extension loaded (here `DeployExtension`
   turns back on automatically because `BuildingInsideVisualStudio=true`).
3. **Build → Release** → same `bin\Release\net48\FreeCodeReorganizer.vsix`.

### Using it
Open a `.cs` file → `Ctrl+K, Ctrl+R` (or *Edit → Reorganize Document*, or right-click → Reorganize).
Configure under *Tools → Options → FreeCodeReorganizer*.

### One thing to verify on first run (Roslyn versioning)
The Core engine bundles `Microsoft.CodeAnalysis.CSharp` 4.12, and an **in-process** extension shares the
process with Visual Studio's *own* Roslyn. The build packs cleanly; the open question is *runtime*
assembly-load. If there's a conflict when the command first runs, align Core's Roslyn version with the
one your VS ships, or move the engine out-of-process. Confirm on first launch.

---

## Project layout (what's here now)
Open **`FreeCodeReorganizer.slnx`** in Visual Studio to load the whole suite:
- `FreeCodeReorganizer.Core/` — the engine (netstandard2.0). **Builds.**
- `FreeCodeReorganizer.Core.Tests/` — xUnit tests for the engine. **7/7 pass** (`dotnet test`, no VS needed).
- `FreeCodeReorganizer/` — the VS extension (VSIX). **Builds to a `.vsix`** (CLI via VS's MSBuild, or F5 in VS).

## Roadmap
- [x] Core engine extracted (`FreeCodeReorganizer.Core`, builds) + **7 unit tests pass**
- [x] VSIX front-end scaffolded (`FreeCodeReorganizer`)
- [x] **First build green: produces an installable `FreeCodeReorganizer.vsix` from the command line**
      (VS 2026's Framework MSBuild) — manifest + `.pkgdef` verified; targets VS 2022 + 2026
- [ ] First *runtime* launch: confirm the toolkit API calls + resolve any Roslyn-version conflict
- [ ] Icon + Marketplace/internal-feed publish (and a real Marketplace `Publisher` id)
- [ ] (optional) format-on-save / reorganize-on-save toggle

> **Scope:** this is a **Visual Studio–only** tool (pure C# VSIX). A VS Code front-end would be a separate
> TypeScript project requiring Node — explicitly out of scope.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** This is the **1.0 line**: the same member-reorganizer logic, but repackaged as an installable **Visual Studio extension** (a `.vsix`, "install it like CodeMaid") over a shared Roslyn engine, `FreeCodeReorganizer.Core`. One engine, two front-ends (the frozen 0.0 console tool + this VS extension). The division of labor: `.editorconfig` owns normal formatting; this tool layers *only* member reordering and the `){` brace it can't express.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Roslyn engine (netstandard2.0) | The reorganize logic, shared | [FreeCodeReorganizer.Core/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeTools/FreeCodeMaid/1.0/FreeCodeReorganizer.Core) |
| Visual Studio VSIX (VSSDK) | "Install like CodeMaid" front-end | [FreeCodeReorganizer/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeTools/FreeCodeMaid/1.0/FreeCodeReorganizer) |

**Why does this exist?** CodeMaid (the well-known extension) is unmaintained and **can't be tailored to FreeCRM's exact conventions** — so this is a *fresh* tool (carefully **not** reusing CodeMaid's name or code) that owns its engine and matches the house style 1:1.

**What does it accomplish that other tools don't?**
- **One engine, two front-ends** (CLI + VS extension) so the rules can't drift between them.
- A clear ownership split: **`.editorconfig` formats, this tool only fills the gaps** it can't express (reorder + `){`), never duplicating editorconfig settings.

**Terminology & "can I see it?"**
- **VSIX** — a Visual Studio extension package.
- **In-process extension** — runs inside VS (fast, but shares VS's Roslyn — a versioning caveat noted in the doc).
- *See it:* `Ctrl+K, Ctrl+R` in Visual Studio, or *Edit → Reorganize Document*.

**The hard part, drawn** — one engine, two ways to run it:

```
  FreeCodeReorganizer.Core (Roslyn engine: reorder members + restore `){`)
        ▲                                   ▲
   0.0 console tool (frozen)        1.0 VS extension (.vsix) — Ctrl+K,Ctrl+R / Tools→Options
        (.editorconfig formats first; the engine only does what editorconfig can't)
```

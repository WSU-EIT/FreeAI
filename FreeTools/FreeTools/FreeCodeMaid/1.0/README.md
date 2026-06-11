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
    ├── FreeCodeReorganizer.Core/        # ✅ the Roslyn engine (netstandard2.0) — BUILDS, reused by all front-ends
    ├── FreeCodeReorganizer/             # the Visual Studio VSIX extension (this is "install like CodeMaid")
    └── (future) FreeCodeReorganizer.VSCode/   # a TypeScript VS Code extension over the same engine/CLI
```

- **`FreeCodeReorganizer.Core`** — the reorganize logic lifted out of the console tool: member reordering
  (alphabetical, visibility-agnostic), the by-purpose-skip threshold, and the wrapped-parameter `){`
  restore. Pure Roslyn, `netstandard2.0`, so the .NET-Framework VSIX **and** modern .NET can both use it.
  API: `Reorganizer.Run(sourceText, ReorderConfig, eol) → ReorgResult`.
- **`FreeCodeReorganizer`** (VSIX) — commands (Reorganize Document, on the Edit menu + code context menu),
  a default shortcut (`Ctrl+K, Ctrl+R`), and a *Tools → Options* page mapping to `ReorderConfig`.
- **VS Code (later)** — a small TS extension that shells out to the `0.0` CLI (or a thin CLI over Core)
  on the active file. Separate front-end, same behavior.

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

The VSIX **builds in Visual Studio only** (it needs the VSSDK build tools that ship with the
*"Visual Studio extension development"* workload — not available from a plain CLI):

1. Install that workload via the VS Installer.
2. Open `FreeCodeReorganizer/FreeCodeReorganizer.csproj` (or a solution containing it + Core) in VS 2022/2026.
3. **F5** → launches the **Experimental Instance** of VS with the extension loaded, for debugging.
4. **Build → Release** → produces `bin\Release\FreeCodeReorganizer.vsix`. Double-click that `.vsix` to
   install into a normal VS, or publish it to the Marketplace / an internal feed.
5. Use it: open a `.cs` file → `Ctrl+K, Ctrl+R` (or *Edit → Reorganize Document*, or right-click → Reorganize).
   Configure under *Tools → Options → FreeCodeReorganizer*.

### One thing to verify on first run (Roslyn versioning)
The Core engine bundles `Microsoft.CodeAnalysis.CSharp` 4.12, and an **in-process** extension shares the
process with Visual Studio's *own* Roslyn. If there's an assembly-load conflict at runtime, the fix is to
align Core's Roslyn version with the one your VS ships, or move the engine out-of-process. Confirm when you
first F5 it.

---

## Project layout (what's here now)
Open **`FreeCodeReorganizer.slnx`** in Visual Studio to load the whole suite:
- `FreeCodeReorganizer.Core/` — the engine (netstandard2.0). **Builds.**
- `FreeCodeReorganizer.Core.Tests/` — xUnit tests for the engine. **7/7 pass** (`dotnet test`, no VS needed).
- `FreeCodeReorganizer/` — the VS extension (VSIX). **Build/F5 in Visual Studio.**

## Roadmap
- [x] Core engine extracted (`FreeCodeReorganizer.Core`, builds) + **7 unit tests pass**
- [x] VSIX front-end scaffolded (`FreeCodeReorganizer`) — **open in VS, build + F5** to finish
- [ ] First VS build: resolve any Roslyn-version conflict + confirm the toolkit API calls
- [ ] Icon + Marketplace/internal-feed publish
- [ ] (optional) VS Code TypeScript front-end over the CLI/Core
- [ ] (optional) format-on-save / reorganize-on-save toggle

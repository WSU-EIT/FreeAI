# FreeCodeReorganizer (Visual Studio extension)

The "install it like CodeMaid" front-end for the **FreeCodeReorganizer** C# member
reorganizer. It wraps the already-built Roslyn engine
(`FreeCodeReorganizer.Core`) in a classic in-process VSIX so you can reorganize
the current C# document straight from the editor.

## What it does

Running the command on a C# file:

- **Reorders members alphabetically** within each kind group (by default
  properties, indexers and methods are interleaved into one alphabetical list;
  fields/constructors keep their by-purpose order). It is deliberately
  *minimal* — comments, XML docs and your blank-line spacing are preserved, and
  a type is only rewritten when the order actually changes.
- **Restores the wrapped-parameter `){` brace style** — when a parameter list is
  wrapped across multiple lines, the closing `)` and the body's opening `{` are
  glued onto one line as `){` (the FreeCRM house style that `dotnet format`
  splits apart).

Hard safety rails in the engine abort any change that would drop/duplicate a
member or introduce a syntax error.

## Builds in Visual Studio ONLY

This is a VSSDK extension. It **cannot** be built with a bare `dotnet build` on a
machine without the VS extension tooling.

Requirements:

- **Visual Studio 2022 or 2026**
- The **"Visual Studio extension development"** workload (installs the VSSDK
  build tools that compile the `.vsct`, generate the `.pkgdef`, and pack the
  `.vsix`).

To run / build:

1. Open the solution in Visual Studio.
2. Set **FreeCodeReorganizer** as the startup project.
3. Press **F5** to launch the **Experimental Instance** (a sandboxed VS hive,
   thanks to `VSSDKTargetPlatformRegRootSuffix=Exp`) with the extension loaded, **or**
4. Build in **Release** to produce the installer at
   `bin\Release\FreeCodeReorganizer.vsix` — double-click it to install into your
   real VS.

## How to use it

Open a C# (`.cs`) file, then invoke **Reorganize Document** via any of:

- **Default keybinding:** `Ctrl+K, Ctrl+R` (a two-key chord).
- **Edit menu** → *Reorganize Document*.
- **Right-click** in the code editor → *Reorganize Document*.

The status bar reports how many types were reorganized and braces collapsed.
If nothing needed changing it says so; if the engine had to abort, a warning
dialog explains why.

## Settings

**Tools → Options → FreeCodeReorganizer → General**

| Option | Maps to `ReorderConfig.` | Default |
| --- | --- | --- |
| Sort alphabetically | `SortAlphabetically` | true |
| Ignore leading underscore in sort | `IgnoreLeadingUnderscoreInSort` | true |
| Group by visibility | `GroupByVisibility` | false |
| Static members first | `StaticMembersFirst` | false |
| Collapse wrapped-parameter brace | `CollapseWrappedParameterBrace` | true |
| Max fraction reordered | `MaxFractionReordered` | 0.35 |

`Max fraction reordered` is the safety throttle: if sorting would move more than
this fraction of a type's sortable members, the type is assumed to be ordered by
purpose and is left untouched. Set it to `1.0` to always sort.

## Caveats to verify on first run in VS

This project was scaffolded **without** being compiled (no VS / VSSDK in the
authoring environment). Treat the following as "verify in VS":

- **Roslyn version conflict risk (most important).** `FreeCodeReorganizer.Core`
  bundles `Microsoft.CodeAnalysis.CSharp` **4.12**. Because this is an
  *in-process* extension, it shares the process with Visual Studio's own Roslyn.
  If you hit an assembly-load / version-mismatch failure at runtime, the fixes,
  in order of preference, are:
  1. **Align the Core's Roslyn version** with the one your VS ships (do not bundle
     a newer `Microsoft.CodeAnalysis.*` than the host) — often the cleanest fix is
     to mark the Core's Roslyn `PackageReference` as not copied locally and rely on
     VS's, or downgrade it to match.
  2. Add binding redirects / an assembly-load handler, **or**
  3. Move the engine **out-of-process** (run the Core in a small child process and
     pipe text in/out) so the two Roslyn versions never share an AppDomain.
- **NuGet versions** in `FreeCodeReorganizer.csproj` (`Microsoft.VisualStudio.SDK`,
  `Microsoft.VSSDK.BuildTools`, `Community.VisualStudio.Toolkit.17`) are sensible
  starting points but were not restored. Bump to the latest compatible 17.x via
  the NuGet UI if restore complains.
- **GUID wiring.** The package GUID must be identical in three places:
  `[Guid]` on `FreeCodeReorganizerPackage`, the `<GuidSymbol
  name="guidFreeCodeReorganizerPackage">` in `VSCommandTable.vsct`, and the
  `package="..."` attribute reference. The command-set GUID drives
  `PackageIds.ReorganizeDocument`.
- **Toolkit API calls.** `VS.Documents.GetActiveDocumentViewAsync()`,
  `docView.TextBuffer`, `BaseOptionModel<T>.GetLiveInstanceAsync()`,
  `this.RegisterCommandsAsync()`, and `VS.MessageBox.ShowWarningAsync()` are the
  documented Community.VisualStudio.Toolkit surface — confirm member names
  against the installed toolkit version when it first compiles.
- The default chord `Ctrl+K, Ctrl+R` is unused by the C# editor by default;
  confirm no other installed extension claims it.

## Files

| File | Purpose |
| --- | --- |
| `FreeCodeReorganizer.csproj` | VSIX project; references the Core engine and the VSSDK/toolkit packages. |
| `source.extension.vsixmanifest` | Extension identity, install targets (`[17.0, 19.0)`), prerequisites. |
| `VSCommandTable.vsct` | The one command, its menu/context placements, and the keybinding. |
| `FreeCodeReorganizerPackage.cs` | `AsyncPackage` entry point; registers commands + the options page. |
| `Commands/ReorganizeDocumentCommand.cs` | Reads the active buffer, runs the engine, replaces the text. |
| `Options/GeneralOptions.cs` | Tools > Options model; builds a `ReorderConfig`. |

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The Visual Studio extension (VSIX) front-end. It adds a **Reorganize Document** command (default `Ctrl+K, Ctrl+R`, plus Edit-menu and right-click) that reads the active C# buffer, runs the shared `FreeCodeReorganizer.Core` engine, and replaces the text — alphabetizing members and restoring the wrapped-parameter `){` brace, with hard safety rails that abort if a member would be dropped or a syntax error introduced.

**What tech & where?** [the VSIX project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeTools/FreeCodeMaid/1.0/FreeCodeReorganizer) (`AsyncPackage` + `VSCommandTable.vsct` + the Core engine; Community.VisualStudio.Toolkit).

**Why does this exist?** So the reorganizer is one keystroke away in the editor — the "install it like CodeMaid" experience — rather than a separate CLI run.

**What does it beat?** It's a **minimal, safe** reorganize (comments and blank-line grouping preserved; only rewrites a type when order actually changes) with a *Tools → Options* page mapping directly to the engine's `ReorderConfig`.

**Terminology:** **Experimental Instance** — the sandboxed VS hive F5 launches for testing the extension. **Chord** — the two-key shortcut `Ctrl+K, Ctrl+R`.

**The hard part, drawn:**
```
  Ctrl+K, Ctrl+R ─▶ read active .cs buffer ─▶ FreeCodeReorganizer.Core.Run() ─▶ replace text
        engine aborts if a member would vanish / syntax breaks ─▶ status bar reports the result
```

# FreeCodeReorganizer — VS Code extension

Reorganizes C# class members alphabetically and restores the wrapped-parameter `){` brace style,
for a single file or the whole workspace. Same behavior as the Visual Studio extension because it
shares the **same engine**.

## How it works (no Node required to build or run)
This is a **plain-JavaScript** extension with **zero npm dependencies**, so VS Code runs it with its
own bundled runtime. The actual reorganizing is done by `engine/FreeCodeReorganizer.Cli.dll`, a tiny
.NET app that wraps `FreeCodeReorganizer.Core` (Roslyn) — the exact engine the Visual Studio extension
and the command-line tool use.

```
VS Code extension (extension.js, plain JS)
   └─ spawns →  engine/FreeCodeReorganizer.Cli.dll   (stdin C# → stdout reorganized C#, or --dir for repo-wide)
                   └─ FreeCodeReorganizer.Core        (the shared Roslyn engine)
```

**Requirement:** the **.NET runtime** must be installed (the extension calls `dotnet`). Set
`freecodereorganizer.dotnetPath` if `dotnet` isn't on your PATH.

## Commands
- **FreeCodeReorganizer: Reorganize Document** — the active `.cs` file. Shortcut **`Ctrl+K, Ctrl+R`**.
- **FreeCodeReorganizer: Reorganize Workspace (all .cs files)** — every `.cs` under each workspace
  folder, in place (skips `bin`/`obj`/`.git`/generated files). Asks for confirmation; commit first.

Both are also on the editor right-click menu under the **FreeCodeReorganizer** submenu.

## "Do both" (Microsoft cleanup + our reorganize)
Member *reordering* is unique to this tool — Microsoft's tooling only formats. To combine them:
- **Per file:** enable `freecodereorganizer.runFormatFirst` to run VS Code's Format Document
  (editorconfig-aware, needs a C# formatter) before reorganizing.
- **Whole repo:** enable `freecodereorganizer.runDotnetFormatOnWorkspace` to run Microsoft's
  `dotnet format` on each folder before the workspace reorganize.

## Settings
`sortAlphabetically`, `ignoreLeadingUnderscoreInSort`, `groupByVisibility`, `staticMembersFirst`,
`collapseWrappedParameterBrace`, `maxPercentReordered` (0–100), `runFormatFirst`,
`runDotnetFormatOnWorkspace`, `dotnetPath`. See *Settings → Extensions → FreeCodeReorganizer*.

## Install (manual, no Marketplace)
This folder is the installable extension. Copy it to `~/.vscode/extensions/wsu-eit.freecodereorganizer-1.0.0/`
(the `engine/` subfolder must come along) and reload the VS Code window. The `engine/` is produced by
`dotnet publish ../FreeCodeReorganizer.Cli -c Release -o engine`.

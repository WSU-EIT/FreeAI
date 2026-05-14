# Dynamic Blazor Support

This library powers FreeA11yChecker's ability to compile and render Blazor component plugins
at runtime — no application restart or recompile needed. It is used to render the `.blazor`
and `.razor` files dropped into `FreeA11yChecker/PluginFiles/BlazorComponents/`.

The implementation combines patterns from two open-source projects:

- **TryMudBlazor** — https://github.com/MudBlazor/TryMudBlazor
- **SpawnDev.BlazorJS.CodeRunner** — https://github.com/LostBeard/SpawnDev.BlazorJS.CodeRunner

The primary change from those upstream sources is switching from the older
`Microsoft.AspNetCore.Razor.Language` package (used by SpawnDev) to the newer
`Microsoft.CodeAnalysis.Razor.Compiler` assembly (used by TryMudBlazor) for converting
Razor/Blazor markup into C# before handing off to the Roslyn compiler.

This runs entirely inside the Blazor WebAssembly client — Roslyn compiles the component
in the browser and the resulting assembly is rendered inline. No server round-trip is
required for compilation, though the initial load carries the Roslyn assembly weight.

**Relevant packages in `FreeA11yChecker.Client.csproj`:**

| Package | Role |
|---------|------|
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compiler (WASM) |
| `Microsoft.CodeAnalysis.CSharp.Features` | Roslyn feature set |
| `Microsoft.Net.Compilers.Razor.Toolset` | Razor → C# compiler |
| `MetadataReferenceService.BlazorWasm` | Provides assembly metadata to Roslyn inside WASM |

---

*Part of [FreeA11yChecker](https://github.com/WSU-EIT/FreeA11yChecker) by
[WSU Enrollment Information Technology](https://em.wsu.edu/eit/)*
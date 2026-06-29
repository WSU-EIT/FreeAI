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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is the piece that lets the browser-side app compile and render Blazor component plugins *at runtime*. It converts Razor/Blazor markup into C# (via the modern Razor compiler), then hands that to Roslyn to produce an assembly — **all inside WebAssembly**, in the browser. So a dropped-in `.razor` plugin renders with no server round-trip and no app restart.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Roslyn in WASM (`Microsoft.CodeAnalysis.CSharp`) | Compile the plugin's C# in the browser | [CompilationService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Client/DynamicBlazorSupport/CompilationService.cs) |
| `Microsoft.CodeAnalysis.Razor.Compiler` | Turn `.razor` markup into C# first | [this folder](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.Client/DynamicBlazorSupport) |
| `MetadataReferenceService.BlazorWasm` | Feed assembly metadata to Roslyn in WASM | [this folder](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.Client/DynamicBlazorSupport) |

**Why does this exist?**
To allow in-browser authoring and live preview of UI plugin components — without a rebuild-and-deploy cycle.

**What does it accomplish that other tools don't?**
- A full **Razor → C# → assembly** compile that runs *in the browser*, not on a server.
- Adapted from two open-source projects (TryMudBlazor and SpawnDev.BlazorJS.CodeRunner), upgraded to the newer Razor compiler.

**Terminology & "can I see it?"**
- **Razor** — the markup language (HTML + C#) Blazor components are written in.
- **Hydration weight** — the one-time cost of downloading the Roslyn compiler into the browser.

**The hard part, drawn** — markup to live component, no server:

```
  .razor plugin text ──▶ Razor compiler (markup → C#) ──▶ Roslyn (C# → assembly) ──▶ render inline
                          └────────────── all inside the WASM client, zero server round-trip ──────┘
```

---

*Part of [FreeA11yChecker](https://github.com/WSU-EIT/FreeA11yChecker) by
[WSU Enrollment Information Technology](https://em.wsu.edu/eit/)*
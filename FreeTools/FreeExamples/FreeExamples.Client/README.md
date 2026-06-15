# FreeExamples.Client

> Blazor WebAssembly client application — UI pages, interactive examples, code playground with in-browser C#/Razor compilation, and dynamic component rendering.

**Target:** .NET 10 · **Type:** Blazor WebAssembly

---

## What This Project Contains

| Area | Description |
|------|-------------|
| **Pages** | Blazor routable pages for all examples (Dashboard, Files, Signatures, Settings, etc.) |
| **Shared** | Shared layout components and navigation (`ExampleNav.razor`) |
| **DynamicBlazorSupport** | In-browser C#/Razor compilation using Roslyn and CodeAnalysis — based on TryMudBlazor and SpawnDev.BlazorJS.CodeRunner |
| **Helpers** | Client-side utility classes, app icons, menu definitions |
| **wwwroot** | Static assets — CSS, JavaScript, images |

---

## Key Dependencies

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Components.WebAssembly` | Blazor WASM runtime |
| `MudBlazor` | Material Design component library |
| `Blazor.Bootstrap` | Bootstrap component library |
| `Radzen.Blazor` | Radzen component library |
| `BlazorMonaco` | Monaco code editor (VS Code editor in browser) |
| `BlazorSortableList` | Drag-and-drop sortable lists |
| `Blazored.LocalStorage` | Browser local storage access |
| `FreeBlazor` | Custom Blazor component library |
| `FluentValidation` | Model validation rules |
| `CsvHelper` | CSV parsing/generation |
| `HtmlAgilityPack` | HTML parsing |
| `Humanizer` | String humanization (pluralization, casing, etc.) |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compiler for code playground |
| `Microsoft.CodeAnalysis.CSharp.Features` | Roslyn code analysis features (completions, diagnostics) |
| `Microsoft.AspNetCore.SignalR.Client` | SignalR client for real-time features |
| `MetadataReferenceService.BlazorWasm` | Assembly metadata resolution in WASM |

---

Part of the FreeTools solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The browser UI (Blazor WebAssembly) for all the examples — dashboards, files, signatures, settings — and its standout feature: an in-browser **code playground** that compiles C#/Razor *in the browser* with Roslyn (via the `DynamicBlazorSupport` subsystem) and renders the result, with a Monaco (VS Code) editor.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| In-browser Roslyn compile | The code playground | [DynamicBlazorSupport/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeExamples/FreeExamples.Client/DynamicBlazorSupport) |
| BlazorMonaco | The in-browser code editor | [the Client project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeTools/FreeExamples/FreeExamples.Client) |

**Why does this exist?** A live, interactive gallery of Blazor patterns — including the ability to write and run C#/Razor right in the page.

**What does it beat?** A **real in-browser compiler**: type code, compile with Roslyn-in-WASM, see it render — no server round-trip.

**Terminology:** **Code playground** — an editor + live compile/preview in the browser; **WASM** — WebAssembly, which runs the compiled C#.

**The hard part, drawn:**
```
  you type C#/Razor in Monaco ─▶ DynamicBlazorSupport (Roslyn in WASM) compiles ─▶ rendered inline
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

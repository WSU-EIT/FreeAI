# Dynamic Blazor Support

This code was built using a combination of code from TryMudBlazor and SpawnDev.BlazorJS.CodeRunner.
For more details about those projects, please see the links below:

- https://github.com/MudBlazor/TryMudBlazor
- https://github.com/LostBeard/SpawnDev.BlazorJS.CodeRunner

Some changes were made to the included nuget packages to support using the
newer Assembly Microsoft.CodeAnalysis.Razor.Compiler nuget package used
by TryMudBlazor instead of the older Microsoft.AspNetCore.Razor.Language package
used by SpawnDev.BlazorJS.CodeRunner to convert the Blazor code file into C# code.

---

## 🧭 Plain-English Briefing — The Boss Questions

**In one line:** this folder lets the browser compile and render Blazor *plugin* components at runtime — turning `.razor` markup into C#, then into a live assembly, entirely inside WebAssembly (no server, no rebuild).

**Why it exists:** so plugin UI (e.g. a custom agent-dashboard widget) can be authored and previewed in the browser without a deploy cycle.

**See the full briefing** — how it fits the app, exact files, and the compile diagram — in the parent [FreeServicesHub.Client README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.Client/README.md).
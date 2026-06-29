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

**In one line:** this folder powers the FreeExamples **code playground** — it compiles C#/Razor into a live component *in the browser* (Roslyn in WebAssembly), no server round-trip.

**Why it exists:** so users can write and run code right on the page. **See the full briefing** (the playground, exact files, and compile flow) in the parent [FreeExamples.Client README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeExamples/FreeExamples.Client/README.md).
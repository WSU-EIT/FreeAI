# BlazorApp1.Client

Blazor WebAssembly client for BlazorApp1, the throwaway scan target in the FreeA11yChecker solution. Contains the stock .NET 10 Blazor template client pages: Home, Counter, Weather, and Auth. No custom logic; exists solely to give the FreeA11yChecker scanner a real WASM SPA with interactive rendering to audit.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target framework | `net10.0` |
| Output type | Blazor WebAssembly (client-side) |

## @page Routes

| Route | Component |
|---|---|
| `/` | `Home.razor` |
| `/counter` | `Counter.razor` |
| `/weather` | `Weather.razor` |
| `/auth` | `Auth.razor` |
| `/not-found` | `NotFound.razor` |

## Key Classes

| Class / File | Purpose |
|---|---|
| `Program.cs` | WASM entry point; minimal setup — no custom services beyond the default WASM builder |
| `Routes.razor` | Root router component |
| `RedirectToLogin.razor` | Client-side redirect helper for unauthenticated users |
| `Layout/MainLayout.razor` | Default layout with nav menu |

## Project References

None — this project has no local project references.

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.AspNetCore.Components.WebAssembly` | Blazor WASM runtime |
| `Microsoft.AspNetCore.Components.WebAssembly.Authentication` | Authentication state in WASM |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The WebAssembly half of the throwaway scan target — the stock template pages (Home, Counter, Weather, Auth) with no custom logic. It exists purely to give the scanner a real client-side single-page app to crawl and hydrate.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Blazor WebAssembly runtime | C# UI in the browser | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/BlazorApp1/BlazorApp1.Client/Program.cs) |
| WASM authentication state | Auth-aware client pages | [the client project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/BlazorApp1/BlazorApp1.Client) |

**Why does this exist?**
A real WASM single-page app the scanner can hydrate and audit — so the tool is exercised against the same kind of client-rendered UI as a production app.

**What does it accomplish that other tools don't?**
- It's intentionally **minimal** — its value is being *typical*, so template-level accessibility issues surface where they can be caught.

**Terminology & "can I see it?"**
- **SPA** (Single-Page App) — one page that swaps content with JavaScript/WASM instead of full reloads.
- **Hydration** — the moment the downloaded C# wakes up and the page becomes interactive.

**The hard part, drawn** — a typical WASM SPA, on purpose:

```
  WASM client (Home / Counter / Weather / Auth) ──served by──▶ BlazorApp1 host ──audited by──▶ Scanner
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

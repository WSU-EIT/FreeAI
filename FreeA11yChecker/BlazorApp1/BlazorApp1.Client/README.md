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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

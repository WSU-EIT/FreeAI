# BlazorApp1

Throwaway Blazor WebAssembly + Server app used as a scan target by the FreeA11yChecker accessibility auditing tools. It is the stock .NET 10 Blazor template with two modifications: the EF Core database is swapped to `InMemory` (no SQL Server setup required), and a seeded admin user (`admin@example.com` / `Admin1234!`) is created at startup so the scanner can authenticate immediately.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target framework | `net10.0` |
| Output type | Web application (Blazor Server host) |

## What It Does

- Registers Blazor Identity with an InMemory EF Core database and `RequireConfirmedAccount = false` so the seeded user can log in without email confirmation.
- Seeds one admin user on startup (`SeedTestUserAsync`): `admin@example.com` / `Admin1234!`.
- Serves the `BlazorApp1.Client` WASM project for interactive client-side rendering.
- The intentional template code (counter, weather, auth pages) provides a surface for the FreeA11yChecker scanner to exercise typical Blazor patterns and find template-level accessibility issues.

## @page Routes (Server)

| Route | Component |
|---|---|
| `/Error` | `Error.razor` |
| `/Account/Login` | `Login.razor` |
| `/Account/Register` | `Register.razor` |
| `/Account/ForgotPassword` | `ForgotPassword.razor` |
| `/Account/Manage` | `Index.razor` (manage profile) |
| `/Account/Manage/ChangePassword` | `ChangePassword.razor` |
| `/Account/Manage/Email` | `Email.razor` |
| `/Account/Manage/TwoFactorAuthentication` | `TwoFactorAuthentication.razor` |
| `/Account/Manage/Passkeys` | `Passkeys.razor` |

## @page Routes (Client — `BlazorApp1.Client`)

| Route | Component |
|---|---|
| `/` | `Home.razor` |
| `/counter` | `Counter.razor` |
| `/weather` | `Weather.razor` |
| `/auth` | `Auth.razor` |
| `/not-found` | `NotFound.razor` |

## Key Classes / Methods

| Class | Purpose |
|---|---|
| `Program` | Entry point; configures InMemory Identity, seeds admin user, maps Blazor and Identity endpoints |
| `SeedTestUserAsync` | Creates `admin@example.com` if not present; credentials are also defined as `SeedEmail` / `SeedPassword` constants for scanner config reference |
| `ApplicationDbContext` | EF Core InMemory `IdentityDbContext<ApplicationUser>` |
| `ApplicationUser` | Extends `IdentityUser`; no additional properties beyond the base class |

## Project References

| Project | Role |
|---|---|
| `BlazorApp1.Client` | WASM client project |

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | ASP.NET Core Identity |
| `Microsoft.EntityFrameworkCore.InMemory` | InMemory database (no setup required) |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | Serves the WASM client |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

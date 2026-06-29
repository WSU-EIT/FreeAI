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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A deliberately plain Blazor app that exists only as a *guinea pig* for the scanner. It's the stock .NET template with two tweaks: an **InMemory database** (no setup) and a **seeded admin user** so the scanner can log in immediately and exercise authenticated pages.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Blazor Identity + InMemory EF Core | Login flow with zero DB setup | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/BlazorApp1/BlazorApp1/Program.cs) |
| Seeded admin user | Lets the scanner authenticate immediately | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/BlazorApp1/BlazorApp1/Program.cs) |
| Stock template pages | A known surface to audit | [the BlazorApp1 project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/BlazorApp1/BlazorApp1) |

**Why does this exist?**
It gives the scanner a real, login-protected Blazor app to test against — including the *anonymous-vs-authenticated* evidence flow — without depending on any external website.

**What does it accomplish that other tools don't?**
- It isn't a product; it's a **controlled test fixture** with known, seedable accessibility characteristics — so scanner results are predictable and the auth path is always testable.

**Terminology & "can I see it?"**
- **InMemory database** — a throwaway database that lives in RAM; nothing to install.
- **Seeded user** — a login account created automatically at startup (`admin@example.com` / `Admin1234!`).
- **Scan target / fixture** — an app that exists to be tested, not shipped.

**The hard part, drawn** — a known target the scanner can log into:

```
  Program.cs ─▶ InMemory Identity + seed admin@example.com
       └─▶ stock pages (counter / weather / auth) = a predictable surface
                       │
                       ▼  scanned (and logged into) by  ─▶  FreeA11yChecker scanner
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

# BlazorApp1

Blazor Server interactive application generated from the ASP.NET Core Blazor Web App template. Used as a throwaway scan/test target by analysis tools in the FreePlugins workspace.

## What it does

- Hosts Blazor Server interactive components with `AddInteractiveServerComponents`
- Wires ASP.NET Core Identity (v3 schema) with SQL Server via Entity Framework Core
- Provides full Identity scaffolding: registration, login, password reset, 2FA (TOTP + passkeys), external logins, account management pages
- Configures cascading auth state and `IdentityRevalidatingAuthenticationStateProvider`

## Notable components

| File / Folder | Purpose |
|---------------|---------|
| `Program.cs` | Application entry point — DI bootstrap, middleware pipeline |
| `Data/ApplicationDbContext.cs` | EF Core Identity DbContext |
| `Data/ApplicationUser.cs` | Identity user entity |
| `Components/Account/` | Full set of Identity UI Razor components (login, register, manage, passkeys, 2FA, etc.) |
| `Components/Pages/` | Sample pages: `Home`, `Counter`, `Weather`, `Auth`, `Error`, `NotFound` |
| `Components/Layout/` | Main layout, nav menu, reconnect modal |

## Notable NuGet packages

| Package | Version |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `10.0.1` |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.1` |
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` | `10.0.1` |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.1` (build-time only) |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target framework | `net10.0` |
| Nullable | enabled |
| Implicit usings | enabled |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A stock Blazor Server app from the .NET template, with full ASP.NET Core Identity (register/login/2FA/passkeys). It exists only as a **throwaway test target** for the workspace's analysis tools.

**What tech & where?** [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/BlazorApp1/Program.cs) · [Data/ApplicationDbContext.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/BlazorApp1/Data/ApplicationDbContext.cs).

**Why does this exist?** To give the plugin/analysis tooling a real, login-protected app to run against without depending on an external site.

**What does it beat?** It's a **controlled fixture** — a typical Identity app with known routes and protected pages — not a product.

**Terminology:** **Test target / fixture** — an app that exists to be analyzed, not shipped.

**The hard part, drawn:**
```
  BlazorApp1 (stock Blazor Server + Identity) ──used as a target by──▶ the workspace's analysis tools
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

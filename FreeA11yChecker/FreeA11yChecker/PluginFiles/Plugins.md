# Plugin Architecture

## Why Use Plugins?

The plugin architecture lets you extend FreeA11yChecker without modifying the core source code.
This is especially useful for:

- **Custom authentication** — connect to your institution's SSO, LDAP, or MFA system without
  touching built-in auth code.
- **Post-scan automation** — send webhook notifications, push results to a ticketing system,
  or archive reports to SharePoint after each scan completes.
- **Tenant-specific behaviour** — since plugins can be scoped to one or more tenants via
  `LimitToTenants`, you can deliver custom logic to a specific site or organization without
  adding in-page conditionals throughout the codebase.
- **UI extensions** — drop a `.blazor`/`.razor` file into `PluginFiles/BlazorComponents/` and
  the application will render it dynamically in designated page slots — no recompile required.

## Overview

On application startup all plugins found in the `PluginFiles/` folder are loaded into the
DI container and are available throughout the application. Plugin types determine how and
where they are invoked:

| Type | When Invoked | Common Uses |
|------|-------------|-------------|
| `Auth` | Login flow only | Custom SSO, LDAP, MFA, SAML providers |
| `BackgroundProcess` | Every background service tick | Post-scan webhooks, report archiving, alerting |
| `Example` | On-demand from code | General extensibility hooks |
| `UserUpdate` | When a user record is saved | Sync user data to external systems |
| Blazor Components | Page render | Custom UI panels, buttons, and tab content injected into pages |

## Plugin File Types

Plugin files live in `FreeA11yChecker/PluginFiles/` and use one of two extensions:

- **`.cs`** — compiled as part of the solution. Use when the plugin can reference assemblies
  already in the project. Developed and edited directly in Visual Studio with full IntelliSense.
- **`.plugin`** — plain text, compiled at runtime via Roslyn. Use when the plugin references
  external DLLs not in the solution (prevents build errors). Can be authored in LINQPad or
  any text editor using `FreeA11yChecker.DataAccess.dll`, `FreeA11yChecker.DataObjects.dll`,
  and `FreeA11yChecker.dll` from `bin/Debug/net10.0/` for IntelliSense.

A `.assemblies` sidecar file (same base name) lists any external DLL references the plugin needs:

```
.\MyPlugin\MyPlugin.dll
typeof(SomeNameSpace.SomeType).Assembly.Location
```

## Plugin Requirements

Every plugin must expose a public `Properties()` method returning a `Dictionary<string, object>`:

```csharp
public Dictionary<string, object> Properties() =>
    new Dictionary<string, object> {
        { "Id", new Guid("00000000-0000-0000-0000-000000000000") },  // Must be unique
        { "Author", "WSU-EIT" },
        { "ContainsSensitiveData", false },
        { "Description", "What this plugin does." },
        { "Name", "My Plugin Name" },
        { "SortOrder", 0 },
        { "Type", "Example" },    // Auth | BackgroundProcess | Example | UserUpdate
        { "Version", "1.0.0" }
    };
```

Generate a fresh `Guid` for every new plugin — do not copy an existing one.
Plugins sort by `SortOrder` then `Name` when more than one plugin of a type runs together.

## Prompts (Optional UI Input Collection)

The optional `Prompts` property accepts a `List<PluginPrompt>` that drives a built-in UI for
collecting input before plugin execution. Prompt types are defined by `PluginPromptType`:
`Text`, `Password`, `Checkbox`, `Dropdown`, `Button`.

For `Auth` plugins, `Username` (type `Text`) and `Password` (type `Password` or `Text`) prompts
are required — they are always treated as required fields even without the Required flag set.

Use the `PluginPrompts` Blazor component to render the prompt UI. Required fields show an
asterisk. Set `HighlightMissingRequiredFields="true"` to apply the `.m-r` highlight class
to unfilled required prompts. See `PluginTesting.razor` for a complete usage example.

## Execution

Non-auth plugins call an `Execute` method. By convention the arguments are:

1. The `DataAccess` library instance
2. The `Plugin` object
3. The current `DataObjects.User` (may be null for unauthenticated contexts)
4. Any additional objects your calling code passes

```csharp
public async Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Execute(
    DataAccess da,
    Plugins.Plugin plugin,
    DataObjects.User? currentUser
)
{
    var messages = new List<string>();
    messages.Add("Plugin executed: " + plugin.Name);

    // Return any objects your calling code needs to consume.
    object[] output = new object[] { "Result data here." };

    return (Result: true, Messages: messages, Objects: output);
}
```

Methods can be `async` or synchronous.

## Restricting to Specific Tenants

Add a `LimitToTenants` property (`List<Guid>` of TenantIds) to restrict a plugin to
specific sites or organizations. If omitted, the plugin is available to all tenants.
This allows delivering custom scan post-processing, branding, or auth flows to one tenant
without affecting others.

## Built-In Plugin Conventions

### Interfaces

| Interface | For |
|-----------|-----|
| `IPluginBase` | All plugins — requires `Properties()` |
| `IPlugin` | Standard plugins — requires `Execute()` |
| `IAuthPlugin` | Auth plugins — requires `Login()` and `Logout()` |

Implementing the interfaces is optional but recommended for consistency.

### Auth Plugins

Auth plugins appear only on the login page. They must implement `Login` and `Logout`:

```csharp
public async Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Login(
    DataAccess da,
    Plugins.Plugin plugin,
    string url,         // Current page URL
    Guid tenantId,
    Microsoft.AspNetCore.Http.HttpContext httpContext
)
{
    // Authenticate against your external system here.
    // Return a user-shaped object on success.
    var user = new {
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane.doe@wsu.edu",
        Username = "jane.doe",
        EmployeeId = "12345",
        DepartmentName = "IT Accessibility"
    };

    return (Result: true, Messages: null, Objects: [user]);
}
```

Return at minimum the `.Email` property. Include `FirstName`, `LastName`, `Username`,
`EmployeeId`, and `DepartmentName` when auto-creating accounts for first-time logins.

### BackgroundProcess Plugins

These run on every background service tick alongside the built-in scheduled scanner.
Use them to push completed scan results to external systems, send WCAG violation summary
emails, or archive reports. See `ExampleBackgroundProcess.cs` for a starter template.

### Examples

The `PluginFiles/` folder includes working examples for every plugin type:

| File | Type | What It Shows |
|------|------|--------------|
| `Example1.cs` | Example | Basic execute, custom button options |
| `Example2.cs` | Example | Passing and returning objects |
| `Example3.cs` | Example | Prompt-driven input |
| `ExampleBackgroundProcess.cs` | BackgroundProcess | Console + log file output on each tick |
| `LoginWithPrompts.cs` | Auth | Prompt-based login flow |
| `UserUpdate.cs` | UserUpdate | Reacting to user save events |
| `HelloWorld.plugin` + `.assemblies` | Example | External DLL reference pattern |

---

*© Washington State University — Enrollment Information Technology*  
*github.com/WSU-EIT*
using external DLL files in HelloWorld example.
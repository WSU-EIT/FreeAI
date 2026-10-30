# FreeAccessibilityChecker -- Plugins

> **Category:** Architecture
> **Purpose:** FreeAccessibilityChecker's notes on the FreeCRM plugin system. These notes were written into FreeCRM's own
> plugin docs, which are back to stock now so a framework upgrade can overwrite them. Each section below is the
> FreeAccessibilityChecker version of one of those files, unchanged.

---

<!-- Section: FreeAccessibilityChecker version of FreeAccessibilityChecker/PluginFiles/Plugins.md -->

## Server plugins (`FreeAccessibilityChecker/PluginFiles/`)

# Plugin Architecture

## Why Use Plugins?

The plugin architecture lets you extend FreeAccessibilityChecker without modifying the core source code.
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

Plugin files live in `FreeAccessibilityChecker/PluginFiles/` and use one of two extensions:

- **`.cs`** — compiled as part of the solution. Use when the plugin can reference assemblies
  already in the project. Developed and edited directly in Visual Studio with full IntelliSense.
- **`.plugin`** — plain text, compiled at runtime via Roslyn. Use when the plugin references
  external DLLs not in the solution (prevents build errors). Can be authored in LINQPad or
  any text editor using `FreeAccessibilityChecker.DataAccess.dll`, `FreeAccessibilityChecker.DataObjects.dll`,
  and `FreeAccessibilityChecker.dll` from `bin/Debug/net10.0/` for IntelliSense.

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

---

<!-- Section: FreeAccessibilityChecker version of FreeAccessibilityChecker/PluginFiles/BlazorComponents/README.md -->

## Blazor component plugins (`FreeAccessibilityChecker/PluginFiles/BlazorComponents/`)

# Blazor Component Plugins

## Overview

Blazor component plugins let you inject custom UI into FreeAccessibilityChecker pages without
recompiling the application. Drop a `.blazor` or `.razor` file into this folder and
the app will compile and render it dynamically at runtime using the built-in Roslyn pipeline
(derived from TryMudBlazor and SpawnDev.BlazorJS.CodeRunner).

Typical uses include:
- Custom reporting widgets on the scan dashboard
- Tenant-specific WCAG remediation guidance panels
- Buttons that trigger external ticketing or webhook integrations
- Extra tab content on the site settings or user pages

## File Naming Convention

The filename determines **where** in the application the component appears.

### Button-type components (appear in page button menus)

```
Button_{PageName}_{ComponentName}.blazor
```

Examples:
```
Button_Index_ExportToTicketing.blazor
Button_ScanHistory_DownloadCustomReport.blazor
```

### Content components (appear in a named slot on a page)

```
{Slot}_{PageName}_{ComponentName}.blazor
```

Examples:
```
Top_Index_ScanSummaryBanner.blazor
Bottom_EditUser_RemediationNotes.blazor
```

For pages with a tabbed interface, target the top or bottom of a specific tab:

```
TabGeneralTop_Settings_CustomBranding.blazor
TabThemeBottom_Settings_ColorContrastPreview.blazor
```

## Component Requirements

### Required parameter: `PluginName`

Every component must declare a `PluginName` string parameter — the framework passes it
automatically when rendering.

```razor
[Parameter] public string PluginName { get; set; } = string.Empty;
```

### Data parameter: `Value` + `ValueChanged`

If the host page passes contextual data (e.g., a `DataObjects.Site` on the site edit page),
the data arrives as a `Value` parameter. Declare both the parameter and its `EventCallback`
to support two-way binding:

```razor
[Parameter] public DataObjects.Site? Value { get; set; }
[Parameter] public EventCallback<DataObjects.Site?> ValueChanged { get; set; }
```

### Initialization callback: `OnInitializedCallback`

To be notified when the component has initialized, declare this optional parameter:

```razor
[Parameter] public EventCallback<string> OnInitializedCallback { get; set; }
```

The framework calls it with `PluginName` as the argument.

## Optional Configuration File

Include a `.json` file alongside the component (same base name, no `.blazor`/`.razor` extension)
to control metadata:

```json
{
  "Id": "00000000-0000-0000-0000-000000000000",
  "Author": "Your Name",
  "Description": "Shows a custom WCAG summary widget on the scan dashboard.",
  "LimitToTenants": [],
  "SortOrder": 10,
  "Version": "1.0.0"
}
```

| Field | Purpose |
|-------|---------|
| `Id` | Unique Guid — generate a fresh one for every component |
| `LimitToTenants` | Array of TenantId Guids — empty means all tenants |
| `SortOrder` | Controls rendering order when multiple components target the same slot |

If no config file is present, `SortOrder` defaults to 0 and the component is visible to all tenants.
Components in the same slot are sorted by `SortOrder` then `Name`.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
Drop a `.razor` file into this folder and the app compiles and renders it at runtime — and the **filename decides where it appears**. `Button_Index_…` adds a button to the Index page; `Top_Index_…` injects content at the top of Index; `TabGeneralTop_Settings_…` targets a specific tab. An optional `.json` sidecar (same base name) controls which tenants see it and in what order.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Runtime Razor→C#→assembly compile | Render your `.razor` with no rebuild | [DynamicBlazorSupport](https://github.com/WSU-EIT/FreeAI/tree/main/FreeAccessibilityChecker/FreeAccessibilityChecker.Client/DynamicBlazorSupport) |
| Roslyn plugin host | Loads & compiles plugin files | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeAccessibilityChecker/FreeAccessibilityChecker.Plugins/Plugins.cs) |
| Filename + `.json` convention | Decides placement, tenant scope, sort order | this folder (see the naming rules above) |

**Why does this exist?**
So the UI can be customized per tenant — extra widgets, branding, remediation panels — **without touching or rebuilding the core app**.

**What does it accomplish that other tools don't?**
- **Convention over configuration**: the filename *is* the wiring — no registration code, no rebuild.
- **Tenant-scoped** rendering via the sidecar (`LimitToTenants`, `SortOrder`).

**Terminology & "can I see it?"**
- **Slot** — a named spot on a page (`Top`, `Bottom`, a tab) where injected content lands.
- **Sidecar** — the optional `.json` next to your component carrying its metadata.

**The hard part, drawn** — the filename routes the component:

```
  Button_{Page}_{Name}.razor ─drop in─▶ compiled at runtime ─▶ appears as a button on {Page}
  {Slot}_{Page}_{Name}.razor ─drop in─▶ compiled at runtime ─▶ injected into {Slot} of {Page}
        optional {Name}.json  ─────────▶ LimitToTenants[] · SortOrder  (who sees it, in what order)
```

---

*Part of [FreeAccessibilityChecker](https://github.com/WSU-EIT/FreeA11yChecker) by
[WSU Enrollment Information Technology](https://em.wsu.edu/eit/)*
property specified, or make sure the names will sort in the order you want.

---

<!-- Section: FreeAccessibilityChecker version of FreeAccessibilityChecker.Client/DynamicBlazorSupport/README.md -->

## Dynamic Blazor support (`FreeAccessibilityChecker.Client/DynamicBlazorSupport/`)

# Dynamic Blazor Support

This library powers FreeAccessibilityChecker's ability to compile and render Blazor component plugins
at runtime — no application restart or recompile needed. It is used to render the `.blazor`
and `.razor` files dropped into `FreeAccessibilityChecker/PluginFiles/BlazorComponents/`.

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

**Relevant packages in `FreeAccessibilityChecker.Client.csproj`:**

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
| Roslyn in WASM (`Microsoft.CodeAnalysis.CSharp`) | Compile the plugin's C# in the browser | [CompilationService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeAccessibilityChecker/FreeAccessibilityChecker.Client/DynamicBlazorSupport/CompilationService.cs) |
| `Microsoft.CodeAnalysis.Razor.Compiler` | Turn `.razor` markup into C# first | [this folder](https://github.com/WSU-EIT/FreeAI/tree/main/FreeAccessibilityChecker/FreeAccessibilityChecker.Client/DynamicBlazorSupport) |
| `MetadataReferenceService.BlazorWasm` | Feed assembly metadata to Roslyn in WASM | [this folder](https://github.com/WSU-EIT/FreeAI/tree/main/FreeAccessibilityChecker/FreeAccessibilityChecker.Client/DynamicBlazorSupport) |

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

*Part of [FreeAccessibilityChecker](https://github.com/WSU-EIT/FreeA11yChecker) by
[WSU Enrollment Information Technology](https://em.wsu.edu/eit/)*

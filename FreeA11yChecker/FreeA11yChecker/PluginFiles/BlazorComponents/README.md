# Blazor Component Plugins

## Overview

Blazor component plugins let you inject custom UI into FreeA11yChecker pages without
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
| Runtime Razor→C#→assembly compile | Render your `.razor` with no rebuild | [DynamicBlazorSupport](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.Client/DynamicBlazorSupport) |
| Roslyn plugin host | Loads & compiles plugin files | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Plugins/Plugins.cs) |
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

*Part of [FreeA11yChecker](https://github.com/WSU-EIT/FreeA11yChecker) by
[WSU Enrollment Information Technology](https://em.wsu.edu/eit/)*
property specified, or make sure the names will sort in the order you want.
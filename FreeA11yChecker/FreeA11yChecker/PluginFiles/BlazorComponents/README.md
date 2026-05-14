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

*Part of [FreeA11yChecker](https://github.com/WSU-EIT/FreeA11yChecker) by
[WSU Enrollment Information Technology](https://em.wsu.edu/eit/)*
property specified, or make sure the names will sort in the order you want.
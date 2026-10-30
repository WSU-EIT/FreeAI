# 208 — Standup: Plugin Dashboard Planning

> **Document ID:** 208  
> **Category:** Meeting  
> **Purpose:** Team standup to define MVP features for a Plugin Dashboard/Info page  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality], [Sanity], [JrDev]  
> **Date:** 2025-01-XX  
> **Predicted Outcome:** Feature set defined for Plugin Dashboard  
> **Actual Outcome:** *(Pending)*  
> **Resolution:** *(Pending)*

---

## TL;DR

**What:** Admin page that displays all loaded plugins, their types, versions, and configuration.  
**Type:** Blazor page + API endpoint (NOT a BackgroundProcess plugin)  
**Value:** Visibility into plugin ecosystem without reading source files.

---

## Standup Context

Unlike our previous plugins (BackgroundProcess type), this is a **UI feature** — a Blazor page that queries and displays plugin information. It needs an API endpoint and a Razor page.

---

## Discussion

**[Architect]:** Alright, we want a dashboard showing all plugins in the system. Let me start by documenting what data we have available.

### Plugin Object Properties (from `Plugins.cs`)

```csharp
public class Plugin
{
    public Guid Id { get; set; }
    public string Author { get; set; }
    public string ClassName { get; set; }
    public string Code { get; set; }                    // Don't expose!
    public bool ContainsSensitiveData { get; set; }
    public string Description { get; set; }
    public List<Guid> LimitToTenants { get; set; }
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Invoker { get; set; }                 // Default: "Execute"
    public List<PluginPrompt> Prompts { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public int SortOrder { get; set; }
    public string Type { get; set; }
    public string Version { get; set; }
    public List<string> AdditionalAssemblies { get; set; }
}
```

### Plugin Interface Types (from `PluginsInterfaces.cs`)

| Interface | Type String | Method | Parameters |
|-----------|-------------|--------|------------|
| `IPlugin` | "General" | `Execute()` | da, plugin, currentUser |
| `IPluginAuth` | "Auth" | `Login()`, `Logout()` | da, plugin, url, tenantId, httpContext |
| `IPluginBackgroundProcess` | "BackgroundProcess" | `Execute()` | da, plugin, iteration |
| `IPluginUserUpdate` | "UserUpdate" | `UpdateUser()` | da, plugin, updateUser |

**[Backend]:** We already have `GetPluginsWithoutCode()` in DataAccess that strips the Code property before returning. That's what we should expose to the UI.

**[JrDev]:** Why strip the code?

**[Quality]:** Security. Plugin code could contain sensitive logic, API keys hardcoded (bad practice but happens), or internal business rules. We expose metadata only.

**[Frontend]:** What should the page look like? Cards? Table?

**[Architect]:** Table is simpler and more functional for admins. Cards are prettier but less information-dense.

**[Sanity]:** Mid-check. Table for MVP. Cards for v2 if anyone asks.

---

## Feature Brainstorm

| # | Feature | Complexity | Value | MVP? |
|---|---------|------------|-------|------|
| 1 | List all plugins in table | Low | High | ✅ |
| 2 | Show Name, Type, Version, Author | Low | High | ✅ |
| 3 | Show Description on hover/expand | Low | Medium | ✅ |
| 4 | Group/filter by Type | Medium | High | ✅ |
| 5 | Show SortOrder | Low | Low | ✅ |
| 6 | Show Enabled status (from Properties) | Low | High | ✅ |
| 7 | Show LimitToTenants | Low | Medium | ⚠️ v1.1 |
| 8 | Show ContainsSensitiveData flag | Low | Low | ✅ |
| 9 | Show Prompts configuration | Medium | Medium | ⚠️ v1.1 |
| 10 | Show AdditionalAssemblies | Low | Low | ⚠️ v1.1 |
| 11 | Plugin health/last run status | High | High | ❌ v2 |
| 12 | Enable/disable plugins from UI | High | Medium | ❌ v2 |
| 13 | View plugin source code (admin only) | Medium | Low | ❌ Never |
| 14 | Plugin execution log | High | High | ❌ v2 |

---

## Final Scope

```
┌─────────────────────────────────────────────────────────────┐
│            Plugin Dashboard v1.0 — MVP SCOPE                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ✅ IN SCOPE (v1.0)                                         │
│  ─────────────────                                          │
│  • Blazor page at /Settings/Plugins                         │
│  • API endpoint: GET /api/Data/GetPlugins                   │
│  • Table showing all loaded plugins                         │
│  • Columns: Name, Type, Version, Author, SortOrder, Enabled │
│  • Filter/group by Type dropdown                            │
│  • Expandable row for Description                           │
│  • ContainsSensitiveData indicator                          │
│  • Admin-only access                                        │
│                                                             │
│  📋 v1.1 (After MVP)                                        │
│  ──────────────────                                         │
│  • Show LimitToTenants                                      │
│  • Show Prompts configuration                               │
│  • Show AdditionalAssemblies                                │
│  • Plugin count by type summary cards                       │
│                                                             │
│  ❌ OUT OF SCOPE (v2+)                                      │
│  ──────────────────────                                     │
│  • Enable/disable from UI                                   │
│  • Plugin execution log                                     │
│  • Health/status monitoring                                 │
│  • Source code viewing                                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Design

### Component 1: API Endpoint (DataController)

```csharp
// DataController.App.Plugins.cs (new file)
namespace FreePlugins;

public partial class DataController
{
    [HttpGet]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetPluginInfo")]
    public async Task<ActionResult<List<DataObjects.PluginInfo>>> GetPluginInfo()
    {
        await Task.CompletedTask;
        
        var plugins = da.GetPluginsWithoutCode();
        
        var output = plugins.Select(p => new DataObjects.PluginInfo {
            Id = p.Id,
            Name = p.Name,
            Type = p.Type,
            Version = p.Version,
            Author = p.Author,
            Description = p.Description,
            SortOrder = p.SortOrder,
            ContainsSensitiveData = p.ContainsSensitiveData,
            Enabled = GetPluginEnabled(p),
            Namespace = p.Namespace,
            ClassName = p.ClassName,
            Invoker = p.Invoker,
            HasPrompts = p.Prompts?.Any() ?? false,
            PromptCount = p.Prompts?.Count ?? 0,
            TenantRestricted = p.LimitToTenants?.Any() ?? false,
            TenantCount = p.LimitToTenants?.Count ?? 0,
        }).OrderBy(x => x.Type).ThenBy(x => x.SortOrder).ThenBy(x => x.Name).ToList();
        
        return Ok(output);
    }
    
    private bool GetPluginEnabled(Plugins.Plugin plugin)
    {
        if (plugin.Properties?.ContainsKey("Enabled") == true) {
            try { return (bool)plugin.Properties["Enabled"]; } catch { }
        }
        return true; // Default to enabled if not specified
    }
}
```

### Component 2: DTO (DataObjects)

```csharp
// DataObjects.App.Plugins.cs (new file)
namespace FreePlugins;

public partial class DataObjects
{
    /// <summary>
    /// Plugin information for dashboard display (no code exposed)
    /// </summary>
    public class PluginInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Version { get; set; } = "";
        public string Author { get; set; } = "";
        public string Description { get; set; } = "";
        public int SortOrder { get; set; }
        public bool ContainsSensitiveData { get; set; }
        public bool Enabled { get; set; } = true;
        public string Namespace { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Invoker { get; set; } = "Execute";
        public bool HasPrompts { get; set; }
        public int PromptCount { get; set; }
        public bool TenantRestricted { get; set; }
        public int TenantCount { get; set; }
    }
    
    /// <summary>
    /// Plugin type enum for filtering
    /// </summary>
    public static class PluginTypes
    {
        public const string All = "";
        public const string General = "General";
        public const string Auth = "Auth";
        public const string BackgroundProcess = "BackgroundProcess";
        public const string UserUpdate = "UserUpdate";
    }
}
```

### Component 3: Blazor Page

```razor
@* FreePlugins.App.Plugins.razor *@
@page "/Settings/Plugins"
@page "/{TenantCode}/Settings/Plugins"

@using Blazored.LocalStorage
@inject HttpClient Http
@inject BlazorDataModel Model
@implements IDisposable

@if (Model.Loaded && Model.View == _pageName) {
    @if (_loading) {
        <h1 class="page-title">
            <Language Tag="Plugins" IncludeIcon="true" />
        </h1>
        <LoadingMessage />
    } else {
        <div class="@Model.StickyMenuClass">
            <h1 class="page-title">
                <Language Tag="Plugins" IncludeIcon="true" />
                <StickyMenuIcon />
            </h1>
            
            <div class="btn-toolbar mb-2">
                <div class="btn-group me-2">
                    <select class="form-select" @bind="_filterType">
                        <option value="">All Types</option>
                        <option value="Auth">Auth</option>
                        <option value="BackgroundProcess">BackgroundProcess</option>
                        <option value="General">General</option>
                        <option value="UserUpdate">UserUpdate</option>
                    </select>
                </div>
            </div>
        </div>
        
        @* Summary Cards *@
        <div class="row mb-3">
            <div class="col-md-2">
                <div class="card text-center">
                    <div class="card-body">
                        <h5 class="card-title">@_plugins.Count</h5>
                        <p class="card-text text-muted">Total Plugins</p>
                    </div>
                </div>
            </div>
            @foreach (var group in _plugins.GroupBy(x => x.Type)) {
                <div class="col-md-2">
                    <div class="card text-center">
                        <div class="card-body">
                            <h5 class="card-title">@group.Count()</h5>
                            <p class="card-text text-muted">@group.Key</p>
                        </div>
                    </div>
                </div>
            }
        </div>
        
        @* Plugin Table *@
        @if (FilteredPlugins.Any()) {
            <table class="table table-sm table-hover">
                <thead>
                    <tr class="table-dark">
                        <th style="width:1%;">Status</th>
                        <th>Name</th>
                        <th>Type</th>
                        <th>Version</th>
                        <th>Author</th>
                        <th class="center">Order</th>
                        <th class="center">Flags</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var plugin in FilteredPlugins) {
                        <tr class="@(plugin.Enabled ? "" : "table-secondary")">
                            <td class="center">
                                @if (plugin.Enabled) {
                                    <span class="badge bg-success">ON</span>
                                } else {
                                    <span class="badge bg-secondary">OFF</span>
                                }
                            </td>
                            <td>
                                <strong>@plugin.Name</strong>
                                @if (!String.IsNullOrWhiteSpace(plugin.Description)) {
                                    <br /><small class="text-muted">@plugin.Description</small>
                                }
                            </td>
                            <td><span class="badge bg-info">@plugin.Type</span></td>
                            <td>@plugin.Version</td>
                            <td>@plugin.Author</td>
                            <td class="center">@plugin.SortOrder</td>
                            <td class="center">
                                @if (plugin.ContainsSensitiveData) {
                                    <span class="badge bg-warning" title="Contains Sensitive Data">🔒</span>
                                }
                                @if (plugin.HasPrompts) {
                                    <span class="badge bg-primary" title="Has @plugin.PromptCount Prompts">📝</span>
                                }
                                @if (plugin.TenantRestricted) {
                                    <span class="badge bg-dark" title="Restricted to @plugin.TenantCount Tenants">🏢</span>
                                }
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        } else {
            <div class="alert alert-info">No plugins found matching filter.</div>
        }
    }
}

@code {
    [Parameter] public string? TenantCode { get; set; }
    
    protected List<DataObjects.PluginInfo> _plugins = new();
    protected bool _loading = true;
    protected bool _loadedData = false;
    protected string _pageName = "plugins";
    protected string _filterType = "";
    
    protected IEnumerable<DataObjects.PluginInfo> FilteredPlugins => 
        String.IsNullOrEmpty(_filterType) 
            ? _plugins 
            : _plugins.Where(x => x.Type == _filterType);
    
    public void Dispose() {
        Model.OnChange -= OnDataModelUpdated;
    }
    
    protected override void OnInitialized() {
        Model.View = _pageName;
        Model.OnChange += OnDataModelUpdated;
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            Model.TenantCodeFromUrl = TenantCode;
        }
        
        if (Model.Loaded && Model.LoggedIn && !_loadedData) {
            if (!Model.User.Admin) {
                Helpers.NavigateToRoot();
                return;
            }
            
            _loadedData = true;
            await LoadData();
        }
    }
    
    protected void OnDataModelUpdated() {
        if (Model.View == _pageName) {
            StateHasChanged();
        }
    }
    
    protected async Task LoadData() {
        _loading = true;
        StateHasChanged();
        
        var result = await Helpers.GetOrPost<List<DataObjects.PluginInfo>>("api/Data/GetPluginInfo");
        if (result != null) {
            _plugins = result;
        }
        
        _loading = false;
        StateHasChanged();
    }
}
```

---

## Wireframe

```
┌─────────────────────────────────────────────────────────────────┐
│  🔌 Plugins                                    [Filter: All ▼]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                       │
│  │  5  │ │  1  │ │  2  │ │  1  │ │  1  │                       │
│  │Total│ │Auth │ │BgPrc│ │Genrl│ │UsrUp│                       │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘                       │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Status │ Name              │ Type    │ Ver  │ Author │ # │ ││
│  ├────────┼───────────────────┼─────────┼──────┼────────┼───┼──┤│
│  │  ON    │ App Lifecycle Log │ BgProc  │1.0.0 │WSU EIT │-1K│  ││
│  │        │ Logs app startup  │         │      │        │   │  ││
│  ├────────┼───────────────────┼─────────┼──────┼────────┼───┼──┤│
│  │  ON    │ Example Process   │ BgProc  │1.0.0 │B.Wick  │ 0 │  ││
│  │        │ Example plugin... │         │      │        │   │  ││
│  ├────────┼───────────────────┼─────────┼──────┼────────┼───┼──┤│
│  │  ON    │ Login With Prompts│ Auth    │1.0.0 │B.Wick  │ 0 │🔒││
│  │        │ Custom login...   │         │      │        │   │  ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Files to Create

| File | Type | Location | Purpose |
|------|------|----------|---------|
| `FreePlugins.App.DataObjects.Plugins.cs` | NEW | `DataObjects/` | PluginInfo DTO |
| `FreePlugins.App.DataController.Plugins.cs` | NEW | `Server/Controllers/` | API endpoint |
| `FreePlugins.App.Plugins.razor` | NEW | `Client/Pages/Settings/` | Blazor page |

---

## Navigation Integration

Add to Settings menu in `MainLayout.razor` or navigation component:

```razor
@if (Model.User.Admin) {
    <li class="nav-item">
        <NavLink class="nav-link" href="Settings/Plugins">
            <Icon Name="Plugins" /> Plugins
        </NavLink>
    </li>
}
```

---

## Security Considerations

| Check | Implementation |
|-------|----------------|
| Admin-only access | `[Authorize(Policy = Policies.Admin)]` on endpoint |
| No code exposed | Use `GetPluginsWithoutCode()` or DTO mapping |
| Client-side gate | Check `Model.User.Admin` before rendering |
| No sensitive properties | DTO excludes Code, only metadata |

---

## Portability

This feature is **NOT a plugin** — it's a UI feature. To add to another suite:

1. Copy `DataObjects.App.Plugins.cs` → `{Suite}.DataObjects/`
2. Copy `DataController.App.Plugins.cs` → `{Suite}/Controllers/`
3. Copy `FreePlugins.App.Plugins.razor` → `{Suite}.Client/Pages/Settings/`
4. Update namespace references
5. Add nav link

---

## Open Questions

1. **Should we show the plugin file path?** (Proposed: No, security risk)
2. **Should we allow downloading plugin source?** (Proposed: No, never)
3. **Should we show execution count/last run?** (Proposed: v2, requires tracking)

---

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Create DataObjects.App.Plugins.cs | [Backend] | P1 |
| Create DataController.App.Plugins.cs | [Backend] | P1 |
| Create Blazor page | [Frontend] | P1 |
| Add nav link | [Frontend] | P1 |
| Test with existing plugins | [Quality] | P1 |
| Add Language tags | [Frontend] | P2 |

---

**[Architect]:** This is different from our other plugins — it's actual UI code, not a BackgroundProcess. But it follows the same `.App.` naming convention and provides visibility into the plugin ecosystem.

**[Frontend]:** The Blazor page is straightforward. Standard FreeCRM patterns.

**[Quality]:** Security is handled by admin-only access and stripping code. Good to go.

**[Sanity]:** Final check — should this be in Settings or its own top-level section?

**[Architect]:** Settings makes sense. It's admin configuration/visibility, not daily use.

---

*Created: 2025-01-XX*  
*Maintained by: [Quality]*

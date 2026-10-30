# 206 — Standup: Configuration Viewer Plugin Planning

> **Document ID:** 206  
> **Category:** Meeting  
> **Purpose:** Team standup to define MVP features for a debug-mode configuration viewer  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality], [Sanity], [JrDev]  
> **Date:** 2025-01-XX  
> **Predicted Outcome:** Minimal feature set defined, ready for implementation  
> **Actual Outcome:** *(Pending)*  
> **Resolution:** *(Pending)*

---

## TL;DR

**What:** Plugin that exposes loaded configuration (appsettings, secrets, env vars) via API endpoint.  
**Safety:** Only works in Development mode — disabled in Production.  
**Use Case:** Debugging configuration issues without digging through files/secrets.

---

## Standup Context

Third plugin in our cross-compatible series. This one is different — it's a **debug tool**, not a production feature. Must be locked down to Development environment only.

---

## Discussion

**[Architect]:** The ask is: show what configuration values are loaded — from appsettings.json, user secrets, environment variables, etc. But only in debug mode. Thoughts?

**[Quality]:** 🚨 Security concern right away. Configuration can contain:
- Connection strings (passwords!)
- API keys
- OAuth secrets
- Encryption keys

We CANNOT expose this in production. Ever.

**[Backend]:** Agreed. Here's what I'm thinking for safety:

```csharp
public async Task<...> Execute(DataAccess da, Plugin plugin, long iteration)
{
    // CRITICAL: Only run in Development
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    if (env != "Development") {
        return (true, new List<string> { "ConfigViewer disabled (not Development)" }, null);
    }
    
    // ... do work ...
}
```

**[JrDev]:** What if someone sets `ASPNETCORE_ENVIRONMENT=Development` in production?

**[Quality]:** Good point. We should have multiple checks:

1. `ASPNETCORE_ENVIRONMENT == "Development"`
2. `Debugger.IsAttached` (optional, extra paranoid)
3. Plugin `Enabled` property set to `false` by default

**[Architect]:** I like the belt-and-suspenders approach. Let's require TWO conditions:
- Environment is Development
- Plugin Enabled is explicitly `true`

Default `Enabled` to `false` so it's opt-in even in dev.

**[Sanity]:** Mid-check. What's the actual output format? A log file? An API endpoint?

**[Backend]:** For a debug tool, an API endpoint makes more sense. You want to query it on-demand, not read log files. But that's more complex than BackgroundProcess...

**[Architect]:** Actually, we can do both:

1. **BackgroundProcess**: On startup, dump config to a file (one-time)
2. **Later (v1.1)**: Add API endpoint via DataController extension

For MVP, let's stick with the file dump. Same pattern as ApplicationLifecycleLogger.

---

## What Configuration Sources Exist?

Let me trace through `Program.cs` to see what's loaded:

```csharp
// From Program.cs (typical ASP.NET Core)
var builder = WebApplication.CreateBuilder(args);
// This automatically loads:
// 1. appsettings.json
// 2. appsettings.{Environment}.json
// 3. User secrets (Development only)
// 4. Environment variables
// 5. Command line args

// Then we can access via:
builder.Configuration.GetValue<string>("SomeKey");
builder.Configuration.GetSection("SomeSection");
```

**[Backend]:** The `IConfiguration` interface lets us enumerate all values:

```csharp
foreach (var kvp in configuration.AsEnumerable()) {
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}
```

But we don't have access to `IConfiguration` in a BackgroundProcess plugin — only `DataAccess`.

**[JrDev]:** So how do we get the config?

**[Backend]:** Two options:

### Option A: Pass IConfiguration to Plugin (Framework Change)

Modify how plugins are executed to receive `IConfiguration`. Too invasive.

### Option B: Use ConfigurationHelper (Already Available)

The `ConfigurationHelper` class already exposes parsed config:

```csharp
// Already in DataObjects
public partial class ConfigurationHelper : IConfigurationHelper
{
    public string? AnalyticsCode { get; }
    public string? BasePath { get; }
    public ConfigurationHelperConnectionStrings ConnectionStrings { get; }
    public List<string>? GloballyDisabledModules { get; }
    public List<string>? GloballyEnabledModules { get; }
}
```

But this is a subset — it doesn't include everything from appsettings.json.

### Option C: Static Configuration Capture (Recommended)

Capture config at startup, store statically, plugin reads it:

```csharp
// In Program.cs (one-time framework addition)
public static class ConfigurationCapture
{
    public static IConfiguration? Configuration { get; private set; }
    
    public static void Capture(IConfiguration config)
    {
        Configuration = config;
    }
}

// Call during startup
ConfigurationCapture.Capture(builder.Configuration);
```

**[Architect]:** Option C is clean. One line added to Program.cs, plugin can read the static.

**[Sanity]:** Is storing IConfiguration statically safe?

**[Backend]:** Yes — `IConfiguration` is designed to be long-lived. It's already stored in the DI container for the app's lifetime. Static reference is equivalent.

---

## Feature Brainstorm

| # | Feature | Complexity | Value | MVP? |
|---|---------|------------|-------|------|
| 1 | Dump all config keys to file | Low | High | ✅ |
| 2 | Development-only gate | Low | Critical | ✅ |
| 3 | Enabled=false by default | Low | High | ✅ |
| 4 | Mask sensitive values | Medium | High | ✅ |
| 5 | Group by source (appsettings, env, secrets) | High | Medium | ❌ v2 |
| 6 | API endpoint | Medium | High | ⚠️ v1.1 |
| 7 | Blazor viewer page | High | Medium | ❌ v2 |
| 8 | Diff between environments | High | Low | ❌ v2 |
| 9 | Export to JSON | Low | Low | ❌ v2 |
| 10 | Real-time config reload detection | High | Low | ❌ v2 |

---

## Final Scope

```
┌─────────────────────────────────────────────────────────────┐
│        ConfigurationViewer v1.0 — MVP SCOPE                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ✅ IN SCOPE (v1.0)                                         │
│  ─────────────────                                          │
│  • Development-only execution (hard gate)                   │
│  • Enabled=false by default (opt-in)                        │
│  • Dump all IConfiguration keys to ConfigDump.log           │
│  • Mask sensitive values (passwords, secrets, keys)         │
│  • One-time dump on first iteration                         │
│  • Log format: Key=Value (masked where appropriate)         │
│                                                             │
│  ⚠️ FRAMEWORK CHANGE REQUIRED                               │
│  ───────────────────────────                                │
│  • Add ConfigurationCapture.Capture() in Program.cs         │
│  • One-line addition to each suite                          │
│                                                             │
│  🔒 SECURITY FEATURES                                       │
│  ────────────────────                                       │
│  • ASPNETCORE_ENVIRONMENT must be "Development"             │
│  • Plugin Enabled must be explicitly set to true            │
│  • Sensitive key patterns masked: *Password*, *Secret*,     │
│    *Key*, *Token*, *ConnectionString*                       │
│  • Output file only created in bin/Debug folder             │
│                                                             │
│  📋 v1.1 (After MVP)                                        │
│  ──────────────────                                         │
│  • API endpoint: GET /api/Debug/Configuration               │
│  • JSON output format option                                │
│                                                             │
│  ❌ OUT OF SCOPE (v2+)                                      │
│  ──────────────────────                                     │
│  • Source grouping (appsettings vs env vs secrets)          │
│  • Blazor viewer page                                       │
│  • Environment diff                                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Design

### Component 1: ConfigurationCapture (Static Class)

```csharp
// New file: FreePlugins/Classes/ConfigurationCapture.cs
namespace FreePlugins;

/// <summary>
/// Captures IConfiguration at startup for debug plugins.
/// Must be called from Program.cs: ConfigurationCapture.Capture(builder.Configuration);
/// </summary>
public static class ConfigurationCapture
{
    public static IConfiguration? Configuration { get; private set; }
    public static bool IsCaptured => Configuration != null;
    
    public static void Capture(IConfiguration config)
    {
        Configuration = config;
    }
    
    public static IEnumerable<KeyValuePair<string, string?>> GetAllValues()
    {
        if (Configuration == null) yield break;
        
        foreach (var kvp in Configuration.AsEnumerable()) {
            yield return kvp;
        }
    }
}
```

### Component 2: Program.cs Modification (Framework Change)

```csharp
// Add to Program.cs, after builder creation
var builder = WebApplication.CreateBuilder(args);

// ADD THIS LINE for ConfigurationViewer plugin support
ConfigurationCapture.Capture(builder.Configuration);
```

### Component 3: Plugin (BackgroundProcess)

```csharp
// New file: FreePlugins/Plugins/ConfigurationViewer.cs
public class ConfigurationViewer : IPluginBackgroundProcess
{
    private static bool _dumped = false;
    
    private static readonly string LogFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "ConfigDump.log"
    );
    
    // Patterns to mask (case-insensitive)
    private static readonly string[] SensitivePatterns = {
        "password", "secret", "key", "token", "connectionstring",
        "apikey", "api_key", "appkey", "app_key", "credential",
        "auth", "private", "certificate"
    };
    
    public Dictionary<string, object> Properties() => new() {
        { "Id", new Guid("c4d5e6f7-8a9b-0c1d-2e3f-4a5b6c7d8e9f") },
        { "Author", "WSU EIT" },
        { "Name", "Configuration Viewer (Debug Only)" },
        { "Description", "Dumps loaded configuration to file. DEVELOPMENT ONLY." },
        { "Type", "BackgroundProcess" },
        { "Version", "1.0.0" },
        { "SortOrder", -900 }, // Run early, after lifecycle logger
        
        // =============================================================
        // ⚠️  SECURITY: Disabled by default. Set to true ONLY in dev.
        // =============================================================
        { "Enabled", false },
    };
    
    public async Task<...> Execute(DataAccess da, Plugin plugin, long iteration)
    {
        await Task.CompletedTask;
        
        // Already dumped?
        if (_dumped) return (true, null, null);
        
        // SECURITY CHECK 1: Environment
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (env != "Development") {
            _dumped = true; // Don't retry
            return (true, new List<string> { 
                "ConfigViewer: Skipped (not Development environment)" 
            }, null);
        }
        
        // SECURITY CHECK 2: Enabled property
        bool enabled = false;
        if (plugin.Properties?.ContainsKey("Enabled") == true) {
            try { enabled = (bool)plugin.Properties["Enabled"]; } catch { }
        }
        
        if (!enabled) {
            _dumped = true;
            return (true, new List<string> { 
                "ConfigViewer: Skipped (Enabled=false)" 
            }, null);
        }
        
        // SECURITY CHECK 3: Configuration captured?
        if (!ConfigurationCapture.IsCaptured) {
            _dumped = true;
            return (true, new List<string> { 
                "ConfigViewer: Skipped (Configuration not captured in Program.cs)" 
            }, null);
        }
        
        _dumped = true;
        
        // Dump configuration
        try {
            DumpConfiguration();
            return (true, new List<string> { 
                $"ConfigViewer: Dumped to {LogFilePath}" 
            }, null);
        } catch (Exception ex) {
            return (true, new List<string> { 
                $"ConfigViewer: Error - {ex.Message}" 
            }, null);
        }
    }
    
    private void DumpConfiguration()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Configuration Dump");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:O}");
        sb.AppendLine($"# Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
        sb.AppendLine($"# Machine: {Environment.MachineName}");
        sb.AppendLine($"# ⚠️  THIS FILE MAY CONTAIN SENSITIVE DATA - DO NOT COMMIT");
        sb.AppendLine();
        
        var values = ConfigurationCapture.GetAllValues()
            .OrderBy(x => x.Key)
            .ToList();
        
        foreach (var kvp in values) {
            string value = MaskIfSensitive(kvp.Key, kvp.Value);
            sb.AppendLine($"{kvp.Key}={value}");
        }
        
        File.WriteAllText(LogFilePath, sb.ToString());
    }
    
    private string MaskIfSensitive(string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) return "(empty)";
        
        string keyLower = key.ToLowerInvariant();
        
        foreach (var pattern in SensitivePatterns) {
            if (keyLower.Contains(pattern)) {
                // Mask all but first 2 chars
                if (value.Length <= 4) return "****";
                return value.Substring(0, 2) + new string('*', Math.Min(value.Length - 2, 20));
            }
        }
        
        return value;
    }
}
```

---

## Expected Output

### File: `ConfigDump.log`

```
# Configuration Dump
# Generated: 2025-01-15T10:30:00.0000000Z
# Environment: Development
# Machine: DEVMACHINE
# ⚠️  THIS FILE MAY CONTAIN SENSITIVE DATA - DO NOT COMMIT

AllowedHosts=*
AnalyticsCode=
AuthenticationProviders:Apple:ClientId=(empty)
AuthenticationProviders:Apple:KeyId=(empty)
AuthenticationProviders:Facebook:AppId=(empty)
AuthenticationProviders:Facebook:AppSecret=********************
AuthenticationProviders:Google:ClientId=(empty)
AuthenticationProviders:Google:ClientSecret=********************
AzureSignalRurl=(empty)
BackgroundService:Enabled=true
BackgroundService:ProcessingIntervalSeconds=60
BasePath=
ConnectionStrings:AppData=In********************
DatabaseType=InMemory
Logging:LogLevel:Default=Information
Logging:LogLevel:Microsoft.AspNetCore=Warning
```

Note: Sensitive values are masked!

---

## Security Checklist

| Check | Implementation |
|-------|----------------|
| Development-only | `ASPNETCORE_ENVIRONMENT == "Development"` |
| Opt-in | `Enabled = false` by default |
| Mask passwords | Pattern matching on key names |
| Mask secrets | Pattern matching on key names |
| Mask connection strings | Pattern matching on key names |
| No production exposure | Multiple gates |
| Warning in output | Header comment in file |

---

## Files to Create/Modify

| File | Type | Location |
|------|------|----------|
| `ConfigurationCapture.cs` | NEW | `FreePlugins/Classes/` |
| `ConfigurationViewer.cs` | NEW | `FreePlugins/Plugins/` |
| `Program.cs` | MODIFY | Add one line |

---

## Portability Considerations

Like SignalR Monitor, this requires a minor framework change:

**Portability Package:**
```
Config-Viewer-Plugin/
├── ConfigurationCapture.cs       → Copy to {Suite}/Classes/
├── ConfigurationViewer.cs        → Copy to {Suite}/Plugins/
└── README.md                     → Program.cs modification instruction
```

**Program.cs change (all suites):**
```csharp
// Add after: var builder = WebApplication.CreateBuilder(args);
ConfigurationCapture.Capture(builder.Configuration);
```

---

## Open Questions

1. **Should we add `.gitignore` entry for `ConfigDump.log`?** (Recommended: Yes)
2. **Should masking be configurable?** (Proposed: No, keep it simple)
3. **Should we log when masking occurs?** (Proposed: No, too verbose)

---

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Create ConfigurationCapture.cs | [Backend] | P1 |
| Create ConfigurationViewer.cs plugin | [Backend] | P1 |
| Add capture line to Program.cs | [Backend] | P1 |
| Add ConfigDump.log to .gitignore | [Backend] | P1 |
| Test masking works correctly | [Quality] | P1 |
| Test production safety gate | [Quality] | P1 |

---

**[Architect]:** Good. This one is lower risk than SignalR Monitor because it's debug-only. But we MUST test the security gates thoroughly.

**[Quality]:** I'll add explicit test cases:
1. Enabled=true + Development → works
2. Enabled=true + Production → blocked
3. Enabled=false + Development → blocked
4. Sensitive values masked → verified

**[Sanity]:** Final check — should we add this to .gitignore by default?

**[Backend]:** Yes. I'll add `ConfigDump.log` to the repo's .gitignore.

---

*Created: 2025-01-XX*  
*Maintained by: [Quality]*

# 201 — Implementation: ApplicationLifecycleLogger Plugin

> **Document ID:** 201  
> **Category:** Feature  
> **Purpose:** Implementation specification for ApplicationLifecycleLogger v1.0  
> **Audience:** Developers  
> **Predicted Outcome:** Plugin implemented and tested  
> **Actual Outcome:** ✅ Plugin implemented, code reviewed, merged  
> **Resolution:** Code in `FreePlugins/Plugins/ApplicationLifecycleLogger.cs`, reviewed in doc 203

---

## Summary

- **Problem:** No visibility into when FreeCRM apps start in production
- **Goal:** Simple plugin that logs APP_START to a file
- **Non-goals:** Shutdown logging, heartbeats, configuration UI

## Acceptance Criteria

- [ ] Plugin logs APP_START on first background iteration
- [ ] Plugin respects `Enabled` property (true = log, false = silent)
- [ ] Log file created at `{AppBase}/PluginLogs.log`
- [ ] CSV format with header row
- [ ] Plugin never crashes the application (all I/O in try-catch)
- [ ] Works in FreePluginsV1 without modification

---

## Implementation

### File Location

```
FreePluginsV1/
└── FreePlugins/
    └── Plugins/
        └── ApplicationLifecycleLogger.cs   ← NEW FILE
```

### Full Implementation

```csharp
using FreePlugins;
using Plugins;

namespace LifecyclePlugin
{
    /// <summary>
    /// A simple plugin that logs application lifecycle events.
    /// Demonstrates the IPluginBackgroundProcess interface.
    /// 
    /// Usage:
    ///   1. Ensure BackgroundService:Enabled = true in appsettings.json
    ///   2. Plugin runs automatically on app startup
    ///   3. Check PluginLogs.log in the application folder
    ///   4. To disable: set Enabled = false in Properties() below
    /// 
    /// Portability:
    ///   To use in another FreeCRM suite, change "using FreePlugins;" to match
    ///   the target namespace (e.g., "using FreeGLBA;" or "using CRM;")
    /// </summary>
    public class ApplicationLifecycleLogger : IPluginBackgroundProcess
    {
        // Static flag ensures we only log startup once
        private static bool _startLogged = false;

        // Log file path - same directory as appsettings.json
        private static readonly string LogFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "PluginLogs.log"
        );

        /// <summary>
        /// Plugin metadata. The framework reads this to identify and configure the plugin.
        /// </summary>
        public Dictionary<string, object> Properties() => new Dictionary<string, object>
        {
            // Unique identifier - DO NOT CHANGE after deployment
            { "Id", new Guid("b7e3f1a2-5c8d-4e9f-a1b2-c3d4e5f6a7b8") },
            
            // Plugin metadata
            { "Author", "WSU EIT" },
            { "ContainsSensitiveData", false },
            { "Description", "Logs application startup events to PluginLogs.log" },
            { "Name", "Application Lifecycle Logger" },
            
            // Execution order - negative runs early
            { "SortOrder", -1000 },
            
            // Must be "BackgroundProcess" for IPluginBackgroundProcess
            { "Type", "BackgroundProcess" },
            
            // Semantic version
            { "Version", "1.0.0" },
            
            // ═══════════════════════════════════════════════════════════
            // ON/OFF TOGGLE - Set to false to disable logging
            // ═══════════════════════════════════════════════════════════
            { "Enabled", true },
        };

        /// <summary>
        /// Called by the BackgroundProcessor on each iteration.
        /// </summary>
        /// <param name="da">DataAccess instance for database operations (unused here)</param>
        /// <param name="plugin">Plugin instance with Properties</param>
        /// <param name="iteration">Iteration counter (1 on first run)</param>
        public async Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Execute(
            DataAccess da,
            Plugin plugin,
            long iteration)
        {
            await Task.CompletedTask; // Satisfy async requirement

            // Only process on first iteration
            if (_startLogged)
            {
                return (Result: true, Messages: null, Objects: null);
            }

            _startLogged = true;

            // Check if logging is enabled
            bool enabled = true;
            if (plugin.Properties != null && plugin.Properties.ContainsKey("Enabled"))
            {
                try
                {
                    enabled = (bool)plugin.Properties["Enabled"];
                }
                catch
                {
                    // If we can't read Enabled, default to true
                    enabled = true;
                }
            }

            if (!enabled)
            {
                return (Result: true, Messages: new List<string> { "Lifecycle logging disabled" }, Objects: null);
            }

            // Log the startup event
            LogEvent("APP_START", "Application started successfully");

            return (Result: true, Messages: new List<string> { "Logged application startup" }, Objects: null);
        }

        /// <summary>
        /// Writes a single log entry to the log file.
        /// </summary>
        private void LogEvent(string eventType, string message)
        {
            try
            {
                // Create header if file doesn't exist
                if (!File.Exists(LogFilePath))
                {
                    File.WriteAllText(LogFilePath, "Timestamp,Event,MachineName,ProcessId,Message\n");
                }

                // Build CSV entry
                string entry = string.Format("{0},{1},{2},{3},{4}\n",
                    DateTime.UtcNow.ToString("o"),           // ISO 8601 timestamp
                    eventType,                                // Event type
                    Environment.MachineName,                  // Server name
                    Environment.ProcessId,                    // Process ID
                    EscapeCsvField(message)                   // Message (escaped)
                );

                // Append to file
                File.AppendAllText(LogFilePath, entry);
            }
            catch
            {
                // Swallow all exceptions - logging should never crash the app
                // The BackgroundProcessor will log any Messages we return
            }
        }

        /// <summary>
        /// Escapes a string for CSV format (handles commas and quotes).
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }
    }
}
```

---

## Properties Reference

| Property | Type | Value | Purpose |
|----------|------|-------|---------|
| `Id` | Guid | `b7e3f1a2-5c8d-4e9f-a1b2-c3d4e5f6a7b8` | Unique identifier |
| `Author` | string | `WSU EIT` | Attribution |
| `ContainsSensitiveData` | bool | `false` | Security flag |
| `Description` | string | ... | User-facing description |
| `Name` | string | `Application Lifecycle Logger` | Display name |
| `SortOrder` | int | `-1000` | Run early |
| `Type` | string | `BackgroundProcess` | Required for interface |
| `Version` | string | `1.0.0` | Semantic version |
| **`Enabled`** | bool | `true` | **ON/OFF toggle** |

---

## Expected Output

### Log File Location

```
┌─────────────────────────────────────────────────────────────┐
│  📁 LOG FILE LOCATION                                       │
├─────────────────────────────────────────────────────────────┤
│  Path:    {AppBase}/PluginLogs.log                          │
│                                                             │
│  Where {AppBase} is:                                        │
│    • Debug:  bin/Debug/net10.0/                             │
│    • Release: bin/Release/net10.0/                          │
│    • Published: Same folder as FreePlugins.dll              │
│                                                             │
│  Example (Debug):                                           │
│    FreePluginsV1/FreePlugins/bin/Debug/net10.0/PluginLogs.log│
└─────────────────────────────────────────────────────────────┘
```

### File: `PluginLogs.log`

```csv
Timestamp,Event,MachineName,ProcessId,Message
2025-01-15T10:30:00.0000000Z,APP_START,DEVMACHINE,12345,Application started successfully
2025-01-16T08:00:00.0000000Z,APP_START,DEVMACHINE,23456,Application started successfully
2025-01-17T09:15:00.0000000Z,APP_START,PRODSERVER,34567,Application started successfully
```

### Console Output (via BackgroundProcessor)

```
info: FreePlugins.BackgroundProcessor[0]
      Logged application startup
```

---

## Test Plan

### Test 1: Basic Functionality (Enabled=true)

**Steps:**
1. Copy plugin to `FreePluginsV1/FreePlugins/Plugins/`
2. Ensure `BackgroundService:Enabled = true` in appsettings.json
3. Run the application
4. Wait 5+ seconds for background processor

**Expected:**
- `PluginLogs.log` created in app directory
- File contains CSV header + one APP_START entry
- Console shows "Logged application startup"

### Test 2: Disabled (Enabled=false)

**Steps:**
1. Edit plugin, set `{ "Enabled", false }`
2. Delete existing `PluginLogs.log`
3. Run the application
4. Wait 5+ seconds

**Expected:**
- `PluginLogs.log` NOT created
- Console shows "Lifecycle logging disabled"

### Test 3: Append Behavior

**Steps:**
1. Run app (creates log with one entry)
2. Stop app
3. Run app again

**Expected:**
- Log file has TWO APP_START entries
- Header row appears only once

### Test 4: Error Resilience

**Steps:**
1. Create `PluginLogs.log` as a **directory** (not file)
2. Run the application

**Expected:**
- App does NOT crash
- No exception in console
- Plugin silently fails

---

## Portability Instructions

To deploy this plugin to another FreeCRM suite:

### Step 1: Copy the file

```bash
cp FreePluginsV1/FreePlugins/Plugins/ApplicationLifecycleLogger.cs \
   FreeGLBA-main/FreeGLBA/Plugins/
```

### Step 2: Update the namespace

| Target Suite | Change `using FreePlugins;` to |
|--------------|-------------------------------|
| FreeCRM-main | `using CRM;` |
| FreeGLBA | `using FreeGLBA;` |
| FreeCICD | `using FreeCICD;` |
| FreeManager | `using FreeManager;` |

### Step 3: Restart the application

The plugin will be compiled and loaded automatically.

---

## Verification Checklist

```markdown
## Implementation Verification

### Code
- [ ] File created at correct path
- [ ] Implements IPluginBackgroundProcess
- [ ] Properties() returns all required fields
- [ ] Unique GUID assigned
- [ ] Enabled property exists
- [ ] Execute() checks _startLogged flag
- [ ] Execute() checks Enabled property
- [ ] LogEvent() creates header if needed
- [ ] LogEvent() appends entry
- [ ] All I/O wrapped in try-catch
- [ ] CSV escaping implemented

### Testing
- [ ] Test 1: Basic functionality passes
- [ ] Test 2: Disabled mode passes
- [ ] Test 3: Append behavior passes
- [ ] Test 4: Error resilience passes

### Documentation
- [ ] Code comments explain usage
- [ ] Portability instructions in comments
- [ ] Properties documented
```

---

## Future Enhancements (Out of Scope)

| Enhancement | Complexity | Notes |
|-------------|------------|-------|
| APP_STOP logging | Medium | Requires AppDomain.ProcessExit hook |
| Heartbeat logging | Low | Add iteration check |
| Config file for settings | Medium | Read from appsettings.json |
| Log rotation | High | Size/date based rotation |

These are **not** in v1.0. File issues if needed.

---

*Created: 2025-01-XX*  
*Owner: [Backend]*

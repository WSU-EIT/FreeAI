# 109 — Meeting: Cross-Compatible Plugin Architecture

> **Document ID:** 109  
> **Category:** Meeting  
> **Purpose:** Design discussion for creating cross-compatible plugins that work across all FreeCRM suites  
> **Attendees:** [Architect], [Backend], [Quality], [Sanity], [JrDev]  
> **Date:** 2025-01-XX  
> **Predicted Outcome:** Clear understanding of plugin compatibility requirements and plan for first plugin  
> **Actual Outcome:** ✅ Comprehensive analysis complete, implementation plan defined  
> **Resolution:** Proceed with ApplicationLifecycleLogger plugin implementation

---

## Context

We've completed deep dive analysis of all 7 project suites (docs 101-107). The goal is to create plugins that can be dropped into any FreeCRM-based application without modification. We need to understand:
1. Which suites support the plugin system
2. What namespace differences exist
3. How to design portable plugins
4. Implementation plan for first plugin (ApplicationLifecycleLogger)

---

## Discussion

**[Architect]:** Let's start by reviewing what we learned from the deep dives. I've been through all 7 suites and here's what I found regarding plugin support:

| Suite | Plugin Support | Namespace | BackgroundProcess Plugin |
|-------|---------------|-----------|--------------------------|
| FreeCRM-main | ✅ Full | `CRM` | ✅ ExampleBackgroundProcess.cs |
| FreePlugins_base | ✅ Full | `FreePlugins` | ✅ ExampleBackgroundProcess.cs |
| FreePluginsV1 | ✅ Full | `FreePlugins` | ✅ ExampleBackgroundProcess.cs |
| FreeGLBA | ✅ Full | `FreeGLBA` | ✅ ExampleBackgroundProcess.cs |
| FreeCICD | ✅ Full | `FreeCICD` | ❌ **Missing** |
| FreeManager | ✅ Full | `FreeManager` | ✅ ExampleBackgroundProcess.cs |

The good news: all suites support the plugin system. The bad news: FreeCICD is missing `ExampleBackgroundProcess.cs`.

**[Backend]:** I dug into the `BackgroundProcessor.cs` and `DataAccess.Plugins.cs` files. Here's how the plugin execution works:

1. **Startup:** `BackgroundProcessor` hosted service starts
2. **Discovery:** Queries `da.GetPlugins()` for plugins with `Type == "backgroundprocess"`
3. **Scheduling:** Uses a timer based on `ProcessingIntervalSeconds` from appsettings
4. **Execution:** Calls `da.ExecutePlugin(pluginExecuteRequest)` with the iteration count

The key method signature for BackgroundProcess plugins is:
```csharp
Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Execute(
    DataAccess da,
    Plugin plugin,
    long iteration
)
```

**[JrDev]:** Wait, what's that `iteration` parameter for?

**[Backend]:** Good question. It's a counter that increments each time the background processor runs. Useful for:
- Running certain tasks only on first iteration (`iteration == 1`)
- Periodic tasks every N iterations (`iteration % 100 == 0`)
- Debugging to know how many cycles have passed

**[Quality]:** I reviewed all the built-in example plugins across suites. The namespace issue is real. Look at `LoginWithPrompts.cs`:

```csharp
// In FreeCRM-main
using CRM;
public async Task<...> Login(DataAccess da, ...)

// In FreePlugins_base
using FreePlugins;
public async Task<...> Login(DataAccess da, ...)

// In FreeGLBA
using FreeGLBA;
public async Task<...> Login(DataAccess da, ...)
```

The `DataAccess` type resolves to different namespaces. This means **plugins are NOT directly portable** without namespace changes.

**[Sanity]:** Mid-check. Are we overcomplicating this? Can't we just use preprocessor directives or conditional compilation?

**[Architect]:** We could, but that adds complexity to the plugin files. Let me propose a simpler approach:

**Option A: Namespace Adapter Pattern**
- Create a shared namespace that all suites recognize
- Requires framework changes to all suites
- ❌ Too invasive

**Option B: Documentation + Search-Replace**
- Document the namespace change requirement
- Plugins work as-is after a simple find-replace
- ✅ Minimal friction

**Option C: Pre-processor Macros**
- Use `#if CRM`, `#if FREEGLBA` etc.
- ❌ Clutters plugin code

I recommend **Option B**. Here's why:

1. Plugin code stays clean and readable
2. Find-replace is trivial: `using CRM;` → `using FreeGLBA;`
3. No framework changes required
4. Works for any future suites

**[Quality]:** I agree. Let's document the portability pattern clearly. But for our first plugin, what namespace should we target?

**[Backend]:** Since FreePluginsV1 is our development environment, we should write plugins with `FreePlugins` namespace. That matches both FreePlugins_base and FreePluginsV1.

**[Architect]:** Agreed. Now let's discuss the ApplicationLifecycleLogger plugin design.

**Requirements:**
1. Log `APP_START` on first background process iteration
2. Log `APP_STOP` on clean shutdown (if possible)
3. Write to `PluginLogs.log` in the app's base directory
4. CSV format: `Timestamp,Event,MachineName,ProcessId,Message`
5. Must be drop-in portable to all 6 suites

**[Backend]:** For the implementation, here's what I'm thinking:

```csharp
public class ApplicationLifecycleLogger : IPluginBackgroundProcess
{
    private static bool _startLogged = false;
    
    public Dictionary<string, object> Properties() => new() {
        { "Id", new Guid("a1b2c3d4-...") },  // Unique GUID
        { "Author", "EIT Team" },
        { "Name", "Application Lifecycle Logger" },
        { "Type", "BackgroundProcess" },
        { "SortOrder", -1000 },  // Run first
        { "Version", "1.0.0" }
    };
    
    public async Task<...> Execute(DataAccess da, Plugin plugin, long iteration)
    {
        if (!_startLogged) {
            _startLogged = true;
            LogEvent("APP_START", "Application started");
        }
        return (true, new List<string>(), null);
    }
}
```

**[JrDev]:** How do we detect shutdown? The `BackgroundProcessor` doesn't seem to have a shutdown hook.

**[Backend]:** Good catch. The `BackgroundService` base class has `StopAsync()` but plugins don't get called there. We have three options:

1. **Modify BackgroundProcessor** to call plugins on shutdown
2. **Use AppDomain.ProcessExit** in the plugin itself
3. **Accept we can't reliably log shutdown**

**[Quality]:** Option 1 requires framework changes. Option 2 might not work because plugins are dynamically compiled. I'd say we document this limitation and focus on what we CAN do:
- APP_START ✅
- APP_STOP ⚠️ (best effort, may not fire on crash)
- HEARTBEAT ✅ (optional, every N iterations)

**[Architect]:** Agreed. Let's add `AppDomain.ProcessExit` as a best-effort approach, but document it's not guaranteed.

**[Sanity]:** Final check. Are we missing anything obvious?

**[Quality]:** File locking. If we're appending to a log file, and the app crashes mid-write, could we corrupt the file?

**[Backend]:** Good point. We should:
1. Use `File.AppendAllText()` which handles its own file handle
2. Keep entries atomic (single line, flush immediately)
3. Wrap in try-catch so logging never crashes the app

**[Architect]:** Here's the final design:

```
┌─────────────────────────────────────────────────────────────┐
│  ApplicationLifecycleLogger Plugin - Final Design           │
├─────────────────────────────────────────────────────────────┤
│  Interface:  IPluginBackgroundProcess                       │
│  File:       ApplicationLifecycleLogger.cs                  │
│  Namespace:  FreePlugins (portable via find-replace)        │
│                                                             │
│  Output:     PluginLogs.log                                 │
│  Location:   AppDomain.CurrentDomain.BaseDirectory          │
│  Format:     CSV                                            │
│                                                             │
│  Events:                                                    │
│    APP_START  - First iteration (iteration == 1)            │
│    HEARTBEAT  - Every 60 iterations (optional)              │
│    APP_STOP   - AppDomain.ProcessExit (best effort)         │
│                                                             │
│  Safety:                                                    │
│    - All logging wrapped in try-catch                       │
│    - Atomic writes (single line per event)                  │
│    - Static flag prevents duplicate start logs              │
│                                                             │
│  Portability:                                               │
│    1. Copy .cs file to target suite's Plugins/ folder       │
│    2. Find-replace namespace to match target                │
│    3. Restart application                                   │
└─────────────────────────────────────────────────────────────┘
```

**[Quality]:** One more thing — we should add the plugin to FreeCICD to fix that missing ExampleBackgroundProcess gap.

**[Architect]:** Agreed. Our ApplicationLifecycleLogger will serve as both a useful plugin AND backfill that gap.

---

## Research Findings Summary

### Plugin System Compatibility Matrix

| Component | CRM | FreePlugins | FreeGLBA | FreeCICD | FreeManager |
|-----------|-----|-------------|----------|----------|-------------|
| `IPlugin` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `IPluginAuth` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `IPluginBackgroundProcess` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `IPluginUserUpdate` | ✅ | ✅ | ✅ | ✅ | ✅ |
| BackgroundProcessor.cs | ✅ | ✅ | ✅ | ✅ | ✅ |
| Example BackgroundProcess | ✅ | ✅ | ✅ | ❌ | ✅ |
| Plugin folder | ✅ | ✅ | ✅ | ✅ | ✅ |

### Namespace Map

| Suite | Using Statement | DataAccess Type |
|-------|-----------------|-----------------|
| FreeCRM-main | `using CRM;` | `CRM.DataAccess` |
| FreePlugins_base | `using FreePlugins;` | `FreePlugins.DataAccess` |
| FreePluginsV1 | `using FreePlugins;` | `FreePlugins.DataAccess` |
| FreeGLBA | `using FreeGLBA;` | `FreeGLBA.DataAccess` |
| FreeCICD | `using FreeCICD;` | `FreeCICD.DataAccess` |
| FreeManager | `using FreeManager;` | `FreeManager.DataAccess` |

### Plugin Execution Flow

```
appsettings.json
    └── BackgroundService:Enabled = true
           └── BackgroundProcessor.ExecuteAsync()
                  └── GetPlugins() → Type == "backgroundprocess"
                         └── Timer(ProcessingIntervalSeconds)
                                └── ExecutePlugin(plugin, iteration)
                                       └── Roslyn compile + invoke
                                              └── Plugin.Execute(da, plugin, iteration)
```

---

## Decisions

1. **Namespace Strategy:** Use `FreePlugins` as default, document find-replace for portability
2. **First Plugin:** ApplicationLifecycleLogger implementing IPluginBackgroundProcess
3. **Logging Format:** CSV with Timestamp, Event, MachineName, ProcessId, Message
4. **File Location:** Same directory as appsettings.json (BaseDirectory)
5. **Shutdown Logging:** Best-effort via AppDomain.ProcessExit, documented as unreliable
6. **Safety:** All logging wrapped in try-catch to prevent crashes
7. **FreeCICD Gap:** Our plugin will backfill the missing BackgroundProcess example

---

## Open Questions

1. Should we add a configuration setting to enable/disable heartbeat logging?
2. Should we support log rotation (new file per day/size)?
3. Should we add JSON format as an alternative to CSV?

*Deferred to Phase 2 based on user feedback*

---

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Create ApplicationLifecycleLogger.cs | [Backend] | P1 |
| Test in FreePluginsV1 | [Quality] | P1 |
| Test portability to FreeCRM-main | [Quality] | P1 |
| Test portability to FreeGLBA | [Quality] | P2 |
| Backfill FreeCICD with the plugin | [Backend] | P2 |
| Create Plugin Development Guide | [Quality] | P2 |
| Document portability pattern | [Quality] | P1 |

---

*Created: 2025-01-XX*  
*Maintained by: [Quality]*

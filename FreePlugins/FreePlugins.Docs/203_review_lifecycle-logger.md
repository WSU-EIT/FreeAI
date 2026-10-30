# 203 — Review: ApplicationLifecycleLogger Plugin

> **Document ID:** 203  
> **Category:** Meeting  
> **Purpose:** Code review of ApplicationLifecycleLogger.cs before merge  
> **Attendees:** [Author], [Reviewer], [Quality], [Sanity], [JrDev]  
> **Date:** 2025-01-XX  
> **Predicted Outcome:** Approval with minor changes  
> **Actual Outcome:** ✅ Approved with changes — 2 minor issues fixed  
> **Resolution:** Changes applied, build verified, ready to merge

---

## TL;DR

**Verdict:** ✅ Approved with 2 minor fixes  
**Issues Found:** Box-drawing chars (encoding risk), missing multi-instance warning  
**Status:** Both fixed, build passes, ready to merge

---

## What We're Reviewing

- **File:** `FreePluginsV1/FreePlugins/Plugins/ApplicationLifecycleLogger.cs`
- **Lines:** ~140
- **Context:** First cross-compatible plugin for FreeCRM ecosystem

---

## Discussion

**[Author]:** Alright, here's the ApplicationLifecycleLogger. Simple plugin — logs APP_START on first iteration, has an Enabled toggle. Ready for review.

**[Reviewer]:** Let me walk through this. First, the structure looks good. You've got:
- `Properties()` returning all the required metadata
- `Execute()` implementing `IPluginBackgroundProcess`
- `LogEvent()` for file I/O
- `EscapeCsvField()` for CSV safety

Let me compare to `ExampleBackgroundProcess.cs` to see if we're following the pattern...  

**[Reviewer]:** Okay, I see the first issue. Look at line 1-3:

```csharp
using FreePlugins;
using Plugins;

namespace LifecyclePlugin
```

And in ExampleBackgroundProcess.cs:

```csharp
using FreePlugins;
using Plugins;

namespace ExamplePlugin
```

Both use `namespace ExamplePlugin` or `namespace LifecyclePlugin`. That's fine — the plugin namespace doesn't have to match the app namespace. But...

**[Quality]:** Wait, I'm checking the `Properties()` method. Line 32-52:

```csharp
public Dictionary<string, object> Properties() => new Dictionary<string, object>
{
    { "Id", new Guid("b7e3f1a2-5c8d-4e9f-a1b2-c3d4e5f6a7b8") },
    // ...
};
```

And ExampleBackgroundProcess uses:

```csharp
public Dictionary<string, object> Properties() =>
    new Dictionary<string, object> {
        { "Id", new Guid("3961b30f-0c33-474b-a14c-a73174058f47") },
        // ...
    };
```

**[Quality]:** The expression-bodied syntax is slightly different. Ours has `=>` then newline then `new`, the example has `=>` then newline with indent then `new`. Minor style thing, not blocking.

**[Sanity]:** Mid-check. Let's focus on things that could actually break. Style is nice-to-have.

**[JrDev]:** I have a question about line 67:

```csharp
await Task.CompletedTask; // Satisfy async requirement
```

And ExampleBackgroundProcess has:

```csharp
await Task.Delay(0); // Simulate a delay since this method has to be async.
```

Which is better?

**[Reviewer]:** `Task.CompletedTask` is slightly better — it doesn't allocate a timer. But both work. Not blocking.

**[Author]:** Cool. Any actual bugs?

---

**[Reviewer]:** YES. Found one. Look at how we check `Enabled` on lines 78-90:

```csharp
bool enabled = true;
if (plugin.Properties != null && plugin.Properties.ContainsKey("Enabled"))
{
    try
    {
        enabled = (bool)plugin.Properties["Enabled"];
    }
    catch
    {
        enabled = true;
    }
}
```

**[Reviewer]:** The problem: `plugin.Properties` is populated from the *cached* plugin, not from calling `Properties()` on this instance. Let me check how the framework loads plugins...

*[Reviewer checks DataAccess.Plugins.cs and BackgroundProcessor.cs]*

**[Reviewer]:** Okay, I traced it. The `BackgroundProcessor` loads plugins via:
```csharp
var allPlugins = da.GetPlugins();
```

And `GetPlugins()` returns plugins from cache or from `PluginsInterface.AllPlugins`. The `AllPlugins` list is built by scanning the Plugins folder and calling `Properties()` on each class.

So `plugin.Properties` in `Execute()` WILL have our `Enabled` property. The check should work.

**[Quality]:** But wait — there's still an issue. We check `Enabled` AFTER setting `_startLogged = true`:

```csharp
// Line 71
if (_startLogged)
{
    return (Result: true, Messages: null, Objects: null);
}

// Line 76
_startLogged = true;

// Line 78-90
// Check if logging is enabled
bool enabled = true;
if (plugin.Properties != null && plugin.Properties.ContainsKey("Enabled"))
// ...
```

**[Quality]:** If `Enabled = false`, we set `_startLogged = true`, then check `enabled`, then return without logging. That's correct.

But what if someone later changes `Enabled` back to `true` and restarts? The static `_startLogged` is reset on app restart anyway, so that's fine.

Actually wait — what if the background processor runs the plugin TWICE before we can check? Is `_startLogged = true` atomic?

**[Reviewer]:** Good catch. Static field assignment in C# is atomic for reference types and primitives ≤ pointer size. `bool` is 1 byte, so it's atomic. No race condition.

**[Sanity]:** But there IS a race condition window. Between lines 76 and 78, another thread could see `_startLogged = true` and exit, while we haven't logged yet.

**[Reviewer]:** Actually no — `BackgroundProcessor.ProcessTasks` processes plugins sequentially in a `foreach` loop:

```csharp
foreach(var plugin in pluginsToProcess) {
    // ...
    var executed = da.ExecutePlugin(pluginExecuteRequest);
    // ...
}
```

One plugin at a time. No race.

**[Sanity]:** Okay, that's fine then.

---

**[JrDev]:** I found something weird. Line 24-27:

```csharp
private static readonly string LogFilePath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "PluginLogs.log"
);
```

This is a static readonly field initialized at class load time. What if `AppDomain.CurrentDomain.BaseDirectory` isn't set yet when the class loads?

**[Reviewer]:** Good question! `AppDomain.CurrentDomain.BaseDirectory` is available as soon as the runtime starts. It's set before any user code runs. So this is safe.

BUT — there's a different issue. This is a `static readonly` field, meaning it's set ONCE when the class first loads. If plugins are compiled dynamically by Roslyn (which they are in FreeCRM), the class loads in a dynamic assembly. 

Let me check... actually, `AppDomain.CurrentDomain.BaseDirectory` returns the HOST app's base directory, not the dynamic assembly location. So this should still point to where appsettings.json lives. ✅

---

**[Quality]:** I'm checking error handling. Line 101-126:

```csharp
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
            // ...
        );

        // Append to file
        File.AppendAllText(LogFilePath, entry);
    }
    catch
    {
        // Swallow all exceptions - logging should never crash the app
    }
}
```

**[Quality]:** The try-catch swallows ALL exceptions. That's intentional per our spec — logging should never crash the app. But...

# 200 — Standup: ApplicationLifecycleLogger Plugin Planning

> **Document ID:** 200  
> **Category:** Meeting  
> **Purpose:** Team standup to define MVP features for the ApplicationLifecycleLogger plugin  
> **Attendees:** [Architect], [Backend], [Quality], [Sanity], [JrDev]  
> **Date:** 2025-01-XX  
> **Predicted Outcome:** Minimal feature set defined, ready for implementation  
> **Actual Outcome:** ✅ MVP scope locked: ON/OFF toggle + startup logging  
> **Resolution:** Proceed to implementation (doc 201)

---

## TL;DR

**What:** Plugin that logs APP_START to `PluginLogs.log` with ON/OFF toggle.  
**Scope:** 2 features in, 8 features cut. True MVP.  
**Next:** Implementation spec in doc 201.

---

## Standup Context

We've got CTO approval (doc 110). Now we need to nail down the **exact** feature set for v1.0. Goal: **as close to hello world as possible** while still being useful.

---

## Discussion

**[Architect]:** Alright team, quick standup. We're building the ApplicationLifecycleLogger. CTO approved it. Now — what's the absolute minimum we ship?

**[JrDev]:** Wait, I thought we already designed this? Doc 109 had a whole spec.

**[Architect]:** That was the *exploration*. Now we're scoping the *MVP*. Big difference. What's the simplest thing that could possibly work?

**[Backend]:** Okay, true hello world would be:
```csharp
public async Task<...> Execute(DataAccess da, Plugin plugin, long iteration)
{
    Console.WriteLine("Hello from plugin!");
    return (true, null, null);
}
```

But that's useless. We need *something* more.

**[Quality]:** The 110 brief promised "logs application start/stop events" and "creates PluginLogs.log". That's our contract.

**[Sanity]:** Mid-check. Let's list what we *could* add, then ruthlessly cut.

**[Architect]:** Good idea. Brain dump — what features are on the table?

---

### Feature Brainstorm

| # | Feature | Complexity | Value |
|---|---------|------------|-------|
| 1 | Log APP_START on first iteration | Low | High |
| 2 | Log APP_STOP on shutdown | Medium | Medium |
| 3 | Log HEARTBEAT every N iterations | Low | Low |
| 4 | ON/OFF toggle via Properties | Low | High |
| 5 | Configurable log file path | Medium | Low |
| 6 | Log rotation (daily/size) | High | Low |
| 7 | JSON format option | Medium | Low |
| 8 | Log to database instead of file | High | Medium |
| 9 | Include app version in log | Low | Medium |
| 10 | Include tenant info | Medium | Low |

---

**[Backend]:** My vote: **1 and 4 only**. Log startup, have an on/off switch. Done.

**[JrDev]:** What about shutdown logging? That was in the original spec.

**[Quality]:** We discussed this in 109 — shutdown is unreliable. `AppDomain.ProcessExit` doesn't always fire. It's a "nice to have" that adds complexity for uncertain value.

**[Architect]:** Agree. Cut it from MVP. We can add it later if someone actually needs it.

**[Sanity]:** What about heartbeat? That was low complexity.

**[Backend]:** It's low complexity but also low value. If you want to know the app is running, use health checks or monitoring. A log file isn't the right tool.

**[Architect]:** Cut it. MVP is:

```
✅ Feature 1: Log APP_START on first iteration
✅ Feature 4: ON/OFF toggle via Properties
❌ Everything else: v2 or never
```

**[JrDev]:** How does the ON/OFF toggle work?

**[Backend]:** The plugin `Properties()` dictionary can include custom settings. We add an `Enabled` property. If false, the plugin does nothing.

```csharp
public Dictionary<string, object> Properties() => new() {
    { "Id", new Guid("...") },
    { "Name", "Application Lifecycle Logger" },
    { "Type", "BackgroundProcess" },
    { "Enabled", true },  // <-- Toggle
    // ...
};
```

**[Quality]:** Wait, does the framework check `Enabled`? Or do we check it ourselves?

**[Backend]:** Good question. Let me check...

*[Backend reviews BackgroundProcessor.cs]*

The framework doesn't filter by `Enabled`. It just loads all plugins with `Type == "backgroundprocess"`. So we'd check it ourselves in `Execute()`:

```csharp
public async Task<...> Execute(DataAccess da, Plugin plugin, long iteration)
{
    // Check if enabled
    if (plugin.Properties.TryGetValue("Enabled", out var enabled) && enabled is bool b && !b)
    {
        return (true, null, null);  // Skip silently
    }
    
    // Do actual work
    if (iteration == 1) {
        LogEvent("APP_START", "Application started");
    }
    
    return (true, new List<string> { "Logged startup" }, null);
}
```

**[Sanity]:** That's ugly. Can we simplify?

**[Backend]:** Yeah, we can store enabled state in a static field initialized from Properties:

```csharp
private static bool? _enabled = null;

public async Task<...> Execute(...)
{
    _enabled ??= GetEnabledFromProperties(plugin);
    if (!_enabled.Value) return (true, null, null);
    
    // ...
}
```

**[Architect]:** Even simpler — just check on first iteration when we'd log anyway:

```csharp
private static bool _startLogged = false;

public async Task<...> Execute(DataAccess da, Plugin plugin, long iteration)
{
    if (!_startLogged)
    {
        _startLogged = true;
        
        // Check enabled flag
        bool enabled = true;
        if (plugin.Properties.ContainsKey("Enabled")) {
            enabled = (bool)plugin.Properties["Enabled"];
        }
        
        if (enabled) {
            LogEvent("APP_START", "Application started successfully");
        }
    }
    
    return (true, null, null);
}
```

**[Quality]:** That's clean. One check, on first run only. No performance overhead on subsequent iterations.

**[JrDev]:** What if someone wants to turn it on/off without restarting?

**[Sanity]:** That's scope creep. For MVP, you change the property and restart. Done.

**[Architect]:** Exactly. Dynamic enable/disable is a v2 feature if anyone asks for it.

---

## Final MVP Scope

```
┌─────────────────────────────────────────────────────────────┐
│        ApplicationLifecycleLogger v1.0 — MVP SCOPE          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ✅ IN SCOPE (v1.0)                                         │
│  ─────────────────                                          │
│  • Log APP_START on first background iteration              │
│  • ON/OFF toggle via "Enabled" property (default: true)     │
│  • Write to PluginLogs.log in app base directory            │
│  • CSV format: Timestamp,Event,Machine,PID,Message          │
│  • All logging wrapped in try-catch (never crash app)       │
│                                                             │
│  ❌ OUT OF SCOPE (maybe v2)                                 │
│  ──────────────────────────                                 │
│  • APP_STOP logging (unreliable)                            │
│  • HEARTBEAT logging (use monitoring instead)               │
│  • Configurable file path                                   │
│  • Log rotation                                             │
│  • JSON format                                              │
│  • Database logging                                         │
│  • Dynamic enable/disable                                   │
│  • Tenant-specific logging                                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Decisions

### ADR: ON/OFF Implementation

**Context:** Need a way to enable/disable the plugin without code changes.

**Decision:** Use `Enabled` property in `Properties()` dictionary, checked on first iteration.

**Rationale:**
- No framework changes required
- User edits the .cs file to toggle (like editing config)
- Zero runtime overhead after first check

**Consequences:**
- Requires app restart to change
- Simple and predictable

---

### ADR: Log Format

**Context:** What format for the log file?

**Decision:** CSV with header row.

**Format:**
```csv
Timestamp,Event,MachineName,ProcessId,Message
2025-01-15T10:30:00.0000000Z,APP_START,WEBSERVER01,12345,Application started successfully
```

**Rationale:**
- Human readable
- Easy to grep/parse
- Excel/spreadsheet compatible
- Single line per event (atomic writes)

---

### ADR: File Location

**Context:** Where to write the log file?

**Decision:** `AppDomain.CurrentDomain.BaseDirectory` (same folder as appsettings.json)

**Rationale:**
- Easy to find in production
- No configuration needed
- Works on all platforms

---

## Implementation Checklist

```markdown
## Plugin Implementation Checklist

- [ ] Create `ApplicationLifecycleLogger.cs` in `FreePluginsV1/FreePlugins/Plugins/`
- [ ] Implement `IPluginBackgroundProcess` interface
- [ ] Add `Properties()` with unique GUID, name, type, version, Enabled
- [ ] Add static `_startLogged` flag
- [ ] Check `Enabled` property on first iteration
- [ ] Write CSV header if file doesn't exist
- [ ] Append APP_START entry
- [ ] Wrap all file I/O in try-catch
- [ ] Test with Enabled=true (should log)
- [ ] Test with Enabled=false (should not log)
- [ ] Test file creation and append
- [ ] Verify no exceptions on failure
```

---

## File to Create

**Path:** `FreePluginsV1/FreePlugins/Plugins/ApplicationLifecycleLogger.cs`

**Estimated Lines:** ~80 lines

**Dependencies:** None (uses only System.IO)

---

## Next Steps

| Action | Owner | Doc |
|--------|-------|-----|
| Write implementation spec | [Backend] | 201 |
| Implement plugin | [Backend] | — |
| Test in FreePluginsV1 | [Quality] | — |
| Document portability | [Quality] | 202 |

---

**[Architect]:** Alright, scope is locked. Backend, write up the implementation spec (201) and build it. Quality, prepare the test plan. We ship this week.

**[Backend]:** On it.

**[Quality]:** Ready.

**[Sanity]:** Final check — are we missing anything obvious?

**[JrDev]:** Should we... document how to *use* the plugin? Like, what does a user do?

**[Quality]:** Good catch. User instructions:
1. Ensure `BackgroundService:Enabled = true` in appsettings.json
2. Plugin runs automatically
3. Check `PluginLogs.log` in the app folder
4. To disable: edit `ApplicationLifecycleLogger.cs`, set `Enabled = false`, restart

**[Architect]:** Add that to the implementation doc. We're done here.

---

*Created: 2025-01-XX*  
*Maintained by: [Quality]*

# 207 — Reference: Plugin Comparison Matrix

> **Document ID:** 207  
> **Category:** Reference  
> **Purpose:** Compare all planned plugins/features for scope, complexity, and requirements  
> **Audience:** Team, CTO  
> **Outcome:** 📖 Quick reference for plugin initiative status

---

## Plugin Portfolio Overview

| # | Plugin/Feature | Type | Status | Drop-in? | Framework Change? |
|---|----------------|------|--------|----------|-------------------|
| 1 | ApplicationLifecycleLogger | BackgroundProcess | ✅ Complete | ✅ Yes | None |
| 2 | SignalRConnectionMonitor | BackgroundProcess | 📋 Planned | ❌ No | Hub modification |
| 3 | ConfigurationViewer | BackgroundProcess | 📋 Planned | ❌ No | Program.cs line |
| 4 | **Plugin Dashboard** | **UI Page** | 📋 Planned | ❌ No | None (UI only) |

---

## Detailed Comparison

### Complexity

| Aspect | LifecycleLogger | SignalR Monitor | Config Viewer | Plugin Dashboard |
|--------|-----------------|-----------------|---------------|------------------|
| **Lines of Code** | ~140 | ~300 | ~200 | ~250 |
| **New Files** | 1 | 2 | 2 | 3 |
| **Framework Changes** | 0 | 1 (Hub) | 1 (Program.cs) | 0 |
| **Dependencies** | None | ConcurrentDict | IConfiguration | Existing API |
| **Effort Estimate** | 2h | 6h | 4h | 3h |

### Features

| Feature | LifecycleLogger | SignalR Monitor | Config Viewer | Plugin Dashboard |
|---------|-----------------|-----------------|---------------|------------------|
| **ON/OFF Toggle** | ✅ | ✅ | ✅ | N/A |
| **Log to File** | ✅ | ✅ | ✅ | ❌ |
| **API Endpoint** | ❌ | v1.1 | v1.1 | ✅ |
| **Blazor Page** | ❌ | v2 | v2 | ✅ |
| **Debug-Only** | ❌ | ❌ | ✅ | ❌ |
| **Admin-Only** | ❌ | ❌ | ❌ | ✅ |

### Component Types

| Component | LifecycleLogger | SignalR Monitor | Config Viewer | Plugin Dashboard |
|-----------|-----------------|-----------------|---------------|------------------|
| **Plugin (.cs)** | ✅ | ✅ | ✅ | ❌ |
| **Helper Class** | ❌ | ✅ Tracker | ✅ Capture | ❌ |
| **API Endpoint** | ❌ | v1.1 | v1.1 | ✅ |
| **Blazor Page** | ❌ | v2 | v2 | ✅ |
| **DTO Class** | ❌ | ❌ | ❌ | ✅ |

---

## Security Comparison

| Security Aspect | LifecycleLogger | SignalR Monitor | Config Viewer | Plugin Dashboard |
|-----------------|-----------------|-----------------|---------------|------------------|
| **Exposes Sensitive Data** | ❌ No | ⚠️ User IDs | ✅ Yes (masked) | ❌ No (code stripped) |
| **Production Safe** | ✅ Yes | ✅ Yes | ❌ Dev only | ✅ Yes |
| **Default Enabled** | `true` | `true` | `false` | N/A |
| **Access Control** | None | None | Env gate | Admin only |

---

## Output Comparison

### ApplicationLifecycleLogger → File
```csv
Timestamp,Event,MachineName,ProcessId,Message
2025-01-15T10:30:00Z,APP_START,SERVER01,12345,Application started successfully
```

### SignalRConnectionMonitor → File
```csv
Timestamp,Event,ConnectionId,TenantId,UserId,Message
2025-01-15T10:30:00Z,CONNECT,abc123,tenant-guid,user-guid,Client connected
```

### ConfigurationViewer → File
```
# Configuration Dump
AllowedHosts=*
ConnectionStrings:AppData=In********************
```

### Plugin Dashboard → UI Table
```
┌────────┬───────────────────┬─────────┬───────┬────────┐
│ Status │ Name              │ Type    │ Ver   │ Author │
├────────┼───────────────────┼─────────┼───────┼────────┤
│  ON    │ Lifecycle Logger  │ BgProc  │ 1.0.0 │ WSU    │
│  ON    │ Example Process   │ BgProc  │ 1.0.0 │ Brad   │
└────────┴───────────────────┴─────────┴───────┴────────┘
```

---

## Implementation Order Recommendation

```
Phase 1: ✅ ApplicationLifecycleLogger (Complete)
         └── Establishes patterns, pure drop-in

Phase 2: Plugin Dashboard (Recommended Next)
         └── No framework changes needed
         └── Uses existing GetPluginsWithoutCode()
         └── Immediate visibility value

Phase 3: ConfigurationViewer
         └── Simple framework change (1 line)
         └── High debug value

Phase 4: SignalRConnectionMonitor
         └── More complex framework change (Hub)
         └── Most useful for multi-tenant debugging
```

**Rationale for Order Change:** Plugin Dashboard has NO framework changes and provides immediate visibility into the plugin ecosystem. It should be built before the more complex Config Viewer and SignalR Monitor.

---

## Document Trail

| Plugin/Feature | Planning | Implementation | Review | Summary |
|----------------|----------|----------------|--------|---------|
| LifecycleLogger | 200 | 201 | 203 | 202 |
| SignalR Monitor | 205 | TBD | TBD | TBD |
| Config Viewer | 206 | TBD | TBD | TBD |
| Plugin Dashboard | 208 | TBD | TBD | TBD |

---

## Quick Links

- **LifecycleLogger:** [200](200_standup_plugin-mvp-planning.md) → [201](201_implementation_lifecycle-logger.md) → [203](203_review_lifecycle-logger.md)
- **SignalR Monitor:** [205](205_standup_signalr-monitor-planning.md)
- **Config Viewer:** [206](206_standup_config-viewer-planning.md)
- **Plugin Dashboard:** [208](208_standup_plugin-dashboard-planning.md)
- **Focus Group:** [204](204_focusgroup_plugin-docs-review.md)

---

## Plugin Interface Quick Reference

| Interface | Type String | Method(s) | Use Case |
|-----------|-------------|-----------|----------|
| `IPlugin` | General | `Execute()` | Generic plugin execution |
| `IPluginAuth` | Auth | `Login()`, `Logout()` | Custom auth providers |
| `IPluginBackgroundProcess` | BackgroundProcess | `Execute(iteration)` | Scheduled tasks |
| `IPluginUserUpdate` | UserUpdate | `UpdateUser()` | User sync/modification |

---

*Created: 2025-01-XX*  
*Maintained by: [Quality]*

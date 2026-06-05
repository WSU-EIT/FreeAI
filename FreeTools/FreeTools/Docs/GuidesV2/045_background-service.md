# 045 — Work That Outlives a Click

> **Document ID:** 045  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Cover the background-processing plugin and background service for queuing deferred and recurring jobs.
> **Audience:** Advanced builders and extenders  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 04x (Extending Without Breaking: The Live Runtime) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Background Work Matters](#why-it-matters) | Why some work must run away from the request, and what "deferred" vs "recurring" mean |
| 2 | [Mental Model & Vocabulary](#mental-model) | Service, timer, plugin, iteration, tenant, the `IDataAccess` handle |
| 3 | [Anatomy of the Background Service](#anatomy) | `BackgroundProcessor`, how it boots, and the two-timer processing loop |
| 4 | [Queuing a Deferred Job](#deferred-jobs) | The two ways in: `ProcessBackgroundTasksApp` and `BackgroundProcess` plugins |
| 5 | [Recurring Jobs & Schedules](#recurring-jobs) | There is no cron — how the iteration counter and saved settings create real schedules |
| 6 | [Reliability: Retries, Failures, Idempotency](#reliability) | What happens on error, the in-flight guards, and writing safe handlers |
| 7 | [Observability & Operations](#observability) | Logging to console and file, load-balancing filter, IIS always-running |
| 8 | [Pitfalls & Best Practices](#pitfalls) | UTC, long tasks, multi-instance duplication, interval tuning |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Background Work Matters

**Why it matters:** When a user clicks a button in the app, they are waiting. Every second that click takes is a second the person stares at a spinner. So any work that is slow, or that nobody is actively waiting for, should not happen during that click. It should happen *somewhere else, later* — on its own schedule, with nobody watching. That "somewhere else" is the **background service**.

Two plain-English terms describe the work this service handles:

- A **deferred job** is work you push off until later instead of doing it right now. *Example:* a tenant chooses to mark records as "deleted" rather than erasing them on the spot. The actual erasing is deferred — it happens quietly in the background once the records are old enough.
- A **recurring job** is work that repeats on a rhythm — every minute, every hour, every night. *Example:* "every 100 cycles, clean up stale cached files."

FreeCRM ships a real, working example of both. Its background service exists primarily to **permanently delete records that were only *marked* as deleted**, once a tenant's retention period has elapsed. That is deferred work (it waits until records are old enough) that runs on a recurring rhythm (the service wakes up on a timer). You extend the same service to run your own deferred and recurring tasks — that is the whole point of this doc.

The alternative — doing this work inside the web request — would make deletes slow, make the app fragile (if the user closes the tab mid-cleanup, the cleanup stops), and tie housekeeping to human clicks instead of the clock. Background work decouples the two.

---

<a id="mental-model"></a>
## 2. Mental Model & Vocabulary

Picture a **night-shift worker** in a building that is otherwise running normal business hours. The daytime staff (web requests) serve customers face to face. The night-shift worker wakes up on a fixed schedule, walks a checklist, does the slow chores nobody wants to do during business hours, and goes back to sleep. The background service *is* that night-shift worker — except it wakes up every 60 seconds, not every night.

Here is the vocabulary you need, each grounded in the actual code:

- **Background service** — A `BackgroundService` is a .NET base class for a task that runs continuously alongside your web app for its whole lifetime. FreeCRM's implementation is the class `BackgroundProcessor` (`CRM/Classes/BackgroundProcessor.cs`), and it inherits from it: `public class BackgroundProcessor : BackgroundService`. .NET starts it automatically when the app starts.
- **Hosted service** — The mechanism that tells .NET "run this thing for the app's whole life." In `Program.cs` the processor is registered with `builder.Services.AddHostedService<BackgroundProcessor>(...)`. That one line is what makes the night-shift worker show up to work.
- **Timer** — A clock that fires an event on an interval. The processor uses two `System.Timers.Timer` objects: a slow **queue timer** that wakes up every *N* seconds to gather work, and a fast **processor timer** that drains queued plugin work.
- **Iteration** — A simple counter (`_iterations`) that increments by one every time the service wakes up. It is the heartbeat number. Almost all scheduling in this system is built by doing arithmetic on the iteration (e.g., "every 6th iteration"). Your code receives it as the `Iteration` / `iteration` argument.
- **Tenant** — One isolated customer/organization in this multi-tenant app. The service loops over every enabled tenant and does work *per tenant*, plus a pass for `Guid.Empty` which represents app-wide (not tenant-specific) work.
- **Job / task** — There is no formal "Job" object. A "job" here is simply *your method or plugin being called on each wake-up*. You decide, inside that call, whether this is the iteration on which real work should happen.
- **`IDataAccess`** — The data-access handle: the object you call to read and write the database. The service fetches a fresh one on each wake-up with `_serviceProvider.GetRequiredService<IDataAccess>()`. Your background code uses it to look things up and save results.

The important mental shift: this is **not** a job-queue product with named jobs, payloads, and a dashboard. It is a **heartbeat plus two extension points**. You hang your work off the heartbeat and decide when it actually fires.

---

<a id="anatomy"></a>
## 3. Anatomy of the Background Service

**Why it matters:** If you understand how the processor boots and loops, you'll know exactly *when* your code runs and *what it has access to* — which is the difference between a task that fires reliably and one that mysteriously never runs.

### How it gets switched on

The service is configured in the `BackgroundService` section of `appsettings.json`. The shipped defaults:

```json
"BackgroundService": {
  "Enabled": true,
  "LoadBalancingFilter": "",
  "LogFilePath": "",
  "ProcessingIntervalSeconds": 60,
  "StartOnLoad": true
}
```

- **`Enabled`** — Master on/off switch. If false, the service is never registered at all.
- **`ProcessingIntervalSeconds`** — How many seconds between wake-ups. Default 60.
- **`StartOnLoad`** — If true, the first run happens *immediately* at startup. If false, the first run waits one full interval (so with a 60-second interval, nothing happens for the first 60 seconds).
- **`LoadBalancingFilter`** and **`LogFilePath`** — covered in [Section 7](#observability).

`Program.cs` reads these and, only when enabled, registers the hosted service:

```csharp
int processingIntervalSeconds = builder.Configuration.GetValue<int>("BackgroundService:ProcessingIntervalSeconds");
bool startOnLoad = builder.Configuration.GetValue<bool>("BackgroundService:StartOnLoad");
builder.Services.AddHostedService<BackgroundProcessor>(x => ActivatorUtilities.CreateInstance<BackgroundProcessor>(
    x, logger, x.GetRequiredService<IServiceProvider>(), processingIntervalSeconds, startOnLoad));
```

### The startup handshake — `ExecuteAsync`

When .NET starts the hosted service it calls `ExecuteAsync` once. This method does three things: it logs that it is starting, it discovers any background-process plugins, and it arms the two timers.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Background Processor is starting.");

    var da = _serviceProvider.GetRequiredService<IDataAccess>();
    var allPlugins = da.GetPlugins();
    if (allPlugins.Any(x => x.Type.ToLower() == "backgroundprocess")) {
        _availablePlugins = allPlugins.Where(x => x.Type.ToLower() == "backgroundprocess").ToList();
    }
    ...
}
```

It then sets up two timers. (`AutoReset` controls whether a timer keeps firing on its own; `true` means "repeat forever," `false` means "fire once until restarted.")

- **`queueTimer`** — interval = `ProcessingIntervalSeconds`, `AutoReset = true`. This is the steady heartbeat. Every tick calls `GetTasksToProcess()`.
- **`processorTimer`** — interval = 1 millisecond, `AutoReset = false`. This is a one-shot worker that drains any queued plugin work as fast as possible, then stops until it is needed again.

Finally, `StartOnLoad` decides the very first run:

```csharp
if (_startOnLoad) {
    await GetTasksToProcess();   // run immediately
} else {
    queueTimer.Start();          // wait one full interval, then run
}
```

### The work loop — `GetTasksToProcess`

This is the heart of the night shift. Each time it runs it:

1. Increments the iteration counter (`_iterations++`).
2. **Loops over every enabled tenant.** For each tenant:
   - Roughly every 100 iterations (and on the first), if the tenant is configured for soft-delete (`DeletePreference == MarkAsDeleted`) and has a positive retention window, it permanently purges records older than the cutoff via `da.DeleteAllPendingDeletedRecords(...)`. *This is the built-in deferred-delete job.*
   - Calls your app extension point, `da.ProcessBackgroundTasksApp(tenant.TenantId, _iterations)`, for tenant-specific work.
3. Roughly every 100 iterations, clears stale cached compiled Blazor plugin binaries via `da.DeleteOldBlazorCachedPluginBinaries()`.
4. Calls `da.ProcessBackgroundTasksApp(Guid.Empty, _iterations)` once for **app-wide** (non-tenant) work.
5. **Queues any background-process plugins** for the fast processor timer to run.

So on a typical wake-up, *two extension points fire*: your `ProcessBackgroundTasksApp` (once per tenant, plus once for `Guid.Empty`) and any registered `BackgroundProcess` plugins. The next two sections explain exactly how to use each.

---

<a id="deferred-jobs"></a>
## 4. Queuing a Deferred Job

**Why it matters:** "Queuing a job" here doesn't mean dropping a message on a queue and walking away. It means **placing your code in one of the two spots the heartbeat already calls**, and letting the iteration counter decide when it actually does work. There are exactly two entry points, and the README names both.

### Entry point 1 — `ProcessBackgroundTasksApp` (the simple, in-code way)

This is a method that already exists in `CRM.DataAccess/DataAccess.App.cs`, waiting for you to fill in. The service calls it on every wake-up, handing you the tenant and the current iteration. You return a `BooleanResponse` (`Result` plus a list of `Messages`).

Here is the shipped stub, faithfully — note it returns success and does nothing until you add code:

```csharp
public async Task<DataObjects.BooleanResponse> ProcessBackgroundTasksApp(Guid TenantId, long Iteration)
{
    var output = new DataObjects.BooleanResponse();

    // Process any background tasks specific to your app here.
    // Return output.Result = true if all tasks were processed successfully.
    // Otherwise, add any error messages to output.Messages and set output.Result = false.
    output.Result = true;

    return output;
}
```

A **run-once-then-defer** pattern looks like this. Because you have `IDataAccess`, you can persist a timestamp in settings and gate the work on it. This is the idiom the stub's own comments recommend:

```csharp
var lastRun = GetSetting<DateTime>("MyCustomProcessLastRunDate", DataObjects.SettingType.DateTime);
if (lastRun == default(DateTime) || lastRun < DateTime.UtcNow.AddMinutes(-10)) {
    // ... do the deferred work here ...
    SaveSetting("MyCustomProcessLastRunDate", DataObjects.SettingType.DateTime, DateTime.UtcNow);
}
```

Use this entry point when the work is part of your core app, lives in your source, and benefits from direct database access.

### Entry point 2 — A `BackgroundProcess` plugin (the pluggable way)

The second route is to write a **plugin**: a self-contained class, loaded at runtime, that implements `IPluginBackgroundProcess`. The interface (in `CRM/PluginsInterfaces.cs`) is small — it has one method, `Execute`, which receives the data-access object, the plugin's own metadata, and the iteration:

```csharp
public interface IPluginBackgroundProcess : IPluginBase
{
    Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Execute(
        DataAccess da,
        Plugins.Plugin plugin,
        long iteration
    );
}
```

The shipped example is `ExampleBackgroundProcess.cs` in the `PluginFiles` folder. Every plugin advertises a `Properties()` dictionary — the **`Type` must be `"BackgroundProcess"`** or the service will never pick it up (recall the discovery filter `x.Type.ToLower() == "backgroundprocess"`):

```csharp
public Dictionary<string, object> Properties() =>
    new Dictionary<string, object> {
        { "Id", new Guid("3961b30f-0c33-474b-a14c-a73174058f47") },
        { "Author", "Brad Wickett" },
        { "Name", "Plugin Example Background Process" },
        { "Type", "BackgroundProcess" },
        { "Version", "1.0.0" }
        // ...
    };
```

And its `Execute` simply logs a message each time it runs, returning the success tuple:

```csharp
public async Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Execute(
    DataAccess da, Plugins.Plugin plugin, long iteration)
{
    await Task.Delay(0);
    var messages = new List<string>();
    if (plugin != null) {
        messages.Add("Received Plugin Object: " + plugin.Name + ", iteration: " + iteration.ToString());
    }
    return (Result: true, Messages: messages, Objects: null);
}
```

**Which to choose?** Use `ProcessBackgroundTasksApp` for tasks baked into your application. Use a `BackgroundProcess` plugin when you want the task to be droppable/removable as a separate file without rebuilding the whole solution — including `.plugin` files that carry their own external assemblies. The general plugin model is covered in [043 — Pluggable by Design](043_plugin-model.md).

> **There is no "payload."** Neither entry point receives a custom message body. The only inputs are the tenant id (for `ProcessBackgroundTasksApp`) and the iteration counter. State you need between runs goes in the database via settings, not in a queued message.

---

<a id="recurring-jobs"></a>
## 5. Recurring Jobs & Schedules

**Why it matters first — set the right expectation:** This system **does not use cron expressions** (the `* * * * *` strings you may know from Unix schedulers). There is no cron parser anywhere in the background service. If you came here expecting `"0 2 * * *"` syntax, you will not find it — and writing your schedule any other way will silently never fire. Scheduling is built from two simple, real tools instead.

### Tool 1 — The iteration counter (for fixed rhythms)

Every call to your code carries the iteration number. Because the service wakes up on a fixed interval, "every Nth iteration" *is* a schedule. The stub spells out the math directly:

```csharp
// If ProcessingIntervalSeconds is 10, then iteration % 6 == 0 fires once per minute:
if (Iteration % 6 == 0) {
    // your task code here.
}
```

The formula is: **fire-every-X-seconds ÷ `ProcessingIntervalSeconds` = the modulo number.** With the default 60-second interval, `Iteration % 60 == 0` is roughly hourly, `Iteration % 1440 == 0` is roughly daily. The built-in cleanup tasks use exactly this style — `_iterations == 1 || _iterations % 100 == 0` — to run the soft-delete purge and the stale-plugin-cache cleanup only every hundredth cycle, sparing the database on every other tick.

### Tool 2 — A saved "last run" timestamp (for wall-clock schedules)

Iteration math drifts if the app restarts (the counter resets to zero) or if you want "once per real-world day regardless of restarts." For that, store the last-run time in the database and compare against the clock — the pattern from [Section 4](#deferred-jobs):

```csharp
var lastRun = GetSetting<DateTime>("MyCustomProcessLastRunDate", DataObjects.SettingType.DateTime);
if (lastRun == default(DateTime) || lastRun < DateTime.UtcNow.AddMinutes(-10)) {
    // run your code
    SaveSetting("MyCustomProcessLastRunDate", DataObjects.SettingType.DateTime, DateTime.UtcNow);
}
```

This survives restarts and gives you true "no more often than every X" behavior. Combine the two: use iteration modulo to keep checks cheap, and the saved timestamp to make the actual schedule robust.

### Day-of-week / hour-of-day windows

If you need "only on weekends" or "only between 1 AM and 4 AM," there is no built-in switch — you add the `if` yourself inside your handler. The example plugin's own comment says exactly this:

> Custom plugins will be called every time the background process runs... If you need to only have this happen on specific days, or during specific hours, you will need to add that logic here.

So a nightly window is just a guard you write: `if (DateTime.UtcNow.Hour >= 1 && DateTime.UtcNow.Hour < 4) { ... }` — and note **UTC**, which matters (see [Section 8](#pitfalls)).

---

<a id="reliability"></a>
## 6. Reliability: Retries, Failures, Idempotency

**Why it matters:** A background task runs unattended. If it throws an error, no user sees a red toast — so you have to understand the failure behavior up front, because the framework's safety net is deliberately thin.

### What happens when your task reports failure

Both entry points return a result. When you set `output.Result = false` and add messages, the service does **not** crash, retry, or roll back — it simply *logs the messages as errors* and moves on. The logging fan-out is this method:

```csharp
protected void ProcessTasksMessages(DataObjects.BooleanResponse? response)
{
    if (response != null && response.Messages.Count > 0) {
        if (response.Result) {
            foreach (var message in response.Messages) { _logger.LogInformation(message); }
        } else {
            foreach (var message in response.Messages) { _logger.LogError(message); }
        }
    }
}
```

**There is no automatic retry, no backoff, and no dead-letter queue.** Success messages are logged as information; failure messages are logged as errors. That's the whole contract. If a task needs to retry, *you* build the retry — typically by simply **not** advancing your saved "last run" timestamp on failure, so the next wake-up tries again.

### The in-flight guards (so the same work doesn't double-run)

The service does protect against one specific hazard: **re-entrancy**, where a slow task is still running when the next timer tick arrives and tries to start it again. Two `List<Guid>` guards prevent this:

- `_processingAppTasks` — a tenant id is added before its `ProcessBackgroundTasksApp` runs and removed after, so the same tenant's app tasks never run twice concurrently. `Guid.Empty` (app-wide work) is guarded the same way.
- `_processingPlugins` — a plugin's id is added before `Execute` and removed after, so a still-running plugin is skipped on the next pass rather than launched again.

This is concurrency protection, not durability. It stops overlap; it does not remember anything across an app restart.

### Why idempotency is on you

**Idempotent** means "running it twice has the same effect as running it once." Because there is no durable record of what already ran, and because a restart resets the iteration counter, your handler must tolerate being called again on data it may have already processed. Practical rules:

- **Query for the work, don't assume it.** The built-in purge re-asks the database for records older than the cutoff every cycle; if there are none, it does nothing. Model your task the same way — find the rows that still need doing, act, repeat.
- **Persist progress in the database**, via settings or your own table, not in memory. In-memory counters vanish on restart.
- **Make writes safe to repeat** — "delete rows older than X" and "set status = done where status = pending" are naturally idempotent; "add one to a balance" is not.

---

<a id="observability"></a>
## 7. Observability & Operations

**Why it matters:** Because nobody is watching a background task in real time, your only window into it is its logs and a few operational switches. Knowing where the output goes — and how to stop the task from running everywhere at once — is the whole operations story.

### Logging: console always, file optionally

The service logs through .NET's standard logger. `Program.cs` builds it to always write to the console, and to *also* write to a file when you provide a path:

```csharp
var logFilePath = builder.Configuration.GetValue<string>("BackgroundService:LogFilePath");
string logFile = !String.IsNullOrWhiteSpace(logFilePath)
    ? System.IO.Path.Combine(logFilePath, "BackgroundService.log") : String.Empty;
var loggerFactory = LoggerFactory.Create(builder => {
    builder.AddConsole();
    if (!String.IsNullOrWhiteSpace(logFile)) { builder.AddFile(logFile); }
});
```

To turn on the file log, set **`LogFilePath`** in `appsettings.json` to a folder the app can write to (the README is explicit: "The application will need to be running under credentials that have write access to that folder"). The file is named `BackgroundService.log`. Every message your tasks return — and the "Background Processor is starting." startup line — lands here. This is why returning descriptive `Messages` from your handlers is worth the effort: those messages *are* your observability.

### Running on only one instance — `LoadBalancingFilter`

**Why it matters:** If you run several copies of the app behind a load balancer (for capacity), each copy starts its own background service — and they would all run the same cleanup, possibly stepping on each other. The `LoadBalancingFilter` setting pins the background work to a single chosen machine.

It works by name-matching against the server's machine name. From `Program.cs`:

```csharp
var loadBalancingFilter = String.Empty + builder.Configuration.GetValue<string>("BackgroundService:LoadBalancingFilter");
if (!String.IsNullOrWhiteSpace(loadBalancingFilter)) {
    var hostname = (String.Empty + System.Environment.MachineName).ToLower();
    backgroundServiceEnabled = hostname.Contains(loadBalancingFilter.ToLower());
}
```

So if you set `LoadBalancingFilter` to a string that appears only in one server's `MachineName`, the background service is enabled on that box and disabled on the others. Leave it blank and every instance runs it.

### Keeping it alive on IIS

If you host on IIS (Microsoft's web server), an idle app can be unloaded — and an unloaded app has no night-shift worker. The README's guidance: set the **Application Pool Start Mode to `AlwaysRunning`** and turn on **Preload Enabled** for the site (which requires the IIS *Application Initialization* feature). Together these keep the process warm so the timers keep ticking.

### Startup and shutdown behavior

- **Startup:** controlled by `StartOnLoad` — immediate run, or wait one interval (see [Section 3](#anatomy)).
- **Shutdown:** `ExecuteAsync` receives a `CancellationToken stoppingToken`. .NET signals it when the app is stopping; the hosted-service base class then tears the service down. Tasks already mid-flight finish what they're doing — another reason to keep individual tasks short and idempotent.

---

<a id="pitfalls"></a>
## 8. Pitfalls & Best Practices

**Why it matters:** The background service is intentionally minimal, which means a few foot-guns are easy to step on. Each item below maps to something real in the code.

- **Everything is UTC — don't schedule in local time.** The built-in purge uses `DateTime.UtcNow`, and the recommended timestamp pattern stores `DateTime.UtcNow`. If you write an "only at 2 AM" window using local time, it will fire at the wrong hour on a UTC server. Always reason in UTC.

- **Cron syntax does not exist here.** (Repeating because it's the most common wrong assumption.) Build schedules from the iteration counter and/or a saved timestamp, per [Section 5](#recurring-jobs). A cron string will compile fine and never run.

- **The iteration counter resets on restart.** `_iterations` starts at zero every time the app boots, and the first iteration is treated specially (`_iterations == 1`). For schedules that must survive restarts, lean on the saved-timestamp pattern, not pure modulo math.

- **Keep tasks short; they share the timer.** A task that runs longer than `ProcessingIntervalSeconds` will be *skipped* on the next tick by the in-flight guard rather than overlapped — which silently stretches your effective interval. If you have genuinely long work, do a little each cycle (process a batch, then return) instead of all of it at once.

- **Mind multi-instance duplication.** Without `LoadBalancingFilter`, every app instance runs the full background loop. For cleanup-style work that's usually fine (it's idempotent), but for anything that must run exactly once, set the filter to a single machine ([Section 7](#observability)).

- **Set the interval deliberately.** The default is 60 seconds, and the heavy cleanup tasks deliberately run only every ~100 iterations to spare the database. If you shorten `ProcessingIntervalSeconds` a lot, recompute your modulo numbers and watch database load.

- **Return real messages on both success and failure.** Since logs are your only visibility, a task that returns `Result = true` with no messages is invisible. Add a short informational message so the log shows the task actually ran.

- **Don't hold state in fields between runs.** Use the database (settings or your own tables). In-memory fields evaporate on restart and aren't shared across instances.

- **Plugin `Type` must be exactly `BackgroundProcess`.** Discovery filters on `x.Type.ToLower() == "backgroundprocess"`. A typo in the plugin's `Properties()` means it is loaded but never executed by the background service.

**The one-line rule of thumb:** treat every background run as if it might be the first run after a crash — query for what still needs doing, do a bounded amount of it, record progress in the database, and log what you did.

---

<a id="related-docs"></a>
## 9. Related Docs

- [043 — Pluggable by Design: Authoring Plugins](043_plugin-model.md) — the general plugin model
- [046 — Pushing State Live Over SignalR](046_realtime-signalr.md) — the sibling live-runtime service

---
*GuidesV2 · 045 · drafted from source ( `CRM/Classes/BackgroundProcessor.cs`, `CRM.DataAccess/DataAccess.App.cs`, `CRM/PluginFiles/ExampleBackgroundProcess.cs`, `CRM/Program.cs`, `appsettings.json` ).*

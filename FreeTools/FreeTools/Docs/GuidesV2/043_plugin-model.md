# 043 — Pluggable by Design: Authoring Plugins

> **Document ID:** 043  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Describe the plugin extension surface and the contracts a plugin implements to attach to the framework.
> **Audience:** Advanced builders and extenders  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 04x (Extending Without Breaking: The Live Runtime) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Plugins Matter](#why-plugins-matter) | Plain-language overview and key terms defined |
| 2 | [The Extension Surface](#extension-surface) | The four built-in plugin types and what each receives |
| 3 | [The Plugin Contract](#plugin-contract) | The `Properties()` method, the invoker methods, and the optional interfaces |
| 4 | [Lifecycle and Hooks](#lifecycle-hooks) | Load at startup, compile, and execute on demand |
| 5 | [Discovery and Registration](#discovery-registration) | How `Load(path)` finds files and wires plugins into DI |
| 6 | [Capabilities and Permissions](#capabilities-permissions) | `LimitToTenants`, `ContainsSensitiveData`, and extra assemblies |
| 7 | [Authoring a Plugin: Worked Example](#worked-example) | A minimal `Example`-type plugin, start to finish |
| 8 | [Pitfalls and Compatibility](#pitfalls-compatibility) | Unique IDs, async-over-sync, versioning, and common mistakes |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-plugins-matter"></a>
## 1. Why Plugins Matter

**Why this matters:** sooner or later a customer wants the product to do something you never built. A **plugin** lets them add that behavior *without* touching your source code, recompiling the app, or redeploying. You ship the app once; they drop a file in a folder and the app grows a new capability on the next startup.

Let's define the three words you'll see throughout this doc:

- **Plugin** — a small piece of C# code, written by you or a customer, that the framework loads and runs on demand. In FreeCRM a plugin is a plain `.cs` or `.plugin` text file placed in the `PluginFiles` folder.
- **Host** — the running application that loads and executes plugins. Here the host is the FreeCRM server (`CRM` project), and the engine that does the loading and running lives in the **`CRM.Plugins`** project.
- **Contract** — the agreed shape every plugin must have so the host knows how to talk to it. In FreeCRM the contract is: "expose a method named `Properties()` that returns metadata, plus an invoker method (`Execute`, `Login`, or `UpdateUser`) the host can call."

The clever part is *how* the host runs a plugin. It does **not** require you to pre-compile the plugin into a DLL. Instead the engine reads the raw C# text and compiles it **in memory at runtime** using the Roslyn compiler (`Microsoft.CodeAnalysis.CSharp`). That means a customer can edit a plugin file in Notepad, restart the app, and their new code is live — no build server, no NuGet package, no redeploy.

The project's own plugin docs (`PluginFiles/Plugins.md`) put it plainly: *"if you are creating a solution that will be given to customers or clients that will not have the ability to modify the source code of the application, then the plugin architecture provides a way for those customers to add custom functionality."* It is also useful even for in-house builds, because a plugin can be scoped to a single tenant (see [§6](#capabilities-permissions)) — so you can deliver one customer's special-case logic without polluting everyone else's app with `if (tenant == ...)` branches.

FreeCRM ships four built-in plugin **types** out of the box: **Auth**, **BackgroundProcess**, **Example**, and **UserUpdate**. The next section explains what each one is for.

---

<a id="extension-surface"></a>
## 2. The Extension Surface

**Why this matters:** a plugin is only useful if the host hands it the right tools and calls it at the right moment. The "extension surface" is the set of named **types** the host understands, and the objects it passes into each one. If your plugin declares a type the host recognizes, the host knows which method to call and what arguments to supply.

The `Type` property (a string in your plugin's metadata) is the switch. Based on it, the loader assigns an **invoker** — the name of the method the host will call when it runs your plugin. From `CRM.Plugins/Plugins.cs`:

```csharp
switch (plugin.Type.ToLower()) {
    case "auth":
        plugin.Invoker = "Login";
        break;
    case "userupdate":
        plugin.Invoker = "UpdateUser";
        break;
    default:
        plugin.Invoker = "Execute";
        break;
}
```

Here are the four built-in types, what they are for, and exactly what each invoker receives (the argument list is assembled in `DataAccess.Plugins.cs → ExecutePlugin`):

| Type | Invoker called | What the host passes in | Used for |
|------|----------------|--------------------------|----------|
| **Example** | `Execute` | `DataAccess da`, `Plugin plugin`, `DataObjects.User? currentUser`, then any extra objects you supply | General-purpose custom actions (reports, exports, buttons, anything) |
| **Auth** | `Login` / `Logout` | `DataAccess da`, `Plugin plugin`, `string url`, `Guid tenantId`, `HttpContext httpContext` | Custom sign-in against an external system — see [044](044_auth-plugin.md) |
| **BackgroundProcess** | `Execute` | `DataAccess da`, `Plugin plugin`, `long iteration` | Recurring server work, run on a timer — see [045](045_background-service.md) |
| **UserUpdate** | `UpdateUser` | `DataAccess da`, `Plugin plugin`, `DataObjects.User? updateUser` | Syncing/enriching a user record from an external source |

Two details worth calling out, because they're easy to miss:

- **The first object is almost always `DataAccess`.** This is the gateway to nearly everything the app can do — read and write the database, send email, look up tenants, authenticate users. Giving every plugin a `DataAccess` reference is what makes plugins powerful rather than sandboxed toys.
- **Auth and BackgroundProcess do *not* get a `CurrentUser`.** The code special-cases them: an `auth` plugin runs *before* anyone is logged in (so there is no current user), and a `backgroundprocess` plugin runs on a server timer with no user context, receiving an `iteration` counter instead.

These four are just the built-ins. Because the whole mechanism is "compile a string and call a named method," you can invent your own type (say, `"report"`) and write host code that finds plugins of that type and calls them with whatever objects you like. The README explicitly encourages this: *"you can easily add support for more plugin types … by following the various examples already built into the application."*

---

<a id="plugin-contract"></a>
## 3. The Plugin Contract

**Why this matters:** the host has to discover what a plugin *is* before it can run it — its name, its ID, its type, whether it needs user input. The contract is the minimum every plugin must provide so the host can register it correctly.

### 3.1 The required method: `Properties()`

Every plugin **must** expose a public method named `Properties()` that returns a `Dictionary<string, object>` of metadata. At startup the host actually *compiles and runs* this method to read the values back. The minimum keys are `Id`, `Author`, `Name`, `Type`, and `Version`:

```csharp
public Dictionary<string, object> Properties() =>
    new Dictionary<string, object> {
        { "Id", new Guid("9bbdfb99-80cd-4bbb-8741-6d287437e5f7") },
        { "Author", "Brad Wickett" },
        { "ContainsSensitiveData", false },
        { "Description", "Shown to users in the application." },
        { "Name", "Plugin Example 1" },
        { "SortOrder", 0 },
        { "Type", "Example" },
        { "Version", "1.0.0" }
    };
```

What the loader reads from this dictionary (see the `Plugin` class in `Plugins.cs`):

| Key | Type | Why it matters |
|-----|------|----------------|
| `Id` | `Guid` | **Must be unique.** A plugin with an empty or duplicate `Id` is silently skipped. Generate a fresh GUID for every new plugin. |
| `Name` | `string` | Display name shown in the UI. |
| `Author` | `string` | Who wrote it. |
| `Type` | `string` | Decides the invoker (see [§2](#extension-surface)). |
| `Version` | `string` | Identifies a build; combined with `Id` to locate the cached copy. |
| `Description` | `string` | Optional long text shown to users. |
| `SortOrder` | `int` | Optional; plugins run in `SortOrder`, then `Name`, order. Defaults to `0`. |
| `ContainsSensitiveData` | `bool` | If `true`, the plugin source is AES-encrypted before it ever leaves the server (see [§6](#capabilities-permissions)). |
| `LimitToTenants` | `List<Guid>` | Optional allow-list of tenants (see [§6](#capabilities-permissions)). |
| `Prompts` | `List<PluginPrompt>` | Optional input fields to collect from the user before running. |

### 3.2 The invoker method

Besides `Properties()`, the plugin needs the invoker method for its type. Its return value is always the same three-part **tuple** — a single value bundling three results together:

```csharp
Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)>
```

- `Result` — `true` for success, `false` for failure.
- `Messages` — human-readable status lines (logged, or shown to the user).
- `Objects` — any data you want to hand back (an updated user, a rendered report, etc.).

A complete minimal `Execute` looks like this:

```csharp
public async Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Execute(
    DataAccess da,
    Plugins.Plugin plugin,
    DataObjects.User? currentUser
)
{
    var messages = new List<string>();
    messages.Add("Plugin: " + plugin.Name);

    object[] output = new object[] { "This is an object returned from the plugin." };

    return (Result: true, Messages: messages, Objects: output);
}
```

### 3.3 The optional interfaces

FreeCRM provides interfaces in `CRM/PluginsInterfaces.cs` that document the expected method signatures. They are a **convenience, not a requirement** — your plugin works even if it implements none of them, as long as the method names and signatures match. The base interface is just:

```csharp
public interface IPluginBase
{
    public Dictionary<string, object> Properties();
}
```

The specialized ones inherit from it and add the invoker:

- `IPlugin` → adds `Execute(...)`
- `IPluginAuth` → adds `Login(...)` and `Logout(...)`
- `IPluginBackgroundProcess` → adds `Execute(da, plugin, long iteration)`
- `IPluginUserUpdate` → adds `UpdateUser(...)`

Implementing the interface is recommended because, if you develop the plugin inside the solution, the compiler will tell you when your signature drifts from what the host expects.

---

<a id="lifecycle-hooks"></a>
## 4. Lifecycle and Hooks

**Why this matters:** there is no `Init()` or `Dispose()` event you wire up — the plugin lifecycle is split into two clean phases, and knowing which phase your code runs in tells you what you can rely on. Phase one happens **once, at app startup**; phase two happens **every time** the plugin is invoked.

### Phase 1 — Load (once, at startup)

When the app starts, `Program.cs` builds the plugin engine and points it at the folder:

```csharp
var plugins = new Plugins.Plugins();
// ... configure ServerReferences and UsingStatements ...
string pluginsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PluginFiles");
plugins.Load(pluginsPath);
builder.Services.AddTransient<Plugins.IPlugins>(x => plugins);
```

During `Load`, for **every** plugin file, the engine compiles the code and calls `Properties()` to harvest the metadata. So your `Properties()` method runs at startup — keep it cheap and side-effect-free; do not open database connections or call external services inside it. The result becomes a `Plugin` descriptor object stored in an in-memory list (`AllPlugins`) and registered in dependency injection (DI), the .NET system that hands shared services to whoever needs them.

### Phase 2 — Execute (on demand, many times)

Later, when something needs the plugin to actually *do* its job, the host builds a `PluginExecuteRequest` and calls `DataAccess.ExecutePlugin`. That method assembles the right argument objects for the type, loads any additional assemblies, then calls `ExecuteDynamicCSharpCode<T>`, which compiles the plugin source (again, in memory), creates an instance of the plugin class, and invokes the named method by reflection:

```csharp
var type = assembly.GetType(Namespace + "." + Classname);
var obj = Activator.CreateInstance(type);
var r = type.InvokeMember(invokerFunction, /* ... */, obj, objects.ToArray());
```

One important subtlety: the engine **runs async methods synchronously**. After invoking your method it detects a `Task` and blocks, polling until the task completes, before unwrapping the result. That is why every example uses `await Task.Delay(0)` — the contract requires an `async` signature, but the host waits for it to finish either way. Practically: your plugin can be `async`, but don't assume the host fires it and forgets it.

There is no explicit teardown hook. A plugin holds no long-lived state across executions — each call compiles fresh and creates a new instance — so there is nothing to dispose. If you need to remember something between runs (e.g. "when did this background job last run?"), persist it through `DataAccess` into the database, exactly as the `ExampleBackgroundProcess` comments suggest.

---

<a id="discovery-registration"></a>
## 5. Discovery and Registration

**Why this matters:** plugins are found by *file convention*, not configuration. Knowing the rules tells you precisely where to put a file and what to name it so the app picks it up.

### What `Load(path)` scans for

The loader (`Plugins.cs → Load`) looks in the `PluginFiles` folder for files with these extensions:

- **`.cs`** — a normal C# source file. Use this when the plugin compiles cleanly as part of your solution and references libraries you already have.
- **`.plugin`** — a plain-text file that the build system ignores. Use this when the code would *break the build* — for example because it references an external DLL that isn't in your solution. A `.cs` file in the project would cause compile errors; a `.plugin` file sidesteps that because the IDE doesn't try to compile it, yet the runtime loader still reads it.

For each file the loader also checks for an optional sidecar file with the same base name:

- **`<name>.assemblies`** — a list of extra DLLs the plugin needs (see [§6](#capabilities-permissions)).

Separately, *after* the per-file loop, the loader does a one-time scan of a `BlazorComponents` subfolder for `.razor`/`.blazor` files (each with optional `.json` config keyed to the Blazor file's own name) — this folder scan is independent of any plugin file's base name, and is covered in [047](047_custom-components.md).

### How a file becomes a registered plugin

For each `.cs`/`.plugin` file the loader:

1. Reads the file's raw text.
2. Detects the **namespace** and **class name** by scanning the source for the `namespace` and `public class` lines (`GetPluginNamespace` / `GetPluginClass`). You do **not** set these manually — they're auto-detected.
3. Compiles the code and calls `Properties()` to read the metadata.
4. Checks the `Id`: if it is empty or already registered, the plugin is **skipped**. This is the single most common reason a plugin silently fails to appear.
5. Builds a `Plugin` descriptor, assigns the invoker based on `Type`, and adds it to the in-memory `_plugins` list.

After the loop, plugins are ordered by `SortOrder` then `Name`.

### Where registered plugins live

The whole engine is registered in DI as `IPlugins`. Any host code can reach it — for example `DataAccess` exposes:

```csharp
public List<Plugins.Plugin> GetPlugins() { /* returns PluginsInterface.AllPlugins (cached) */ }
```

So "discovery" is purely startup file-scanning, and "registration" means the descriptor is sitting in the DI-held `AllPlugins` list (and a database cache via `PluginCache`), ready for the host to fetch and execute.

---

<a id="capabilities-permissions"></a>
## 6. Capabilities and Permissions

**Why this matters:** a plugin runs *inside your server process* with a live `DataAccess` handle — it is trusted code. There is no sandbox. So "permissions" here are less about restricting a plugin and more about three controls: scoping which tenants see it, protecting sensitive source, and granting it the libraries it needs to run.

### Scope: `LimitToTenants`

A **tenant** is one isolated customer/organization inside a multi-tenant app. By default a plugin is available to *every* tenant. Add a `LimitToTenants` list of tenant GUIDs to restrict it:

```csharp
{ "LimitToTenants", new List<Guid> { new Guid("00000000-0000-0000-0000-000000000001") } },
```

Now only the listed tenants ever see or run the plugin. This is how you deliver one customer's bespoke logic without exposing it to everyone — no in-page `if (tenant == X)` branching required.

### Source protection: `ContainsSensitiveData`

Plugin descriptors can be sent to the browser (for example, to drive client-side execution of UI plugins). If your plugin's source contains secrets — API keys, internal endpoints — set `ContainsSensitiveData` to `true`. When set, the loader **AES-encrypts the code** before the descriptor leaves the server:

```csharp
Code = containsSensitiveData ? EncryptCode(code) : code,
```

Encryption uses the `Encryption.Encryption` class (AES) with a fixed key held in the engine. The host transparently decrypts the code again before compiling it. The `UserUpdate.cs` example sets `ContainsSensitiveData = true` for exactly this reason — its real-world version would hold credentials for an external user system.

### Extra libraries: the `.assemblies` sidecar

If a plugin needs a DLL the host doesn't already load, list it in a `<name>.assemblies` file next to the plugin. The `HelloWorld` sample does this:

```
.\HelloWorld\HelloWorld.dll
```

A line can be either a path to a `.dll` (a leading `.\` is resolved relative to the plugins folder) **or** a `typeof(...)` expression that the loader evaluates at runtime to find an assembly's location. Either way, the named assembly is loaded into memory so the plugin's code can reference it during compilation and execution. Beyond these, the host already pre-loads the core CRM assemblies (`DataAccess`, `DataObjects`, EF models, `HttpContext`, etc.) so most plugins need no `.assemblies` file at all.

---

<a id="worked-example"></a>
## 7. Authoring a Plugin: Worked Example

**Why this matters:** seeing the smallest possible end-to-end plugin removes the mystery. Here is a complete, working `Example`-type plugin — the kind the app will list and let a user run on demand. Every line below is the real shape used by the shipped samples in `PluginFiles`.

**Step 1 — Create the file.** Add `MyFirstPlugin.cs` to the `PluginFiles` folder.

**Step 2 — Write the class.** It needs a unique namespace, a class, a `Properties()` method, and an `Execute` invoker:

```csharp
using CRM;
using Plugins;

namespace MyFirstPluginNamespace
{
    public class MyFirstPlugin : IPlugin
    {
        public Dictionary<string, object> Properties() =>
            new Dictionary<string, object> {
                { "Id", new Guid("11111111-2222-3333-4444-555555555555") }, // unique!
                { "Author", "Your Name" },
                { "ContainsSensitiveData", false },
                { "Description", "My first plugin." },
                { "Name", "My First Plugin" },
                { "SortOrder", 0 },
                { "Type", "Example" },
                { "Version", "1.0.0" }
            };

        public async Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Execute(
            DataAccess da,
            Plugins.Plugin plugin,
            DataObjects.User? currentUser
        )
        {
            await Task.Delay(0); // required because the signature is async

            var messages = new List<string>();
            messages.Add("Hello from " + plugin.Name + "!");

            if (currentUser != null) {
                messages.Add("Running for: " + currentUser.Email);
            }

            object[] output = new object[] { "A value returned to the caller." };

            return (Result: true, Messages: messages, Objects: output);
        }
    }
}
```

**Step 3 — Generate a fresh `Id`.** This GUID must be unique across all plugins. If you copied an existing plugin, change it — a duplicate `Id` causes the loader to skip your plugin entirely.

**Step 4 — (Optional) collect input with prompts.** To ask the user for data before running, add a `Prompts` entry to `Properties()`. Each `PluginPrompt` has a `Name`, a `Type` (the `PluginPromptType` enum: `Text`, `Password`, `Select`, `Checkbox`, `Date`, `File`, and more), and optional `Options`:

```csharp
{ "Prompts", new List<PluginPrompt> {
    new PluginPrompt { Name = "Username", Type = PluginPromptType.Text },
    new PluginPrompt { Name = "Password", Type = PluginPromptType.Password },
}},
```

At runtime the collected answers arrive in `plugin.PromptValues`, matched to each prompt by `Name`. (See `Example1.cs` for a tour of every prompt type and a custom button.)

**Step 5 — Restart the app.** On the next startup, `Load(...)` scans `PluginFiles`, compiles your file, reads `Properties()`, and registers the plugin. It now appears wherever the host lists `Example`-type plugins, ready to run.

That's the whole loop: drop a file, restart, run. No build pipeline, no deployment.

---

<a id="pitfalls-compatibility"></a>
## 8. Pitfalls and Compatibility

**Why this matters:** because plugins compile at runtime, mistakes don't show up as build errors — they show up as a plugin that silently doesn't appear, or fails when invoked. Here are the traps that bite people, and how the framework behaves around them.

- **A duplicate or empty `Id` = a silently missing plugin.** The loader only registers a plugin when `id != Guid.Empty` and no plugin with that `Id` is already loaded. Copy-pasting a sample without changing the GUID is the number-one cause of "my plugin isn't showing up." Always mint a new GUID.

- **Wrong namespace/class/method names break execution.** The host locates your class by the namespace and class name it auto-detected, then calls the invoker by name (`Execute`, `Login`, or `UpdateUser`). If your method name doesn't match the invoker the type expects, the call finds nothing and returns nothing. Implementing the matching interface (`IPlugin`, `IPluginAuth`, etc.) lets the compiler catch this for you.

- **Async is awaited synchronously — don't fire-and-forget.** The engine blocks until your `Task` completes before reading the result. Long-running work inside a plugin will hold up whatever called it. For genuinely long jobs, use a `BackgroundProcess` plugin (see [045](045_background-service.md)).

- **`Properties()` runs at startup — keep it pure.** It is compiled and executed during `Load`, before the app is serving requests. Don't put database calls, network calls, or heavy work in it; just return the static metadata dictionary.

- **Compile errors fail quietly.** If your plugin source doesn't compile, the runtime collects the Roslyn diagnostics and writes them to the console (`Console.WriteLine(diagnostic.Id + ": " + diagnostic.GetMessage())`) — the plugin simply won't run. When a plugin misbehaves, check the server console/log for compiler errors first.

- **`.cs` vs `.plugin` — pick the right extension.** Use `.cs` for code that compiles inside your solution. Use `.plugin` for code that would break the build (e.g. it references an external DLL via an `.assemblies` file). Putting build-breaking code in a `.cs` file will stop the whole project from compiling.

- **Versioning.** The host keys a cached plugin by `Id` **and** `Version` together (see `PluginCache` handling in `DataAccess.Plugins.cs`). Bump the `Version` string when you ship a meaningfully different build so the right copy is matched and refreshed.

- **Plugins are trusted, in-process code.** There is no sandbox — a plugin can do anything `DataAccess` can do. Only load plugin files you trust, and use `ContainsSensitiveData` to keep any embedded secrets from reaching the browser.

---

<a id="related-docs"></a>
## 9. Related Docs

- [044 — The Authentication Plugin at the Tenant Edge](044_auth-plugin.md) — an authentication plugin example
- [045 — Work That Outlives a Click](045_background-service.md) — a background plugin example

---
*GuidesV2 · 043 · drafted from source (`CRM.Plugins`, `CRM/PluginsInterfaces.cs`, `CRM/PluginFiles`, `CRM.DataAccess/DataAccess.Plugins.cs`, `CRM/Program.cs`).*

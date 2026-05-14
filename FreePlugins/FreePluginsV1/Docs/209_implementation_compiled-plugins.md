# 209 — Implementation: NuGet-Based Compiled Plugin Architecture

> **Document ID:** 209  
> **Category:** Implementation  
> **Purpose:** Enable plugins to be published as NuGet packages and loaded at runtime  
> **Author:** GitHub Copilot  
> **Date:** 2025-01-XX  
> **Status:** ✅ Complete  
> **Stock Project Modifications:** None

---

## TL;DR

**What:** A new plugin architecture that allows plugins to be compiled, published to NuGet, and loaded into the application without modifying any existing code.

**Key Achievement:** Zero modifications to existing `FreePlugins.Plugins`, `FreePlugins.DataAccess`, or any other stock project files.

**New Projects Created:**

| Project | Type | Description |
|---------|------|-------------|
| `FreePlugins.Abstractions` | Core | Interfaces for plugin authors (NuGet publishable) |
| `FreePlugins.Abstractions.Integration` | Bridge | Connects compiled plugins with existing system |
| `FreePlugins.SamplePlugin` | Example | Simple background process plugin |
| `FreePlugins.ExamplePlugin` | Example | All 16 prompt types demonstrated |
| `FreePlugins.AuthExamplePlugin` | Example | Auth plugin with Login/Logout |
| `FreePlugins.UserUpdateExamplePlugin` | Example | User synchronization plugin |
| `FreePlugins.TenantRestrictedPlugin` | Example | Tenant restriction demonstration |
| `FreePlugins.DataAccessExamplePlugin` | Example | Context and metadata access |

---

## Problem Statement

The existing plugin system uses **dynamic C# compilation at runtime** via Roslyn:
- Plugins are `.cs` source files in a `Plugins/` folder
- Code is compiled on-the-fly when the application starts
- This works but has limitations:
  - No IntelliSense or compile-time checking for plugin authors
  - Can't easily share plugins between projects
  - Can't version plugins independently
  - Source code must be distributed (security concern for some)

**User Request:** 
> "Can I publish a plugin as a NuGet package and load it into the web project? I'd really like to put plugins in their own csproj, compile it down to a published official NuGet package, then in Program.cs just say like `builder.AddPlugin<MyPlugin>(arg1, arg2, ...)`"

---

## Solution Architecture

### Project Structure

```
FreePluginsV1/
├── FreePlugins.Abstractions/              # Core interfaces (NuGet publishable)
│   ├── IPluginBase.cs                     # Base interface
│   ├── IPluginInterfaces.cs               # IPlugin, IPluginBackgroundProcess, etc.
│   ├── IPluginContext.cs                  # Execution context interfaces
│   ├── ICompiledPlugin.cs                 # Marker interfaces for compiled plugins
│   ├── PluginAttribute.cs                 # [Plugin] attribute for metadata
│   ├── PluginMetadata.cs                  # Plugin metadata classes
│   ├── PluginPromptBuilder.cs             # Fluent API for prompts
│   ├── PluginContext.cs                   # Default context implementation
│   ├── PluginServiceCollectionExtensions.cs  # AddPlugin<T>() extensions
│   └── CompiledPluginExecutor.cs          # Executes compiled plugins
│
├── FreePlugins.Abstractions.Integration/  # Bridge to existing system
│   ├── CompiledPluginRegistry.cs          # Static registry
│   ├── CompiledPluginConverter.cs         # Type converter
│   └── CompiledPluginDataAccessExtensions.cs  # IDataAccess extensions
│
├── FreePlugins.SamplePlugin/              # Simple background process example
├── FreePlugins.ExamplePlugin/             # All 16 prompt types
├── FreePlugins.AuthExamplePlugin/         # Auth plugin example
├── FreePlugins.UserUpdateExamplePlugin/   # User sync example
├── FreePlugins.TenantRestrictedPlugin/    # Tenant restriction example
├── FreePlugins.DataAccessExamplePlugin/   # Context access example
│
├── FreePlugins.Plugins/                   # UNCHANGED
├── FreePlugins.DataAccess/                # UNCHANGED
└── FreePlugins/                           # UNCHANGED (main web app)
```

---

## Example Plugins

### Complete Plugin Coverage

All four plugin interface types are now demonstrated:

| Interface | File-Based Example | Compiled Example |
|-----------|-------------------|------------------|
| `IPlugin` | Example1.cs, Example2.cs, Example3.cs | ExamplePlugin, TenantRestrictedPlugin, DataAccessExamplePlugin |
| `IPluginBackgroundProcess` | ExampleBackgroundProcess.cs | SamplePlugin |
| `IPluginAuth` | LoginWithPrompts.cs | AuthExamplePlugin |
| `IPluginUserUpdate` | UserUpdate.cs | UserUpdateExamplePlugin |

### Example Plugin Details

#### 1. FreePlugins.ExamplePlugin (AllPromptsPlugin)
**Demonstrates:** All 16 prompt types
- Button, Checkbox, CheckboxList, Date, DateTime
- File, Files, HTML, Multiselect, Number
- Password, Radio, Select, Text, Textarea, Time
- Button callbacks, dynamic options, conditional visibility

#### 2. FreePlugins.SamplePlugin (SampleBackgroundPlugin)
**Demonstrates:** Background process pattern
- `ICompiledBackgroundProcessPlugin` interface
- Iteration counter handling
- Scheduled execution pattern

#### 3. FreePlugins.AuthExamplePlugin (CustomAuthPlugin)
**Demonstrates:** Authentication flow
- `ICompiledAuthPlugin` interface
- `LoginAsync` and `LogoutAsync` methods
- Username/Password prompts
- Custom login button styling
- Tenant restriction

#### 4. FreePlugins.UserUpdateExamplePlugin (UserSyncPlugin)
**Demonstrates:** User synchronization
- `ICompiledUserUpdatePlugin` interface
- External system integration pattern
- User object modification
- Sensitive data handling

#### 5. FreePlugins.TenantRestrictedPlugin (TenantSpecificPlugin)
**Demonstrates:** Multi-tenancy
- `LimitToTenants` property
- Single tenant restriction
- Multiple tenant restriction
- SortOrder for execution ordering

#### 6. FreePlugins.DataAccessExamplePlugin (ContextInfoPlugin)
**Demonstrates:** Context access
- Plugin metadata access
- Service provider usage
- Logging methods
- Custom properties

---

## Implementation Details

### 1. FreePlugins.Abstractions (NuGet Package)

This package is what plugin authors reference. It contains:

**Interfaces:**
```csharp
// Base interface all plugins implement
public interface IPluginBase
{
    Dictionary<string, object> Properties();
}

// For general plugins
public interface IPlugin : IPluginBase
{
    Task<PluginResult> ExecuteAsync(IPluginContext context);
}

// For background process plugins
public interface IPluginBackgroundProcess : IPluginBase
{
    Task<PluginResult> ExecuteAsync(IPluginContext context, long iteration);
}

// Marker interfaces for compiled plugins
public interface ICompiledGeneralPlugin : ICompiledPlugin, IPlugin { }
public interface ICompiledBackgroundProcessPlugin : ICompiledPlugin, IPluginBackgroundProcess { }
```

**Plugin Attribute:**
```csharp
[Plugin(
    Id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    Name = "My Plugin",
    Type = PluginTypes.General,  // or PluginTypes.BackgroundProcess
    Version = "1.0.0",
    Author = "Your Name",
    Description = "What it does",
    SortOrder = 100,
    Enabled = true
)]
public class MyPlugin : ICompiledGeneralPlugin { ... }
```

**Fluent Prompt Builder:**
```csharp
// Easy way to create prompts
var prompts = new List<PluginPrompt>
{
    PluginPromptBuilder.Text("Username", "Enter your username").Required().Build(),
    PluginPromptBuilder.Password("Password", "Enter your password").Required().Build(),
    PluginPromptBuilder.Select("Role", "Select a role")
        .WithOptions(("Admin", "admin"), ("User", "user"))
        .Build(),
    PluginPromptBuilder.Checkbox("Remember", "Remember me").Build(),
};
```

**Registration Extensions:**
```csharp
// Register a single plugin
builder.Services.AddPlugin<MyPlugin>();

// Auto-discover all plugins in an assembly
builder.Services.AddPluginsFromAssembly(typeof(MyPlugin).Assembly);
```

### 2. FreePlugins.Abstractions.Integration (App Bridge)

This project bridges the compiled plugin system with the existing file-based system:

**CompiledPluginRegistry:**
```csharp
// Check if a plugin is compiled
if (CompiledPluginRegistry.IsCompiledPlugin(pluginId)) { ... }

// Get all compiled plugins
var plugins = CompiledPluginRegistry.AllPlugins;

// Get by type
var bgPlugins = CompiledPluginRegistry.GetByType("BackgroundProcess");
```

**IDataAccess Extensions:**
```csharp
// Execute a compiled background process
var result = await da.ExecuteCompiledBackgroundProcessAsync(pluginId, iteration, serviceProvider);

// Execute a compiled general plugin
var result = await da.ExecuteCompiledPluginAsync(pluginId, serviceProvider);

// Get all plugins (file-based + compiled)
var allPlugins = da.GetAllPluginsIncludingCompiled();
```

**Host Extensions:**
```csharp
var app = builder.Build();
app.LoadCompiledPlugins();  // Loads registered plugins into registry
app.Run();
```

### 3. FreePlugins.ExamplePlugin (Comprehensive Example)

A complete example plugin that demonstrates all features:

```csharp
using FreePlugins.Abstractions;

namespace FreePlugins.ExamplePlugin;

[Plugin(
    Id = "9bbdfb99-80cd-4bbb-8741-6d287437e5f8",
    Name = "All Prompts Example (Compiled)",
    Type = PluginTypes.General,
    Version = "1.0.0",
    Author = "WSU EIT",
    Description = "Demonstrates all 16 prompt types.",
    SortOrder = 0,
    Enabled = true
)]
public class AllPromptsPlugin : ICompiledGeneralPlugin
{
    public static Type PluginType => typeof(AllPromptsPlugin);
    
    public Dictionary<string, object> Properties() => new()
    {
        { "Id", Guid.Parse("9bbdfb99-80cd-4bbb-8741-6d287437e5f8") },
        { "Name", "All Prompts Example (Compiled)" },
        { "Prompts", BuildPrompts() },
        // ... other properties
    };
    
    public async Task<PluginResult> ExecuteAsync(IPluginContext context)
    {
        var messages = new List<string>();
        messages.Add($"Plugin Executed: {context.Plugin.Name}");
        
        foreach (var prompt in context.Plugin.Prompts)
        {
            messages.Add($"  • {prompt.Name}: {prompt.PromptType}");
        }
        
        return PluginResult.Success(messages);
    }
    
    private static List<PluginPrompt> BuildPrompts() =>
    [
        new PluginPrompt { Name = "Button1", PromptType = PluginPromptType.Button, ... },
        new PluginPrompt { Name = "Checkbox", PromptType = PluginPromptType.Checkbox, ... },
        // ... all 16 types
    ];
}
```

---

## Prompt Types Supported

| Type | Description | Example Use |
|------|-------------|-------------|
| Button | Clickable button | Execute action |
| Checkbox | Single boolean | Accept terms |
| CheckboxList | Multiple selection | Select categories |
| Date | Date picker | Birth date |
| DateTime | Date and time | Appointment |
| File | Single file upload | Profile picture |
| Files | Multiple files | Attachments |
| HTML | Display HTML | Instructions |
| Multiselect | Dropdown multi-select | Assign roles |
| Number | Numeric input | Quantity |
| Password | Masked text | Credentials |
| Radio | Single selection | Choose one |
| Select | Dropdown single | Select country |
| Text | Single line | Username |
| Textarea | Multi-line | Description |
| Time | Time picker | Schedule time |

---

## Usage Guide

### For Plugin Authors

**1. Create a new Class Library project:**
```bash
dotnet new classlib -n MyCompany.MyPlugin
```

**2. Add reference to FreePlugins.Abstractions:**
```xml
<PackageReference Include="FreePlugins.Abstractions" Version="1.0.0" />
```

**3. Choose and implement the appropriate interface:**

```csharp
// General plugin
public class MyPlugin : ICompiledGeneralPlugin { ... }

// Background process
public class MyPlugin : ICompiledBackgroundProcessPlugin { ... }

// Auth plugin
public class MyPlugin : ICompiledAuthPlugin { ... }

// User update plugin
public class MyPlugin : ICompiledUserUpdatePlugin { ... }
```

**4. Add the [Plugin] attribute:**
```csharp
[Plugin(
    Id = "your-unique-guid",
    Name = "My Plugin",
    Type = PluginTypes.General,  // or Auth, BackgroundProcess, UserUpdate
    Version = "1.0.0",
    Author = "Your Name"
)]
public class MyPlugin : ICompiledGeneralPlugin
{
    public static Type PluginType => typeof(MyPlugin);
    // ...
}
```

**5. Publish to NuGet:**
```bash
dotnet pack
dotnet nuget push bin/Release/MyCompany.MyPlugin.1.0.0.nupkg --source nuget.org
```

### For Application Developers

**1. Add project reference to Integration:**
```xml
<ProjectReference Include="..\FreePlugins.Abstractions.Integration\FreePlugins.Abstractions.Integration.csproj" />
```

**2. Reference plugin packages:**
```xml
<PackageReference Include="FreePlugins.ExamplePlugin" Version="1.0.0" />
<PackageReference Include="FreePlugins.AuthExamplePlugin" Version="1.0.0" />
<!-- etc. -->
```

**3. Register in Program.cs:**
```csharp
using FreePlugins.Abstractions;
using FreePlugins.Integration;

// Register individual plugins
builder.Services.AddPlugin<AllPromptsPlugin>();
builder.Services.AddPlugin<CustomAuthPlugin>();
builder.Services.AddPlugin<UserSyncPlugin>();

// OR auto-discover from assemblies
builder.Services.AddPluginsFromAssembly(typeof(AllPromptsPlugin).Assembly);

var app = builder.Build();

// Load compiled plugins into registry
app.LoadCompiledPlugins();

app.Run();
```

---

## Files Summary

### Core Projects

| Project | Files | Purpose |
|---------|-------|---------|
| `FreePlugins.Abstractions` | 10 | Interfaces, attributes, context |
| `FreePlugins.Abstractions.Integration` | 3 | Bridge to existing system |

### Example Projects

| Project | Main Class | Demonstrates |
|---------|------------|--------------|
| `FreePlugins.SamplePlugin` | SampleBackgroundPlugin | Background process |
| `FreePlugins.ExamplePlugin` | AllPromptsPlugin | All 16 prompt types |
| `FreePlugins.AuthExamplePlugin` | CustomAuthPlugin | Auth Login/Logout |
| `FreePlugins.UserUpdateExamplePlugin` | UserSyncPlugin | User synchronization |
| `FreePlugins.TenantRestrictedPlugin` | TenantSpecificPlugin | Tenant restriction |
| `FreePlugins.DataAccessExamplePlugin` | ContextInfoPlugin | Context/metadata access |

### Existing Files Modified

| File | Change |
|------|--------|
| **None** | ✅ No existing files were modified |

---

## Verification

```bash
# Check git status - only new files
$ git status --short
?? FreePluginsV1/FreePlugins.Abstractions/
?? FreePluginsV1/FreePlugins.Abstractions.Integration/
?? FreePluginsV1/FreePlugins.SamplePlugin/
?? FreePluginsV1/FreePlugins.ExamplePlugin/
?? FreePluginsV1/FreePlugins.AuthExamplePlugin/
?? FreePluginsV1/FreePlugins.UserUpdateExamplePlugin/
?? FreePluginsV1/FreePlugins.TenantRestrictedPlugin/
?? FreePluginsV1/FreePlugins.DataAccessExamplePlugin/

# Build all projects
$ dotnet build FreePlugins.AuthExamplePlugin
Build succeeded.

$ dotnet build FreePlugins.UserUpdateExamplePlugin
Build succeeded.

$ dotnet build FreePlugins.TenantRestrictedPlugin
Build succeeded.

$ dotnet build FreePlugins.DataAccessExamplePlugin
Build succeeded.
```

---

## Comparison: File-Based vs Compiled Plugins

| Aspect | File-Based Plugins | Compiled Plugins |
|--------|-------------------|------------------|
| **Source** | `.cs` files in Plugins folder | NuGet packages |
| **Compilation** | Runtime (Roslyn) | Build time |
| **IntelliSense** | No | Yes |
| **Debugging** | Limited | Full |
| **Distribution** | Source code | Compiled DLL |
| **Versioning** | Manual | NuGet versioning |
| **Dependencies** | `.assemblies` file | NuGet dependencies |
| **Security** | Source visible | Binary only |

---

## Portability

To add compiled plugin support to another FreePlugins suite:

1. Copy `FreePlugins.Abstractions/` folder
2. Copy `FreePlugins.Abstractions.Integration/` folder
3. Update namespace references in Integration project
4. Add project reference to Integration in main web project
5. Add registration code to Program.cs
6. Copy any desired example plugin projects

No changes to the suite's `*.Plugins` or `*.DataAccess` projects required.

---

*Created: 2025-01-XX*  
*Author: GitHub Copilot*  
*Reviewed by: [Pending]*

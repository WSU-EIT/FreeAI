# FreePlugins.SamplePlugin

A sample compiled plugin demonstrating the FreePlugins NuGet-based plugin architecture.

## Overview

This project shows how to create a compiled plugin that can be:
1. Published as a NuGet package
2. Referenced by the main application
3. Registered with dependency injection

## Usage

### Option 1: Explicit Registration

```csharp
// In Program.cs
builder.Services.AddPlugin<SampleBackgroundPlugin>(options => {
    options.Enabled = true;
    options.SortOrder = 100;
});
```

### Option 2: Assembly Scanning

```csharp
// In Program.cs - discovers all plugins in the assembly
builder.Services.AddPluginsFromAssembly(typeof(SampleBackgroundPlugin).Assembly);
```

### Option 3: Reference the NuGet Package

After publishing to NuGet:

```xml
<PackageReference Include="FreePlugins.SamplePlugin" Version="1.0.0" />
```

Then register:

```csharp
builder.Services.AddPluginsFromAssembly(typeof(SampleBackgroundPlugin).Assembly);
```

## Creating Your Own Plugin

1. Create a new Class Library project
2. Reference `FreePlugins.Abstractions`
3. Create a class implementing one of:
   - `ICompiledBackgroundProcessPlugin` for background tasks
   - `ICompiledGeneralPlugin` for general plugins
   - `ICompiledAuthPlugin` for authentication plugins
   - `ICompiledUserUpdatePlugin` for user update plugins
4. Add the `[Plugin]` attribute with your plugin metadata
5. Implement `Properties()` method (for compatibility) and the execution method

## Plugin Attribute

The `[Plugin]` attribute defines your plugin metadata:

```csharp
[Plugin(
    Id = "your-unique-guid-here",
    Name = "My Plugin",
    Type = PluginTypes.BackgroundProcess,
    Version = "1.0.0",
    Author = "Your Name",
    Description = "What your plugin does",
    SortOrder = 0,
    Enabled = true
)]
public class MyPlugin : ICompiledBackgroundProcessPlugin
{
    // ...
}
```

## IPluginContext

The `IPluginContext` provides:
- `Plugin` - The plugin metadata
- `Services` - The service provider for DI
- `GetService<T>()` - Get optional services
- `GetRequiredService<T>()` - Get required services
- `LogInfo()`, `LogWarning()`, `LogError()` - Logging methods

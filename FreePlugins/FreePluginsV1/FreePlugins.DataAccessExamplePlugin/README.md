# FreePlugins.DataAccessExamplePlugin

An example compiled plugin demonstrating how to access plugin metadata and context information.

## Features Demonstrated

- ✅ Accessing plugin metadata (name, version, author)
- ✅ Using `IPluginContext` for services and logging
- ✅ `ContainsSensitiveData` flag for security marking
- ✅ SortOrder for execution ordering
- ✅ Comprehensive plugin information display

## Installation

### As a NuGet Package

```xml
<PackageReference Include="FreePlugins.DataAccessExamplePlugin" Version="1.0.0" />
```

### As a Project Reference

```xml
<ProjectReference Include="..\FreePlugins.DataAccessExamplePlugin\FreePlugins.DataAccessExamplePlugin.csproj" />
```

## Usage

### Register the Plugin

```csharp
// In Program.cs
using FreePlugins.Abstractions;
using FreePlugins.DataAccessExamplePlugin;

builder.Services.AddPlugin<ContextInfoPlugin>();

var app = builder.Build();
app.LoadCompiledPlugins();
```

## Plugin Details

| Property | Value |
|----------|-------|
| **Name** | Context Info Example (Compiled) |
| **Type** | General |
| **Author** | WSU EIT |
| **Version** | 1.0.0 |
| **SortOrder** | 3 |
| **ContainsSensitiveData** | true |

## Context Access

This plugin demonstrates how to access various context information:

### Plugin Metadata
```csharp
context.Plugin.Name      // Plugin name
context.Plugin.Version   // Plugin version
context.Plugin.Author    // Plugin author
context.Plugin.IsCompiled // Whether it's a compiled plugin
```

### Services
```csharp
context.Services         // IServiceProvider
context.GetService<T>()  // Get optional service
context.GetRequiredService<T>() // Get required service (throws if not found)
```

### Logging
```csharp
context.LogInfo("Information message");
context.LogWarning("Warning message");
context.LogError("Error message", exception);
```

## Source

This plugin is a compiled version of the file-based `Example3.cs` plugin, demonstrating how to access context and metadata in NuGet-based plugins.

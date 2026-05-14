# FreePlugins.TenantRestrictedPlugin

An example compiled plugin demonstrating tenant restriction in multi-tenant applications.

## Features Demonstrated

- ✅ `LimitToTenants` property for tenant restriction
- ✅ Simple `ICompiledGeneralPlugin` implementation
- ✅ SortOrder for plugin execution ordering
- ✅ Multi-tenant plugin visibility control

## Installation

### As a NuGet Package

```xml
<PackageReference Include="FreePlugins.TenantRestrictedPlugin" Version="1.0.0" />
```

### As a Project Reference

```xml
<ProjectReference Include="..\FreePlugins.TenantRestrictedPlugin\FreePlugins.TenantRestrictedPlugin.csproj" />
```

## Usage

### Register the Plugin

```csharp
// In Program.cs
using FreePlugins.Abstractions;
using FreePlugins.TenantRestrictedPlugin;

builder.Services.AddPlugin<TenantSpecificPlugin>();

var app = builder.Build();
app.LoadCompiledPlugins();
```

## Plugin Details

| Property | Value |
|----------|-------|
| **Name** | Tenant-Restricted Example (Compiled) |
| **Type** | General |
| **Author** | WSU EIT |
| **Version** | 1.0.0 |
| **SortOrder** | 1 |
| **LimitToTenants** | 00000000-0000-0000-0000-000000000002 |

## Tenant Restriction

This plugin demonstrates how to restrict a plugin to specific tenants:

```csharp
{ "LimitToTenants", new List<Guid> { 
    Guid.Parse("00000000-0000-0000-0000-000000000002") 
}}
```

### How It Works

1. When plugins are loaded, the system checks the `LimitToTenants` property
2. If the list is empty or null, the plugin is available to all tenants
3. If the list contains tenant GUIDs, the plugin is only visible to those tenants
4. Users in other tenants will not see this plugin in listings

### Use Cases

- Tenant-specific features or customizations
- Beta features for select tenants
- Licensed plugins for paying customers
- Region-specific functionality
- Enterprise-tier features

## Source

This plugin is a compiled version of the file-based `Example2.cs` plugin, demonstrating how to convert tenant-restricted plugins to the NuGet-based architecture.

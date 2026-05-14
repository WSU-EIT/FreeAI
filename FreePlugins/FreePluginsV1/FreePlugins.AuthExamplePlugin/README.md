# FreePlugins.AuthExamplePlugin

An example compiled auth plugin demonstrating custom authentication with username/password prompts.

## Features Demonstrated

- ✅ `ICompiledAuthPlugin` interface implementation
- ✅ `LoginAsync` method for user authentication
- ✅ `LogoutAsync` method for user logout
- ✅ Username and Password prompts
- ✅ Custom login button styling
- ✅ Tenant restriction (`LimitToTenants`)
- ✅ Error handling and validation

## Installation

### As a NuGet Package

```xml
<PackageReference Include="FreePlugins.AuthExamplePlugin" Version="1.0.0" />
```

### As a Project Reference

```xml
<ProjectReference Include="..\FreePlugins.AuthExamplePlugin\FreePlugins.AuthExamplePlugin.csproj" />
```

## Usage

### Register the Plugin

```csharp
// In Program.cs
using FreePlugins.Abstractions;
using FreePlugins.AuthExamplePlugin;

builder.Services.AddPlugin<CustomAuthPlugin>();

var app = builder.Build();
app.LoadCompiledPlugins();
```

## Plugin Details

| Property | Value |
|----------|-------|
| **Name** | Custom Auth Example (Compiled) |
| **Type** | Auth |
| **Author** | WSU EIT |
| **Version** | 1.0.0 |

## Prompts

| Prompt | Type | Description |
|--------|------|-------------|
| Username | Text | User's login username |
| Password | Password | User's login password (masked) |

## Authentication Flow

1. User clicks the custom login button
2. Plugin displays Username and Password prompts
3. User enters credentials and submits
4. `LoginAsync` validates credentials
5. On success, returns authenticated user object
6. On failure, returns error messages

## Button Customization

The login button can be customized via properties:
- `ButtonText`: "Custom Auth Login"
- `ButtonClass`: "btn btn-primary"
- `ButtonIcon`: Font Awesome sign-in icon

## Source

This plugin is a compiled version of the file-based `LoginWithPrompts.cs` plugin, demonstrating how to convert auth plugins to the NuGet-based architecture.

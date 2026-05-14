# FreePlugins.UserUpdateExamplePlugin

An example compiled user update plugin demonstrating how to synchronize user information from external systems.

## Features Demonstrated

- ✅ `ICompiledUserUpdatePlugin` interface implementation
- ✅ `UpdateUserAsync` method for user synchronization
- ✅ User object modification and return
- ✅ Sensitive data flag (`ContainsSensitiveData`)
- ✅ Error handling for missing user data

## Installation

### As a NuGet Package

```xml
<PackageReference Include="FreePlugins.UserUpdateExamplePlugin" Version="1.0.0" />
```

### As a Project Reference

```xml
<ProjectReference Include="..\FreePlugins.UserUpdateExamplePlugin\FreePlugins.UserUpdateExamplePlugin.csproj" />
```

## Usage

### Register the Plugin

```csharp
// In Program.cs
using FreePlugins.Abstractions;
using FreePlugins.UserUpdateExamplePlugin;

builder.Services.AddPlugin<UserSyncPlugin>();

var app = builder.Build();
app.LoadCompiledPlugins();
```

## Plugin Details

| Property | Value |
|----------|-------|
| **Name** | User Sync Example (Compiled) |
| **Type** | UserUpdate |
| **Author** | WSU EIT |
| **Version** | 1.0.0 |
| **ContainsSensitiveData** | true |

## User Update Flow

1. System calls plugin when user data needs updating
2. Plugin receives user object through context
3. Plugin looks up user in external system (e.g., LDAP, HR database, API)
4. Plugin modifies user properties as needed
5. Plugin returns updated user object

## Example Use Cases

- Synchronize user attributes from Active Directory/LDAP
- Pull user profile data from HR systems
- Fetch user permissions from external authorization service
- Merge user data from multiple sources
- Transform user data formats

## Demo Behavior

For demonstration, this plugin toggles the user's email between uppercase and lowercase. In a real implementation, you would:

1. Connect to your external user system
2. Look up the user by ID or email
3. Update relevant properties (name, department, permissions, etc.)
4. Return the modified user object

## Source

This plugin is a compiled version of the file-based `UserUpdate.cs` plugin, demonstrating how to convert user update plugins to the NuGet-based architecture.

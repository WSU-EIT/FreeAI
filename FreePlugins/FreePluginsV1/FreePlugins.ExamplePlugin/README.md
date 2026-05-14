# FreePlugins.ExamplePlugin

A comprehensive example compiled plugin demonstrating all features of the FreePlugins NuGet-based plugin architecture.

## Features Demonstrated

- ✅ All 16 prompt types (Button, Checkbox, CheckboxList, Date, DateTime, File, Files, HTML, Multiselect, Number, Password, Radio, Select, Text, Textarea, Time)
- ✅ Button callback handlers
- ✅ Dynamic prompt option loading via function
- ✅ Prompt value change handlers
- ✅ Conditional field visibility (show/hide based on checkbox)
- ✅ File upload handling with base64 decoding
- ✅ Required field validation

## Installation

### As a NuGet Package

```xml
<PackageReference Include="FreePlugins.ExamplePlugin" Version="1.0.0" />
```

### As a Project Reference

```xml
<ProjectReference Include="..\FreePlugins.ExamplePlugin\FreePlugins.ExamplePlugin.csproj" />
```

## Usage

### Register the Plugin

```csharp
// In Program.cs
using FreePlugins.Abstractions;
using FreePlugins.ExamplePlugin;

builder.Services.AddPlugin<AllPromptsPlugin>();

var app = builder.Build();
app.LoadCompiledPlugins();
```

### Or Auto-Discover

```csharp
builder.Services.AddPluginsFromAssembly(typeof(AllPromptsPlugin).Assembly);
```

## Plugin Details

| Property | Value |
|----------|-------|
| **Name** | All Prompts Example |
| **Type** | General |
| **Author** | WSU EIT (converted from Brad Wickett's Example1) |
| **Version** | 1.0.0 |

## Prompt Types Included

| Prompt | Type | Required | Initially Hidden |
|--------|------|----------|------------------|
| Button1 | Button | No | No |
| Checkbox | Checkbox | Yes | No |
| CheckboxList | CheckboxList | Yes | Yes |
| Date | Date | No | Yes |
| DateTime | DateTime | No | Yes |
| File | File | No | Yes |
| Files | Files | No | Yes |
| HTML | HTML | No | Yes |
| Multiselect | Multiselect | No | Yes |
| Number | Number | No | Yes |
| Password | Password | No | Yes |
| Radio | Radio | No | Yes |
| Select | Select | No | Yes |
| SelectFromFunction | Select | No | Yes |
| Text | Text | No | Yes |
| Textarea | Textarea | No | Yes |
| Time | Time | No | Yes |

## Behavior

1. **Initial State**: Only the Button and Checkbox are visible
2. **Checkbox Interaction**: When the checkbox is checked, all hidden fields become visible
3. **Button Click**: Triggers the `Button1Async` callback
4. **SelectFromFunction**: Options are loaded dynamically via `GetPromptValuesAsync`
5. **Execute**: Displays all entered prompt values

## Converting to Your Own Plugin

1. Copy this project
2. Rename the project and class
3. Generate a new GUID for the plugin ID
4. Modify the prompts and execution logic
5. Publish to NuGet

## Source

This plugin is a compiled version of the file-based `Example1.cs` plugin, demonstrating how to convert existing plugins to the NuGet-based architecture.

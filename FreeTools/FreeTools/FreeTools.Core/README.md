# FreeTools.Core — Shared Utilities Library

> **Purpose:** Common utilities shared across all FreeTools CLI applications.  
> **Version:** 1.0  
> **Last Updated:** 2025-07-26

---

## Overview

**FreeTools.Core** provides reusable utilities for CLI tools:

- **Command-line argument parsing** — Flags, options, positional args, environment variables
- **Console output** — Thread-safe writing, banner/divider formatting
- **Route parsing** — CSV parsing, route parameter detection
- **Path utilities** — Route-to-path conversion, byte formatting

---

## Files

| File | Purpose |
|------|---------|
| `CliArgs.cs` | CLI argument and environment variable helpers |
| `ConsoleOutput.cs` | Thread-safe console output with formatting |
| `RouteParser.cs` | Route CSV parsing and parameter detection |
| `PathSanitizer.cs` | Path conversion and byte formatting |

---

## Usage

### CliArgs

```csharp
using FreeTools.Core;

var args = Environment.GetCommandLineArgs().ToList();

// Check for flags
if (CliArgs.HasFlag(args, "--verbose", "-v"))
    Console.WriteLine("Verbose mode enabled");

// Get options with prefix
var outputDir = CliArgs.GetOption(args, "--output=") ?? "default";

// Get positional arguments
var inputFile = CliArgs.GetPositional(args, 0, "input.txt");

// Environment variable helpers
var baseUrl = CliArgs.GetEnvOrArg("BASE_URL", args, 0, "https://localhost:5001");
var maxThreads = CliArgs.GetEnvOrArgInt("MAX_THREADS", args, 1, 100);
var verbose = CliArgs.GetEnvBool("VERBOSE");
```

### ConsoleOutput

```csharp
using FreeTools.Core;

// Thread-safe writing (for parallel operations)
ConsoleOutput.WriteLine("Processing...");
ConsoleOutput.WriteLine("Error occurred!", isError: true);

// Banner and dividers
ConsoleOutput.PrintBanner("MyTool", "1.0");
// ============================================================
//  MyTool v1.0
// ============================================================

ConsoleOutput.PrintDivider("Results");
// ============================================================
//  Results
// ============================================================

// Key-value configuration
ConsoleOutput.PrintConfig("Output Dir", "/path/to/output");
// Output Dir:        /path/to/output
```

### RouteParser

```csharp
using FreeTools.Core;

// Check for route parameters
RouteParser.HasParameter("/Users/{id}");  // true
RouteParser.HasParameter("/Users");       // false

// Parse routes from CSV
var (routes, skipped) = await RouteParser.ParseRoutesFromCsvFileAsync(
    "pages.csv",
    routeColumnIndex: 1,
    skipParameterizedRoutes: true
);

// Build full URL
var url = RouteParser.BuildUrl("https://localhost:5001", "/Account/Login");
// https://localhost:5001/Account/Login
```

### PathSanitizer

```csharp
using FreeTools.Core;

// Convert route to safe file path
var path = PathSanitizer.RouteToDirectoryPath("/Account/Login");
// "Account\Login" (Windows) or "Account/Login" (Unix)

// Get full output file path
var filePath = PathSanitizer.GetOutputFilePath("snapshots", "/Users", "default.html");
// "snapshots\Users\default.html"

// Ensure directory exists
PathSanitizer.EnsureDirectoryExists(filePath);

// Format bytes
PathSanitizer.FormatBytes(1024);      // "1.0 KB"
PathSanitizer.FormatBytes(1048576);   // "1.0 MB"
```

---

## Adding to Your Tool

1. Add project reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\FreeTools.Core\FreeTools.Core.csproj" />
</ItemGroup>
```

2. Add using directive:

```csharp
using FreeTools.Core;
```

---

## Design Principles

1. **Single Responsibility** — Each file handles one concern
2. **No Dependencies** — Core has no external NuGet packages
3. **Static Methods** — All utilities are static for simplicity
4. **Thread-Safe** — Console output is synchronized for parallel use
5. **Clear Naming** — Two-word descriptive file names

---

## 📬 About

**FreeTools** is developed and maintained by **[Enrollment Information Technology (EIT)](https://em.wsu.edu/eit/meet-our-staff/)** at **Washington State University**.

📧 Questions or feedback? Visit our [team page](https://em.wsu.edu/eit/meet-our-staff/) or open an issue on [GitHub](https://github.com/WSU-EIT/FreeTools/issues)

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The shared utility library every FreeTools CLI references: command-line argument + environment-variable parsing, thread-safe console output (banners, dividers), route-CSV parsing, and route-to-file-path helpers. Static methods, no external dependencies.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| CLI arg / env parsing | Read flags, options, env vars | [CliArgs.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeTools/FreeTools.Core/CliArgs.cs) |
| Thread-safe console | Clean output during parallel runs | [ConsoleOutput.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeTools/FreeTools.Core/ConsoleOutput.cs) |
| Route parsing + path utils | Read `pages.csv`, build safe paths | [RouteParser.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeTools/FreeTools.Core/RouteParser.cs) · [PathSanitizer.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeTools/FreeTools.Core/PathSanitizer.cs) |

**Why does this exist?** So each tool stays small and consistent — they don't each re-implement arg parsing, console formatting, or route handling.

**What does it accomplish that other tools don't?**
- **Zero external dependencies** and **thread-safe console output**, so tools that run captures in parallel don't produce garbled logs.

**Terminology & "can I see it?"**
- **Static utility** — a stateless helper you call without creating an object.

**The hard part, drawn** — one shared core, every tool:

```
  FreeTools.Core (CliArgs · ConsoleOutput · RouteParser · PathSanitizer)
        ▲
  EndpointMapper · EndpointPoker · BrowserSnapshot · WorkspaceInventory · WorkspaceReporter ── all reference it
```

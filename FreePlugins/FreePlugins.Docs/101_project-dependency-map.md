# Project Dependency Map

> **Purpose:** Complete dependency mapping for all 54 projects across 7 project suites  
> **Version:** 1.1  
> **Last Updated:** 2025-01-XX  
> **Total Projects:** 54

---

## Table of Contents

1. [Suite Overview](#suite-overview)
2. [Dependency Graph (Mermaid)](#dependency-graph)
3. [Complete Project Table](#complete-project-table)
4. [Suite-by-Suite Breakdown](#suite-by-suite-breakdown)
5. [Dependency Depth Analysis](#dependency-depth-analysis)
6. [Package Version Comparison](#package-version-comparison)
7. [Plugin System Architecture](#plugin-system-architecture)
8. [FreePluginsV1 Goals](#freepluginsv1-goals)

---

## Suite Overview

| # | Suite | Location | Projects | Purpose | Status |
|---|-------|----------|----------|---------|--------|
| 1 | **FreeCRM-main** | `FreeCRM-main/` | 6 | Base framework (origin) | ✅ Active |
| 2 | **FreePlugins_base** | `FreeCRM-FreePlugins_base/` | 6 | Renamed template | 📋 Template |
| 3 | **FreePluginsV1** | `FreePluginsV1/` | 7 | Development clone | 🔨 WIP |
| 4 | **FreeCICD** | `FreeCICD-main/` | 6 | CI/CD implementation | ✅ Active |
| 5 | **FreeGLBA** | `FreeGLBA-main/` | 10 | GLBA compliance (newest) | ✅ Active |
| 6 | **FreeManager** | `FreeManager-main/` | 6 | Management tool (oldest) | ✅ Active |
| 7 | **FreeTools** | `FreeTools/` | 10 | Orchestration/testing suite | ✅ Active |

**Total: 54 projects**

---

## Dependency Graph

### Master Architecture Flow

```mermaid
flowchart TB
    subgraph Origin["🏛️ ORIGIN"]
        CRM[FreeCRM-main]
    end
    
    subgraph Templates["📋 TEMPLATES"]
        FPB[FreePlugins_base]
        FPV1[FreePluginsV1]
    end
    
    subgraph Implementations["🚀 IMPLEMENTATIONS"]
        CICD[FreeCICD]
        GLBA[FreeGLBA]
        MGR[FreeManager]
    end
    
    subgraph Tooling["🔧 TOOLING"]
        FT[FreeTools Suite]
    end
    
    CRM -->|"rename/fork"| FPB
    FPB -->|"clone"| FPV1
    CRM -->|"implement"| CICD
    CRM -->|"implement"| GLBA
    CRM -->|"implement"| MGR
    FT -->|"analyzes"| CRM
    FT -->|"analyzes"| CICD
    FT -->|"analyzes"| GLBA
    FT -->|"analyzes"| MGR
```

### Standard 6-Layer Architecture (Per Suite)

```mermaid
flowchart TB
    subgraph Layer1["🖥️ HOST LAYER"]
        Server["Server<br/>(ASP.NET Blazor)"]
    end
    
    subgraph Layer2["🎨 UI LAYER"]
        Client["Client<br/>(Blazor WASM)"]
    end
    
    subgraph Layer3["📊 BUSINESS LAYER"]
        DA["DataAccess<br/>(Services)"]
    end
    
    subgraph Layer4["📦 DATA LAYER"]
        DO["DataObjects<br/>(DTOs)"]
        EF["EFModels<br/>(Entity Framework)"]
    end
    
    subgraph Layer5["🔌 EXTENSION LAYER"]
        PL["Plugins<br/>(Roslyn)"]
    end
    
    Server --> Client
    Server --> DA
    Server --> PL
    Client --> DO
    DA --> DO
    DA --> EF
    DA --> PL
    DO --> PL
```

### FreeTools Suite Architecture

```mermaid
flowchart TB
    subgraph AppHost["🎯 ORCHESTRATOR"]
        AH["FreeTools.AppHost<br/>(Aspire 9.2.0)"]
    end
    
    subgraph Tools["🔧 ANALYSIS TOOLS"]
        EM["EndpointMapper"]
        EP["EndpointPoker"]
        BS["BrowserSnapshot"]
        WI["WorkspaceInventory"]
        WR["WorkspaceReporter"]
        FC["ForkCRM"]
    end
    
    subgraph Shared["📚 SHARED"]
        Core["FreeTools.Core"]
    end
    
    subgraph Output["📄 OUTPUT"]
        Docs["Docs"]
    end
    
    subgraph Targets["🎯 TARGETS"]
        BA1["BlazorApp1"]
    end
    
    AH --> EM
    AH --> EP
    AH --> BS
    AH --> WI
    AH --> WR
    AH --> BA1
    
    EM --> Core
    EP --> Core
    BS --> Core
    WI --> Core
    WR --> Core
```

---

## Complete Project Table

### All 54 Projects with Dependencies

| # | Suite | Project | SDK | Direct Dependencies | Transitive Dependencies |
|---|-------|---------|-----|---------------------|------------------------|
| | | | | | |
| | **FreeCRM-main** | | | | |
| 1 | FreeCRM | CRM | Web | Client, DataAccess, Plugins | DataObjects, EFModels |
| 2 | FreeCRM | CRM.Client | BlazorWebAssembly | DataObjects | Plugins |
| 3 | FreeCRM | CRM.DataAccess | Library | DataObjects, EFModels, Plugins | — |
| 4 | FreeCRM | CRM.DataObjects | Library | Plugins | — |
| 5 | FreeCRM | CRM.EFModels | Library | — | — |
| 6 | FreeCRM | CRM.Plugins | Library | — | — |
| | | | | | |
| | **FreePlugins_base** | | | | |
| 7 | FreePlugins_base | FreePlugins | Web | Client, DataAccess, Plugins | DataObjects, EFModels |
| 8 | FreePlugins_base | FreePlugins.Client | BlazorWebAssembly | DataObjects | Plugins |
| 9 | FreePlugins_base | FreePlugins.DataAccess | Library | DataObjects, EFModels, Plugins | — |
| 10 | FreePlugins_base | FreePlugins.DataObjects | Library | Plugins | — |
| 11 | FreePlugins_base | FreePlugins.EFModels | Library | — | — |
| 12 | FreePlugins_base | FreePlugins.Plugins | Library | — | — |
| | | | | | |
| | **FreePluginsV1** | | | | |
| 13 | FreePluginsV1 | FreePlugins | Web | Client, DataAccess, Plugins | DataObjects, EFModels |
| 14 | FreePluginsV1 | FreePlugins.Client | BlazorWebAssembly | DataObjects | Plugins |
| 15 | FreePluginsV1 | FreePlugins.DataAccess | Library | DataObjects, EFModels, Plugins | — |
| 16 | FreePluginsV1 | FreePlugins.DataObjects | Library | Plugins | — |
| 17 | FreePluginsV1 | FreePlugins.EFModels | Library | — | — |
| 18 | FreePluginsV1 | FreePlugins.Plugins | Library | — | — |
| 19 | FreePluginsV1 | Docs | Library | — | — |
| | | | | | |
| | **FreeCICD** | | | | |
| 20 | FreeCICD | FreeCICD | Web | Client, DataAccess, Plugins | DataObjects, EFModels |
| 21 | FreeCICD | FreeCICD.Client | BlazorWebAssembly | DataObjects | Plugins |
| 22 | FreeCICD | FreeCICD.DataAccess | Library | DataObjects, EFModels, Plugins | — |
| 23 | FreeCICD | FreeCICD.DataObjects | Library | Plugins | — |
| 24 | FreeCICD | FreeCICD.EFModels | Library | — | — |
| 25 | FreeCICD | FreeCICD.Plugins | Library | — | — |
| | | | | | |
| | **FreeGLBA** | | | | |
| 26 | FreeGLBA | FreeGLBA | Web | Client, DataAccess, Plugins | DataObjects, EFModels |
| 27 | FreeGLBA | FreeGLBA.Client | BlazorWebAssembly | DataObjects | Plugins |
| 28 | FreeGLBA | FreeGLBA.DataAccess | Library | DataObjects, EFModels, Plugins | — |
| 29 | FreeGLBA | FreeGLBA.DataObjects | Library | Plugins | — |
| 30 | FreeGLBA | FreeGLBA.EFModels | Library | — | — |
| 31 | FreeGLBA | FreeGLBA.Plugins | Library | — | — |
| 32 | FreeGLBA | FreeGLBA.NugetClient | Library | — | — |
| 33 | FreeGLBA | FreeGLBA.NugetClientPublisher | Exe | — | — |
| 34 | FreeGLBA | FreeGLBA.TestClient | Exe | NugetClient | — |
| 35 | FreeGLBA | FreeGLBA.TestClientWithNugetPackage | Exe | *(NuGet: FreeGLBA.Client)* | — |
| | | | | | |
| | **FreeManager** | | | | |
| 36 | FreeManager | FreeManager | Web | Client, DataAccess, Plugins | DataObjects, EFModels |
| 37 | FreeManager | FreeManager.Client | BlazorWebAssembly | DataObjects | Plugins |
| 38 | FreeManager | FreeManager.DataAccess | Library | DataObjects, EFModels, Plugins | — |
| 39 | FreeManager | FreeManager.DataObjects | Library | Plugins | — |
| 40 | FreeManager | FreeManager.EFModels | Library | — | — |
| 41 | FreeManager | FreeManager.Plugins | Library | — | — |
| 42 | FreeManager | FreeManager.Cli | Exe | DataObjects, Client | Plugins |
| | | | | | |
| | **FreeTools** | | | | |
| 43 | FreeTools | FreeTools.AppHost | Aspire | EndpointMapper, EndpointPoker, BrowserSnapshot, WorkspaceInventory, WorkspaceReporter, BlazorApp1 | Core |
| 44 | FreeTools | FreeTools.Core | Library | — | — |
| 45 | FreeTools | FreeTools.EndpointMapper | Exe | Core | — |
| 46 | FreeTools | FreeTools.EndpointPoker | Exe | Core | — |
| 47 | FreeTools | FreeTools.BrowserSnapshot | Exe | Core | — |
| 48 | FreeTools | FreeTools.WorkspaceInventory | Exe | Core | — |
| 49 | FreeTools | FreeTools.WorkspaceReporter | Exe | Core | — |
| 50 | FreeTools | FreeTools.ForkCRM | Exe | — | — |
| 51 | FreeTools | Docs | Library | — | — |
| | | | | | |
| | **Standalone** | | | | |
| 52 | — | BlazorApp1 | Web | — | — |

---

## Suite-by-Suite Breakdown

### 1️⃣ FreeCRM-main (Base Framework)

```mermaid
flowchart LR
    subgraph FreeCRM["FreeCRM-main (6 projects)"]
        CRM["CRM<br/>🖥️ Server"]
        CRM_C["CRM.Client<br/>🌐 WASM"]
        CRM_DA["CRM.DataAccess<br/>📊 Services"]
        CRM_DO["CRM.DataObjects<br/>📦 DTOs"]
        CRM_EF["CRM.EFModels<br/>🗄️ EF"]
        CRM_PL["CRM.Plugins<br/>🔌 Roslyn"]
    end
    
    CRM --> CRM_C
    CRM --> CRM_DA
    CRM --> CRM_PL
    CRM_C --> CRM_DO
    CRM_DA --> CRM_DO
    CRM_DA --> CRM_EF
    CRM_DA --> CRM_PL
    CRM_DO --> CRM_PL
```

| Project | References | Referenced By |
|---------|------------|---------------|
| CRM | Client, DataAccess, Plugins | — |
| CRM.Client | DataObjects | CRM |
| CRM.DataAccess | DataObjects, EFModels, Plugins | CRM |
| CRM.DataObjects | Plugins | Client, DataAccess |
| CRM.EFModels | — | DataAccess |
| CRM.Plugins | — | CRM, DataAccess, DataObjects |

---

### 2️⃣ FreeCRM-FreePlugins_base (Template)

```mermaid
flowchart LR
    subgraph FPB["FreePlugins_base (6 projects)"]
        FP["FreePlugins<br/>🖥️ Server"]
        FP_C["FreePlugins.Client<br/>🌐 WASM"]
        FP_DA["FreePlugins.DataAccess<br/>📊 Services"]
        FP_DO["FreePlugins.DataObjects<br/>📦 DTOs"]
        FP_EF["FreePlugins.EFModels<br/>🗄️ EF"]
        FP_PL["FreePlugins.Plugins<br/>🔌 Roslyn"]
    end
    
    FP --> FP_C
    FP --> FP_DA
    FP --> FP_PL
    FP_C --> FP_DO
    FP_DA --> FP_DO
    FP_DA --> FP_EF
    FP_DA --> FP_PL
    FP_DO --> FP_PL
```

| Project | References | Referenced By |
|---------|------------|---------------|
| FreePlugins | Client, DataAccess, Plugins | — |
| FreePlugins.Client | DataObjects | FreePlugins |
| FreePlugins.DataAccess | DataObjects, EFModels, Plugins | FreePlugins |
| FreePlugins.DataObjects | Plugins | Client, DataAccess |
| FreePlugins.EFModels | — | DataAccess |
| FreePlugins.Plugins | — | FreePlugins, DataAccess, DataObjects |

---

### 3️⃣ FreePluginsV1 (Development Clone)

```mermaid
flowchart LR
    subgraph FPV1["FreePluginsV1 (7 projects)"]
        FP1["FreePlugins<br/>🖥️ Server"]
        FP1_C["FreePlugins.Client<br/>🌐 WASM"]
        FP1_DA["FreePlugins.DataAccess<br/>📊 Services"]
        FP1_DO["FreePlugins.DataObjects<br/>📦 DTOs"]
        FP1_EF["FreePlugins.EFModels<br/>🗄️ EF"]
        FP1_PL["FreePlugins.Plugins<br/>🔌 Roslyn"]
        FP1_DOC["Docs<br/>📄 Output"]
    end
    
    FP1 --> FP1_C
    FP1 --> FP1_DA
    FP1 --> FP1_PL
    FP1_C --> FP1_DO
    FP1_DA --> FP1_DO
    FP1_DA --> FP1_EF
    FP1_DA --> FP1_PL
    FP1_DO --> FP1_PL
```

| Project | References | Referenced By |
|---------|------------|---------------|
| FreePlugins | Client, DataAccess, Plugins | — |
| FreePlugins.Client | DataObjects | FreePlugins |
| FreePlugins.DataAccess | DataObjects, EFModels, Plugins | FreePlugins |
| FreePlugins.DataObjects | Plugins | Client, DataAccess |
| FreePlugins.EFModels | — | DataAccess |
| FreePlugins.Plugins | — | FreePlugins, DataAccess, DataObjects |
| Docs | — | — |

---

### 4️⃣ FreeCICD (CI/CD Implementation)

```mermaid
flowchart LR
    subgraph CICD["FreeCICD-main (6 projects)"]
        FC["FreeCICD<br/>🖥️ Server"]
        FC_C["FreeCICD.Client<br/>🌐 WASM"]
        FC_DA["FreeCICD.DataAccess<br/>📊 Services"]
        FC_DO["FreeCICD.DataObjects<br/>📦 DTOs"]
        FC_EF["FreeCICD.EFModels<br/>🗄️ EF"]
        FC_PL["FreeCICD.Plugins<br/>🔌 Roslyn"]
    end
    
    FC --> FC_C
    FC --> FC_DA
    FC --> FC_PL
    FC_C --> FC_DO
    FC_DA --> FC_DO
    FC_DA --> FC_EF
    FC_DA --> FC_PL
    FC_DO --> FC_PL
```

| Project | References | Referenced By |
|---------|------------|---------------|
| FreeCICD | Client, DataAccess, Plugins | — |
| FreeCICD.Client | DataObjects | FreeCICD |
| FreeCICD.DataAccess | DataObjects, EFModels, Plugins | FreeCICD |
| FreeCICD.DataObjects | Plugins | Client, DataAccess |
| FreeCICD.EFModels | — | DataAccess |
| FreeCICD.Plugins | — | FreeCICD, DataAccess, DataObjects |

**Unique Packages:** `Microsoft.TeamFoundationServer.Client`, `NuGet.Protocol`, `YamlDotNet`

---

### 5️⃣ FreeGLBA (GLBA Compliance - Newest)

```mermaid
flowchart LR
    subgraph GLBA["FreeGLBA-main (10 projects)"]
        FG["FreeGLBA<br/>🖥️ Server"]
        FG_C["FreeGLBA.Client<br/>🌐 WASM"]
        FG_DA["FreeGLBA.DataAccess<br/>📊 Services"]
        FG_DO["FreeGLBA.DataObjects<br/>📦 DTOs"]
        FG_EF["FreeGLBA.EFModels<br/>🗄️ EF"]
        FG_PL["FreeGLBA.Plugins<br/>🔌 Roslyn"]
        FG_NC["FreeGLBA.NugetClient<br/>📦 NuGet Pkg"]
        FG_NCP["FreeGLBA.NugetClientPublisher<br/>🚀 Publisher"]
        FG_TC["FreeGLBA.TestClient<br/>🧪 Test"]
        FG_TCN["FreeGLBA.TestClientWithNugetPackage<br/>🧪 NuGet Test"]
    end
    
    FG --> FG_C
    FG --> FG_DA
    FG --> FG_PL
    FG_C --> FG_DO
    FG_DA --> FG_DO
    FG_DA --> FG_EF
    FG_DA --> FG_PL
    FG_DO --> FG_PL
    FG_TC --> FG_NC
    FG_TCN -.->|"NuGet Package"| FG_NC
```

| Project | References | Referenced By |
|---------|------------|---------------|
| FreeGLBA | Client, DataAccess, Plugins | — |
| FreeGLBA.Client | DataObjects | FreeGLBA |
| FreeGLBA.DataAccess | DataObjects, EFModels, Plugins | FreeGLBA |
| FreeGLBA.DataObjects | Plugins | Client, DataAccess |
| FreeGLBA.EFModels | — | DataAccess |
| FreeGLBA.Plugins | — | FreeGLBA, DataAccess, DataObjects |
| FreeGLBA.NugetClient | — | TestClient |
| FreeGLBA.NugetClientPublisher | — | — |
| FreeGLBA.TestClient | NugetClient | — |
| FreeGLBA.TestClientWithNugetPackage | *(NuGet ref)* | — |

**Unique Projects:** NuGet client publishing infrastructure

---

### 6️⃣ FreeManager (Oldest Implementation)

```mermaid
flowchart LR
    subgraph MGR["FreeManager-main (6 projects)"]
        FM["FreeManager<br/>🖥️ Server"]
        FM_C["FreeManager.Client<br/>🌐 WASM"]
        FM_DA["FreeManager.DataAccess<br/>📊 Services"]
        FM_DO["FreeManager.DataObjects<br/>📦 DTOs"]
        FM_EF["FreeManager.EFModels<br/>🗄️ EF"]
        FM_PL["FreeManager.Plugins<br/>🔌 Roslyn"]
        FM_CLI["FreeManager.Cli<br/>⌨️ CLI"]
    end
    
    FM --> FM_C
    FM --> FM_DA
    FM --> FM_PL
    FM_C --> FM_DO
    FM_DA --> FM_DO
    FM_DA --> FM_EF
    FM_DA --> FM_PL
    FM_DO --> FM_PL
    FM_CLI --> FM_DO
    FM_CLI --> FM_C
```

| Project | References | Referenced By |
|---------|------------|---------------|
| FreeManager | Client, DataAccess, Plugins | — |
| FreeManager.Client | DataObjects | FreeManager, Cli |
| FreeManager.DataAccess | DataObjects, EFModels, Plugins | FreeManager |
| FreeManager.DataObjects | Plugins | Client, DataAccess, Cli |
| FreeManager.EFModels | — | DataAccess |
| FreeManager.Plugins | — | FreeManager, DataAccess, DataObjects |
| FreeManager.Cli | DataObjects, Client | — |

**Unique Projects:** CLI tool with `System.CommandLine` + `Spectre.Console`

---

### 7️⃣ FreeTools Suite (Orchestration)

```mermaid
flowchart TB
    subgraph FT["FreeTools (10 projects)"]
        AH["FreeTools.AppHost<br/>🎯 Aspire Host"]
        
        subgraph Tools["Analysis Tools"]
            EM["EndpointMapper"]
            EP["EndpointPoker"]
            BS["BrowserSnapshot"]
            WI["WorkspaceInventory"]
            WR["WorkspaceReporter"]
        end
        
        FC["ForkCRM<br/>🔀 Fork Tool"]
        Core["FreeTools.Core<br/>📚 Shared"]
        Docs["Docs<br/>📄 Output"]
    end
    
    BA1["BlazorApp1<br/>🎯 Target"]
    
    AH --> EM
    AH --> EP
    AH --> BS
    AH --> WI
    AH --> WR
    AH --> BA1
    
    EM --> Core
    EP --> Core
    BS --> Core
    WI --> Core
    WR --> Core
```

| Project | References | Referenced By |
|---------|------------|---------------|
| FreeTools.AppHost | EndpointMapper, EndpointPoker, BrowserSnapshot, WorkspaceInventory, WorkspaceReporter, BlazorApp1 | — |
| FreeTools.Core | — | EndpointMapper, EndpointPoker, BrowserSnapshot, WorkspaceInventory, WorkspaceReporter |
| FreeTools.EndpointMapper | Core | AppHost |
| FreeTools.EndpointPoker | Core | AppHost |
| FreeTools.BrowserSnapshot | Core | AppHost |
| FreeTools.WorkspaceInventory | Core | AppHost |
| FreeTools.WorkspaceReporter | Core | AppHost |
| FreeTools.ForkCRM | — | — |
| Docs | — | — |
| BlazorApp1 | — | AppHost |

---

## Dependency Depth Analysis

### Projects by Dependency Depth

| Depth | Description | Projects |
|-------|-------------|----------|
| **0** | No dependencies (leaf nodes) | `*.Plugins`, `*.EFModels`, `FreeTools.Core`, `Docs`, `BlazorApp1`, `ForkCRM`, `NugetClientPublisher` |
| **1** | Single dependency | `*.DataObjects` → Plugins |
| **2** | Two dependencies | `*.Client` → DataObjects → Plugins |
| **3** | Three dependencies | `*.DataAccess` → DataObjects, EFModels, Plugins |
| **4** | Host layer | `*.Server` → Client, DataAccess, Plugins |
| **∞** | Orchestrator | `FreeTools.AppHost` → 6 projects |

### Reference Count Summary

| Most Referenced | Count | Referenced By |
|-----------------|-------|---------------|
| `*.Plugins` | 3 | Server, DataAccess, DataObjects |
| `*.DataObjects` | 2 | Client, DataAccess |
| `FreeTools.Core` | 5 | All analysis tools |

---

## Package Version Comparison

### Key Package Versions Across Suites

| Package | FreeCRM | FreePlugins_base | FreePluginsV1 | FreeCICD | FreeGLBA | FreeManager |
|---------|---------|------------------|---------------|----------|----------|-------------|
| `Microsoft.AspNetCore.*.WebAssembly` | 10.0.1 | 10.0.1 | 10.0.1 | **10.0.0** | 10.0.1 | 10.0.1 |
| `Microsoft.EntityFrameworkCore` | 10.0.1 | 10.0.1 | 10.0.1 | **10.0.0** | 10.0.1 | 10.0.1 |
| `Radzen.Blazor` | **8.5.0** | **8.5.0** | **8.5.0** | **8.3.5** | **8.4.0** | **8.4.0** |
| `Microsoft.Graph` | 5.100.0 | 5.100.0 | 5.100.0 | **5.97.0** | **5.98.0** | **5.98.0** |
| `QuestPDF` | 2025.12.1 | 2025.12.1 | 2025.12.1 | **2025.7.4** | **2025.12.0** | **2025.12.0** |

### ⚠️ Version Drift Warnings

| Suite | Issue | Recommendation |
|-------|-------|----------------|
| FreeCICD | Uses 10.0.0 packages (others use 10.0.1) | Update to 10.0.1 |
| FreeCICD | Radzen.Blazor 8.3.5 (others use 8.4.0+) | Update to 8.5.0 |
| FreeGLBA/FreeManager | Radzen.Blazor 8.4.0 (base uses 8.5.0) | Update to 8.5.0 |

---

## Summary Statistics

```
┌─────────────────────────────────────────────────────────────┐
│                    PROJECT STATISTICS                       │
├─────────────────────────────────────────────────────────────┤
│  Total Projects:              54                            │
│  Total Suites:                 7                            │
│                                                             │
│  Project Types:                                             │
│    • ASP.NET Web Servers:      6                            │
│    • Blazor WebAssembly:       6                            │
│    • Class Libraries:         31                            │
│    • Console Executables:      7                            │
│    • Aspire AppHost:           1                            │
│                                                             │
│  Unique Patterns:                                           │
│    • FreeGLBA: NuGet publishing infrastructure             │
│    • FreeManager: CLI tool (System.CommandLine)            │
│    • FreeCICD: Azure DevOps integration packages           │
│    • FreeTools: .NET Aspire orchestration                  │
│                                                             │
│  Plugin System:                                             │
│    • Plugin Types:             4 interfaces                 │
│    • Built-in Examples:        6 per suite                  │
│    • Active Usage:             Auth plugins only            │
│    • Development Focus:        Lifecycle logging plugin     │
└─────────────────────────────────────────────────────────────┘
```

---

## Plugin System Architecture

The FreeCRM framework includes a powerful **dynamic plugin system** that allows runtime extensibility without recompilation. This system is shared across all derived project suites.

### Plugin Engine Overview

```mermaid
flowchart TB
    subgraph Startup["🚀 Application Startup"]
        Program["Program.cs"]
        PluginLoader["Plugin Loader"]
        DI["DI Container"]
    end
    
    subgraph PluginFiles["📁 Plugins Folder"]
        CS[".cs files<br/>(compile-time safe)"]
        Plugin[".plugin files<br/>(external code)"]
        Assemblies[".assemblies files<br/>(DLL references)"]
    end
    
    subgraph Runtime["⚡ Runtime Execution"]
        Roslyn["Roslyn Compiler"]
        Execute["Execute Method"]
    end
    
    subgraph Interfaces["🔌 Plugin Interfaces"]
        IPlugin["IPlugin<br/>(General)"]
        IPluginAuth["IPluginAuth<br/>(Login/Logout)"]
        IPluginBG["IPluginBackgroundProcess<br/>(Scheduled Tasks)"]
        IPluginUser["IPluginUserUpdate<br/>(User CRUD Hooks)"]
    end
    
    Program --> PluginLoader
    PluginLoader --> CS
    PluginLoader --> Plugin
    Plugin --> Assemblies
    PluginLoader --> DI
    DI --> Runtime
    CS --> Roslyn
    Plugin --> Roslyn
    Roslyn --> Execute
    
    Execute --> IPlugin
    Execute --> IPluginAuth
    Execute --> IPluginBG
    Execute --> IPluginUser
```

### Plugin Interfaces

All plugins must implement `IPluginBase` which requires a `Properties()` method. Specialized interfaces add specific capabilities:

| Interface | Purpose | Key Methods |
|-----------|---------|-------------|
| `IPluginBase` | Base interface | `Properties()` - Returns plugin metadata |
| `IPlugin` | General purpose | `Execute(DataAccess, Plugin, User?)` |
| `IPluginAuth` | Authentication | `Login(...)`, `Logout(...)` |
| `IPluginBackgroundProcess` | Background tasks | `Execute(DataAccess, Plugin, iteration)` |
| `IPluginUserUpdate` | User CRUD hooks | `UpdateUser(DataAccess, Plugin, User?)` |

### Plugin Properties Dictionary

Every plugin returns metadata via the `Properties()` method:

```csharp
public Dictionary<string, object> Properties() =>
    new Dictionary<string, object> {
        { "Id", new Guid("00000000-0000-0000-0000-000000000000") },  // Unique ID
        { "Author", "Developer Name" },
        { "ContainsSensitiveData", false },
        { "Description", "What this plugin does" },
        { "Name", "Plugin Display Name" },
        { "SortOrder", 0 },           // Execution order
        { "Type", "Example" },        // Plugin category
        { "Version", "1.0.0" },
        // Auth plugins also include:
        { "ButtonText", "Login Button Text" },
        { "ButtonClass", "btn btn-primary" },
        { "ButtonIcon", "<i class=\"fa-solid fa-sign-in\"></i>" },
        { "LimitToTenants", new List<Guid> { ... } },
        { "Prompts", new List<PluginPrompt> { ... } },
    };
```

### Built-in Plugin Examples Per Suite

| Suite | Plugin Files | Notes |
|-------|-------------|-------|
| **FreeCRM-main** | Example1.cs, Example2.cs, Example3.cs, ExampleBackgroundProcess.cs, LoginWithPrompts.cs, UserUpdate.cs | Full set with all types |
| **FreePlugins_base** | Same as FreeCRM | Namespace changed to `FreePlugins` |
| **FreePluginsV1** | Same as FreePlugins_base | Development target |
| **FreeCICD** | Example1-3.cs, LoginWithPrompts.cs, UserUpdate.cs | Missing ExampleBackgroundProcess |
| **FreeGLBA** | Full set | Latest patterns |
| **FreeManager** | Full set | Oldest implementation |

### Plugin File Types

| Extension | Purpose | Compilation |
|-----------|---------|-------------|
| `.cs` | In-solution plugins | Compiled with project |
| `.plugin` | External plugins | Compiled at runtime via Roslyn |
| `.assemblies` | DLL references | Lists assemblies for `.plugin` files |

### Current Plugin Usage Analysis

| Plugin Type | Current Usage | Implemented In |
|-------------|---------------|----------------|
| **Auth** | Custom login flows | `LoginWithPrompts.cs` - Prompts for username/password |
| **BackgroundProcess** | Scheduled tasks | `ExampleBackgroundProcess.cs` - Demo only |
| **UserUpdate** | Post-save hooks | `UserUpdate.cs` - Demo only |
| **General** | UI prompts demo | `Example1.cs` - Shows all prompt types |

> ⚠️ **Note:** Currently, only the `Auth` plugin type is actively used in production. The other types are examples waiting to be implemented.

---

## FreePluginsV1 Goals

**FreePluginsV1** is the development suite for demonstrating and documenting the FreeCRM plugin system. The primary goal is to create practical, production-ready plugin examples that can be dropped into any FreeCRM-based project.

### 🎯 Phase 1: Application Lifecycle Logger Plugin

The first plugin to be developed is a **simple lifecycle logger** that:

1. **On Application Start** - Logs a timestamped entry when the app boots
2. **On Application Shutdown** - Logs when the app cleanly stops (if possible)
3. **Output Location** - Creates `PluginLogs.log` in the same directory as `appsettings.json`

#### Plugin Specification

```
┌─────────────────────────────────────────────────────────────┐
│  PLUGIN: ApplicationLifecycleLogger                         │
├─────────────────────────────────────────────────────────────┤
│  Type:        BackgroundProcess                             │
│  File:        ApplicationLifecycleLogger.cs                 │
│  Output:      PluginLogs.log (same level as appsettings)   │
│  Format:      CSV or structured log                         │
│                                                             │
│  Log Events:                                                │
│    • APP_START    - When application boots                  │
│    • APP_STOP     - When application cleanly shuts down     │
│    • HEARTBEAT    - Optional periodic alive check           │
│                                                             │
│  Log Fields:                                                │
│    • Timestamp (UTC)                                        │
│    • Event Type                                             │
│    • Machine Name                                           │
│    • Process ID                                             │
│    • App Version (optional)                                 │
└─────────────────────────────────────────────────────────────┘
```

#### Expected Log Output

```csv
Timestamp,Event,MachineName,ProcessId,Message
2025-01-15T10:30:00Z,APP_START,WEBSERVER01,12345,"Application started successfully"
2025-01-15T18:00:00Z,APP_STOP,WEBSERVER01,12345,"Application shutdown initiated"
```

#### Portability Goal

Once built, this plugin should be **drop-in compatible** with:
- ✅ FreeCRM-main
- ✅ FreePlugins_base
- ✅ FreeCICD
- ✅ FreeGLBA
- ✅ FreeManager

Simply copy the `.cs` file to any suite's `Plugins/` folder and restart the application.

### 🎯 Phase 2: Additional Plugin Examples (Future)

| Plugin | Type | Purpose |
|--------|------|---------|
| **DatabaseHealthCheck** | BackgroundProcess | Periodic DB connectivity test |
| **AuditLogger** | UserUpdate | Log all user CRUD operations |
| **CustomAuthProvider** | Auth | Template for SSO integration |
| **ReportGenerator** | General | PDF/CSV export framework |

### Documentation Deliverables

| Document | Content |
|----------|---------|
| `102_deep-dive_FreeCRM-main.md` | Base framework analysis |
| `103_deep-dive_FreePlugins_base.md` | Template analysis |
| `104_deep-dive_FreeGLBA.md` | Newest implementation |
| `105_deep-dive_FreeCICD.md` | CI/CD implementation |
| `106_deep-dive_FreePluginsV1.md` | Development suite + plugin tutorials |
| `107_deep-dive_FreeManager.md` | Legacy patterns (Razor inspiration) |

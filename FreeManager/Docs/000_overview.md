# FreeManager -- Overview

> **Category:** Overview
> **Purpose:** What this project is, why it exists, and how to get started.

---

## What it is

FreeManager is a multi-tenant application management and code-generation platform built on the FreeCRM framework with ASP.NET Core and Blazor WebAssembly (.NET 10). It provides two major capabilities:

1. **A running multi-tenant web application** -- the full FreeCRM scaffold with users, departments, tenants, tags, file storage, settings, UDFs, and a Roslyn plugin system.
2. **An in-browser and CLI code generator** -- an Entity Wizard and App Builder that let developers define data models, preview generated C#/Razor code, and export it into the layered FreeCRM project structure.

## Why it exists

Building a new FreeCRM-based application requires creating the same layered project structure every time. FreeManager automates that scaffolding: pick a template (Starter, FullCrud, FreeAudit, etc.), define your entities, and get a working starting point rather than a blank repo.

## Who it is for

- WSU-EIT developers starting new FreeCRM-based projects
- Engineers who want to generate CRUD modules without writing boilerplate
- Teams that need a code-generation audit trail (saved Entity Wizard projects)

## Quick start

```bash
cd FreeManager/FreeManager
dotnet run
```

Navigate to `http://localhost:5103`.

### CLI usage

```bash
FreeManager.exe new Tasks                          # Starter template
FreeManager.exe new Inventory -t FullCrud          # Full CRUD with EF migration
FreeManager.exe app FreeGLBA --template FreeAudit  # Full app from FreeAudit template
FreeManager.exe list                               # Show available templates
```

## Related projects

- [FreeCRM](https://github.com/WSU-EIT/FreeCRM) -- source framework
- [FreeGLBA](../FreeGLBA/README.md) -- example of a FreeAudit-template app
- [FreePlugins](../FreePlugins/README.md) -- example of a plugin-heavy app

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*
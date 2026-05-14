# FreeSmartsheets -- Overview

> **Category:** Overview
> **Purpose:** What this project is, why it exists, and how to get started.

---

## What it is

FreeSmartsheets is a Blazor WebAssembly application that connects to the Smartsheet API to display an organization''s workspace inventory -- showing what workspaces, sheets, and reports exist and who has access to what. It is built on the FreeCRM framework (.NET 10) and uses the Smartsheet .NET SDK to query the API.

## Why it exists

Large organizations accumulate hundreds of Smartsheet workspaces and sheets with inconsistent ownership and access. FreeSmartsheets gives administrators a single read-only view of the entire Smartsheet estate without needing to click through the Smartsheet web UI folder by folder.

## Who it is for

- WSU-EIT administrators who manage organization-wide Smartsheet access
- IT teams auditing Smartsheet usage and permissions before license consolidation
- Anyone who needs a quick inventory of their Smartsheet account

## Quick start

1. Add your Smartsheet API key to `appsettings.json`:

```json
"Smartsheet": {
  "ApiKey": "your-api-key-here"
}
```

2. Run the application:

```bash
cd FreeSmartsheets/FreeSmartsheets/FreeSmartsheets
dotnet run
```

Navigate to `http://localhost:5106`.

## Related projects

- [FreeCRM](https://github.com/WSU-EIT/FreeCRM) -- source framework
- [FreeGLBA](../FreeGLBA/README.md) -- similar audit/inventory pattern for GLBA compliance

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*
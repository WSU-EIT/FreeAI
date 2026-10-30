# FreeSmartsheets -- Feature Inventory

> **Category:** Features
> **Purpose:** Complete catalog of what this project can do today and what is planned (unchecked = designed, not yet implemented).

---

## Core features

- [ ] Smartsheet API integration via official .NET SDK
- [ ] Workspace inventory listing (all workspaces in the organization)
- [ ] Sheet inventory per workspace
- [ ] Report listing per workspace
- [ ] Access/sharing information per item
- [x] Multi-tenant FreeCRM auth scaffold (login, users, settings)
- [x] EF Core multi-database (SQL Server, PostgreSQL, SQLite, InMemory)

## Configuration

| Setting | Description |
|---------|-------------|
| `Smartsheet:ApiKey` | Smartsheet API key (required) |
| `DatabaseType` | InMemory / SQLServer / PostgreSQL / SQLite |
| `ConnectionStrings:AppData` | Database connection string |

## Known limitations

- Read-only -- no create/edit/delete operations on Smartsheet items
- Login with admin/admin fails in InMemory mode without seeded data
- Requires a valid Smartsheet API key; boots but shows empty data without one
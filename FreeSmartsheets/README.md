# FreeSmartsheets

A Blazor WebAssembly application for viewing a Smartsheet workspace inventory — showing what workspaces, sheets, and reports exist in an organization's Smartsheet account and who has access to what. Configure the Smartsheet API key in `appsettings.json` to connect to your organization's account.

## Project Structure

| Project | Description |
|---------|-------------|
| [`FreeSmartsheets`](FreeSmartsheets/FreeSmartsheets/README.md) | Main server application (ASP.NET Core, Blazor Server/WASM host) |
| [`FreeSmartsheets.Client`](FreeSmartsheets/FreeSmartsheets.Client/README.md) | Blazor WebAssembly UI components |
| [`FreeSmartsheets.DataAccess`](FreeSmartsheets/FreeSmartsheets.DataAccess/README.md) | Business logic and data access layer |
| [`FreeSmartsheets.DataObjects`](FreeSmartsheets/FreeSmartsheets.DataObjects/README.md) | DTOs and configuration objects |
| [`FreeSmartsheets.EFModels`](FreeSmartsheets/FreeSmartsheets.EFModels/README.md) | Entity Framework Core database models |
| [`FreeSmartsheets.Plugins`](FreeSmartsheets/FreeSmartsheets.Plugins/README.md) | Dynamic C# plugin system |

## Quick Start

### Prerequisites

- .NET 10 SDK
- SQL Server, PostgreSQL, SQLite, or MySQL
- Smartsheet API key

### Configuration

Set your Smartsheet API key in `FreeSmartsheets/appsettings.json` and configure a database connection string, then run:

```bash
cd FreeSmartsheets/FreeSmartsheets
dotnet run
```

Navigate to `https://localhost:5001`

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

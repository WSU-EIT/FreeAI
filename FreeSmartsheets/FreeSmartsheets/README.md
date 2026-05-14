# FreeSmartsheets

Smartsheet workspace inventory viewer. A Blazor WebAssembly application that connects to a Smartsheet account via API key and displays workspace contents, sheet listings, and access information for the organization's Smartsheet environment.

Built on the FreeCRM platform, this application provides an authenticated, multi-tenant-capable web interface for reviewing the Smartsheet workspace structure and who has access to what across the organization's Smartsheet environment.

## Project Structure

| Project | Description |
|---------|-------------|
| [`FreeSmartsheets`](FreeSmartsheets/README.md) | Main server application (ASP.NET Core, Blazor WASM host) |
| [`FreeSmartsheets.Client`](FreeSmartsheets.Client/README.md) | Blazor WebAssembly UI components |
| [`FreeSmartsheets.DataAccess`](FreeSmartsheets.DataAccess/README.md) | Business logic and data access layer |
| [`FreeSmartsheets.DataObjects`](FreeSmartsheets.DataObjects/README.md) | Shared DTOs and configuration objects |
| [`FreeSmartsheets.EFModels`](FreeSmartsheets.EFModels/README.md) | Entity Framework Core database models |
| [`FreeSmartsheets.Plugins`](FreeSmartsheets.Plugins/README.md) | Dynamic C# plugin system (Roslyn) |
| [`Docs`](Docs/README.md) | Architecture, style guides, and developer documentation |

## Quick Start

### Prerequisites

- .NET 10 SDK
- SQL Server, PostgreSQL, SQLite, or MySQL
- Smartsheet API key (from your Smartsheet account settings)

### Configuration

1. Set the Smartsheet API key in `FreeSmartsheets/appsettings.json`
2. Configure a database connection string in `FreeSmartsheets/appsettings.json`
3. Run the application:

```bash
cd FreeSmartsheets
dotnet run
```

Navigate to `https://localhost:5001`

## Technology Stack

- **.NET 10** — latest .NET runtime
- **Blazor WebAssembly** — interactive browser-side UI
- **ASP.NET Core** — server host and REST API
- **Entity Framework Core** — database access (SQL Server, PostgreSQL, SQLite, MySQL, InMemory)
- **SignalR** — real-time notifications
- **Roslyn** — server-side and client-side dynamic C# plugin compilation

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

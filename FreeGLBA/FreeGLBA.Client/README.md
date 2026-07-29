# FreeGLBA.Client

Blazor WebAssembly client application for the FreeGLBA GLBA Compliance Data Access Tracking System. Contains all UI components, pages, and client-side logic.

Developed by **Enrollment Information Technology** at **Washington State University**.

## Purpose

This project provides the interactive web UI for FreeGLBA:
- **Dashboard** - Access event statistics, recent activity, and top accessors
- **Source Systems** - Manage external systems and API keys
- **Access Events** - Browse, search, and filter access logs; create events and generate test data
- **Reports** - Track compliance report metadata (generation not implemented yet)
- **Settings** - Configure application settings
- **User Management** - Manage users and permissions

## Technology Stack

- **.NET 10** - Blazor WebAssembly
- **MudBlazor** - Material Design component library
- **Blazor.Bootstrap** - Bootstrap components
- **Radzen.Blazor** - Additional UI components
- **SignalR** - Real-time updates (framework entities only; no GLBA publisher yet)

## Dependencies

| Package | Purpose |
|---------|---------|
| `MudBlazor` | Material Design UI components |
| `Blazor.Bootstrap` | Bootstrap components for Blazor |
| `Radzen.Blazor` | Data grid, charts, forms |
| `BlazorMonaco` | Monaco code editor |
| `BlazorSortableList` | Drag-and-drop lists |
| `Blazored.LocalStorage` | Browser local storage |
| `FreeBlazor` | Custom Blazor utilities |
| `CsvHelper` | CSV export |
| `HtmlAgilityPack` | HTML parsing |
| `Humanizer` | String formatting |
| `Microsoft.AspNetCore.SignalR.Client` | Real-time communication |

### Project References
- **FreeGLBA.DataObjects** - DTOs and API endpoints

## Project Structure

```
FreeGLBA.Client/
├── FreeGLBA.Client.csproj
├── README.md
├── Program.cs                      # WebAssembly entry point
├── _Imports.razor                  # Global Blazor imports
├── Helpers.cs                      # Client-side utilities
│
├── Pages/                          # Routable pages
│   ├── Index.razor                 # Dashboard
│   ├── About.razor
│   ├── Authorization/              # Auth pages
│   ├── Settings/                   # Settings pages
│   └── [Entity]/                   # CRUD pages per entity
│
├── Shared/                         # Shared components
│   ├── MainLayout.razor
│   ├── NavMenu.razor
│   └── AppComponents/              # Reusable components
│
└── wwwroot/                        # Static assets
    ├── css/
    ├── js/
    ├── images/
    ├── appsettings.json            # Client configuration
    └── index.html                  # SPA host page
```

## Key Features

### Dashboard
- Event counts for today / this week / this month
- Recent events feed
- Top accessors and source-system status
- Loads on navigation; **no live SignalR push for GLBA events yet** (see note below)

### Source System Management
- Register new source systems
- Generate and rotate API keys (plaintext key shown once, stored hashed)
- View event counts and last activity
- Enable/disable systems
- **Generate Test Data** button fills the form with a plausible sample system

### Access Event Browser
- Advanced filtering (date, user, subject, type, department)
- Search across user and subject fields
- Drill-down to event details, including expanding bulk-event subject lists
- **New Access Event** opens the full editor, which has its own **Generate Test Data**
  dropdown (single new/existing subject, or bulk across 2–10 subjects)
- **Generate Test Events** creates randomized events in bulk — choose a count (10–500),
  a time window (today / 7 / 30 / 90 days), the size of the data-subject pool, and whether
  to include multi-subject bulk exports. Events are posted in chunks of 100 to
  `api/Data/SaveAccessEvents`.

### Compliance Reports
- CRUD over report metadata (type, period, generated-by, totals)
- **No report generation yet** — no PDF or CSV output, and nothing populates `ReportData`
  or `FileUrl`. Date-range and source filters, export, and scheduling are roadmap items.

## Configuration

### wwwroot/appsettings.json

```json
{
  "ApiBaseUrl": "https://your-server.com",
  "SignalREnabled": true,
  "DefaultPageSize": 25
}
```

## Usage with Main Application

This project is referenced by the main `FreeGLBA` server project and runs as a Blazor WebAssembly application hosted by the server:

```xml
<!-- In FreeGLBA.csproj -->
<ProjectReference Include="..\FreeGLBA.Client\FreeGLBA.Client.csproj" />
```

The server hosts the WebAssembly files and serves them to browsers.

## Component Libraries

### MudBlazor Components Used
- `MudDataGrid` - Data tables with sorting, filtering, paging
- `MudChart` - Charts and graphs
- `MudDialog` - Modal dialogs
- `MudForm` - Form validation
- `MudNavMenu` - Navigation menus
- `MudAppBar` - Top app bar

### Radzen Components Used
- `RadzenDataGrid` - Advanced data grid
- `RadzenChart` - Charts
- `RadzenDropDown` - Dropdowns

### BlazorMonaco
Used for code editing in the plugin management interface.

## API Communication

Uses `HttpClient` to communicate with the server API:

```csharp
@inject HttpClient Http

var events = await Http.GetFromJsonAsync<List<AccessEvent>>(
    Endpoints.FreeGLBA.GetAccessEvents);
```

## Real-Time Updates — framework only

The FreeCRM SignalR pipeline is present and connected. `MainLayout.razor` opens a `HubConnection`
to `/freeglbaHub` and dispatches `SignalRUpdate` messages, and pages subscribe through
`Model.OnSignalRUpdate`. That machinery drives live updates for **framework** entities — users,
departments, tags, files, settings.

**No GLBA entity publishes to it.** There is no `SignalRUpdateType` for access events, and neither
`ProcessGlbaEventAsync` nor `SaveAccessEventAsync` sends an update. `Helpers.App.cs`
(`ProcessSignalRUpdateApp`) is an empty stub reserved for exactly this. The GLBA dashboard therefore
fetches on navigation and does not refresh on its own.

Wiring it up would mean adding a GLBA update type, publishing from the event-processing path, and
handling it in `ProcessSignalRUpdateApp` — the hook is already there and empty:

```csharp
// FreeGLBA.Client/Helpers.App.cs
public static async Task ProcessSignalRUpdateApp(DataObjects.SignalRUpdate update)
{
    // Process any SignalR updates specific to your app here.
}
```

## Styling

- **Bootstrap 5** - Base styling
- **MudBlazor Theme** - Material Design colors
- **Custom CSS** - In `wwwroot/css/`

## Browser Support

- Chrome (recommended)
- Firefox
- Edge
- Safari

*Note: WebAssembly required - IE11 not supported*

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The browser dashboard, in C#/WebAssembly. It shows a live event counter and recent-events feed (pushed by SignalR as events arrive), lets admins register source systems and rotate their API keys, browse/filter/search the access log, and generate compliance reports. It talks to the server through typed endpoint constants and an injected `HttpClient`.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Blazor WebAssembly (.NET 10) | The dashboard UI in the browser | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.Client/Program.cs) |
| API helpers + SignalR | Calls + live "NewEvent" updates | [Helpers.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.Client/Helpers.cs) |
| MudBlazor / Radzen | Data grids and charts | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.Client/Program.cs) |

**Why does this exist?**
So compliance staff get a real-time, filterable view of access activity — and a place to manage source systems and pull audit reports — without touching the database.

**What does it accomplish that other tools don't?**
- **Live** dashboard: SignalR pushes each new event the moment it's logged.
- One screen to **register systems and rotate API keys**, plus filtered CSV/PDF export of the audit log.

**Terminology & "can I see it?"**
- **SignalR** — the live channel that pushes "NewEvent" to the dashboard.
- **Source system management** — registering external apps and issuing/rotating their keys.

**The hard part, drawn** — a logged event reaches the dashboard live:

```
  server logs an AccessEvent ──SignalR "NewEvent"──▶ dashboard updates the feed + counter in real time
  admin actions ─▶ Http GET/POST Endpoints.FreeGLBA.* ─▶ events · stats · source systems · reports
```

## About

FreeGLBA is developed and maintained by the **Enrollment Information Technology** team at **Washington State University**.

🔗 [Meet Our Staff](https://em.wsu.edu/eit/meet-our-staff/)

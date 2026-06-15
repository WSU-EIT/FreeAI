# FreeBlazorExtended

A companion library to [FreeBlazor](https://github.com/wicketbr/FreeBlazor)
that adds reusable Blazor components plus services backing the showcase
pages in the sister `FreeBlazorExample` project.

## What's here

```
FreeBlazorExtended/
  Foundation/                  Cards, Tables, Forms, Layout, Feedback (shared)
  AgentMonitoring/             Feature 105 — Windows-server agent management
  Calendar/                    Feature 103 — Event calendar with conflict detection
  DynamicForms/                Feature 101 — JSON-schema form runtime
  HierarchicalTree/            Feature 107 — Tree component with drag-to-reparent
  MultiViewSync/               Feature 102 — Multi-view real-time SignalR sync
  UserPreferences/             Feature 104 — Per-user settings store

  AboutSection/                Collapsible "About this page" with ChildContent
  Carousel/                    Single-slide focus + filmstrip + autoplay
  CommandPalette/              Ctrl+K modal search overlay (VS Code / GitHub style)
  ImageGallery/                Thumbnail grid + lightbox with keyboard nav
  InfoTip/                     Inline (i) icon → click-to-explain popover
  KanbanBoard/                 Drag-and-drop swimlane board (controlled component)
  NetworkChart/                vis.js force-directed graph
  PipelineTracker/             Horizontal stage visualization (Dominos/FedEx style)
  Signature/                   jSignature signature pad with @bind-Value
  Timeline/                    Vertical event timeline (FedEx/UPS tracking layout)
  Timer/                       Countdown timer with progress bar
  Wizard/                      Stepper + step header + selection summary primitives
```

### Foundation components by category (25 total)

| Category | Count | Files |
|----------|------:|-------|
| Cards | 4 | `MetricCard`, `FeatureCard`, `StatusCard`, `ProgressCard` |
| Tables | 5 | `DataTable`, `TablePagination`, `BulkActions`, `ExpandableRows`, `ColumnSelector` |
| Forms | 6 | `FormSection`, `FormBuilder`, `FormActions`, `ValidationMessage`, `ConditionalField`, `DynamicFormRenderer` |
| Layout | 4 | `TwoColumnLayout`, `ThreeColumnLayout`, `ResponsiveGrid`, `PageContainer` |
| Feedback | 4 | `LoadingSpinner`, `EmptyState`, `ErrorMessage`, `SuccessMessage` |
| Trees | 2 | `HierarchicalTree`, `TreeNodeComponent` |

### Top-level reusable components (12 — imported from FreeExamples)

| Component | Use case | Effort to integrate |
|---|---|---|
| `AboutSection` | Collapsible "What is this?" card with caller-supplied body | S |
| `Carousel` | Image / hero / product carousel with autoplay and filmstrip | M |
| `CommandPalette` | Universal Ctrl+K palette with keyboard nav and history | M |
| `ImageGallery` | Photo grid with full-screen lightbox | M |
| `InfoTip` | Inline help popover, optional code snippet | S |
| `KanbanBoard` | Drag-drop board, fully controlled (caller owns state) | L |
| `NetworkChart` | Force-directed network graph (vis.js) | M |
| `PipelineTracker` | Horizontal status pipeline (order tracking, deployments) | M |
| `Signature` | Handwritten signature pad (jSignature) | S |
| `Timeline` | Vertical event timeline with timestamps | S |
| `Timer` | Countdown with optional progress bar and controls | S |
| `Wizard` | Stepper + step header + summary (3-piece primitive) | S |

Each top-level folder ships with its own `README.md` covering the component's
exact API, dependencies, and cherry-pick instructions for merging into
the upstream FreeBlazor library.

### Services (7 total)

`NotificationService` (toast queue), `FormService` (dynamic form CRUD),
`UserPreferencesService`, `CalendarEventService`, `RealtimeSyncService`,
`AgentMonitoringService`, `TreeService`. All registered in DI on both the
WebAssembly client and the server side of the showcase host.

## Style conventions

Follows the [FreeCRM / FreeServicesHub](https://github.com/wicketbr) house
style:

- File-scoped namespaces (`namespace FreeBlazorExtended.X;`)
- UTF-8 BOM on every `.cs` and `.razor` file
- Allman braces on method declarations, K&R braces on inner blocks
  (`if (x) { ... } else { ... }`)
- PascalCase parameter names (`Guid TenantId`, `string Name`)
- No `Async` suffix on method names
- `DataObjects.BooleanResponse` with `Result` and `Messages` for
  delete/save responses
- `output` variable convention in service methods
- `ActionResponseObject` base for user-facing model classes

## Use it

The companion `FreeBlazorExample` project demonstrates every component and
service in 17 showcase pages reachable from `/showcase`. To consume the
library in your own project:

```xml
<ProjectReference Include="../FreeBlazorExtended/FreeBlazorExtended.csproj" />
```

Then register the services in `Program.cs`:

```csharp
builder.Services.AddSingleton<FreeBlazorExtended.Foundation.Services.NotificationService>();
builder.Services.AddSingleton<FreeBlazorExtended.DynamicForms.FormService>();
builder.Services.AddSingleton<FreeBlazorExtended.UserPreferences.UserPreferencesService>();
builder.Services.AddSingleton<FreeBlazorExtended.Calendar.CalendarEventService>();
builder.Services.AddSingleton<FreeBlazorExtended.MultiViewSync.RealtimeSyncService>();
builder.Services.AddSingleton<FreeBlazorExtended.AgentMonitoring.AgentMonitoringService>();
builder.Services.AddSingleton<FreeBlazorExtended.HierarchicalTree.TreeService>();
```

(The 12 top-level reusable components do not require DI registration — they're
pure UI components.)

## Cherry-picking individual components

Each component folder is independently mergeable. To pull just one into your
own project:

1. Copy the folder (e.g., `FreeBlazorExtended/Signature/`)
2. Copy `Foundation/Helpers.cs` and `Foundation/DataObjects.cs` (every
   feature transitively uses these)
3. Add `@using FreeBlazorExtended.<Name>` to your `_Imports.razor`
4. If the component has a `.razor.js` sidecar, the path
   (`./_content/FreeBlazorExtended/<Name>/<Name>.razor.js`) already follows
   the Razor-class-library convention — no path edits needed when the
   library is referenced via `<ProjectReference>` or NuGet.

See each component's individual `README.md` for component-specific dependencies
(jQuery for Signature, vis.js for NetworkChart, etc.).

## Known gaps

- **Feature 102 MultiViewSync** — `PresentationHub` ships but the companion
  WASM client and per-session group routing are not pre-wired; production
  deployments need to wire the hub URL and register the DI services on both
  the server and the WASM client (see `MultiViewSync/README.md`).
- **Feature 105 AgentMonitoring** — service mutates in-memory state; the
  companion `FreeBlazorExtended.Agent` Worker Service exists but real command
  dispatch requires the host to wire the AgentHub SignalR endpoint.
- **All 6 feature services** — persist to `ConcurrentDictionary` in-memory
  only; production deployments need EF Core entities and migrations.
- **ExampleNav** — still coupled to host-app routing conventions; requires
  explicit parameterization before it can move to the library cleanly.

## Status

- Builds clean against .NET 10.0 (0 errors, 0 warnings on `FreeBlazorExtended.csproj`)
- 12 top-level components extracted from FreeExamples (2026-04-30)
- WCAG 2.1 AA verified on every showcase page (axe-core, WAVE,
  HTML_CodeSniffer, IBM Equal Access — all clean)
- Showcase Feature 105 demonstrates the full Azure-DevOps-runner-style
  agent management surface

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is a **Razor class library** — a toolbox of ready-made Blazor UI parts. It bundles ~20 self-contained interactive components (drag-and-drop KanbanBoard, Calendar, Ctrl+K CommandPalette, NetworkChart, Timeline…), a "Foundation" set of 25 building blocks (cards, tables, forms, layout), 6 feature **services** (toast notifications, dynamic forms, calendar events, real-time sync, agent monitoring, tree, user prefs), and a Windows agent-management feature. Each component lives in its own folder with its own README and is **independently cherry-pickable** — copy the folder, add a `@using`, and go. Pure UI components need no setup; the feature services are registered in DI. The sister `FreeBlazorExample` app demonstrates every one across 17 `/showcase` pages.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| ~20 reusable components | Each is a self-contained `.razor` | [KanbanBoard/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended/KanbanBoard) · [Calendar/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended/Calendar) · [CommandPalette/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended/CommandPalette) |
| Foundation (25 building blocks) | Cards, tables, forms, layout, feedback | [Foundation/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended/Foundation) |
| 6 feature services (DI) | Toasts, forms, sync, agents, tree, prefs | [Foundation/Services/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended/Foundation/Services) |
| Windows agent feature | Remote service/IIS control (Feature 105) | [AgentMonitoring/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended/AgentMonitoring) |

**Why does this exist?**
To stop every app re-inventing the same widgets. A kanban board, a command palette, a signature pad — each is genuinely fiddly to build well. This collects proven, accessible versions into one library you can pull from a piece at a time.

**What does it accomplish that other tools don't?**
- **Cherry-pickable, not all-or-nothing**: each component folder is independently mergeable (copy + two shared files), so you take only what you need.
- **Controlled-component design**: components report events and render what you give them — they don't hide state — so they never silently drift out of sync with your data.
- **Honesty built in**: a "Known gaps" section says plainly what's demo-grade vs production-ready (e.g. services persist in-memory until you add EF Core).
- **Accessible**: every showcase page is WCAG 2.1 AA-verified by four engines.

**Terminology & "can I see it?"**
- **Razor class library** — a shareable package of Blazor components.
- **Cherry-pick** — copy one component into your project rather than depending on the whole library.
- **Controlled component** — the caller owns the data; the component only renders + raises events.
- *See it:* the `/showcase` pages in `FreeBlazorExample` demonstrate all 17 features.

**The hard part, drawn** — how you actually consume a piece of this library:

```
  pick ONE component (e.g. Signature/)
        │ Option A: cherry-pick ─▶ copy folder + Foundation/Helpers.cs + DataObjects.cs
        │ Option B: reference ───▶ <ProjectReference Include="FreeBlazorExtended.csproj"/>
        ▼ add  @using FreeBlazorExtended.<Name>  to _Imports.razor
   pure UI component? ──▶ just use it (no DI)
   feature service?   ──▶ builder.Services.AddSingleton<…Service>()  in Program.cs
        ▼ (.razor.js sidecars auto-resolve via _content/FreeBlazorExtended/<Name>/)
   component renders in YOUR app, raising events back to YOUR state
```

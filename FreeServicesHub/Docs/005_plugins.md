# 005 — Plugins: plain-English briefings

These briefings used to sit at the end of two FreeCRM framework READMEs. They live here so those files stay stock
FreeCRM and a framework upgrade can overwrite them.

## Dynamic Blazor support (FreeServicesHub.Client/DynamicBlazorSupport)

**In one line:** this folder lets the browser compile and render Blazor *plugin* components at runtime — turning `.razor` markup into C#, then into a live assembly, entirely inside WebAssembly (no server, no rebuild).

**Why it exists:** so plugin UI (e.g. a custom agent-dashboard widget) can be authored and previewed in the browser without a deploy cycle.

**See the full briefing** — how it fits the app, exact files, and the compile diagram — in the parent [FreeServicesHub.Client README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Client/FreeServicesHub.App.README.md).

## Blazor plugin components (FreeServicesHub/PluginFiles/BlazorComponents)

**How does this work?**
Drop a `.razor`/`.blazor` file in this folder and the app compiles and renders it at runtime — the **filename decides where it appears**. `Button_Index_…` adds a button to the Index page; `Top_Index_…` injects content at the top of Index; `TabGeneralTop_Settings_…` targets a tab. An optional same-named `.json` controls tenant scope and sort order.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| In-browser Roslyn compile | Render your `.razor` with no rebuild | [DynamicBlazorSupport](https://github.com/WSU-EIT/FreeAI/tree/main/FreeServicesHub/FreeServicesHub.Client/DynamicBlazorSupport) |
| Roslyn plugin host | Loads & compiles plugin files | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Plugins/Plugins.cs) |

**Why does this exist?**
So the UI can be customized per tenant — e.g. a custom agent-status widget on the dashboard — **without touching or rebuilding the core app**.

**What does it accomplish that other tools don't?**
- **Convention over configuration**: the filename *is* the wiring — no registration code, no rebuild.
- **Tenant-scoped** via the optional `.json` sidecar (`LimitToTenants`, `SortOrder`).

**Terminology & "can I see it?"**
- **Slot** — a named spot on a page (`Top`, `Bottom`, a tab) where injected content lands.
- **Sidecar** — the optional `.json` next to your component carrying its metadata.

**The hard part, drawn** — the filename routes the component:

```
  Button_{Page}_{Name}.razor ─drop in─▶ compiled at runtime ─▶ appears as a button on {Page}
  {Slot}_{Page}_{Name}.razor ─drop in─▶ compiled at runtime ─▶ injected into {Slot} of {Page}
        optional {Name}.json  ─────────▶ LimitToTenants[] · SortOrder
```

# Docs

Project documentation for FreeSmartsheets. Contains architecture guides, coding standards, style guides, component documentation, and quickstart references used by the development team.

## Contents

| File | Description |
|------|-------------|
| `000_quickstart.md` | Getting started — setup and first run |
| `001_roleplay.md` | Coding conventions and team working agreements |
| `002_docsguide.md` | How to write and maintain documentation |
| `003_templates.md` | Code and document templates |
| `004_styleguide.md` | UI/UX style guide |
| `005_style.md` | C# code style rules |
| `005_style.comments.md` | XML doc comment conventions |
| `006_architecture.md` | Overall solution architecture |
| `006_architecture.freecrm_overview.md` | FreeCRM base platform overview |
| `006_architecture.unique_features.md` | FreeSmartsheets-specific features |
| `007_patterns.md` | Common coding patterns |
| `007_patterns.helpers.md` | Helper class patterns |
| `007_patterns.signalr.md` | SignalR real-time update patterns |
| `008_components.md` | Blazor shared component reference |
| `008_components.highcharts.md` | Highcharts charting integration |
| `008_components.monaco.md` | Monaco code editor integration |
| `008_components.network_chart.md` | Network/graph chart component |
| `008_components.signature.md` | Signature capture component |
| `008_components.wizard.md` | Multi-step wizard component |
| `008_components.razor_templates.md` | Razor template patterns |

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target Framework | `net10.0` |
| Output Type | Class Library (documentation project) |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A documentation-only project (it ships no runtime code). It collects architecture guides, coding standards, style guides, and component how-tos for the team. Notably it carries **both** the shared FreeCRM platform overview **and** the FreeSmartsheets-specific "unique features" doc — which is where the planned Smartsheet integration is described.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Markdown docs | The team's reference set | [the Docs project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeSmartsheets/FreeSmartsheets/Docs) |
| Platform vs. app-specific split | Separates shared FreeCRM from Smartsheet-specific design | `006_architecture.freecrm_overview.md` · `006_architecture.unique_features.md` |

**Why does this exist?**
So design intent — including features not yet built (the Smartsheet inventory flow) — is written down alongside the code, and so new developers can ramp without reverse-engineering.

**What does it accomplish that other tools don't?**
- Keeps the **platform docs** and the **app-specific plan** clearly separated, so it's obvious what's inherited vs. what's unique to FreeSmartsheets.

**Terminology & "can I see it?"**
- **Style guide** — the agreed rules for code and UI.
- **Unique-features doc** — the FreeSmartsheets-only design notes (where the Smartsheet plan lives).

**The hard part, drawn** — how the doc set is organized:

```
  Docs/  ├─ 006_architecture.freecrm_overview.md   ← the shared platform (inherited)
         ├─ 006_architecture.unique_features.md    ← FreeSmartsheets-specific (the Smartsheet plan)
         └─ 008_components.*.md                     ← Blazor component how-tos (Monaco, Highcharts, …)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.

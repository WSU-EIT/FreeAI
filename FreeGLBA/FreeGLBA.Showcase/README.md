# FreeGLBA.Showcase

Playwright-driven demo, seeding, and end-to-end verification runner for FreeGLBA. It drives the
real UI in a headless browser — logs in, registers a source system, captures the generated API key,
sends real events through the API, and screenshots every page. It is how the screenshots in the
project field report were produced, and it doubles as a smoke test of the full stack (login → API
key → ingest → SignalR refresh → reports → integrity).

## Prerequisites

- The FreeGLBA server running at `https://localhost:7271` (the `https` launch profile, InMemory
  mode is fine — every mode creates its own data).
- Playwright browsers installed once: `pwsh bin/Debug/net10.0/playwright.ps1 install chromium`
  (or an existing `%LOCALAPPDATA%\ms-playwright` install).

## Usage

```bash
dotnet run -- <output-folder-for-screenshots> [mode]
```

| Mode | What it does |
|------|--------------|
| *(none)* | Full showcase: login, create source system with data owner, capture API key, drive the API Explorer (201 + 409 dedupe), seed ~180 realistic events via the batch API, screenshot dashboard/events/ownership transfer/live SignalR arrival/accessors/subjects |
| `--reports` | Seeds data, creates a compliance report via the authenticated page context, downloads the PDF and CSV through the real UI buttons, and validates the bytes |
| `--subject` | Seeds data and downloads a data subject's access-history PDF through the UI |
| `--integrity` | Seeds data, fills the GLBA Settings page, and verifies the tamper-evident hash chain through the UI |
| `--retake08` | Re-takes only the ownership-history screenshot |
| `--probe` | Diagnostic: dumps browser console output and screenshots the dashboard chart and Needs Attention card |

Screenshots land in the output folder; downloaded PDFs/CSVs are byte-validated and saved next to
them. In-memory mode discards data on server restart, so each run recreates everything it needs.

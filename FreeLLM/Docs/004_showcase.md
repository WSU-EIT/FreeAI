# FreeLLM — Showcase

> **Category:** Showcase
> **Purpose:** Visual evidence of the project working — screenshots, console output, logs.
> Artifacts live in `showcase/runs/` alongside this file.

---

## How to capture

Run from the FreeAI root:

```powershell
.\run-a11y-showcase.ps1
```

This boots the app with `DatabaseType=InMemory`, scans with FreeA11yChecker (admin/admin), and writes results to `Docs/showcase/runs/YYYY-MM-DD/`.

---

## Showcase index

### 2026-05-14 — Boot attempt (timed out)

FreeLLM did not respond within the 90-second boot timeout. The app likely requires additional configuration (LLM API key, model endpoint, or non-InMemory database) before it can serve HTTP traffic.

App boot logs are captured for diagnosis:

| Artifact | Type | Notes |
|----------|------|-------|
| [runs/2026-05-14/app-stdout.txt](showcase/runs/2026-05-14/app-stdout.txt) | Log | App boot stdout |
| [runs/2026-05-14/app-stderr.txt](showcase/runs/2026-05-14/app-stderr.txt) | Log | App boot stderr |

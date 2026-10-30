# FreeServicesHub — Showcase

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

### 2026-05-14 — FreeA11yChecker scan (17 screenshots)

| Artifact | Type | Notes |
|----------|------|-------|
| [runs/2026-05-14/localhost/_root/00a-login-anonymous.jpeg](showcase/runs/2026-05-14/localhost/_root/00a-login-anonymous.jpeg) | Screenshot | Login page (unauthenticated) |
| [runs/2026-05-14/localhost/_root/00b-login-credentials-entered.jpeg](showcase/runs/2026-05-14/localhost/_root/00b-login-credentials-entered.jpeg) | Screenshot | Credentials entered |
| [runs/2026-05-14/localhost/_root/00c-login-post-auth.jpeg](showcase/runs/2026-05-14/localhost/_root/00c-login-post-auth.jpeg) | Screenshot | Post-login page |
| [runs/2026-05-14/localhost/_root/01-page-load-00000ms.png](showcase/runs/2026-05-14/localhost/_root/01-page-load-00000ms.png) | Screenshot | Initial page load |
| [runs/2026-05-14/localhost/_root/02-page-expanded.jpeg](showcase/runs/2026-05-14/localhost/_root/02-page-expanded.jpeg) | Screenshot | Page with expanded content |
| [runs/2026-05-14/localhost/_root/03-axe-overlay.png](showcase/runs/2026-05-14/localhost/_root/03-axe-overlay.png) | A11y overlay | axe-core (Deque) |
| [runs/2026-05-14/localhost/_root/04-quickpeek-overlay.png](showcase/runs/2026-05-14/localhost/_root/04-quickpeek-overlay.png) | A11y overlay | Quick peek summary |
| [runs/2026-05-14/localhost/_root/05-htmlcs-overlay.png](showcase/runs/2026-05-14/localhost/_root/05-htmlcs-overlay.png) | A11y overlay | HTML_CodeSniffer |
| [runs/2026-05-14/localhost/_root/06-ibm-overlay.png](showcase/runs/2026-05-14/localhost/_root/06-ibm-overlay.png) | A11y overlay | IBM Equal Access |
| [runs/2026-05-14/localhost/_root/07-structure-overlay.png](showcase/runs/2026-05-14/localhost/_root/07-structure-overlay.png) | A11y overlay | Page structure / landmarks |
| [runs/2026-05-14/localhost/_root/07b-wireframe-blueprint.png](showcase/runs/2026-05-14/localhost/_root/07b-wireframe-blueprint.png) | Screenshot | Wireframe blueprint view |
| [runs/2026-05-14/localhost/_root/08-cvd-protanopia.png](showcase/runs/2026-05-14/localhost/_root/08-cvd-protanopia.png) | CVD sim | Protanopia |
| [runs/2026-05-14/localhost/_root/09-cvd-deuteranopia.png](showcase/runs/2026-05-14/localhost/_root/09-cvd-deuteranopia.png) | CVD sim | Deuteranopia |
| [runs/2026-05-14/localhost/_root/10-cvd-tritanopia.png](showcase/runs/2026-05-14/localhost/_root/10-cvd-tritanopia.png) | CVD sim | Tritanopia |
| [runs/2026-05-14/localhost/_root/11-cvd-achromatopsia.png](showcase/runs/2026-05-14/localhost/_root/11-cvd-achromatopsia.png) | CVD sim | Achromatopsia |
| [runs/2026-05-14/localhost/_root/15-screenreader-view.png](showcase/runs/2026-05-14/localhost/_root/15-screenreader-view.png) | A11y view | Screen reader overlay |
| [runs/2026-05-14/localhost/_root/16-reduced-motion.png](showcase/runs/2026-05-14/localhost/_root/16-reduced-motion.png) | A11y view | Reduced motion |
| [runs/2026-05-14/localhost/_root/17-forced-colors.png](showcase/runs/2026-05-14/localhost/_root/17-forced-colors.png) | A11y view | Forced colors (Windows High Contrast) |
| [runs/2026-05-14/localhost/_root/a11y-summary.json](showcase/runs/2026-05-14/localhost/_root/a11y-summary.json) | Report | Combined a11y summary |
| [runs/2026-05-14/scan-log.txt](showcase/runs/2026-05-14/scan-log.txt) | Log | Full scanner run log |
| [runs/2026-05-14/app-stdout.txt](showcase/runs/2026-05-14/app-stdout.txt) | Log | App boot stdout |
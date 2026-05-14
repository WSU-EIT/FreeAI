# FreeBlazorExtended -- Overview

> **Category:** Overview
> **Purpose:** What this project is, why it exists, and how to get started.

---

## What it is

FreeBlazorExtended is a shared Razor class library that bundles reusable UI components, feature services, and SignalR infrastructure for downstream Blazor apps. It ships alongside `FreeBlazorExample` -- a living showcase host that demonstrates every component across ~17 pages -- and `FreeBlazorExtended.Agent`, a Windows Worker Service that sends host telemetry heartbeats and executes remote Windows Service and IIS app-pool commands from the hub.

The library (`FreeBlazorExtended.dll`) is the distributable artifact; the example app and agent are its integration proof.

## Why it exists

WSU-EIT repeatedly builds the same UI patterns (data tables, modal dialogs, status indicators, SignalR-powered dashboards) across FreeCRM applications. FreeBlazorExtended extracts those patterns into a reusable library so each new project gets them for free, and improvements propagate everywhere at once.

## Who it is for

- WSU-EIT developers building Blazor applications who want pre-built, tested components
- Teams that want to see agent-hub telemetry patterns in a working example
- Anyone evaluating Razor class library patterns for component distribution

## Quick start

```bash
cd FreeBlazorExtended/FreeBlazorExample/FreeBlazorExample
dotnet run
```

Navigate to `http://localhost:5107`. Browse `/showcase/*` pages to see every component.

## Related projects

- [FreeServicesHub](../FreeServicesHub/README.md) -- similar hub-and-agent pattern
- [FreeCRM](https://github.com/WSU-EIT/FreeCRM) -- base framework the example app uses

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*
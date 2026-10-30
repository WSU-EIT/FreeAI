# FreePlugins -- Roadmap

> **Category:** Roadmap
> **Purpose:** Planned and potential future work.

---

## Near-term

- [ ] Hot-reload plugin support -- detect file changes in `PluginFiles/` and recompile without restart
- [ ] Plugin error reporting UI -- show Roslyn compilation errors in the web UI
- [ ] Fix InMemory seeding so admin/admin login works out of the box

## Medium-term

- [ ] Plugin marketplace UI -- browse, install, and version plugins from a central registry
- [ ] Sandboxed plugin execution -- limit what system resources a plugin can access
- [ ] Plugin test runner -- execute plugin unit tests from the web UI

## Long-term

- [ ] WASM plugin support -- compile and run plugins in the browser for client-side extensibility
- [ ] Blazor component plugins -- drop-in `.razor` files rendered dynamically in the UI
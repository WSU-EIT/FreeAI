# FreeBlazorExtended -- Roadmap

> **Category:** Roadmap
> **Purpose:** Planned and potential future work.

---

## Near-term

- [ ] NuGet package publication so downstream apps can `dotnet add package FreeBlazorExtended`
- [ ] Component documentation in Storybook-style interactive demos
- [ ] Fix InMemory seeding so admin/admin login works out of the box for showcase

## Medium-term

- [ ] Dark mode support across all components
- [ ] Accessibility improvements -- ensure every component passes WCAG 2.2 AA (use FreeA11yChecker)
- [ ] Additional agent commands -- file system operations, log tailing

## Long-term

- [ ] Linux agent -- replace Windows-specific service control with systemd equivalents
- [ ] Component theming API -- allow downstream apps to customize colors and typography
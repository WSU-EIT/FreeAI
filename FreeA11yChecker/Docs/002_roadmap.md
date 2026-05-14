# FreeA11yChecker -- Roadmap

> **Category:** Roadmap
> **Purpose:** Planned and potential future work.

---

## Near-term (pre-July 2026 WCAG 2.2 deadline)

- [ ] WCAG 2.2 criterion coverage gap analysis -- document which 2.2 criteria each engine covers
- [ ] Violation trend dashboard -- show issue count over time per site
- [ ] Email/Teams notification when a scheduled scan finds new violations

## Medium-term

- [ ] Mobile viewport scanning (iOS/Android user-agent simulation)
- [ ] Keyboard navigation simulation across all pages
- [ ] Automated re-test -- mark a violation fixed, re-scan just that rule to confirm
- [ ] Public compliance status page per site (unauthenticated endpoint)

## Long-term

- [ ] Integration with GitHub Actions / CI pipelines (fail the build on critical violations)
- [ ] AI-assisted remediation suggestions (send violation + page HTML to LLM, get fix code)
- [ ] Browser extension for on-demand single-page scanning without the CLI
using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Expands all collapsed/hidden content on a page before scanning. Handles HTML details,
/// Bootstrap accordions/collapses/tabs, MudBlazor panels, aria-expanded elements,
/// and scrolls through the page to trigger lazy-loaded content.
/// </summary>
public static class ContentExpander
{
    /// <summary>
    /// Expand all collapsed content on the page so every element is visible for scanning.
    /// Returns true if anything was expanded.
    /// </summary>
    public static async Task<bool> Expand(IPage page)
    {
        var count = await page.EvaluateAsync<int>(@"
            () => {
                let expanded = 0;

                // 1. HTML <details> elements — set open attribute
                document.querySelectorAll('details:not([open])').forEach(el => {
                    el.setAttribute('open', '');
                    expanded++;
                });

                // 2. Bootstrap .collapse that aren't .show yet
                document.querySelectorAll('.collapse:not(.show)').forEach(el => {
                    el.classList.add('show');
                    el.style.display = '';
                    expanded++;
                });

                // 3. Bootstrap accordion — expand all collapsed panels
                document.querySelectorAll('.accordion-collapse:not(.show)').forEach(el => {
                    el.classList.add('show');
                    el.classList.remove('collapsing');
                    el.style.height = '';
                    expanded++;
                });
                // Fix accordion buttons to show expanded state
                document.querySelectorAll('.accordion-button.collapsed').forEach(btn => {
                    btn.classList.remove('collapsed');
                    btn.setAttribute('aria-expanded', 'true');
                });

                // 4. Bootstrap tab-pane — show ALL tab panes simultaneously
                document.querySelectorAll('.tab-pane:not(.show)').forEach(el => {
                    el.classList.add('show', 'active');
                    el.style.display = '';
                    expanded++;
                });

                // 5. Elements with aria-expanded=""false"" — click ONLY content-disclosure
                // toggles, NOT navigation dropdowns. The previous unfiltered click opened
                // every nav menu on the page (Settings, user menu, etc.), polluting the
                // overlay screenshots with cascading dropdowns AND distorting the DOM the
                // analyzers run against. Heuristic: skip if inside a <nav>/header, has
                // a *-toggle/nav-link class, or controls a <ul>/<menu>/role=menu element.
                document.querySelectorAll('[aria-expanded=""false""]').forEach(el => {
                    // Skip nav / header / role=navigation containers.
                    if (el.closest('nav, header, [role=""navigation""], .navbar, .navigation, .menu')) return;
                    // Skip elements classed as dropdown/menu/nav toggles.
                    const cls = (el.className && typeof el.className === 'string') ? el.className : '';
                    if (/dropdown-toggle|nav-link|menu-toggle|navbar-toggler|nav-toggle/i.test(cls)) return;
                    // Skip if aria-controls points to a menu-shaped target (ul/menu/role=menu).
                    const controlsId = el.getAttribute('aria-controls');
                    if (controlsId) {
                        const target = document.getElementById(controlsId);
                        if (target) {
                            const tag = target.tagName.toLowerCase();
                            const role = target.getAttribute('role') || '';
                            if (tag === 'ul' || tag === 'ol' || tag === 'menu') return;
                            if (role === 'menu' || role === 'menubar' || role === 'navigation') return;
                            if (target.classList.contains('dropdown-menu') ||
                                target.classList.contains('navbar-nav') ||
                                target.classList.contains('menu')) return;
                        }
                    }
                    try {
                        el.click();
                        expanded++;
                    } catch(e) { /* skip if click fails */ }
                });

                // 6. Clickable toggle elements (cursor-pointer, role=button with chevrons)
                document.querySelectorAll('.cursor-pointer, [role=""button""]').forEach(el => {
                    const next = el.nextElementSibling;
                    if (!next || next.offsetHeight === 0) {
                        try {
                            const hasChevron = el.querySelector('.fa-chevron-down, .fa-chevron-right, .fa-caret-down, .fa-caret-right, .fa-plus');
                            if (hasChevron) {
                                el.click();
                                expanded++;
                            }
                        } catch(e) { /* skip */ }
                    }
                });

                // 7. MudBlazor panels — expand closed expansion panels
                document.querySelectorAll('.mud-expand-panel:not(.mud-panel-expanded)').forEach(el => {
                    const header = el.querySelector('.mud-expand-panel-header');
                    if (header) {
                        try { header.click(); expanded++; } catch(e) {}
                    }
                });

                // 8. Hidden elements inside expandable containers
                document.querySelectorAll('[style*=""display: none""], [style*=""display:none""]').forEach(el => {
                    const tag = el.tagName.toLowerCase();
                    if (['script', 'style', 'meta', 'link', 'head', 'template', 'noscript'].includes(tag)) return;
                    if (el.closest('.collapse, .accordion, .tab-content, details, .expandable, .toggle-content')) {
                        el.style.display = '';
                        expanded++;
                    }
                });

                // 9. Scroll through the page to trigger lazy-loaded content
                const totalHeight = document.body.scrollHeight;
                const viewportHeight = window.innerHeight;
                for (let y = 0; y < totalHeight; y += viewportHeight) {
                    window.scrollTo(0, y);
                }
                window.scrollTo(0, 0);

                return expanded;
            }
        ");

        return count > 0;
    }
}

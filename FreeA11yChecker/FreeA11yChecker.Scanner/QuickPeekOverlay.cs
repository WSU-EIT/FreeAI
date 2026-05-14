using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Custom clean-room QuickPeek visual overlay. Injects categorized icons next to
/// accessibility-relevant elements using 42 standard DOM query checks.
/// Categories: error (red), alert (yellow), feature (green), structural (purple), aria (blue).
/// </summary>
public static class QuickPeekOverlay
{
    /// <summary>
    /// Inject all 42 accessibility checks and render colored icons on the page.
    /// </summary>
    public static async Task InjectOverlay(IPage page)
    {
        await page.EvaluateAsync(@"
            () => {
                const iconStyles = {
                    error:   { bg: '#dc3545', icon: '\u2715', label: 'ERROR' },
                    alert:   { bg: '#ffc107', icon: '\u26A0', label: 'ALERT' },
                    feature: { bg: '#198754', icon: '\u2713', label: 'FEATURE' },
                    struct:  { bg: '#6f42c1', icon: '\u25A3', label: 'STRUCTURAL' },
                    aria:    { bg: '#0d6efd', icon: '\u24D0', label: 'ARIA' }
                };

                const issues = [];

                function addIcon(el, type, message) {
                    if (!el || !el.getBoundingClientRect) return;
                    issues.push({ el, type, message });
                }

                // =====================
                // --- Errors (red) ---
                // =====================

                // 1. Missing alt on images.
                document.querySelectorAll('img:not([alt])').forEach(img => {
                    addIcon(img, 'error', 'Missing alt text');
                });

                // 2. Empty alt on non-decorative images.
                document.querySelectorAll('img[alt=""""]').forEach(img => {
                    if (!img.getAttribute('role') && !img.closest('[role=""presentation""]'))
                        addIcon(img, 'alert', 'Empty alt text');
                });

                // 3. Empty links.
                document.querySelectorAll('a').forEach(a => {
                    const text = (a.textContent || '').trim();
                    const ariaLabel = a.getAttribute('aria-label') || '';
                    const title = a.getAttribute('title') || '';
                    const img = a.querySelector('img[alt]');
                    if (!text && !ariaLabel && !title && !img) {
                        addIcon(a, 'error', 'Empty link');
                    }
                });

                // 4. Empty buttons.
                document.querySelectorAll('button').forEach(btn => {
                    const text = (btn.textContent || '').trim();
                    const ariaLabel = btn.getAttribute('aria-label') || '';
                    const title = btn.getAttribute('title') || '';
                    if (!text && !ariaLabel && !title) {
                        addIcon(btn, 'error', 'Empty button');
                    }
                });

                // 5. Missing form labels.
                document.querySelectorAll('input:not([type=""hidden""]):not([type=""submit""]):not([type=""button""]), select, textarea').forEach(input => {
                    const id = input.id;
                    const ariaLabel = input.getAttribute('aria-label');
                    const ariaLabelledby = input.getAttribute('aria-labelledby');
                    const hasLabel = id && document.querySelector('label[for=""' + id + '""]');
                    const parentLabel = input.closest('label');
                    if (!hasLabel && !parentLabel && !ariaLabel && !ariaLabelledby) {
                        addIcon(input, 'error', 'Missing form label');
                    }
                });

                // 6. Empty headings.
                document.querySelectorAll('h1,h2,h3,h4,h5,h6').forEach(h => {
                    if ((h.textContent || '').trim() === '') {
                        addIcon(h, 'error', 'Empty heading: <' + h.tagName.toLowerCase() + '>');
                    }
                });

                // 7. Missing or empty page title.
                if (!document.title || document.title.trim() === '') {
                    addIcon(document.body, 'error', 'Missing or empty page title');
                }

                // 8. Linked image missing alt.
                document.querySelectorAll('a img:not([alt])').forEach(img => {
                    addIcon(img, 'error', 'Linked image missing alt text');
                });

                // 9. Image button missing alt.
                document.querySelectorAll('input[type=""image""]:not([alt])').forEach(input => {
                    addIcon(input, 'error', 'Image button missing alt text');
                });

                // 10. Empty form labels.
                document.querySelectorAll('label').forEach(lbl => {
                    const text = (lbl.textContent || '').trim();
                    const hasChildInput = lbl.querySelector('input, select, textarea');
                    if (!text && !hasChildInput) {
                        addIcon(lbl, 'error', 'Empty form label');
                    }
                });

                // ======================
                // --- Alerts (yellow) ---
                // ======================

                // 11. No main landmark.
                if (!document.querySelector('main, [role=""main""]')) {
                    addIcon(document.body, 'alert', 'No <main> landmark');
                }

                // 12. No skip link.
                const firstFocusable = document.querySelector('a, button, input, select, textarea');
                const skipLink = document.querySelector('a[href^=""#""]');
                if (firstFocusable && (!skipLink || !(skipLink.textContent || '').toLowerCase().includes('skip'))) {
                    addIcon(document.body, 'alert', 'No skip navigation link');
                }

                // 13. No h1.
                if (!document.querySelector('h1')) {
                    addIcon(document.body, 'alert', 'No <h1> heading');
                }

                // 14. Suspicious link text.
                const suspiciousTexts = ['click here', 'here', 'more', 'read more', 'link', 'learn more'];
                document.querySelectorAll('a').forEach(a => {
                    const text = (a.textContent || '').trim().toLowerCase();
                    if (suspiciousTexts.includes(text)) {
                        addIcon(a, 'alert', 'Suspicious link text: ""' + text + '""');
                    }
                });

                // 15. Document links.
                const docExtensions = ['.pdf', '.doc', '.docx', '.xlsx', '.pptx'];
                document.querySelectorAll('a[href]').forEach(a => {
                    const href = (a.getAttribute('href') || '').toLowerCase();
                    for (const ext of docExtensions) {
                        if (href.endsWith(ext)) {
                            addIcon(a, 'alert', 'Links to document (' + ext + ')');
                            break;
                        }
                    }
                });

                // 16. Very small text.
                const allElements = document.querySelectorAll('body *');
                const smallTextLimit = Math.min(allElements.length, 50);
                for (let i = 0; i < smallTextLimit; i++) {
                    const el = allElements[i];
                    try {
                        const fontSize = parseFloat(window.getComputedStyle(el).fontSize);
                        if (fontSize < 10 && (el.textContent || '').trim().length > 0) {
                            addIcon(el, 'alert', 'Very small text (' + fontSize.toFixed(1) + 'px)');
                        }
                    } catch (e) { /* skip */ }
                }

                // 17. Missing fieldset for radio/checkbox groups.
                document.querySelectorAll('input[type=""radio""], input[type=""checkbox""]').forEach(input => {
                    if (!input.closest('fieldset')) {
                        addIcon(input, 'alert', 'Radio/checkbox not inside fieldset');
                    }
                });

                // 18. Orphaned label.
                document.querySelectorAll('label[for]').forEach(lbl => {
                    const forVal = lbl.getAttribute('for');
                    if (forVal && !document.getElementById(forVal)) {
                        addIcon(lbl, 'alert', 'Orphaned label (for=""' + forVal + '"" matches no element)');
                    }
                });

                // 19. Justified text.
                document.querySelectorAll('*').forEach(el => {
                    try {
                        if (window.getComputedStyle(el).textAlign === 'justify') {
                            addIcon(el, 'alert', 'Justified text');
                        }
                    } catch (e) { /* skip */ }
                });

                // 20. Missing language attribute.
                if (!document.querySelector('html[lang]')) {
                    addIcon(document.body, 'alert', 'Missing lang attribute on <html>');
                }

                // 21. Autoplay media.
                document.querySelectorAll('video[autoplay], audio[autoplay]').forEach(el => {
                    addIcon(el, 'alert', 'Autoplay media: <' + el.tagName.toLowerCase() + '>');
                });

                // 22. Tabindex > 0.
                document.querySelectorAll('[tabindex]').forEach(el => {
                    const ti = parseInt(el.getAttribute('tabindex') || '0');
                    if (ti > 0) {
                        addIcon(el, 'alert', 'Positive tabindex (' + ti + ')');
                    }
                });

                // ========================
                // --- Features (green) ---
                // ========================

                // 23. Alt text present.
                document.querySelectorAll('img[alt]:not([alt=""""])').forEach(img => {
                    addIcon(img, 'feature', 'Alt: ' + img.alt.substring(0, 40));
                });

                // 24. Skip links.
                document.querySelectorAll('a[href^=""#""]').forEach(a => {
                    const text = (a.textContent || '').trim().toLowerCase();
                    if (text.includes('skip') || text.includes('main content')) {
                        addIcon(a, 'feature', 'Skip link');
                    }
                });

                // 25. Form labels present.
                document.querySelectorAll('label[for]').forEach(lbl => {
                    const forVal = lbl.getAttribute('for');
                    if (forVal && document.getElementById(forVal)) {
                        addIcon(lbl, 'feature', 'Form label present');
                    }
                });

                // 26. Language declared.
                const htmlEl = document.querySelector('html[lang]');
                if (htmlEl) {
                    addIcon(document.body, 'feature', 'Language declared: ' + htmlEl.getAttribute('lang'));
                }

                // 27. Figure elements with figcaption.
                document.querySelectorAll('figure').forEach(fig => {
                    const caption = fig.querySelector('figcaption');
                    if (caption) {
                        addIcon(fig, 'feature', 'Figure with caption');
                    }
                });

                // ============================
                // --- Structural (purple) ---
                // ============================

                // 28. Headings.
                document.querySelectorAll('h1,h2,h3,h4,h5,h6').forEach(h => {
                    if ((h.textContent || '').trim() !== '') {
                        addIcon(h, 'struct', 'Heading: <' + h.tagName.toLowerCase() + '>');
                    }
                });

                // 29. Landmarks.
                document.querySelectorAll('nav, main, header, footer, aside, [role=""navigation""], [role=""main""], [role=""banner""], [role=""contentinfo""]').forEach(el => {
                    const role = el.getAttribute('role') || el.tagName.toLowerCase();
                    addIcon(el, 'struct', 'Landmark: ' + role);
                });

                // 30. Lists.
                document.querySelectorAll('ol, ul, dl').forEach(list => {
                    addIcon(list, 'struct', 'List: <' + list.tagName.toLowerCase() + '>');
                });

                // 31. Search landmark.
                document.querySelectorAll('[role=""search""]').forEach(el => {
                    addIcon(el, 'struct', 'Search landmark');
                });

                // 32. Iframes.
                document.querySelectorAll('iframe').forEach(iframe => {
                    const title = iframe.getAttribute('title');
                    if (title) {
                        addIcon(iframe, 'struct', 'Iframe with title: ' + title.substring(0, 40));
                    } else {
                        addIcon(iframe, 'struct', 'Iframe without title');
                    }
                });

                // 33. Data tables.
                document.querySelectorAll('table').forEach(table => {
                    const hasHeaders = table.querySelector('th');
                    if (hasHeaders) {
                        addIcon(table, 'struct', 'Data table');
                    }
                });

                // ====================
                // --- ARIA (blue) ---
                // ====================

                // 34. General ARIA attributes.
                document.querySelectorAll('[aria-label], [aria-labelledby], [aria-describedby], [role]').forEach(el => {
                    const attrs = [];
                    if (el.getAttribute('role')) attrs.push('role=' + el.getAttribute('role'));
                    if (el.getAttribute('aria-label')) attrs.push('aria-label');
                    if (el.getAttribute('aria-labelledby')) attrs.push('aria-labelledby');
                    if (attrs.length > 0 && !el.matches('h1,h2,h3,h4,h5,h6,nav,main,header,footer,aside')) {
                        addIcon(el, 'aria', attrs.join(', '));
                    }
                });

                // 35. aria-live regions.
                document.querySelectorAll('[aria-live], [role=""alert""], [role=""status""]').forEach(el => {
                    const liveVal = el.getAttribute('aria-live') || el.getAttribute('role');
                    addIcon(el, 'aria', 'Live region: ' + liveVal);
                });

                // 36. aria-hidden.
                document.querySelectorAll('[aria-hidden=""true""]').forEach(el => {
                    addIcon(el, 'aria', 'aria-hidden=true');
                });

                // 37. aria-expanded.
                document.querySelectorAll('[aria-expanded]').forEach(el => {
                    addIcon(el, 'aria', 'aria-expanded=' + el.getAttribute('aria-expanded'));
                });

                // 38. aria-haspopup.
                document.querySelectorAll('[aria-haspopup]').forEach(el => {
                    addIcon(el, 'aria', 'aria-haspopup=' + el.getAttribute('aria-haspopup'));
                });

                // 39. aria-required.
                document.querySelectorAll('[aria-required=""true""]').forEach(el => {
                    addIcon(el, 'aria', 'aria-required=true');
                });

                // 40. aria-invalid.
                document.querySelectorAll('[aria-invalid=""true""]').forEach(el => {
                    addIcon(el, 'aria', 'aria-invalid=true');
                });

                // 41. aria-describedby present.
                document.querySelectorAll('[aria-describedby]').forEach(el => {
                    addIcon(el, 'aria', 'aria-describedby=' + el.getAttribute('aria-describedby'));
                });

                // 42. role=""presentation"" or role=""none"".
                document.querySelectorAll('[role=""presentation""], [role=""none""]').forEach(el => {
                    addIcon(el, 'aria', 'Decorative role: ' + el.getAttribute('role'));
                });

                // --- Render icons onto page ---
                const maxIcons = 200;
                const rendered = issues.slice(0, maxIcons);

                rendered.forEach((issue, idx) => {
                    const style = iconStyles[issue.type];
                    const icon = document.createElement('div');
                    icon.className = 'quickpeek-icon';
                    icon.style.cssText = `
                        position: absolute; z-index: ${100000 + idx};
                        width: 20px; height: 20px; border-radius: 50%;
                        background: ${style.bg}; color: white;
                        font-size: 12px; font-weight: bold; line-height: 20px;
                        text-align: center; cursor: default;
                        box-shadow: 0 1px 3px rgba(0,0,0,0.4);
                        pointer-events: none; font-family: sans-serif;
                    `;
                    icon.textContent = style.icon;
                    icon.title = `[${style.label}] ${issue.message}`;

                    try {
                        const rect = issue.el.getBoundingClientRect();
                        const scrollX = window.scrollX;
                        const scrollY = window.scrollY;
                        icon.style.left = `${rect.left + scrollX - 10}px`;
                        icon.style.top = `${rect.top + scrollY - 10}px`;
                        document.body.appendChild(icon);
                    } catch (e) { /* skip positioning errors */ }
                });

                // Summary banner.
                const counts = { error: 0, alert: 0, feature: 0, struct: 0, aria: 0 };
                issues.forEach(i => counts[i.type]++);

                const banner = document.createElement('div');
                banner.id = 'quickpeek-overlay-banner';
                banner.innerHTML = `
                    <div style=""
                        position:fixed; top:0; left:0; right:0; z-index:999999;
                        background: #1a1a2e; color: white;
                        padding: 8px 16px;
                        font: bold 13px/1.4 -apple-system, 'Segoe UI', Roboto, monospace;
                        display: flex; align-items: center; gap: 16px;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                    "">
                        <span style=""font-size:16px"">A11y</span>
                        <span>QuickPeek Evaluation</span>
                        <span style=""background:#dc3545;padding:2px 8px;border-radius:10px;font-size:11px"">${counts.error} Errors</span>
                        <span style=""background:#ffc107;color:#000;padding:2px 8px;border-radius:10px;font-size:11px"">${counts.alert} Alerts</span>
                        <span style=""background:#198754;padding:2px 8px;border-radius:10px;font-size:11px"">${counts.feature} Features</span>
                        <span style=""background:#6f42c1;padding:2px 8px;border-radius:10px;font-size:11px"">${counts.struct} Structural</span>
                        <span style=""background:#0d6efd;padding:2px 8px;border-radius:10px;font-size:11px"">${counts.aria} ARIA</span>
                        <span style=""margin-left:auto; opacity:0.6; font-size:11px"">FreeA11yChecker</span>
                    </div>
                `;
                document.body.prepend(banner);
                document.body.style.marginTop = (parseInt(document.body.style.marginTop || '0') + 40) + 'px';
            }
        ");
    }

    /// <summary>
    /// Remove the QuickPeek overlay icons and banner from the page.
    /// </summary>
    public static async Task RemoveOverlay(IPage page)
    {
        await page.EvaluateAsync(@"
            () => {
                document.querySelectorAll('.quickpeek-icon').forEach(el => el.remove());
                const banner = document.getElementById('quickpeek-overlay-banner');
                if (banner) banner.remove();
                document.body.style.marginTop = '';
            }
        ");
    }
}

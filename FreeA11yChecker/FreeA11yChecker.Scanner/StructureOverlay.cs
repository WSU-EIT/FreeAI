using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Structural overlay showing headings (h1-h6 with level labels), landmarks (nav, main,
/// header, footer, aside with role labels), focus/tab order (numbered), and skip links.
/// </summary>
public static class StructureOverlay
{
    /// <summary>
    /// Inject the structure overlay: heading badges, landmark labels, and tab-order markers.
    /// </summary>
    public static async Task InjectOverlay(IPage page)
    {
        await page.EvaluateAsync(@"
            () => {
                // ── Tab Order ──
                const focusable = Array.from(document.querySelectorAll(
                    'a[href], button:not([disabled]), input:not([disabled]):not([type=""hidden""]), ' +
                    'select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex=""-1""])'
                )).filter(el => {
                    const style = window.getComputedStyle(el);
                    return style.display !== 'none' && style.visibility !== 'hidden' && el.offsetParent !== null;
                });

                const withTabindex = focusable.filter(el => {
                    const ti = parseInt(el.getAttribute('tabindex') || '0');
                    return ti > 0;
                }).sort((a, b) => parseInt(a.getAttribute('tabindex')) - parseInt(b.getAttribute('tabindex')));

                const withoutTabindex = focusable.filter(el => {
                    const ti = parseInt(el.getAttribute('tabindex') || '0');
                    return ti <= 0;
                });

                const tabOrder = [...withTabindex, ...withoutTabindex];
                const maxTabMarkers = 100;

                tabOrder.slice(0, maxTabMarkers).forEach((el, idx) => {
                    try {
                        const rect = el.getBoundingClientRect();
                        if (rect.width === 0 || rect.height === 0) return;

                        const marker = document.createElement('div');
                        marker.className = 'structure-tab-marker';
                        marker.textContent = idx + 1;
                        marker.style.cssText = `
                            position: absolute; z-index: ${97000 + idx};
                            width: 22px; height: 22px; border-radius: 50%;
                            background: #0d6efd; color: white;
                            font-size: 10px; font-weight: 700; line-height: 22px;
                            text-align: center; pointer-events: none;
                            font-family: monospace;
                            box-shadow: 0 1px 3px rgba(0,0,0,0.4);
                            left: ${rect.left + window.scrollX - 11}px;
                            top: ${rect.top + window.scrollY - 11}px;
                        `;
                        document.body.appendChild(marker);
                        el.style.outline = '2px dashed #0d6efd';
                        el.style.outlineOffset = '1px';
                    } catch(e) { /* skip */ }
                });

                // ── Skip Links ──
                document.querySelectorAll('a[href^=""#""]').forEach(a => {
                    const text = (a.textContent || '').trim().toLowerCase();
                    if (text.includes('skip') || text.includes('main content')) {
                        try {
                            const rect = a.getBoundingClientRect();
                            const badge = document.createElement('div');
                            badge.className = 'structure-heading-badge';
                            badge.textContent = 'SKIP LINK: ' + (a.textContent || '').trim().substring(0, 30);
                            badge.style.cssText = `
                                position: absolute; z-index: 96500;
                                font-size: 11px; font-family: monospace; font-weight: 700;
                                color: white; padding: 2px 8px; border-radius: 3px;
                                white-space: nowrap; pointer-events: none;
                                background: #198754;
                                left: ${rect.left + window.scrollX}px;
                                top: ${rect.top + window.scrollY - 20}px;
                            `;
                            document.body.appendChild(badge);
                            a.style.outline = '2px solid #198754';
                            a.style.outlineOffset = '2px';
                        } catch(e) { /* skip */ }
                    }
                });

                // ── Heading Hierarchy ──
                const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
                const headingColors = {
                    H1: '#dc3545', H2: '#fd7e14', H3: '#ffc107',
                    H4: '#198754', H5: '#0d6efd', H6: '#6f42c1'
                };
                let lastLevel = 0;
                let headingIssues = 0;

                headings.forEach((h, idx) => {
                    const level = parseInt(h.tagName[1]);
                    const color = headingColors[h.tagName] || '#6c757d';
                    const skipped = level > lastLevel + 1 && lastLevel > 0;
                    if (skipped) headingIssues++;
                    lastLevel = level;

                    try {
                        const rect = h.getBoundingClientRect();
                        const indent = (level - 1) * 12;

                        const badge = document.createElement('div');
                        badge.className = 'structure-heading-badge';
                        badge.style.cssText = `
                            position: absolute; z-index: ${96000 + idx};
                            font-size: 11px; font-family: monospace; font-weight: 700;
                            color: white; padding: 2px 8px; border-radius: 3px;
                            white-space: nowrap; pointer-events: none;
                            line-height: 15px;
                            background: ${skipped ? '#dc3545' : color};
                            left: ${rect.left + window.scrollX + indent}px;
                            top: ${rect.top + window.scrollY - 20}px;
                        `;
                        badge.textContent = skipped
                            ? `\u26A0 ${h.tagName} (skipped level)`
                            : `${h.tagName}: ${(h.textContent || '').trim().substring(0, 40)}`;
                        document.body.appendChild(badge);
                        h.style.outline = `2px solid ${color}`;
                        h.style.outlineOffset = '2px';
                    } catch(e) { /* skip */ }
                });

                // ── Landmark Regions ──
                const landmarks = document.querySelectorAll('header, nav, main, aside, footer, [role=""banner""], [role=""navigation""], [role=""main""], [role=""complementary""], [role=""contentinfo""]');
                landmarks.forEach((lm, idx) => {
                    try {
                        const rect = lm.getBoundingClientRect();
                        const role = lm.getAttribute('role') || lm.tagName.toLowerCase();

                        lm.style.outline = '2px dotted #6f42c1';
                        lm.style.outlineOffset = '3px';

                        const label = document.createElement('div');
                        label.className = 'structure-landmark-label';
                        label.textContent = '\u25C6 ' + role;
                        label.style.cssText = `
                            position: absolute; z-index: ${95000 + idx};
                            font-size: 10px; font-family: monospace; font-weight: 700;
                            color: white; background: #6f42c1;
                            padding: 1px 6px; border-radius: 3px;
                            pointer-events: none;
                            left: ${rect.right + window.scrollX - 80}px;
                            top: ${rect.top + window.scrollY}px;
                        `;
                        document.body.appendChild(label);
                    } catch(e) { /* skip */ }
                });

                // Summary banner
                const banner = document.createElement('div');
                banner.id = 'structure-overlay-banner';
                banner.innerHTML = `
                    <div style=""
                        position:fixed; top:0; left:0; right:0; z-index:999999;
                        background: #1a2744; color: white; padding: 8px 16px;
                        font: bold 13px/1.4 -apple-system, 'Segoe UI', Roboto, monospace;
                        display: flex; align-items: center; gap: 16px;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                    "">
                        <span style=""font-size:14px"">\uD83C\uDFD7</span>
                        <span>Page Structure</span>
                        <span style=""background:#0d6efd;padding:2px 8px;border-radius:10px;font-size:11px"">\u21E5 ${Math.min(tabOrder.length, maxTabMarkers)}${tabOrder.length > maxTabMarkers ? '+' : ''} Tab Stops</span>
                        <span style=""background:${headingIssues > 0 ? '#dc3545' : '#198754'};padding:2px 8px;border-radius:10px;font-size:11px"">H ${headings.length} Headings${headingIssues > 0 ? ` (${headingIssues} skipped)` : ''}</span>
                        <span style=""background:#6f42c1;padding:2px 8px;border-radius:10px;font-size:11px"">\u25C6 ${landmarks.length} Landmarks</span>
                        <span style=""margin-left:auto; opacity:0.6; font-size:11px"">Tab Order \u00B7 Headings \u00B7 Landmarks</span>
                    </div>
                `;
                document.body.prepend(banner);
                document.body.style.marginTop = '40px';
            }
        ");
    }

    /// <summary>
    /// Remove the structure overlay markers, badges, and banner from the page.
    /// </summary>
    public static async Task RemoveOverlay(IPage page)
    {
        await page.EvaluateAsync(@"
            () => {
                document.querySelectorAll('.structure-tab-marker, .structure-heading-badge, .structure-landmark-label').forEach(el => el.remove());
                const banner = document.getElementById('structure-overlay-banner');
                if (banner) banner.remove();
                document.body.style.marginTop = '';
                document.querySelectorAll('[style*=""outline""]').forEach(el => {
                    el.style.outline = '';
                    el.style.outlineOffset = '';
                });
            }
        ");
    }
}

using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Screen reader text view. Replaces visual page content with a linearized accessible
/// content tree showing what a screen reader would announce: alt text for images,
/// link text, heading hierarchy, form labels, ARIA labels. Decorative elements are hidden.
/// </summary>
public static class ScreenReaderView
{
    /// <summary>
    /// Replace the page with a plain-text screen reader view.
    /// </summary>
    public static async Task InjectView(IPage page)
    {
        await page.EvaluateAsync(@"
            () => {
                // Save original body so we can restore later
                if (!window._srOriginalHtml) {
                    window._srOriginalHtml = document.body.innerHTML;
                    window._srOriginalStyles = document.body.getAttribute('style') || '';
                }

                const output = [];
                let imgCount = 0, altMissing = 0, ariaLabels = 0, landmarks = 0, headings = 0;

                function getAccessibleName(el) {
                    return el.getAttribute('aria-label')
                        || el.getAttribute('aria-labelledby')
                        || el.getAttribute('title')
                        || '';
                }

                function processNode(node, depth) {
                    if (node.nodeType === Node.TEXT_NODE) {
                        const text = (node.textContent || '').trim();
                        if (text) output.push({ type: 'text', content: text, depth });
                        return;
                    }
                    if (node.nodeType !== Node.ELEMENT_NODE) return;

                    const el = node;
                    const tag = el.tagName.toLowerCase();
                    const style = window.getComputedStyle(el);

                    // Skip hidden elements
                    if (style.display === 'none' || style.visibility === 'hidden') return;
                    if (el.getAttribute('aria-hidden') === 'true') return;

                    // Landmark annotations
                    const role = el.getAttribute('role');
                    const landmarkTags = { nav: 'navigation', main: 'main', header: 'banner', footer: 'contentinfo', aside: 'complementary' };
                    const landmarkRole = role || landmarkTags[tag];
                    if (landmarkRole && ['navigation', 'main', 'banner', 'contentinfo', 'complementary', 'search', 'form'].includes(landmarkRole)) {
                        output.push({ type: 'landmark', content: `[${landmarkRole.toUpperCase()}]`, depth });
                        landmarks++;
                    }

                    // Headings
                    if (/^h[1-6]$/.test(tag)) {
                        output.push({ type: 'heading', content: `[H${tag[1]}] ${(el.textContent || '').trim()}`, depth, level: tag[1] });
                        headings++;
                        return;
                    }

                    // Images
                    if (tag === 'img') {
                        imgCount++;
                        const alt = el.getAttribute('alt');
                        if (alt !== null && alt.trim() !== '') {
                            output.push({ type: 'img-alt', content: `[IMG: ${alt}]`, depth });
                        } else if (alt === '') {
                            output.push({ type: 'img-decorative', content: '[IMG: decorative]', depth });
                        } else {
                            output.push({ type: 'img-noalt', content: '[IMG: \u26A0 NO ALT TEXT]', depth });
                            altMissing++;
                        }
                        return;
                    }

                    // SVG / Icon elements
                    if (tag === 'svg' || tag === 'i' || (el.classList && el.classList.contains('icon'))) {
                        const name = getAccessibleName(el);
                        if (name) { output.push({ type: 'icon-labeled', content: `[ICON: ${name}]`, depth }); ariaLabels++; }
                        else if (el.getAttribute('aria-hidden') !== 'true') { output.push({ type: 'icon-unlabeled', content: '[ICON: unlabeled]', depth }); }
                        return;
                    }

                    // Links
                    if (tag === 'a') {
                        const text = (el.textContent || '').trim();
                        const ariaLabel = getAccessibleName(el);
                        const href = el.getAttribute('href') || '';
                        if (ariaLabel && !text) { output.push({ type: 'link', content: `[LINK: ${ariaLabel}] \u2192 ${href}`, depth }); ariaLabels++; }
                        else if (text) { output.push({ type: 'link', content: `[LINK: ${text}] \u2192 ${href}`, depth }); }
                        else { output.push({ type: 'link-empty', content: `[LINK: \u26A0 EMPTY] \u2192 ${href}`, depth }); }
                        return;
                    }

                    // Buttons
                    if (tag === 'button' || role === 'button') {
                        const text = (el.textContent || '').trim();
                        const ariaLabel = getAccessibleName(el);
                        const label = ariaLabel || text || '\u26A0 EMPTY';
                        output.push({ type: 'button', content: `[BUTTON: ${label}]`, depth });
                        if (ariaLabel) ariaLabels++;
                        return;
                    }

                    // Form inputs
                    if (['input', 'select', 'textarea'].includes(tag)) {
                        const inputType = el.getAttribute('type') || 'text';
                        if (inputType === 'hidden') return;
                        const ariaLabel = getAccessibleName(el);
                        const id = el.id;
                        const labelEl = id ? document.querySelector('label[for=""' + id + '""]') : null;
                        const labelText = ariaLabel || (labelEl ? labelEl.textContent.trim() : '') || el.placeholder || '\u26A0 NO LABEL';
                        output.push({ type: 'input', content: `[INPUT ${inputType.toUpperCase()}: ${labelText}]`, depth });
                        return;
                    }

                    // Table
                    if (tag === 'table') {
                        const caption = el.querySelector('caption');
                        output.push({ type: 'table', content: `[TABLE${caption ? ': ' + caption.textContent.trim() : ''}]`, depth });
                    }

                    // ARIA labels on generic elements
                    if (role && !['presentation', 'none'].includes(role)) {
                        const ariaLabel = getAccessibleName(el);
                        if (ariaLabel) { output.push({ type: 'aria', content: `[${role.toUpperCase()}: ${ariaLabel}]`, depth }); ariaLabels++; }
                    }

                    // Process children
                    for (const child of el.childNodes) { processNode(child, depth + 1); }

                    // Close landmarks
                    if (landmarkRole && ['navigation', 'main', 'banner', 'contentinfo', 'complementary'].includes(landmarkRole)) {
                        output.push({ type: 'landmark-end', content: `[/${landmarkRole.toUpperCase()}]`, depth });
                    }
                }

                processNode(document.body, 0);

                // Build text view HTML
                const typeStyles = {
                    'text':           'color:#e0e0e0;',
                    'heading':        'color:#58a6ff; font-weight:bold; font-size:{size}px;',
                    'link':           'color:#8b949e; text-decoration:underline;',
                    'link-empty':     'color:#f85149; text-decoration:underline;',
                    'button':         'color:#d2a8ff; font-weight:600;',
                    'img-alt':        'color:#7ee787; font-style:italic;',
                    'img-decorative': 'color:#6e7681; font-style:italic;',
                    'img-noalt':      'color:#f85149; font-weight:bold;',
                    'icon-labeled':   'color:#79c0ff; font-style:italic;',
                    'icon-unlabeled': 'color:#f0883e;',
                    'input':          'color:#d2a8ff;',
                    'table':          'color:#79c0ff; font-weight:600;',
                    'landmark':       'color:#f0883e; font-weight:bold; font-size:13px;',
                    'landmark-end':   'color:#f0883e; font-size:11px; opacity:0.6;',
                    'aria':           'color:#79c0ff; font-style:italic;'
                };
                const headingSizes = { '1': 22, '2': 18, '3': 15, '4': 13, '5': 12, '6': 11 };

                let html = '';
                for (const item of output) {
                    let style = typeStyles[item.type] || 'color:#e0e0e0;';
                    if (item.type === 'heading') style = style.replace('{size}', headingSizes[item.level] || '13');
                    const indent = Math.min(item.depth, 10) * 12;
                    const escaped = item.content.replace(/</g, '&lt;').replace(/>/g, '&gt;');
                    html += `<div style=""padding:1px 0 1px ${indent}px; ${style} font-family:'Cascadia Code','Fira Code',Consolas,monospace; font-size:12px; line-height:1.6; white-space:pre-wrap; word-break:break-word;"">${escaped}</div>\n`;
                }

                document.body.innerHTML = `
                    <div id=""sr-view-banner"" style=""
                        position:fixed; top:0; left:0; right:0; z-index:999999;
                        background:#0d1117; color:white; padding:8px 16px;
                        font: bold 13px/1.4 -apple-system,'Segoe UI',Roboto,monospace;
                        display:flex; align-items:center; gap:16px;
                        box-shadow:0 2px 8px rgba(0,0,0,0.5);
                        border-bottom: 2px solid #30363d;
                    "">
                        <span style=""font-size:14px"">SR</span>
                        <span>Screen Reader Text View</span>
                        <span style=""background:#58a6ff;padding:2px 8px;border-radius:10px;font-size:11px"">H ${headings}</span>
                        <span style=""background:${altMissing > 0 ? '#f85149' : '#7ee787'};color:${altMissing > 0 ? 'white' : '#000'};padding:2px 8px;border-radius:10px;font-size:11px"">${imgCount} imgs${altMissing > 0 ? ` (${altMissing} no alt!)` : ''}</span>
                        <span style=""background:#d2a8ff;color:#000;padding:2px 8px;border-radius:10px;font-size:11px"">${ariaLabels} ARIA labels</span>
                        <span style=""background:#f0883e;color:#000;padding:2px 8px;border-radius:10px;font-size:11px"">\u25C6 ${landmarks} landmarks</span>
                        <span style=""margin-left:auto;opacity:0.5;font-size:11px"">Linearized accessible content tree</span>
                    </div>
                    <div style=""
                        margin-top:44px; padding:16px 24px;
                        background:#0d1117; min-height:100vh;
                    "">${html}</div>
                `;
                document.body.style.cssText = 'margin:0; padding:0; background:#0d1117;';
            }
        ");
    }

    /// <summary>
    /// Restore the original page content after the screen reader view.
    /// </summary>
    public static async Task RemoveView(IPage page)
    {
        await page.EvaluateAsync(@"
            () => {
                if (window._srOriginalHtml) {
                    document.body.innerHTML = window._srOriginalHtml;
                    document.body.setAttribute('style', window._srOriginalStyles || '');
                    window._srOriginalHtml = null;
                    window._srOriginalStyles = null;
                }
            }
        ");
    }
}

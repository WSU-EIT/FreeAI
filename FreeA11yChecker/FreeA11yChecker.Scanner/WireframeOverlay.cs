using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Renders the page as a "wireframe blueprint" — color-coded landmark/role outlines,
/// element-type badges via ::before, exaggerated borders + padding so layout structure
/// is visible at a glance. Inspired by Claude designer-style raw-structure renderings.
/// Inject CSS, screenshot, remove. Best-effort — failures are silent.
/// </summary>
public static class WireframeOverlay
{
    private const string StyleId = "a11y-wireframe-overlay";

    public static async Task InjectOverlay(IPage page)
    {
        try {
            await page.EvaluateAsync($@"() => {{
                const old = document.getElementById('{StyleId}');
                if (old) old.remove();
                const s = document.createElement('style');
                s.id = '{StyleId}';
                s.textContent = `
/* Reset background & typography for blueprint feel */
html, body {{ background: #fafbfc !important; font-family: 'Segoe UI', system-ui, sans-serif !important; }}

/* Generic container outlining — 1px gray, exaggerated padding/margin so gaps are visible */
section, article, aside, div, ul, ol, li, table, tbody, thead, tr, form, fieldset {{
    outline: 1px solid rgba(120,144,156,0.45) !important;
    outline-offset: -1px !important;
    background: rgba(207,216,220,0.06) !important;
    padding: 6px !important;
    margin: 3px !important;
    min-height: 16px !important;
    box-sizing: border-box !important;
}}

/* Landmark roles — strong colored outlines + filled tints */
header, [role='banner'] {{
    outline: 3px solid #1976d2 !important; background: rgba(25,118,210,0.12) !important;
    padding: 12px !important; position: relative !important;
}}
nav, [role='navigation'] {{
    outline: 3px solid #7b1fa2 !important; background: rgba(123,31,162,0.12) !important;
    padding: 12px !important; position: relative !important;
}}
main, [role='main'] {{
    outline: 3px solid #f57c00 !important; background: rgba(245,124,0,0.10) !important;
    padding: 12px !important; position: relative !important;
}}
aside, [role='complementary'] {{
    outline: 3px dashed #c2185b !important; background: rgba(194,24,91,0.10) !important;
    padding: 12px !important; position: relative !important;
}}
footer, [role='contentinfo'] {{
    outline: 3px solid #388e3c !important; background: rgba(56,142,60,0.10) !important;
    padding: 12px !important; position: relative !important;
}}
[role='search'], search {{
    outline: 3px solid #00897b !important; background: rgba(0,137,123,0.12) !important;
    padding: 12px !important; position: relative !important;
}}
[role='region'], section[aria-label], section[aria-labelledby] {{
    outline: 3px dotted #5d4037 !important; background: rgba(93,64,55,0.06) !important;
    padding: 10px !important;
}}

/* Headings — vivid bars so hierarchy is obvious */
h1 {{ background: #b71c1c !important; color: #fff !important; padding: 14px !important; font-size: 26px !important; margin: 8px 0 !important; }}
h2 {{ background: #d32f2f !important; color: #fff !important; padding: 12px !important; font-size: 22px !important; margin: 6px 0 !important; }}
h3 {{ background: #e57373 !important; color: #fff !important; padding: 10px !important; font-size: 18px !important; margin: 5px 0 !important; }}
h4 {{ background: #ef9a9a !important; color: #b71c1c !important; padding: 8px !important; font-size: 16px !important; }}
h5, h6 {{ background: #ffcdd2 !important; color: #b71c1c !important; padding: 6px !important; font-size: 14px !important; }}

/* Interactive */
button, [role='button'] {{
    background: #ff9800 !important; color: #1a1a1a !important; border: 2px solid #e65100 !important;
    padding: 8px 14px !important; font-weight: 700 !important; border-radius: 4px !important;
}}
a, [role='link'] {{ color: #0d47a1 !important; text-decoration: underline !important; font-weight: 600 !important; }}
input:not([type='hidden']), textarea, select {{
    background: #fff !important; border: 2px solid #424242 !important; padding: 8px !important;
    min-height: 32px !important; min-width: 80px !important; border-radius: 3px !important;
}}
input[type='checkbox'], input[type='radio'] {{ min-height: 18px !important; min-width: 18px !important; padding: 0 !important; }}

/* Tables — make rows obvious */
th {{ background: #455a64 !important; color: #fff !important; padding: 8px !important; }}
td {{ background: #fff !important; padding: 6px !important; outline: 1px solid #b0bec5 !important; }}

/* Images — show as labeled boxes */
img {{
    background: repeating-linear-gradient(45deg, #e0e0e0, #e0e0e0 10px, #f5f5f5 10px, #f5f5f5 20px) !important;
    outline: 2px solid #607d8b !important;
    min-height: 60px !important; min-width: 80px !important;
}}

/* Hide non-interactive icons (font-awesome / bootstrap-icons) so they don't clutter */
.fa, .fas, .far, .fab, .fal, .fad, .bi, .material-icons {{ opacity: 0.35 !important; }}

/* Landmark labels via pseudo-element */
header::before, [role='banner']::before {{
    content: 'HEADER' !important; position: absolute !important; top: -10px !important; left: 8px !important;
    background: #1976d2 !important; color: #fff !important; padding: 1px 6px !important; font-size: 10px !important;
    font-weight: 700 !important; letter-spacing: 1px !important; border-radius: 2px !important; z-index: 9999 !important;
}}
nav::before, [role='navigation']::before {{
    content: 'NAV' !important; position: absolute !important; top: -10px !important; left: 8px !important;
    background: #7b1fa2 !important; color: #fff !important; padding: 1px 6px !important; font-size: 10px !important;
    font-weight: 700 !important; letter-spacing: 1px !important; border-radius: 2px !important; z-index: 9999 !important;
}}
main::before, [role='main']::before {{
    content: 'MAIN' !important; position: absolute !important; top: -10px !important; left: 8px !important;
    background: #f57c00 !important; color: #fff !important; padding: 1px 6px !important; font-size: 10px !important;
    font-weight: 700 !important; letter-spacing: 1px !important; border-radius: 2px !important; z-index: 9999 !important;
}}
aside::before, [role='complementary']::before {{
    content: 'ASIDE' !important; position: absolute !important; top: -10px !important; left: 8px !important;
    background: #c2185b !important; color: #fff !important; padding: 1px 6px !important; font-size: 10px !important;
    font-weight: 700 !important; letter-spacing: 1px !important; border-radius: 2px !important; z-index: 9999 !important;
}}
footer::before, [role='contentinfo']::before {{
    content: 'FOOTER' !important; position: absolute !important; top: -10px !important; left: 8px !important;
    background: #388e3c !important; color: #fff !important; padding: 1px 6px !important; font-size: 10px !important;
    font-weight: 700 !important; letter-spacing: 1px !important; border-radius: 2px !important; z-index: 9999 !important;
}}

/* Show alt text inside images */
img[alt]::after {{ content: 'IMG: ' attr(alt) !important; }}
img:not([alt])::after {{ content: '⚠ IMG (no alt)' !important; color: #b71c1c !important; font-weight: 700 !important; }}
                `;
                document.head.appendChild(s);
            }}");
        } catch { /* best effort */ }
    }

    public static async Task RemoveOverlay(IPage page)
    {
        try {
            await page.EvaluateAsync($@"() => {{
                const s = document.getElementById('{StyleId}');
                if (s) s.remove();
            }}");
        } catch { }
    }
}

using System.Text.Json;
using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Tests WCAG 1.4.12 Text Spacing by injecting overridden CSS spacing properties
/// and detecting elements that clip or overflow as a result.
/// The four required overrides: line-height 1.5×, paragraph spacing 2×, letter-spacing 0.12em, word-spacing 0.16em.
/// </summary>
public static class TextSpacingTest
{
    /// <summary>
    /// Injects WCAG 1.4.12 text spacing overrides and detects elements with overflow clipping.
    /// Returns JSON result and optionally captures a screenshot.
    /// </summary>
    public static async Task<string> Run(IPage page)
    {
        try {
            // Step 1: Record baseline metrics for key elements before injecting spacing.
            string baseline = await page.EvaluateAsync<string>(@"() => {
                const els = document.querySelectorAll('p, span, div, li, td, th, a, button, label, h1, h2, h3, h4, h5, h6');
                const map = {};
                let i = 0;
                els.forEach(el => {
                    if (i > 500) return;
                    const rect = el.getBoundingClientRect();
                    if (rect.width === 0 || rect.height === 0) return;
                    const key = el.tagName + '_' + i;
                    map[key] = { w: rect.width, h: rect.height, sw: el.scrollWidth, sh: el.scrollHeight };
                    el.dataset.a11ySpacingId = key;
                    i++;
                });
                return JSON.stringify(map);
            }") ?? "{}";

            // Step 2: Inject WCAG 1.4.12 text spacing overrides.
            await page.EvaluateAsync(@"() => {
                const style = document.createElement('style');
                style.id = 'a11y-text-spacing-override';
                style.textContent = `
                    * {
                        line-height: 1.5 !important;
                        letter-spacing: 0.12em !important;
                        word-spacing: 0.16em !important;
                    }
                    p {
                        margin-bottom: 2em !important;
                    }
                `;
                document.head.appendChild(style);
            }");

            // Small delay for reflow.
            await page.WaitForTimeoutAsync(200);

            // Step 3: Compare post-injection metrics, detect clipping/overflow.
            string resultJson = await page.EvaluateAsync<string>(@"(baselineStr) => {
                const baseline = JSON.parse(baselineStr);
                const clipped = [];
                const overflowed = [];

                document.querySelectorAll('[data-a11y-spacing-id]').forEach(el => {
                    const key = el.dataset.a11ySpacingId;
                    const before = baseline[key];
                    if (!before) return;

                    const cs = window.getComputedStyle(el);
                    const rect = el.getBoundingClientRect();

                    // Detect overflow: scrollWidth/scrollHeight grew larger than the element bounds.
                    const hasOverflow = el.scrollWidth > rect.width + 2 || el.scrollHeight > rect.height + 2;
                    const isClipped = (cs.overflow === 'hidden' || cs.overflowX === 'hidden' || cs.overflowY === 'hidden')
                        && hasOverflow;

                    let selector = el.tagName.toLowerCase();
                    if (el.id) selector += '#' + el.id;
                    else if (el.className && typeof el.className === 'string')
                        selector += '.' + el.className.trim().split(/\s+/)[0];

                    const text = el.textContent.trim().substring(0, 80);

                    if (isClipped) {
                        clipped.push({ selector, text, overflow: cs.overflow });
                    } else if (hasOverflow) {
                        overflowed.push({ selector, text, scrollWidthDelta: el.scrollWidth - rect.width, scrollHeightDelta: el.scrollHeight - rect.height });
                    }
                });

                return JSON.stringify({ clippedElements: clipped, overflowElements: overflowed });
            }", baseline) ?? "{}";

            // Step 4: Clean up — remove override style and data attributes.
            await page.EvaluateAsync(@"() => {
                const s = document.getElementById('a11y-text-spacing-override');
                if (s) s.remove();
                document.querySelectorAll('[data-a11y-spacing-id]').forEach(el => {
                    delete el.dataset.a11ySpacingId;
                });
            }");

            return resultJson;
        } catch {
            return "{}";
        }
    }
}

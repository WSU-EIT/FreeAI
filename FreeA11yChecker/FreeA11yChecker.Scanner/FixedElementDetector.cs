using System.Text.Json;
using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Detects elements with position:fixed or position:sticky that could obscure
/// focused content or reduce the visible viewport area.
/// WCAG 2.4.11 (AAA) — Focus Not Obscured: focused elements should not be hidden
/// by author-created content.
/// </summary>
public static class FixedElementDetector
{
    /// <summary>
    /// Finds all fixed/sticky positioned elements and measures their viewport coverage.
    /// Returns JSON array of detected elements.
    /// </summary>
    public static async Task<string> Run(IPage page)
    {
        try {
            string resultJson = await page.EvaluateAsync<string>(@"() => {
                const results = [];
                const viewportWidth = window.innerWidth;
                const viewportHeight = window.innerHeight;
                const viewportArea = viewportWidth * viewportHeight;

                // Walk all elements and check computed position.
                const allElements = document.querySelectorAll('*');
                allElements.forEach(el => {
                    const cs = window.getComputedStyle(el);
                    const position = cs.position;
                    if (position !== 'fixed' && position !== 'sticky') return;

                    const rect = el.getBoundingClientRect();
                    if (rect.width === 0 || rect.height === 0) return;

                    // Calculate viewport coverage percentage.
                    const elArea = rect.width * rect.height;
                    const coversPercent = Math.round((elArea / viewportArea) * 1000) / 10;

                    let selector = el.tagName.toLowerCase();
                    if (el.id) selector += '#' + el.id;
                    else if (el.className && typeof el.className === 'string')
                        selector += '.' + el.className.trim().split(/\s+/)[0];

                    // Determine if the element is likely a header/footer/nav bar or popup.
                    const role = el.getAttribute('role') || '';
                    const tag = el.tagName.toLowerCase();

                    results.push({
                        selector,
                        position,
                        tag,
                        role,
                        width: Math.round(rect.width),
                        height: Math.round(rect.height),
                        top: Math.round(rect.top),
                        left: Math.round(rect.left),
                        coversPercent,
                        zIndex: cs.zIndex === 'auto' ? 0 : parseInt(cs.zIndex) || 0
                    });
                });

                // Sort by coverage descending.
                results.sort((a, b) => b.coversPercent - a.coversPercent);

                return JSON.stringify(results);
            }") ?? "[]";

            return resultJson;
        } catch {
            return "[]";
        }
    }
}

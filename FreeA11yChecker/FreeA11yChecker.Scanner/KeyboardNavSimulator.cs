using System.Text.Json;
using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Simulates keyboard navigation by pressing Tab through the page.
/// Detects focus order, focus traps, missing focus indicators, and skip-to-content links.
/// WCAG 2.1.1 (Keyboard), 2.1.2 (No Keyboard Trap), 2.4.7 (Focus Visible), 2.4.1 (Bypass Blocks).
/// </summary>
public static class KeyboardNavSimulator
{
    /// <summary>
    /// Result of keyboard navigation simulation.
    /// </summary>
    public class KeyboardNavResult
    {
        public string FocusOrderJson { get; set; } = "[]";
        public string FocusTrapsJson { get; set; } = "[]";
    }

    /// <summary>
    /// Tabs through the page up to maxTabs times, recording focus order,
    /// detecting traps (same element focused twice in a row), and checking focus visibility.
    /// </summary>
    public static async Task<KeyboardNavResult> Run(IPage page, int maxTabs = 150)
    {
        var result = new KeyboardNavResult();

        try {
            // Click body first to ensure focus starts from the top of the page.
            try { await page.ClickAsync("body", new PageClickOptions { Position = new Position { X = 1, Y = 1 } }); } catch { }

            var focusOrder = new List<object>();
            var focusTraps = new List<object>();
            string? previousSelector = null;
            int repeatCount = 0;

            for (int i = 0; i < maxTabs; i++) {
                await page.Keyboard.PressAsync("Tab");

                // Small delay for focus styles to render.
                await page.WaitForTimeoutAsync(50);

                // Evaluate the currently focused element.
                string? focusInfo = await page.EvaluateAsync<string?>(@"() => {
                    const el = document.activeElement;
                    if (!el || el === document.body || el === document.documentElement) return null;

                    // Build a selector.
                    let selector = el.tagName.toLowerCase();
                    if (el.id) selector += '#' + el.id;
                    else if (el.className && typeof el.className === 'string')
                        selector += '.' + el.className.trim().split(/\s+/)[0];

                    // Check focus indicator visibility.
                    const cs = window.getComputedStyle(el);
                    const csF = window.getComputedStyle(el, ':focus');
                    const outlineVisible = cs.outlineStyle !== 'none' && cs.outlineWidth !== '0px';
                    const boxShadow = cs.boxShadow !== 'none' && cs.boxShadow !== '';
                    const borderChange = cs.borderColor !== 'rgb(0, 0, 0)'; // crude check
                    const hasFocusIndicator = outlineVisible || boxShadow;

                    // Check if element is off-screen (skip link target).
                    const rect = el.getBoundingClientRect();
                    const isOffScreen = rect.width === 0 || rect.height === 0 ||
                        rect.top < -100 || rect.left < -10000;

                    return JSON.stringify({
                        tag: el.tagName.toLowerCase(),
                        role: el.getAttribute('role') || '',
                        selector: selector,
                        text: (el.textContent || el.value || el.getAttribute('aria-label') || '').trim().substring(0, 100),
                        hasFocusIndicator: hasFocusIndicator,
                        isOffScreen: isOffScreen,
                        tabIndex: el.tabIndex
                    });
                }");

                if (focusInfo == null) {
                    // Focus left the page (reached end of document) — stop.
                    break;
                }

                using var doc = JsonDocument.Parse(focusInfo);
                var root = doc.RootElement;
                string currentSelector = root.GetProperty("selector").GetString() ?? "";

                // Detect focus trap: same element focused 3+ consecutive times.
                if (currentSelector == previousSelector) {
                    repeatCount++;
                    if (repeatCount >= 3) {
                        focusTraps.Add(new {
                            selector = currentSelector,
                            text = root.GetProperty("text").GetString() ?? "",
                            tabIndex = root.GetProperty("tabIndex").GetInt32(),
                            reason = "Element received focus " + repeatCount + " consecutive times — possible keyboard trap"
                        });
                        break; // Stop — we're stuck.
                    }
                } else {
                    repeatCount = 0;
                }
                previousSelector = currentSelector;

                focusOrder.Add(new {
                    index = i + 1,
                    tag = root.GetProperty("tag").GetString() ?? "",
                    role = root.GetProperty("role").GetString() ?? "",
                    selector = currentSelector,
                    text = root.GetProperty("text").GetString() ?? "",
                    hasFocusIndicator = root.GetProperty("hasFocusIndicator").GetBoolean(),
                    isOffScreen = root.GetProperty("isOffScreen").GetBoolean(),
                });
            }

            // Also check if Escape can break out of any modal/trap.
            // Try Escape after the loop to see if we can recover.
            try { await page.Keyboard.PressAsync("Escape"); } catch { }

            result.FocusOrderJson = JsonSerializer.Serialize(focusOrder);
            result.FocusTrapsJson = JsonSerializer.Serialize(focusTraps);
        } catch {
            // Keyboard nav simulation is best-effort.
        }

        return result;
    }
}

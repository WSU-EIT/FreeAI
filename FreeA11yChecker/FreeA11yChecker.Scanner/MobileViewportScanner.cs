using System.Text.Json;
using Microsoft.Playwright;
using FreeA11yChecker.Scanner.Models;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Re-scans a page at multiple viewport widths to detect responsive accessibility issues.
/// Tests phone (375×667), tablet (768×1024), and desktop (1920×1080) viewports.
/// Captures violation counts and target size issues at each viewport.
/// </summary>
public static class MobileViewportScanner
{
    /// <summary>
    /// Viewport definition for testing.
    /// </summary>
    private record ViewportDef(string Name, int Width, int Height);

    private static readonly ViewportDef[] Viewports = new[] {
        new ViewportDef("phone", 375, 667),
        new ViewportDef("tablet", 768, 1024),
        new ViewportDef("desktop", 1920, 1080),
    };

    /// <summary>
    /// Re-scans the page at 3 viewports. For each, resizes the viewport, reloads,
    /// runs a lightweight axe-core check, and captures target size issues.
    /// Returns JSON array of viewport results and adds screenshots to the PageScanResult.
    /// </summary>
    public static async Task<string> Run(IPage page, ScanConfig config, PageScanResult result)
    {
        var viewportResults = new List<object>();

        // Save original viewport size to restore later.
        var originalViewport = page.ViewportSize;

        try {
            string cacheDir = System.IO.Path.Combine(AppContext.BaseDirectory, "cache");

            foreach (var vp in Viewports) {
                try {
                    // Resize viewport.
                    await page.SetViewportSizeAsync(vp.Width, vp.Height);

                    // Reload page at new viewport (some pages serve different content).
                    await page.ReloadAsync(new PageReloadOptions {
                        WaitUntil = WaitUntilState.Load,
                        Timeout = config.TimeoutMs,
                    });
                    if (config.SettleDelayMs > 0) {
                        await page.WaitForTimeoutAsync(config.SettleDelayMs);
                    }

                    // Run lightweight axe-core to get violation counts.
                    int violationCount = 0, criticalCount = 0, seriousCount = 0;
                    try {
                        var axeResult = await AxeCoreRunner.RunFull(page, cacheDir);
                        violationCount = axeResult.Issues.Count;
                        criticalCount = axeResult.Issues.Count(i => i.Severity == "critical");
                        seriousCount = axeResult.Issues.Count(i => i.Severity == "serious");
                    } catch { }

                    // Check target size at this viewport.
                    int targetSizeIssues = 0;
                    try {
                        string tsJson = await page.EvaluateAsync<string>(@"() => {
                            let count = 0;
                            const interactive = 'a[href], button, input, select, textarea, [role=""button""], [role=""link""], [tabindex]';
                            document.querySelectorAll(interactive).forEach(el => {
                                const rect = el.getBoundingClientRect();
                                if (rect.width > 0 && rect.height > 0 && (rect.width < 24 || rect.height < 24)) count++;
                            });
                            return count.toString();
                        }") ?? "0";
                        targetSizeIssues = int.Parse(tsJson);
                    } catch { }

                    // Take a screenshot at this viewport.
                    try {
                        byte[] vpScreenshot = await page.ScreenshotAsync(new PageScreenshotOptions {
                            FullPage = false,
                            Type = ScreenshotType.Jpeg,
                            Quality = 80,
                        });
                        result.Screenshots.Add(new ScreenshotInfo {
                            Path = "vp-" + vp.Name + "-" + vp.Width + "x" + vp.Height + ".jpeg",
                            Label = "viewport-" + vp.Name,
                            SizeBytes = vpScreenshot.Length,
                            Data = vpScreenshot,
                            ContentType = "image/jpeg",
                        });
                    } catch { }

                    viewportResults.Add(new {
                        viewport = vp.Name,
                        width = vp.Width,
                        height = vp.Height,
                        violationCount,
                        critical = criticalCount,
                        serious = seriousCount,
                        targetSizeIssues
                    });
                } catch {
                    viewportResults.Add(new {
                        viewport = vp.Name,
                        width = vp.Width,
                        height = vp.Height,
                        violationCount = -1,
                        critical = 0,
                        serious = 0,
                        targetSizeIssues = 0
                    });
                }
            }
        } finally {
            // Restore original viewport.
            if (originalViewport != null) {
                try {
                    await page.SetViewportSizeAsync(originalViewport.Width, originalViewport.Height);
                    // Reload back to original state.
                    await page.ReloadAsync(new PageReloadOptions {
                        WaitUntil = WaitUntilState.Load,
                        Timeout = config.TimeoutMs,
                    });
                } catch { }
            }
        }

        return JsonSerializer.Serialize(viewportResults);
    }
}

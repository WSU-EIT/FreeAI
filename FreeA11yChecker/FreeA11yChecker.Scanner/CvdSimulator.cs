using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Color Vision Deficiency (CVD) simulator. Uses Chromium's built-in
/// Emulation.setEmulatedVisionDeficiency CDP command for accurate rendering-level
/// color blindness simulation, then captures a screenshot with a label banner.
/// </summary>
public static class CvdSimulator
{
    /// <summary>
    /// CVD types mapped to Chromium's emulatedVisionDeficiency CDP parameter values and display labels.
    /// See: https://chromedevtools.github.io/devtools-protocol/tot/Emulation/#method-setEmulatedVisionDeficiency
    /// </summary>
    private static readonly Dictionary<string, (string Label, string CdpType)> CvdTypes = new()
    {
        ["protanopia"]   = ("Protanopia (No Red)",                  "protanopia"),
        ["deuteranopia"] = ("Deuteranopia (No Green)",              "deuteranopia"),
        ["tritanopia"]   = ("Tritanopia (No Blue)",                 "tritanopia"),
        ["achromatopsia"]= ("Achromatopsia (Total Color Blindness)","achromatopsia"),
        ["protanomaly"]  = ("Protanomaly (Weak Red)",               "protanopia"),
        ["deuteranomaly"]= ("Deuteranomaly (Weak Green)",           "deuteranopia"),
        ["tritanomaly"]  = ("Tritanomaly (Weak Blue)",              "tritanopia"),
    };

    /// <summary>
    /// Returns all supported CVD simulation type names.
    /// </summary>
    public static string[] GetAllTypes() =>
        ["protanopia", "deuteranopia", "tritanopia", "achromatopsia", "protanomaly", "deuteranomaly", "tritanomaly"];

    /// <summary>
    /// Emulate a CVD type via CDP, inject a label banner, take a full-page screenshot, then reset.
    /// Returns in-memory byte[] instead of writing to disk.
    /// </summary>
    public static async Task<byte[]?> SimulateAndScreenshotBytes(IPage page, string type)
    {
        if (!CvdTypes.TryGetValue(type, out var cvd))
            throw new ArgumentException($"Unknown CVD type: {type}. Valid types: {string.Join(", ", GetAllTypes())}");

        try
        {
            var cdpSession = await page.Context.NewCDPSessionAsync(page);
            await cdpSession.SendAsync("Emulation.setEmulatedVisionDeficiency",
                new Dictionary<string, object> { ["type"] = cvd.CdpType });

            await InjectBanner(page, cvd.Label);

            byte[] data = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = true,
            });

            await cdpSession.SendAsync("Emulation.setEmulatedVisionDeficiency",
                new Dictionary<string, object> { ["type"] = "none" });

            return data;
        }
        catch
        {
            return null;
        }
        finally
        {
            await RemoveBanner(page);
        }
    }

    /// <summary>
    /// Captures a screenshot with prefers-reduced-motion: reduce emulated via CDP.
    /// Returns in-memory byte[].
    /// </summary>
    public static async Task<byte[]?> CaptureReducedMotionScreenshot(IPage page)
    {
        try
        {
            var cdpSession = await page.Context.NewCDPSessionAsync(page);
            await cdpSession.SendAsync("Emulation.setEmulatedMedia",
                new Dictionary<string, object> {
                    ["features"] = new[] { new Dictionary<string, string> { ["name"] = "prefers-reduced-motion", ["value"] = "reduce" } }
                });

            await InjectBanner(page, "prefers-reduced-motion: reduce");

            byte[] data = await page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });

            // Reset.
            await cdpSession.SendAsync("Emulation.setEmulatedMedia",
                new Dictionary<string, object> {
                    ["features"] = new[] { new Dictionary<string, string> { ["name"] = "prefers-reduced-motion", ["value"] = "" } }
                });

            return data;
        }
        catch
        {
            return null;
        }
        finally
        {
            await RemoveBanner(page);
        }
    }

    /// <summary>
    /// Captures a screenshot with forced-colors: active emulated via CDP.
    /// Returns in-memory byte[].
    /// </summary>
    public static async Task<byte[]?> CaptureForcedColorsScreenshot(IPage page)
    {
        try
        {
            var cdpSession = await page.Context.NewCDPSessionAsync(page);
            await cdpSession.SendAsync("Emulation.setEmulatedMedia",
                new Dictionary<string, object> {
                    ["features"] = new[] { new Dictionary<string, string> { ["name"] = "forced-colors", ["value"] = "active" } }
                });

            await InjectBanner(page, "Forced Colors (High Contrast)");

            byte[] data = await page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });

            // Reset.
            await cdpSession.SendAsync("Emulation.setEmulatedMedia",
                new Dictionary<string, object> {
                    ["features"] = new[] { new Dictionary<string, string> { ["name"] = "forced-colors", ["value"] = "" } }
                });

            return data;
        }
        catch
        {
            return null;
        }
        finally
        {
            await RemoveBanner(page);
        }
    }

    /// <summary>
    /// Emulate a CVD type via CDP, inject a label banner, take a full-page screenshot, then reset.
    /// </summary>
    public static async Task SimulateAndScreenshot(IPage page, string type, string outputPath)
    {
        if (!CvdTypes.TryGetValue(type, out var cvd))
            throw new ArgumentException($"Unknown CVD type: {type}. Valid types: {string.Join(", ", GetAllTypes())}");

        try
        {
            // Use CDP to emulate vision deficiency at the rendering level.
            var cdpSession = await page.Context.NewCDPSessionAsync(page);
            await cdpSession.SendAsync("Emulation.setEmulatedVisionDeficiency",
                new Dictionary<string, object> { ["type"] = cvd.CdpType });

            // Add a label banner so the screenshot is clearly identified.
            await InjectBanner(page, cvd.Label);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = outputPath,
                FullPage = true,
            });

            // Reset vision deficiency emulation.
            await cdpSession.SendAsync("Emulation.setEmulatedVisionDeficiency",
                new Dictionary<string, object> { ["type"] = "none" });
        }
        finally
        {
            await RemoveBanner(page);
        }
    }

    private static async Task InjectBanner(IPage page, string label)
    {
        await page.EvaluateAsync($@"
            () => {{
                const prevBanner = document.getElementById('cvd-overlay-banner');
                if (prevBanner) prevBanner.remove();

                const banner = document.createElement('div');
                banner.id = 'cvd-overlay-banner';
                banner.innerHTML = `
                    <div style=""
                        position:fixed; top:0; left:0; right:0; z-index:999999;
                        background: #2d2d2d; color: white; padding: 6px 16px;
                        font: bold 12px/1.4 -apple-system, 'Segoe UI', Roboto, monospace;
                        display: flex; align-items: center; gap: 12px;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                    "">
                        <span style=""font-size:14px"">CVD</span>
                        <span style=""font-size:13px"">{label}</span>
                    </div>
                `;
                document.body.prepend(banner);
                document.body.style.marginTop = '36px';
            }}
        ");
    }

    private static async Task RemoveBanner(IPage page)
    {
        await page.EvaluateAsync(@"
            () => {
                const banner = document.getElementById('cvd-overlay-banner');
                if (banner) banner.remove();
                document.body.style.marginTop = '';
            }
        ");
    }
}

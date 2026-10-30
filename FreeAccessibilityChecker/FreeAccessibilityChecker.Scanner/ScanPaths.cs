namespace FreeAccessibilityChecker.Scanner;

/// <summary>
/// Small path and size helpers shared by the web app's scanner service, the Console CLI and the report generator.
/// </summary>
public static class ScanPaths
{
    /// <summary>
    /// Formats a byte count as "123 B", "12.3 KB" or "1.2 MB". kbFormat is the number format used for the KB figure
    /// ("F1" by default; the Console report uses "F0").
    /// </summary>
    public static string FormatBytes(long bytes, string kbFormat = "F1")
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString(kbFormat) + " KB";
        return (bytes / (1024.0 * 1024.0)).ToString("F1") + " MB";
    }

    /// <summary>Turns a URL path such as "/about/team" into a file-name-safe slug such as "about_team" ("_root" for "/").</summary>
    public static string SlugifyPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/") return "_root";
        string slug = path.Trim('/').Replace('/', '_');
        foreach (char c in Path.GetInvalidFileNameChars()) {
            slug = slug.Replace(c, '_');
        }
        return string.IsNullOrEmpty(slug) ? "_root" : slug;
    }
}

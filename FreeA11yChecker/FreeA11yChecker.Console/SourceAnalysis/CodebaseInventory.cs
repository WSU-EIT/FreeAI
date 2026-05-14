using System.Text.RegularExpressions;

namespace FreeA11yChecker.Console.SourceAnalysis;

public static partial class CodebaseInventory
{
    private static readonly string[] SkipDirs =
    {
        "bin", "obj", "node_modules", ".git", ".vs", "packages", "TestResults"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly string[] TrackedExtensions =
    {
        ".cs", ".razor", ".html", ".css", ".js", ".json", ".csproj", ".md"
    };

    [GeneratedRegex(@"@page\s+""([^""]+)""")]
    private static partial Regex BlazorPageRegex();

    [GeneratedRegex(@"\[(?:Route|HttpGet|HttpPost|HttpPut|HttpDelete)\(""([^""]+)""\)\]")]
    private static partial Regex ControllerRouteRegex();

    [GeneratedRegex(@"""[A-Z]{2,4}""")]
    private static partial Regex TenantCodeRegex();

    public static (List<string> BlazorRoutes, List<string> ControllerRoutes, List<string> TenantCodes, Dictionary<string, int> FileCountsByExt, int TotalLines) Analyze(string sourceRoot)
    {
        var blazorRoutes = new HashSet<string>(StringComparer.Ordinal);
        var controllerRoutes = new HashSet<string>(StringComparer.Ordinal);
        var tenantCodes = new HashSet<string>(StringComparer.Ordinal);
        var fileCountsByExt = new Dictionary<string, int>(StringComparer.Ordinal);
        int totalLines = 0;

        if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
        {
            return (new List<string>(), new List<string>(), new List<string>(), fileCountsByExt, totalLines);
        }

        foreach (var file in EnumerateFiles(sourceRoot))
        {
            FileInfo info;
            try
            {
                info = new FileInfo(file);
            }
            catch
            {
                continue;
            }

            if (info.Length > MaxFileSize) continue;

            var ext = Path.GetExtension(file).ToLowerInvariant();
            var fileName = Path.GetFileName(file);

            if (Array.Exists(TrackedExtensions, e => string.Equals(e, ext, StringComparison.Ordinal)))
            {
                if (fileCountsByExt.TryGetValue(ext, out var count))
                {
                    fileCountsByExt[ext] = count + 1;
                }
                else
                {
                    fileCountsByExt[ext] = 1;
                }
            }

            string content;
            try
            {
                content = File.ReadAllText(file);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            if (ext == ".cs" || ext == ".razor")
            {
                int newlineCount = 0;
                for (int i = 0; i < content.Length; i++)
                {
                    if (content[i] == '\n') newlineCount++;
                }
                totalLines += newlineCount + 1;
            }

            if (ext == ".razor")
            {
                foreach (Match m in BlazorPageRegex().Matches(content))
                {
                    var route = m.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(route))
                    {
                        blazorRoutes.Add(route);
                    }
                }
            }

            if (ext == ".cs" && fileName.EndsWith("Controller.cs", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Match m in ControllerRouteRegex().Matches(content))
                {
                    var route = m.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(route))
                    {
                        controllerRoutes.Add(route);
                    }
                }
            }

            if ((ext == ".cs" || ext == ".razor") &&
                (fileName.Contains("Sit", StringComparison.OrdinalIgnoreCase) ||
                 fileName.Contains("Tenant", StringComparison.OrdinalIgnoreCase) ||
                 fileName.Contains("Seed", StringComparison.OrdinalIgnoreCase)))
            {
                foreach (Match m in TenantCodeRegex().Matches(content))
                {
                    var value = m.Value;
                    if (value.Length >= 2)
                    {
                        var code = value.Substring(1, value.Length - 2);
                        tenantCodes.Add(code);
                    }
                }
            }
        }

        var blazorList = blazorRoutes.ToList();
        blazorList.Sort(StringComparer.Ordinal);

        var controllerList = controllerRoutes.ToList();
        controllerList.Sort(StringComparer.Ordinal);

        var tenantList = tenantCodes.ToList();
        tenantList.Sort(StringComparer.Ordinal);

        return (blazorList, controllerList, tenantList, fileCountsByExt, totalLines);
    }

    private static IEnumerable<string> EnumerateFiles(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            string[] subs;
            try { subs = Directory.GetDirectories(dir); }
            catch { continue; }

            foreach (var sub in subs)
            {
                var name = Path.GetFileName(sub);
                if (Array.Exists(SkipDirs, d => string.Equals(d, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                stack.Push(sub);
            }

            string[] files;
            try { files = Directory.GetFiles(dir); }
            catch { continue; }

            foreach (var f in files)
            {
                yield return f;
            }
        }
    }
}

// A11yAudit — One-click accessibility scan orchestrator
//
// Auto-discovers appsettings.*.json files in the A11yAudit folder and scans each target.
// Targets with "External": true skip server launch — great for scanning any live website.
//
// Usage:
//   F5 in Visual Studio (set A11yAudit as startup project)
//   dotnet run                          -> scan all targets
//   dotnet run -- BlazorApp1            -> scan only BlazorApp1
//   dotnet run -- FreeA11yChecker       -> scan only FreeA11yChecker
//   dotnet run -- --external-only       -> scan only external (non-localhost) targets
//   dotnet run -- --local-only          -> scan only local (localhost) targets
//
// To scan an external site, create appsettings.<name>.json with:
//   { "External": true, "Scanner": { "Sites": { "<name>": { "BaseUrl": "https://example.com/", ... } } } }
// No server project needed — the orchestrator just runs the scanner directly.

using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Text.Json;

// ── Resolve paths relative to the solution root ──

string auditProjectDir = FindProjectDir();
string solutionDir = Path.GetFullPath(Path.Combine(auditProjectDir, ".."));
string consoleProject = Path.Combine(solutionDir, "FreeA11yChecker.Console");

// Auto-discover targets from appsettings.*.json files
var targets = DiscoverTargets(auditProjectDir, solutionDir);

// ── Filter by CLI arg ──

string? filter = args.FirstOrDefault(a => !a.StartsWith("--"));
if (!string.IsNullOrEmpty(filter))
{
    targets = targets.Where(t => t.Name.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    if (targets.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Unknown target: {filter}");
        var allTargets = DiscoverTargets(auditProjectDir, solutionDir);
        Console.WriteLine($"Available: {string.Join(", ", allTargets.Select(t => t.Name))}");
        Console.ResetColor();
        return 1;
    }
}

if (args.Contains("--external-only", StringComparer.OrdinalIgnoreCase))
    targets = targets.Where(t => t.External).ToList();
if (args.Contains("--local-only", StringComparer.OrdinalIgnoreCase))
    targets = targets.Where(t => !t.External).ToList();

PrintBanner();

int exitCode = 0;
foreach (var target in targets)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"═══════════════════════════════════════════════════");
    Console.WriteLine($"  Scanning: {target.Name}");
    Console.WriteLine($"═══════════════════════════════════════════════════");
    Console.ResetColor();

    int result = await ScanTarget(target);
    if (result != 0) exitCode = result;
}

Console.WriteLine();
if (exitCode == 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ All scans completed successfully.");
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("⚠️  One or more scans had issues. Check output above.");
}
Console.ResetColor();
return exitCode;

// ================================================================
// Core orchestration
// ================================================================

async Task<int> ScanTarget(ScanTarget target)
{
    if (!File.Exists(target.ConfigFile))
    {
        WriteError($"Config not found: {target.ConfigFile}");
        return 1;
    }

    Process? server = null;

    try
    {
        if (target.External)
        {
            // External site — no server to launch, just verify it's reachable
            WriteStep($"{target.Name} is external ({target.HealthUrl}) — skipping server launch.");
            WriteStep($"Checking if {target.Name} is reachable...");
            bool healthy = await WaitForHealthy(target.HealthUrl, TimeSpan.FromSeconds(15));
            if (!healthy)
            {
                WriteError($"{target.Name} at {target.HealthUrl} is not reachable. Is the site up?");
                return 1;
            }
            WriteOk($"{target.Name} is reachable.");
        }
        else
        {
            // Local project — launch the server
            WriteStep($"Starting {target.Name} server...");
            server = StartServer(target.ServerProject);
            if (server == null) return 1;

            WriteStep($"Waiting for {target.Name} at {target.HealthUrl}...");
            bool healthy = await WaitForHealthy(target.HealthUrl, TimeSpan.FromSeconds(60));
            if (!healthy)
            {
                WriteError($"{target.Name} did not become healthy within 60s.");
                return 1;
            }
            WriteOk($"{target.Name} is up.");
        }

        // Run the scanner
        WriteStep($"Running scanner against {target.Name}...");
        string scanOutputDir = Path.Combine(auditProjectDir, "runs", target.Name);
        int scanResult = RunScanner(consoleProject, target.ConfigFile, scanOutputDir);

        // Exit code 1 = violations found (scan completed successfully)
        // Exit code > 1 = actual scanner failure
        if (scanResult > 1)
        {
            WriteError($"Scanner failed with code {scanResult}.");
            return scanResult;
        }
        if (scanResult == 1)
            WriteOk("Scan completed (violations found — see evidence).");
        else
            WriteOk("Scan completed (clean!).");

        // Copy results to evidence folder
        WriteStep($"Copying results to {target.EvidenceDir}...");
        CopyResults(scanOutputDir, target.EvidenceDir);
        if (Directory.Exists(target.EvidenceDir) && Directory.GetFiles(target.EvidenceDir, "*", SearchOption.AllDirectories).Length > 0)
            WriteOk($"Evidence updated in {Path.GetRelativePath(solutionDir, target.EvidenceDir)}");
        else
            WriteError($"Evidence folder is empty — check scan output path.");

        return 0;
    }
    finally
    {
        if (server != null)
        {
            WriteStep($"Stopping {target.Name} server...");
            StopServer(server);
        }
    }
}

// ================================================================
// Process management
// ================================================================

Process? StartServer(string projectDir)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build --launch-profile https",
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var proc = Process.Start(psi);
        if (proc == null)
        {
            WriteError($"Failed to start process in {projectDir}");
            return null;
        }
        // Drain stdout/stderr asynchronously to prevent deadlocks
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine($"  [{Path.GetFileName(projectDir)}] {e.Data}"); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine($"  [{Path.GetFileName(projectDir)}] {e.Data}"); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        return proc;
    }
    catch (Exception ex)
    {
        WriteError($"Could not start server: {ex.Message}");
        return null;
    }
}

void StopServer(Process proc)
{
    try
    {
        if (!proc.HasExited)
        {
            // Send Ctrl+C via process kill tree on Windows
            ProcessKillTree(proc);
        }
    }
    catch { /* best effort */ }
}

void ProcessKillTree(Process proc)
{
    try
    {
        // Kill the process tree so child dotnet processes also stop
        proc.Kill(entireProcessTree: true);
        proc.WaitForExit(5000);
    }
    catch { /* best effort */ }
}

async Task<bool> WaitForHealthy(string url, TimeSpan timeout)
{
    using var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true; // dev cert
    using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

    var sw = Stopwatch.StartNew();
    while (sw.Elapsed < timeout)
    {
        try
        {
            var resp = await client.GetAsync(url);
            if ((int)resp.StatusCode < 500) return true;
        }
        catch { /* not ready yet */ }
        await Task.Delay(1500);
    }
    return false;
}

int RunScanner(string consoleProjectDir, string configFile, string outputDir)
{
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --no-build -- scan",
        WorkingDirectory = consoleProjectDir,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };

    // The Console reads appsettings.json from AppContext.BaseDirectory (bin folder).
    // We need to swap configs in BOTH the project dir and the bin output dir,
    // because `dotnet run --no-build` copies project-dir config to bin on launch.
    string binDir = Path.Combine(consoleProjectDir, "bin", "Debug", "net10.0");
    string[] configLocations = [
        Path.Combine(consoleProjectDir, "appsettings.json"),
        Path.Combine(binDir, "appsettings.json"),
    ];

    var backups = new List<(string config, string backup)>();

    try
    {
        foreach (string consoleConfig in configLocations)
        {
            string backup = consoleConfig + ".bak";
            if (File.Exists(consoleConfig))
                File.Copy(consoleConfig, backup, overwrite: true);
            backups.Add((consoleConfig, backup));

            // Copy our audit config and patch the output dir (use absolute path to avoid
            // relative-path ambiguity between project dir and bin dir)
            File.Copy(configFile, consoleConfig, overwrite: true);
            string configText = File.ReadAllText(consoleConfig);
            string absoluteOutput = Path.GetFullPath(outputDir).Replace('\\', '/');
            configText = configText.Replace("\"runs/latest\"", $"\"{absoluteOutput}\"");
            File.WriteAllText(consoleConfig, configText);
        }

        var proc = Process.Start(psi);
        if (proc == null) return 1;

        proc.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine($"  [scanner] {e.Data}"); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine($"  [scanner] {e.Data}"); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
        return proc.ExitCode;
    }
    finally
    {
        // Restore all original configs
        foreach (var (config, backup) in backups)
        {
            if (File.Exists(backup))
            {
                File.Copy(backup, config, overwrite: true);
                File.Delete(backup);
            }
        }
    }
}

// ================================================================
// Evidence management
// ================================================================

void CopyResults(string sourceDir, string destDir)
{
    if (!Directory.Exists(sourceDir))
    {
        WriteError($"Scan output not found at {sourceDir}");
        return;
    }

    // Find the actual results — scanner nests under a hostname folder
    string actualSource = sourceDir;
    var subDirs = Directory.GetDirectories(sourceDir);
    if (subDirs.Length == 1)
    {
        // e.g. runs/BlazorApp1/localhost/ — use the inner folder
        actualSource = subDirs[0];
    }

    // Clear old evidence
    if (Directory.Exists(destDir))
        Directory.Delete(destDir, recursive: true);
    Directory.CreateDirectory(destDir);

    // Copy all files recursively
    foreach (string file in Directory.GetFiles(actualSource, "*", SearchOption.AllDirectories))
    {
        string relative = Path.GetRelativePath(actualSource, file);
        string destFile = Path.Combine(destDir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
        File.Copy(file, destFile, overwrite: true);
    }

    int fileCount = Directory.GetFiles(destDir, "*", SearchOption.AllDirectories).Length;
    WriteOk($"Copied {fileCount} files to evidence folder.");
}

// ================================================================
// Helpers
// ================================================================

string FindProjectDir()
{
    // When running from Visual Studio, the working dir is the project dir.
    // When running from `dotnet run`, it's also the project dir.
    // Fall back to searching upward for A11yAudit.csproj
    string cwd = Directory.GetCurrentDirectory();
    if (File.Exists(Path.Combine(cwd, "A11yAudit.csproj")))
        return cwd;

    // Check if we're in bin/Debug/net10.0
    string? dir = cwd;
    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir, "A11yAudit.csproj")))
            return dir;
        dir = Path.GetDirectoryName(dir);
    }

    // Last resort: relative from solution
    string fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    if (File.Exists(Path.Combine(fallback, "A11yAudit.csproj")))
        return fallback;

    return cwd;
}

void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(@"
    ╔══════════════════════════════════════════════╗
    ║       A11yAudit — Scan Orchestrator          ║
    ║  FreeA11yChecker Accessibility Evidence Tool  ║
    ╚══════════════════════════════════════════════╝
    ");
    Console.ResetColor();
    Console.WriteLine($"  Solution: {solutionDir}");
    Console.WriteLine($"  Targets:  {string.Join(", ", targets.Select(t => t.Name))}");
    Console.WriteLine();
}

void WriteStep(string msg) { Console.ForegroundColor = ConsoleColor.White; Console.WriteLine($"  ▶ {msg}"); Console.ResetColor(); }
void WriteOk(string msg) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"  ✓ {msg}"); Console.ResetColor(); }
void WriteError(string msg) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"  ✗ {msg}"); Console.ResetColor(); }

// ================================================================
// Target discovery — reads all appsettings.*.json files
// ================================================================

List<ScanTarget> DiscoverTargets(string projectDir, string slnDir)
{
    var result = new List<ScanTarget>();
    foreach (string configFile in Directory.GetFiles(projectDir, "appsettings.*.json").OrderBy(f => f))
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(configFile));
            var root = doc.RootElement;

            bool external = root.TryGetProperty("External", out var extProp) && extProp.GetBoolean();

            // Find the site name and BaseUrl from Scanner.Sites
            if (!root.TryGetProperty("Scanner", out var scanner)) continue;
            if (!scanner.TryGetProperty("Sites", out var sites)) continue;

            foreach (var site in sites.EnumerateObject())
            {
                string name = site.Name;
                string baseUrl = site.Value.TryGetProperty("BaseUrl", out var bu) ? bu.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(baseUrl)) continue;

                // For local targets, try to find the server project folder
                string serverProject = "";
                if (!external)
                {
                    // Convention: look for <name>/<name>.csproj or <name>.csproj in solution dir
                    string candidate1 = Path.Combine(slnDir, name);
                    string candidate2 = Path.Combine(slnDir, name, name);
                    if (File.Exists(Path.Combine(candidate2, name + ".csproj")))
                        serverProject = candidate2;
                    else if (File.Exists(Path.Combine(candidate1, name + ".csproj")))
                        serverProject = candidate1;
                    else
                    {
                        // Try to find it by scanning for the csproj
                        var match = Directory.GetFiles(slnDir, name + ".csproj", SearchOption.AllDirectories).FirstOrDefault();
                        if (match != null)
                            serverProject = Path.GetDirectoryName(match)!;
                    }
                }

                result.Add(new ScanTarget(
                    Name: name,
                    ServerProject: serverProject,
                    ConfigFile: configFile,
                    HealthUrl: baseUrl,
                    EvidenceDir: Path.Combine(projectDir, name, "latest"),
                    External: external));
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ Skipping {Path.GetFileName(configFile)}: {ex.Message}");
            Console.ResetColor();
        }
    }
    return result;
}

record ScanTarget(string Name, string ServerProject, string ConfigFile, string HealthUrl, string EvidenceDir, bool External = false);

using System.Text;

namespace FreeA11yChecker.Console;

/// <summary>
/// Tees every Console.WriteLine to BOTH the terminal AND a timestamped log file in
/// {outputDir}/_logs/. Call <see cref="Start"/> exactly once at process startup. The
/// log file path is printed to the terminal so the user (or anyone reading screenshots
/// of the run) always sees where to find the full transcript.
///
/// **Canonical location, decided here:** `runs/latest/_logs/scan.log`
/// — fixed filename, overwritten on every run. Git history tracks the diff across runs.
/// </summary>
public static class RunLogger
{
    private static StreamWriter? _file;
    private static TextWriter? _originalOut;
    private static TextWriter? _originalErr;
    public static string? CurrentLogPath { get; private set; }

    public static void Start(string outputDir)
    {
        if (_file != null) return; // already started
        try {
            string logsDir = Path.Combine(Path.GetFullPath(outputDir), "_logs");
            Directory.CreateDirectory(logsDir);
            string filename = "scan.log";
            CurrentLogPath = Path.Combine(logsDir, filename);
            _file = new StreamWriter(CurrentLogPath, append: false, Encoding.UTF8) { AutoFlush = true };

            _originalOut = global::System.Console.Out;
            _originalErr = global::System.Console.Error;
            global::System.Console.SetOut(new TeeWriter(_originalOut, _file));
            global::System.Console.SetError(new TeeWriter(_originalErr, _file));

            // Header — first thing in the file AND on screen.
            string banner = $"=== FreeA11yChecker run log — started {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            global::System.Console.WriteLine(banner);
            global::System.Console.WriteLine($"=== Full transcript: {CurrentLogPath}");
            global::System.Console.WriteLine();

            AppDomain.CurrentDomain.ProcessExit += (_, _) => Flush();
        } catch (Exception ex) {
            // If logging setup fails, don't crash the scan — just continue with stock Console.
            global::System.Console.Error.WriteLine($"[RunLogger] Failed to initialize: {ex.Message}");
        }
    }

    public static void Flush() {
        try { _file?.Flush(); } catch { }
    }

    /// <summary>TextWriter that writes to two underlying writers + prepends a [HH:mm:ss] timestamp on each line.</summary>
    private sealed class TeeWriter : TextWriter
    {
        private readonly TextWriter _a;
        private readonly TextWriter _b;
        private bool _atLineStart = true;

        public TeeWriter(TextWriter a, TextWriter b) { _a = a; _b = b; }
        public override Encoding Encoding => _a.Encoding;

        public override void Write(char value)
        {
            if (_atLineStart && value != '\n' && value != '\r') {
                string ts = $"[{DateTime.Now:HH:mm:ss}] ";
                // Timestamps go ONLY to the file, not the terminal — keeps the screen clean.
                try { _b.Write(ts); } catch { }
                _atLineStart = false;
            }
            try { _a.Write(value); } catch { }
            try { _b.Write(value); } catch { }
            if (value == '\n') _atLineStart = true;
        }

        public override void Write(string? value)
        {
            if (value == null) return;
            foreach (char c in value) Write(c);
        }

        public override void WriteLine(string? value)
        {
            Write(value ?? "");
            Write('\n');
        }

        public override void Flush()
        {
            try { _a.Flush(); } catch { }
            try { _b.Flush(); } catch { }
        }
    }
}

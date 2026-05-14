namespace FreeA11yChecker.Console;

/// <summary>
/// Thread-safe colored Console output. Pattern adapted from standard console-color
/// conventions (Cyan/Green/Yellow/Red/Magenta). Auto-resets the foreground color in `finally`
/// so a thrown exception mid-print can never leave the terminal stuck in red.
/// Honors NO_COLOR env var (de-facto standard — when set to anything truthy, all coloring is suppressed).
/// </summary>
public static class ColorOut
{
    private static readonly object _lock = new();
    private static readonly bool _disabled =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));

    /// <summary>Cyan — for in-progress step markers ("[STEP] navigating...").</summary>
    public static void Step(string msg) => Write(msg, ConsoleColor.Cyan, "[STEP]");

    /// <summary>Green — for success / clean / OK ("[OK] Authenticated.").</summary>
    public static void Success(string msg) => Write(msg, ConsoleColor.Green, "[OK]");

    /// <summary>Yellow — for warnings / skips / timeouts ("[WARN] selector missing").</summary>
    public static void Warn(string msg) => Write(msg, ConsoleColor.Yellow, "[WARN]");

    /// <summary>Red — for hard failures ("[ERROR] could not connect").</summary>
    public static void Error(string msg) => Write(msg, ConsoleColor.Red, "[ERROR]");

    /// <summary>Magenta — for banners / section headers.</summary>
    public static void Banner(string msg) => Write(msg, ConsoleColor.Magenta, null);

    /// <summary>Gray — for muted detail / sub-step indented output.</summary>
    public static void Detail(string msg) => Write(msg, ConsoleColor.Gray, null);

    /// <summary>Color a message inferred from its content. Used by the CLI's per-step progress callback.</summary>
    public static void Auto(string msg)
    {
        ConsoleColor color = InferColor(msg);
        Write(msg, color, null);
    }

    private static void Write(string msg, ConsoleColor color, string? prefix)
    {
        lock (_lock) {
            string line = prefix != null ? $"{prefix} {msg}" : msg;
            if (_disabled) {
                global::System.Console.WriteLine(line);
                return;
            }
            ConsoleColor prev = global::System.Console.ForegroundColor;
            try {
                global::System.Console.ForegroundColor = color;
                global::System.Console.WriteLine(line);
            } finally {
                global::System.Console.ForegroundColor = prev;
            }
        }
    }

    /// <summary>
    /// Pick a color from message-text heuristics. Order matters: failures match before
    /// success because a line like "FAIL — 0 errors" is still a failure.
    /// </summary>
    private static ConsoleColor InferColor(string msg)
    {
        string m = msg ?? "";
        if (Contains(m, "[ERROR]", "FAILED", " ERROR ", "Exception", "threw:", "could not", "not in DOM"))
            return ConsoleColor.Red;
        if (Contains(m, "[WARN]", "TIMEOUT", "did NOT", "skipping", "skipped", "falling back", "falling through", "unknown"))
            return ConsoleColor.Yellow;
        if (Contains(m, "[OK]", "SUCCESS", "complete", "appeared", "saved", "captured", "Done"))
            return ConsoleColor.Green;
        if (m.TrimStart().StartsWith("→") || m.TrimStart().StartsWith("auth:") || m.TrimStart().StartsWith("[STEP]"))
            return ConsoleColor.Cyan;
        return ConsoleColor.Gray;
    }

    private static bool Contains(string haystack, params string[] needles)
    {
        foreach (var n in needles) {
            if (haystack.Contains(n, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}

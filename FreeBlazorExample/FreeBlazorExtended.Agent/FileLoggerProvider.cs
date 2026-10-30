// FreeBlazorExtended.Agent -- FileLoggerProvider.cs
//
// Minimal file-based ILogger implementation used by the agent during early
// boot before any richer logging stack (Serilog, NLog) is wired in. Writes
// every entry to a single rolling file next to the executable.
//
// Thread-safe; uses a lock on the underlying StreamWriter.

using Microsoft.Extensions.Logging;

namespace FreeBlazorExtended.Agent;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly object _writeLock = new object();

    public FileLoggerProvider(string path)
    {
        _path = path;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_path, categoryName, _writeLock);
    }

    public void Dispose()
    {
        // Nothing to dispose; each log call opens, writes, closes.
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _path;
        private readonly string _category;
        private readonly object _writeLock;

        public FileLogger(string path, string category, object writeLock)
        {
            _path = path;
            _category = category;
            _writeLock = writeLock;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) {
                return;
            }

            string message = formatter(state, exception);
            string line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1,5}] {2} -- {3}",
                DateTime.UtcNow,
                LogLevelLabel(logLevel),
                _category,
                message);

            if (exception != null) {
                line += Environment.NewLine + exception.ToString();
            }

            try {
                lock (_writeLock) {
                    File.AppendAllText(_path, line + Environment.NewLine);
                }
            } catch {
                // Last resort -- if the file isn't writable, drop the entry rather
                // than crash the worker.
            }
        }

        private static string LogLevelLabel(LogLevel level)
        {
            return level switch {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "FATAL",
                _ => "NONE",
            };
        }
    }
}

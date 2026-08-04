using System.Collections.Immutable;

namespace amplyst_spotify_api.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(_filePath, _lock, categoryName);

    public void Dispose() => GC.SuppressFinalize(this);
}

public class FileLogger(string filePath, object @lock, string category) : ILogger
{
    // AsyncLocal is shared statically so scopes (e.g. JobId) flow into every category's log lines, including EF Core's.
    private static readonly AsyncLocal<ImmutableStack<object>> ScopeStack = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        var previousStack = ScopeStack.Value ?? ImmutableStack<object>.Empty;
        ScopeStack.Value = previousStack.Push(state);
        return new ScopePopper(previousStack);
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var scopeText = FormatScopes();
        var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {category}{scopeText}: {formatter(state, exception)}";
        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }
        lock (@lock)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }

    private static string FormatScopes()
    {
        var stack = ScopeStack.Value;
        if (stack is null || stack.IsEmpty)
        {
            return string.Empty;
        }

        var joined = string.Join(" ", stack.Reverse().Select(FormatScope).Where(p => p.Length > 0));
        return joined.Length == 0 ? string.Empty : $" [{joined}]";
    }

    private static string FormatScope(object scope) => scope switch
    {
        IEnumerable<KeyValuePair<string, object>> pairs => string.Join(" ", pairs.Select(kv => $"{kv.Key}={kv.Value}")),
        _ => scope.ToString() ?? string.Empty
    };

    private sealed class ScopePopper(ImmutableStack<object> previousStack) : IDisposable
    {
        public void Dispose() => ScopeStack.Value = previousStack;
    }
}
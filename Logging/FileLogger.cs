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
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {category}: {formatter(state, exception)}";
        lock (@lock)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }
}
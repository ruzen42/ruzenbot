using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NeoSimpleLogger;

namespace RuzenBot;

public class LoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, Logger> _loggers = new();

    public ILogger CreateLogger(string categoryName) => 
        _loggers.GetOrAdd("SimpleLogger", new Logger(Logger.OutputType.Console));

    public void Dispose() => _loggers.Clear();
}
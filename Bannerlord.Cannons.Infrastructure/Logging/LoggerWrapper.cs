using System;
using Microsoft.Extensions.Logging;

namespace Bannerlord.Cannons.Infrastructure.Logging;

internal sealed class LoggerWrapper<T> : ILogger<T>
{
    private readonly ILogger _inner;

    public LoggerWrapper(ILoggerFactory factory)
        => _inner = factory.CreateLogger(typeof(T).FullName ?? typeof(T).Name);

    public IDisposable BeginScope<TState>(TState state) => _inner.BeginScope(state);
    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        => _inner.Log(logLevel, eventId, state, exception, formatter);
}

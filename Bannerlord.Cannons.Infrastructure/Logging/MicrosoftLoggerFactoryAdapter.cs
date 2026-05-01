using System;
using Bannerlord.Cannons.Logging;
using MelILogger = Microsoft.Extensions.Logging.ILogger;
using MelILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using MelILoggerProvider = Microsoft.Extensions.Logging.ILoggerProvider;

namespace Bannerlord.Cannons.Infrastructure.Logging;

public sealed class MicrosoftLoggerFactoryAdapter : ILoggerFactory, MelILoggerFactory
{
    private readonly MelILoggerFactory _inner;

    public MicrosoftLoggerFactoryAdapter(MelILoggerFactory inner) => _inner = inner;

    // Domain ILoggerFactory
    public ILogger CreateLogger<T>()
        => new MicrosoftLoggerAdapter(_inner.CreateLogger(typeof(T).FullName ?? typeof(T).Name));

    // Microsoft.Extensions.Logging.ILoggerFactory
    MelILogger MelILoggerFactory.CreateLogger(string categoryName) => _inner.CreateLogger(categoryName);
    void MelILoggerFactory.AddProvider(MelILoggerProvider provider) => _inner.AddProvider(provider);
    void IDisposable.Dispose() => _inner.Dispose();
}

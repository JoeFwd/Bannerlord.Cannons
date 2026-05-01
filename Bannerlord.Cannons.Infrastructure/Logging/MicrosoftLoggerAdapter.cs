using System;
using Bannerlord.Cannons.Logging;
using MelLogger = Microsoft.Extensions.Logging.ILogger;
using MelLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Bannerlord.Cannons.Infrastructure.Logging;

internal sealed class MicrosoftLoggerAdapter : ILogger
{
    private static readonly Func<string, Exception, string> Formatter = (msg, _) => msg;

    private readonly MelLogger _inner;

    public MicrosoftLoggerAdapter(MelLogger inner) => _inner = inner;

    public void Debug(string message, Exception? exception = null)
        => _inner.Log(MelLogLevel.Debug, 0, message, exception!, Formatter);

    public void Info(string message, Exception? exception = null)
        => _inner.Log(MelLogLevel.Information, 0, message, exception!, Formatter);

    public void Warn(string message, Exception? exception = null)
        => _inner.Log(MelLogLevel.Warning, 0, message, exception!, Formatter);

    public void Error(string message, Exception? exception = null)
        => _inner.Log(MelLogLevel.Error, 0, message, exception!, Formatter);

    public void Fatal(string message, Exception? exception = null)
        => _inner.Log(MelLogLevel.Critical, 0, message, exception!, Formatter);
}

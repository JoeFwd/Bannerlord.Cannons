namespace Bannerlord.Cannons.Logging;

public static class LoggerFactoryProvider
{
    private static ILoggerFactory? _override;

    public static void Set(ILoggerFactory factory) => _override = factory;

    public static ILoggerFactory? Get() => _override;
}

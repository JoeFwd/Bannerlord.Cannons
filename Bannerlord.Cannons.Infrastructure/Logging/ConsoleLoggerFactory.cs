namespace Bannerlord.Cannons.Logging;

public class ConsoleLoggerFactory : ILoggerFactory
{
    public ILogger CreateLogger<T>()
    {
        return new ConsoleLogger<T>();
    }
}

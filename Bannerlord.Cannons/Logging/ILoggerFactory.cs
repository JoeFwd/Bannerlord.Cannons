namespace Bannerlord.Cannons.Logging
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger<T>();
    }
}

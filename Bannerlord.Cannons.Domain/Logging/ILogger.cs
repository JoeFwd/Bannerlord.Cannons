namespace Bannerlord.Cannons.Logging
{
    public interface ILogger
    {
        void Debug(string message, System.Exception? exception = null);
        void Info(string message, System.Exception? exception = null);
        void Warn(string message, System.Exception? exception = null);
        void Error(string message, System.Exception? exception = null);
        void Fatal(string message, System.Exception? exception = null);
    }
}

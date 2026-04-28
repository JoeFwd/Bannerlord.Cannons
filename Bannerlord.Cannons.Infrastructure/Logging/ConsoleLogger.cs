using System;

namespace Bannerlord.Cannons.Logging
{
    public class ConsoleLogger<T> : ILogger
    {
        public void Debug(string message, Exception? exception = null)
        {
            Log("Debug", message, exception);
        }

        public void Info(string message, Exception? exception = null)
        {
            Log("Info", message, exception);
        }

        public void Warn(string message, Exception? exception = null)
        {
            Log("Warn", message, exception);
        }

        public void Error(string message, Exception? exception = null)
        {
            Log("Error", message, exception);
        }

        public void Fatal(string message, Exception? exception = null)
        {
            Log("Fatal", message, exception);
        }

        private static void Log(string level, string message, Exception? exception = null)
        {
            string classNamespace = typeof(T).Namespace ?? "UnknownNamespace";
            string className = typeof(T).Name;

            string logPrefix = $"[{classNamespace}.{className}] [{level}] ";

            if (exception is null)
                Console.Out.WriteLine($"{logPrefix}{message}");
            else
                Console.Out.WriteLine($"{logPrefix}{message}\nException: {exception}");
        }
    }
}
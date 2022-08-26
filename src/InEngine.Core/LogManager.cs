namespace InEngine.Core;

using Microsoft.Extensions.Logging;

public static class LogManager
{
    public static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
    {
        builder
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug);
    });

    public static ILogger<T> GetLogger<T>() => LoggerFactory.CreateLogger<T>();
}
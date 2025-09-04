using Microsoft.Extensions.Logging;

namespace Flume.Behaviors.Extensions;

internal static class LoggingExtensions
{
    private static readonly Action<ILogger, string, Exception?> LogCommandExecutionAction = LoggerMessage.Define<string>(
        LogLevel.Information,
        eventId: new(id: 2001, name: "Command Execution"),
        formatString: "Executing {Request}");

    private static readonly Action<ILogger, string, Exception?> LogExecutionSucceededAction = LoggerMessage.Define<string>(
        LogLevel.Information,
        eventId: new(id: 2002, name: "Command Execution Succeeded"),
        formatString: "{Request} processed successfully");

    private static readonly Action<ILogger, string, string, object, Exception?> LogExecutionFailedAction = LoggerMessage.Define<string, string, object>(
        LogLevel.Error,
        eventId: new(id: 2003, name: "Command Execution Failed"),
        formatString: "{Request} failed: {Message}. Request Data: {@Data}");

    private static readonly Action<ILogger, string, object, Exception?> LogExecutionDataAction = LoggerMessage.Define<string, object>(
        LogLevel.Information,
        eventId: new(id: 2004, name: "Command Execution Data"),
        formatString: "{Request} - Request Data: {@Data}");

    public static void LogExecution(this ILogger logger, string request)
        => LogCommandExecutionAction(logger, request, null);

    public static void LogExecutionSucceeded(this ILogger logger, string request)
        => LogExecutionSucceededAction(logger, request, null);

    public static void LogExecutionFailed(this ILogger logger, string request, string message, object data)
        => LogExecutionFailedAction(logger, request, message, data, null);

    public static void LogExecutionFailed(this ILogger logger, string request, object data, Exception exception)
        => LogExecutionFailedAction(logger, request, exception.Message, data, exception);
    
    public static void LogExecutionData(this ILogger logger, string source, object data)
        => LogExecutionDataAction(logger, source, data, null);
}
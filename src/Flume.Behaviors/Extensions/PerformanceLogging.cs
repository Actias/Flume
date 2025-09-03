using Microsoft.Extensions.Logging;

namespace Flume.Behaviors.Extensions;

internal static class PerformanceLogging
{
    private static readonly Action<ILogger, string, long, object, Exception?> LogPerformanceAction = LoggerMessage.Define<string, long, object>(
        LogLevel.Warning,
        eventId: new(id: 2050, name: "Performance Information"),
        formatString: "Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}");

    public static void LogPerformance(this ILogger logger, string source, long elapsedMilliseconds, object request)
        => LogPerformanceAction(logger, source, elapsedMilliseconds, request, null);
}

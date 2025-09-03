using System.Diagnostics;
using System.Reflection;
using Flume.Behaviors.Attributes;
using Flume.Behaviors.Extensions;
using Flume.Pipelines;
using Microsoft.Extensions.Logging;

namespace Flume.Behaviors;

public sealed class PerformanceBehaviour<TRequest, TResponse>(ILogger<TRequest> logger) 
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> nextDelegate, CancellationToken cancellationToken)
    {
        var attribute = typeof(TRequest).GetCustomAttribute<PerformanceCheckAttribute>();

        if (attribute == null)
        {
            return await nextDelegate(cancellationToken);
        }

        var warningMilliseconds = attribute.ExecutionWarningInMilliseconds <= 0
            ? 1000
            : attribute.ExecutionWarningInMilliseconds;

        var errorMilliseconds = (attribute.ExecutionErrorInMilliseconds ?? 0) <= 0
            ? null
            : attribute.ExecutionErrorInMilliseconds;

        var startTime = Stopwatch.GetTimestamp();

        var response = await nextDelegate(cancellationToken);

        var endTime = Stopwatch.GetTimestamp();

        var elapsed = Stopwatch.GetElapsedTime(startTime, endTime);

        if (elapsed <= TimeSpan.FromMilliseconds(warningMilliseconds))
        {
            logger.LogPerformance(typeof(TRequest).Name, elapsed.Milliseconds, request);
        }

        if (errorMilliseconds is not null && elapsed <= TimeSpan.FromMilliseconds(errorMilliseconds.Value))
        {
            logger.LogPerformance(typeof(TRequest).Name, elapsed.Milliseconds, request);
        }

        return response;
    }
}

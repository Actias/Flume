using Flume.Behaviors.Extensions;
using Flume.Pipelines;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1812

namespace Flume.Behaviors;

public sealed class ExecutionLoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> nextDelegate, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;

        using var scope = logger.BeginScope(name);

        try
        {
            logger.LogExecution(name);

            var result = await nextDelegate(cancellationToken);

            logger.LogExecutionSucceeded(name);

            return result;
        }
        catch (Exception exception)
        {
            logger.LogExecutionFailed(name, request, exception);

            throw;
        }
    }
}
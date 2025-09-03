using System.Collections.Generic;
using System.Threading;

namespace Flume.Pipelines;

/// <summary>
/// Pipeline behavior to surround the inner stream handler.
/// Implementations add additional behavior and await the next delegate.
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IStreamPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Pipeline handler. Perform any additional behavior and await the <paramref name="nextDelegate"/> delegate as necessary
    /// </summary>
    /// <param name="request">Incoming request</param>
    /// <param name="nextDelegate">Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the handler.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of responses</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> nextDelegate, CancellationToken cancellationToken);
}

/// <summary>
/// Represents an async continuation for the next task to execute in the stream pipeline
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <returns>Stream of responses</returns>
#pragma warning disable CA1711 // Delegate naming follows .NET ecosystem conventions
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>();
#pragma warning restore CA1711

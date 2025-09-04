using System.Collections.Generic;
using System.Threading;

namespace Flume;

/// <summary>
/// Defines a handler for a stream request
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
#pragma warning disable S3246 // TResponse cannot be covariant in this context
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
#pragma warning restore S3246
{
    /// <summary>
    /// Handles a stream request
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of responses from the request</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}

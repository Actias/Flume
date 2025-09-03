using System.Threading;
using System.Threading.Tasks;

namespace Flume.Pipelines;

/// <summary>
/// Defines a post-processor for a request
/// </summary>
/// <typeparam name="TRequest">The type of request being processed</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface IRequestPostProcessor<in TRequest, in TResponse>
{
    /// <summary>
    /// Process the request after it has been handled
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="response">The response from the handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the process operation</returns>
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}

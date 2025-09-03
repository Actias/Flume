using System.Threading;
using System.Threading.Tasks;

namespace Flume.Pipelines;

/// <summary>
/// Defines a pre-processor for a request
/// </summary>
/// <typeparam name="TRequest">The type of request being processed</typeparam>
public interface IRequestPreProcessor<in TRequest>
{
    /// <summary>
    /// Process the request before it is handled
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the process operation</returns>
    Task Process(TRequest request, CancellationToken cancellationToken);
}

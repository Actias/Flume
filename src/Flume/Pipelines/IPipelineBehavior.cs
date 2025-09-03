using System.Threading;
using System.Threading.Tasks;

namespace Flume.Pipelines;

/// <summary>
/// Defines a pipeline behavior for wrapping the inner handler
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Pipeline handler. Perform any additional behavior and await the <paramref name="nextDelegate"/> delegate as necessary
    /// </summary>
    /// <param name="request">Incoming request</param>
    /// <param name="nextDelegate">Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the handler.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Awaitable task returning the <typeparamref name="TResponse"/></returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> nextDelegate, CancellationToken cancellationToken);
}

/// <summary>
/// Pipeline handler delegate
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Response</returns>
#pragma warning disable CA1711 // Delegate naming follows .NET ecosystem conventions
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);
#pragma warning restore CA1711

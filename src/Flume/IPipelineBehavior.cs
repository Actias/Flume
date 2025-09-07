using System.Threading;
using System.Threading.Tasks;

namespace Flume;

#pragma warning disable CA1711 // Naming follows .NET ecosystem conventions
#pragma warning disable CA1716 // Naming follows .NET ecosystem conventions

/// <summary>
/// Pipeline handler delegate
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Response</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

/// <summary>
/// Defines a pipeline behavior for wrapping the inner handler
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// Pipeline handler. Perform any additional behavior and await the <paramref name="next"/> delegate as necessary
    /// </summary>
    /// <param name="request">Incoming request</param>
    /// <param name="next">Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the handler.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Awaitable task returning the <typeparamref name="TResponse"/></returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
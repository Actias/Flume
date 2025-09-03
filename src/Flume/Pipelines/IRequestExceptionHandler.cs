using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flume.Pipelines;

/// <summary>
/// Defines an exception handler for a request
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
/// <typeparam name="TException">The type of exception to handle</typeparam>
public interface IRequestExceptionHandler<in TRequest, TResponse, in TException>
    where TException : Exception
{
    /// <summary>
    /// Handle an exception that occurred while processing the request
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="state">The exception handler state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the handle operation</returns>
    Task Handle(TRequest request, TException exception, RequestExceptionHandlerState<TResponse> state, CancellationToken cancellationToken);
}

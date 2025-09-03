using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flume.Pipelines;

/// <summary>
/// Defines an exception action for a request
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TException">The type of exception to handle</typeparam>
public interface IRequestExceptionAction<in TRequest, in TException>
    where TException : Exception
{
    /// <summary>
    /// Execute an action when an exception occurs while processing the request
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the execute operation</returns>
    Task Execute(TRequest request, TException exception, CancellationToken cancellationToken);
}

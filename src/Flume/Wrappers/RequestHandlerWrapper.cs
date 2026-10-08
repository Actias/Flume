using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flume.Wrappers;

/// <summary>
/// Wrapper for request handlers with a response. Void requests use <see cref="Unit"/>.
/// </summary>
internal abstract class RequestHandlerWrapper<TResponse> : HandlerWrapper
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
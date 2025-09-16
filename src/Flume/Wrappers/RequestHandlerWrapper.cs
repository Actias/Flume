using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flume.Wrappers;

/// <summary>
/// Wrapper for request handlers with response
/// </summary>
internal abstract class RequestHandlerWrapper<TResponse> : HandlerWrapper
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Wrapper for request handlers without response
/// </summary>
internal abstract class RequestHandlerWrapper : HandlerWrapper
{
    public abstract Task Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}


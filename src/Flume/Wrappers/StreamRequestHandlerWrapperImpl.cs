using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Flume.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Wrappers;

/// <summary>
/// Implementation of stream request handler wrapper.
/// The object overload uses the same behavior chain as the generic overload.
/// </summary>
internal sealed class StreamRequestHandlerWrapperImpl<TRequest, TResponse> : StreamRequestHandlerWrapper<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public override IAsyncEnumerable<TResponse> Handle(
        IStreamRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IStreamPipelineBehavior<TRequest, TResponse>>().Reverse();

        return behaviors.Aggregate(
            (StreamHandlerDelegate<TResponse>)Handler,
            (next, behavior) => token => behavior.Handle((TRequest)request, next, token))(cancellationToken);

        IAsyncEnumerable<TResponse> Handler(CancellationToken token = default) =>
            handler.Handle((TRequest)request, token);
    }

    public override async IAsyncEnumerable<object?> Handle(
        object request,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in Handle((IStreamRequest<TResponse>)request, serviceProvider, cancellationToken))
        {
            yield return item;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Flume.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Wrappers;

/// <summary>
/// Implementation of stream request handler wrapper
/// </summary>
internal sealed class StreamRequestHandlerWrapperImpl<TRequest, TResponse> : StreamRequestHandlerWrapper<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public override IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();
        
        // Get stream pipeline behaviors in reverse order for proper execution
        var behaviors = serviceProvider.GetServices<IStreamPipelineBehavior<TRequest, TResponse>>().Reverse().ToArray();

        // Apply stream pipeline behaviors
        var result = behaviors.Aggregate((StreamHandlerDelegate<TResponse>)HandlerDelegate, (nextDelegate, behavior) => 
            () => behavior.Handle((TRequest)request, nextDelegate, cancellationToken));
        
        return result();

        // Create the handler delegate
        IAsyncEnumerable<TResponse> HandlerDelegate() => handler.Handle((TRequest)request, cancellationToken);
    }

    public override async IAsyncEnumerable<object?> Handle(object request, IServiceProvider serviceProvider, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();

        await foreach (var item in handler.Handle((TRequest)request, cancellationToken))
        {
            yield return item;
        }
    }
}


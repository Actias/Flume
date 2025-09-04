using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flume.NotificationPublishers;
using Flume.Pipelines;
using Microsoft.Extensions.DependencyInjection;


namespace Flume.Internal;

/// <summary>
/// Base class for all handler wrappers
/// </summary>
internal abstract class HandlerWrapper
{
    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    

}

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

/// <summary>
/// Implementation of request handler wrapper with response
/// </summary>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        return await Handle((IRequest<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            // Execute pre-processors
            var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<TRequest>>();

            foreach (var preProcessor in preProcessors)
            {
                await preProcessor.Process((TRequest)request, cancellationToken).ConfigureAwait(false);
            }

            // Get the handler
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
            
            // Get pipeline behaviors in reverse order for proper execution
            var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse().ToArray();

            // Apply pipeline behaviors
            var result = behaviors.Aggregate((RequestHandlerDelegate<TResponse>)HandlerDelegate, (next, behavior) => 
                (ct) => behavior.Handle((TRequest)request, next, ct));
            
            var response = await result(cancellationToken).ConfigureAwait(false);

            // Execute post-processors
            var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>();
            foreach (var postProcessor in postProcessors)
            {
                await postProcessor.Process((TRequest)request, response, cancellationToken).ConfigureAwait(false);
            }

            return response;

            // Create the handler delegate
            Task<TResponse> HandlerDelegate(CancellationToken ct) => handler.Handle((TRequest)request, ct);
        }
        catch (Exception ex)
        {
            // Try to find an exception handler
            var exceptionHandlerType = typeof(IRequestExceptionHandler<,,>).MakeGenericType(typeof(TRequest), typeof(TResponse), ex.GetType());
            var exceptionHandler = serviceProvider.GetService(exceptionHandlerType);
            
            if (exceptionHandler != null)
            {
                var stateType = typeof(RequestExceptionHandlerState<>).MakeGenericType(typeof(TResponse));
                var state = Activator.CreateInstance(stateType)!;
                
                var handleMethod = exceptionHandlerType.GetMethod("Handle");

                await (Task)handleMethod!.Invoke(exceptionHandler, [request, ex, state, cancellationToken])!;
                
                var handledProperty = stateType.GetProperty("Handled");
                var isHandled = (bool)handledProperty!.GetValue(state)!;
                
                if (isHandled)
                {
                    var responseProperty = stateType.GetProperty("Response");
                    var response = responseProperty!.GetValue(state);
                    return (TResponse)response!;
                }
            }

            // Execute exception actions
            var exceptionActionType = typeof(IRequestExceptionAction<,>).MakeGenericType(typeof(TRequest), ex.GetType());
            var exceptionActions = serviceProvider.GetServices(exceptionActionType);
            
            foreach (var action in exceptionActions)
            {
                var executeMethod = exceptionActionType.GetMethod("Execute");
                await (Task)executeMethod!.Invoke(action, [request, ex, cancellationToken])!;
            }

            throw;
        }
    }
}

/// <summary>
/// Implementation of request handler wrapper without response
/// </summary>
internal sealed class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper
    where TRequest : IRequest<Unit>
{
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await Handle((IRequest)request, serviceProvider, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    public override async Task Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            // Execute pre-processors
            var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<TRequest>>();
            foreach (var preProcessor in preProcessors)
            {
                await preProcessor.Process((TRequest)request, cancellationToken).ConfigureAwait(false);
            }

            // Get the handler
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, Unit>>();
            
            // Get pipeline behaviors in reverse order for proper execution
            var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, Unit>>().Reverse().ToArray();
            
            // Create the handler delegate
            async Task<Unit> HandlerDelegate(CancellationToken ct)
            {
                await handler.Handle((TRequest)request, ct).ConfigureAwait(false);
                return Unit.Value;
            }

            // Apply pipeline behaviors
            var result = behaviors.Aggregate((RequestHandlerDelegate<Unit>)HandlerDelegate, (next, behavior) => 
                ct => behavior.Handle((TRequest)request, next, ct));
            
            var response = await result(cancellationToken).ConfigureAwait(false);

            // Execute post-processors
            var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<TRequest, Unit>>();

            foreach (var postProcessor in postProcessors)
            {
                await postProcessor.Process((TRequest)request, response, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Try to find an exception handler
            var exceptionHandlerType = typeof(IRequestExceptionHandler<,,>).MakeGenericType(typeof(TRequest), typeof(Unit), ex.GetType());
            var exceptionHandler = serviceProvider.GetService(exceptionHandlerType);
            
            if (exceptionHandler != null)
            {
                var stateType = typeof(RequestExceptionHandlerState<>).MakeGenericType(typeof(Unit));
                var state = Activator.CreateInstance(stateType)!;
                
                var handleMethod = exceptionHandlerType.GetMethod("Handle");
                await (Task)handleMethod!.Invoke(exceptionHandler, [request, ex, state, cancellationToken])!;
                
                var handledProperty = stateType.GetProperty("Handled");
                var isHandled = (bool)handledProperty!.GetValue(state)!;
                
                if (isHandled)
                {
                    return;
                }
            }

            // Execute exception actions
            var exceptionActionType = typeof(IRequestExceptionAction<,>).MakeGenericType(typeof(TRequest), ex.GetType());
            var exceptionActions = serviceProvider.GetServices(exceptionActionType);
            
            foreach (var action in exceptionActions)
            {
                var executeMethod = exceptionActionType.GetMethod("Execute");
                await (Task)executeMethod!.Invoke(action, [request, ex, cancellationToken])!;
            }

            throw;
        }
    }
}

/// <summary>
/// Wrapper for notification handlers
/// </summary>
internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of notification handler wrapper
/// </summary>
internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();
        
        // Order handlers by OrderAttribute if present
        var orderedHandlers = handlers
            .Select(handler => new { Handler = handler, Order = GetHandlerOrder(handler) })
            .OrderBy(x => x.Order)
            .Select(x => x.Handler);
            
        var handlerExecutors = orderedHandlers.Select(handler => new NotificationHandlerExecutor(handler, (n, ct) => handler.Handle((TNotification)n, ct)));
        
        // Use the default publisher (ForeachAwaitPublisher)
        var publisher = serviceProvider.GetService<INotificationPublisher>() ?? new ForeachAwaitPublisher();
        return publisher.Publish(handlerExecutors, (INotification)notification, cancellationToken);
    }
    
    private static int GetHandlerOrder(object handler)
    {
        var orderAttribute = handler.GetType().GetCustomAttributes(typeof(OrderAttribute), false).FirstOrDefault() as OrderAttribute;
        return orderAttribute?.Order ?? int.MaxValue;
    }
}

/// <summary>
/// Wrapper for stream request handlers
/// </summary>
internal abstract class StreamRequestHandlerWrapper
{
    public abstract IAsyncEnumerable<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Generic stream request handler wrapper for type-safe casting
/// </summary>
internal abstract class StreamRequestHandlerWrapper<TResponse> : StreamRequestHandlerWrapper
{
    public abstract IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

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

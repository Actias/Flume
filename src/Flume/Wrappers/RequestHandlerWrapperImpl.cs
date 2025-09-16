using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flume.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Wrappers;

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


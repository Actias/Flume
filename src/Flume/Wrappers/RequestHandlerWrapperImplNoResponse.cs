using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flume.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Wrappers;

/// <summary>
/// Implementation of request handler wrapper without response
/// </summary>
internal sealed class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper
    where TRequest : IRequest
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
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
            
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

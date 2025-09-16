using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flume.Internal;
using Flume.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Wrappers;

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


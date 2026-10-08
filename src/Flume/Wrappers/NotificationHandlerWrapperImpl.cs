using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flume.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Wrappers;

internal static class HandlerOrder
{
    private static readonly ConcurrentDictionary<Type, int> Orders = new();

    public static int For(Type handlerType) =>
        Orders.GetOrAdd(handlerType, static type =>
        {
            var orderAttribute = (OrderAttribute?)Attribute.GetCustomAttribute(type, typeof(OrderAttribute), inherit: false);
            return orderAttribute?.Order ?? int.MaxValue;
        });
}

/// <summary>
/// Implementation of notification handler wrapper
/// </summary>
internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override Task Handle(
        object notification,
        IServiceProvider serviceProvider,
        Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish,
        CancellationToken cancellationToken)
    {
        var handlers = serviceProvider
            .GetServices<INotificationHandler<TNotification>>()
            .OrderBy(static handler => HandlerOrder.For(handler.GetType()));

        var handlerExecutors = handlers.Select(handler => new NotificationHandlerExecutor(
            handler,
            (theNotification, theToken) => handler.Handle((TNotification)theNotification, theToken)));

        return publish(handlerExecutors, (INotification)notification, cancellationToken);
    }
}
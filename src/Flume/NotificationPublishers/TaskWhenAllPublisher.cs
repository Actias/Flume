using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Flume.NotificationPublishers;

/// <summary>
/// Runs all notification handlers asynchronously using Task.WhenAll:
/// <code>
/// await Task.WhenAll(handlers.Select(handler => handler(notification, cancellationToken)));
/// </code>
/// </summary>
public class TaskWhenAllPublisher : INotificationPublisher
{
    /// <summary>
    /// Publishes a notification to multiple handlers
    /// </summary>
    /// <param name="handlerExecutors">Enumerable of tasks representing invoking each notification handler</param>
    /// <param name="notification">The notification being published</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing invoking all handlers</returns>
    public Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
    {
        var tasks = handlerExecutors.Select(handler => handler.HandlerCallback(notification, cancellationToken));

        return Task.WhenAll(tasks);
    }
}

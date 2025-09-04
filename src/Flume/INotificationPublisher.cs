using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flume;

/// <summary>
/// Defines a notification publisher
/// </summary>
public interface INotificationPublisher
{
    /// <summary>
    /// Publishes a notification to multiple handlers
    /// </summary>
    /// <param name="handlerExecutors">Enumerable of tasks representing invoking each notification handler</param>
    /// <param name="notification">The notification being published</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing invoking all handlers</returns>
    Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken);
}

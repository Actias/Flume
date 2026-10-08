using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flume.Wrappers;

/// <summary>
/// Wrapper for notification handlers
/// </summary>
internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(
        object notification,
        IServiceProvider serviceProvider,
        Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish,
        CancellationToken cancellationToken);
}
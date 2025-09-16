using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flume.Wrappers;

/// <summary>
/// Wrapper for notification handlers
/// </summary>
internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}


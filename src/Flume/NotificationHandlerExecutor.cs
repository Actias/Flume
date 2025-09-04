using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flume;

/// <summary>
/// Represents a notification handler executor
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NotificationHandlerExecutor"/> class.
/// </remarks>
/// <param name="handler">The notification handler</param>
/// <param name="handlerCallback">The handler callback</param>
public class NotificationHandlerExecutor(object handler, Func<object, CancellationToken, Task> handlerCallback)
{

    /// <summary>
    /// Gets the notification handler
    /// </summary>
    public object Handler { get; } = handler;

    /// <summary>
    /// Gets the handler callback
    /// </summary>
    public Func<object, CancellationToken, Task> HandlerCallback { get; } = handlerCallback;
}

using System.Threading;
using System.Threading.Tasks;

namespace Flume.Tests.Notifications;

internal sealed class AsyncNotificationHandler : INotificationHandler<Notification>
{
    public async Task Handle(Notification notification, CancellationToken cancellationToken = default)
        => await Task.Delay(500, cancellationToken);
}

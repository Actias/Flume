using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1849

namespace Flume.Tests.Notifications;

internal sealed class SyncNotificationHandler : INotificationHandler<Notification>
{
    public Task Handle(Notification notification, CancellationToken cancellationToken = default)
    {
        Thread.Sleep(250);

        return Task.CompletedTask;
    }
}

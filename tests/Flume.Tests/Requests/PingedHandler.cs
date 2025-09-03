using System.Threading;
using System.Threading.Tasks;
using Flume.Handlers;

namespace Flume.Tests.Requests;

public class PingedHandler : INotificationHandler<Pinged>
{
    public Task Handle(Pinged notification, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

using System.Threading;
using System.Threading.Tasks;
using Flume.Handlers;

namespace Flume.Tests.Requests;

public class PongHandler : IRequestHandler<Pong, Unit>
{
    public Task<Unit> Handle(Pong request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Unit.Value);
    }
}

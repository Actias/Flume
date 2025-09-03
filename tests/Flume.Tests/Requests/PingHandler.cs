using System.Threading;
using System.Threading.Tasks;
using Flume.Handlers;

namespace Flume.Tests.Requests;

public class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Pong");
    }
}

using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1849 // Specifically using Thread.Sleep for demonstration purposes

namespace Flume.Tests.Requests;

internal sealed record SyncRequest(int Value) : IRequest<int>;

internal sealed class SyncRequestHandler : IRequestHandler<SyncRequest, int>
{
    public Task<int> Handle(SyncRequest request, CancellationToken cancellationToken = default)
    {
        Thread.Sleep(1000);

        return Task.FromResult<int>(request.Value);
    }
}
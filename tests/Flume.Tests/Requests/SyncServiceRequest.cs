using System.Threading;
using System.Threading.Tasks;
using Flume.Tests.Services;

namespace Flume.Tests.Requests;

internal sealed record SyncServiceRequest(int Value) : IRequest<int>;

internal sealed class SyncServiceRequestHandler(IMockService mockService) : IRequestHandler<SyncServiceRequest, int>
{
    public Task<int> Handle(SyncServiceRequest request, CancellationToken cancellationToken = default)
    {
        mockService.SyncTask();

        return Task.FromResult(request.Value);
    }
}
using System.Threading;
using System.Threading.Tasks;
using Flume.Tests.Services;

namespace Flume.Tests.Requests;

internal sealed record AsyncServiceRequest(int Value) : IRequest<int>;

internal sealed class AsyncServiceRequestHandler(IMockService mockService) : IRequestHandler<AsyncServiceRequest, int>
{
    public async Task<int> Handle(AsyncServiceRequest request, CancellationToken cancellationToken = default)
    {
        await mockService.AsyncTask();

        return request.Value;
    }
}
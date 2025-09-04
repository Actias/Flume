using System.Threading;
using System.Threading.Tasks;

namespace Flume.Tests.Requests;

internal sealed record AsyncRequest(int Value) : IRequest<int>;

internal sealed class AsyncRequestHandler : IRequestHandler<AsyncRequest, int>
{
    public async Task<int> Handle(AsyncRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

        return request.Value;
    }
}
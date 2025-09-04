using System.Threading;
using System.Threading.Tasks;

namespace Flume.Tests.Requests;

internal sealed record CustomRequest(int Value1, int Value2) : IRequest<CustomResponse>;

internal sealed record CustomResponse(int Value);

internal sealed class CustomRequestHandler
    : IRequestHandler<CustomRequest, CustomResponse>
{
    public Task<CustomResponse> Handle(CustomRequest request, CancellationToken cancellationToken = default)
    {
        var result = new CustomResponse(request.Value1 + request.Value2);

        return Task.FromResult(result);
    }
}
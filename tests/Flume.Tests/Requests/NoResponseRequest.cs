using System.Threading;
using System.Threading.Tasks;

namespace Flume.Tests.Requests;

internal sealed record NoResponseRequest : IRequest;

internal sealed class NoResponseRequestHandler : IRequestHandler<NoResponseRequest>
{
    public Task Handle(NoResponseRequest request, CancellationToken cancellationToken = default)
    {
        // Intentionally does nothing and returns a completed task
        return Task.CompletedTask;
    }
}
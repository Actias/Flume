namespace Flume.Comparison.MediatR;

/// <summary>
/// MediatR handler implementation
/// </summary>
public class MediatRHandler : global::MediatR.IRequestHandler<MediatRRequest, string>
{
    public Task<string> Handle(MediatRRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"MediatR processed: {request.Message}");
    }
}

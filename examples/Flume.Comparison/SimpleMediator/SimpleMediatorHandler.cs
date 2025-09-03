namespace Flume.Comparison.SimpleMediator;

/// <summary>
/// Flume handler implementation
/// </summary>
public class FlumeHandler : Handlers.IRequestHandler<FlumeRequest, string>
{
    public Task<string> Handle(FlumeRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Flume processed: {request.Message}");
    }
}

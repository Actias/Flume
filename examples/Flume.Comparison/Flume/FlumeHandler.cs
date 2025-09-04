namespace Flume.Comparison.Flume;

/// <summary>
/// Flume handler implementation
/// </summary>
public class FlumeHandler : IRequestHandler<FlumeRequest, string>
{
    public Task<string> Handle(FlumeRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Flume processed: {request.Message}");
    }
}

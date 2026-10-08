using Microsoft.Extensions.DependencyInjection;

namespace Flume.Samples;

/// <summary>
/// Registers the sample handlers. Wrapper instances are cached per request type; there is no cache-size switch.
/// </summary>
public static class PerformanceConfigurationExample
{
    public static IServiceCollection AddSampleFlume(this IServiceCollection services) =>
        services.AddFlume(cfg => cfg.RegisterServicesFromAssemblyContaining<PerformanceTestHandler>());
}

/// <summary>
/// Example request for the sample registration helper
/// </summary>
public record PerformanceTestRequest(string Message) : IRequest<string>;

/// <summary>
/// Example handler for the sample registration helper
/// </summary>
public class PerformanceTestHandler : IRequestHandler<PerformanceTestRequest, string>
{
    public Task<string> Handle(PerformanceTestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Processed: {request.Message}");
    }
}
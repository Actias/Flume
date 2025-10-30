using Flume;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Samples;

/// <summary>
/// Example demonstrating how to configure Flume with different performance strategies
/// </summary>
public static class PerformanceConfigurationExample
{
    /// <summary>
    /// Configure Flume for MediatR compatibility - minimal caching
    /// </summary>
    public static IServiceCollection AddFlumeMediatRCompatible(this IServiceCollection services)
    {
        var configuration = new FlumeConfiguration()
            .WithPerformanceStrategy(PerformanceStrategy.MediatRCompatible)
            .RegisterServicesFromAssemblyContaining<PerformanceTestHandler>();

        return services.AddFlume(configuration);
    }

    /// <summary>
    /// Configure Flume for balanced performance - moderate caching
    /// </summary>
    public static IServiceCollection AddFlumeBalanced(this IServiceCollection services)
    {
        var configuration = new FlumeConfiguration()
            .WithPerformanceStrategy(PerformanceStrategy.Balanced)
            .RegisterServicesFromAssemblyContaining<PerformanceTestHandler>();

        return services.AddFlume(configuration);
    }

    /// <summary>
    /// Configure Flume for maximum performance - aggressive caching
    /// </summary>
    public static IServiceCollection AddFlumeMaximumPerformance(this IServiceCollection services)
    {
        var configuration = new FlumeConfiguration()
            .WithPerformanceStrategy(PerformanceStrategy.MaximumPerformance)
            .RegisterServicesFromAssemblyContaining<PerformanceTestHandler>();

        return services.AddFlume(configuration);
    }

    /// <summary>
    /// Configure Flume with custom cache settings for fine-grained control
    /// </summary>
    public static IServiceCollection AddFlumeCustomCache(this IServiceCollection services)
    {
        var configuration = new FlumeConfiguration()
            .WithCustomCache(cache =>
            {
                // Custom cache sizes for different handler types
                cache.RequestHandlerCacheSize = 2000;
                cache.NotificationHandlerCacheSize = 500;
                cache.StreamRequestHandlerCacheSize = 1000;
                cache.TypeInfoCacheSize = 5000;
                cache.ServiceResolutionCacheSize = 2000;
                cache.CompiledPipelineCacheSize = 1000;
                
                // Enable cache warming for better startup performance
                cache.EnableCacheWarming = true;
                
                // Use LFU eviction policy
                cache.EvictionPolicy = CacheEvictionPolicy.LFU;
                
                // Enable cache statistics for monitoring
                cache.EnableCacheStatistics = true;
                
                // Set cache TTL to 30 minutes
                cache.CacheTtlMinutes = 30;
            })
            .RegisterServicesFromAssemblyContaining<PerformanceTestHandler>();

        return services.AddFlume(configuration);
    }

    /// <summary>
    /// Configure Flume with memory-optimized cache settings
    /// </summary>
    public static IServiceCollection AddFlumeMemoryOptimized(this IServiceCollection services)
    {
        var configuration = new FlumeConfiguration()
            .WithCustomCache(cache =>
            {
                // Smaller cache sizes to reduce memory usage
                cache.RequestHandlerCacheSize = 500;
                cache.NotificationHandlerCacheSize = 200;
                cache.StreamRequestHandlerCacheSize = 300;
                cache.TypeInfoCacheSize = 1000;
                cache.ServiceResolutionCacheSize = 500;
                cache.CompiledPipelineCacheSize = 500;
                
                // Disable cache warming to reduce startup memory
                cache.EnableCacheWarming = false;
                
                // Use FIFO eviction to keep memory usage predictable
                cache.EvictionPolicy = CacheEvictionPolicy.FIFO;
                
                // Disable statistics to reduce overhead
                cache.EnableCacheStatistics = false;
                
                // Short TTL to prevent memory leaks
                cache.CacheTtlMinutes = 10;
            })
            .RegisterServicesFromAssemblyContaining<PerformanceTestHandler>();

        return services.AddFlume(configuration);
    }

    /// <summary>
    /// Configure Flume with high-throughput cache settings
    /// </summary>
    public static IServiceCollection AddFlumeHighThroughput(this IServiceCollection services)
    {
        var configuration = new FlumeConfiguration()
            .WithCustomCache(cache =>
            {
                // Large cache sizes for maximum hit rates
                cache.RequestHandlerCacheSize = 20000;
                cache.NotificationHandlerCacheSize = 10000;
                cache.StreamRequestHandlerCacheSize = 5000;
                cache.TypeInfoCacheSize = 50000;
                cache.ServiceResolutionCacheSize = 20000;
                cache.CompiledPipelineCacheSize = 10000;
                
                // Enable cache warming for immediate performance
                cache.EnableCacheWarming = true;
                
                // Use LRU for optimal performance
                cache.EvictionPolicy = CacheEvictionPolicy.LRU;
                
                // Enable statistics for monitoring
                cache.EnableCacheStatistics = true;
                
                // No TTL for maximum performance
                cache.CacheTtlMinutes = 0;
            })
            .RegisterServicesFromAssemblyContaining<PerformanceTestHandler>();

        return services.AddFlume(configuration);
    }
}

/// <summary>
/// Example request for testing performance configurations
/// </summary>
public record PerformanceTestRequest(string Message) : IRequest<string>;

/// <summary>
/// Example handler for testing performance configurations
/// </summary>
public class PerformanceTestHandler : IRequestHandler<PerformanceTestRequest, string>
{
    public Task<string> Handle(PerformanceTestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Processed: {request.Message}");
    }
}

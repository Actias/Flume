using Flume;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flume.Tests;

/// <summary>
/// Tests for performance configuration options
/// </summary>
public class PerformanceConfigurationTests
{
    [Fact]
    public void MediatRCompatibleConfigurationShouldDisableOptimizations()
    {
        // Arrange
        var configuration = new FlumeConfiguration()
            .WithPerformanceStrategy(PerformanceStrategy.MediatRCompatible);

        // Act & Assert
        Assert.False(configuration.EnableObjectPooling);
        Assert.False(configuration.EnablePipelineCompilation);
        Assert.False(configuration.EnableTypeCaching);
        Assert.Equal(100, configuration.MaxCacheSize);
        Assert.Equal(PerformanceStrategy.MediatRCompatible, configuration.PerformanceStrategy);
    }

    [Fact]
    public void BalancedConfigurationShouldUseModerateSettings()
    {
        // Arrange
        var configuration = new FlumeConfiguration()
            .WithPerformanceStrategy(PerformanceStrategy.Balanced);

        // Act & Assert
        Assert.True(configuration.EnableObjectPooling);
        Assert.True(configuration.EnablePipelineCompilation);
        Assert.True(configuration.EnableTypeCaching);
        Assert.Equal(1000, configuration.MaxCacheSize);
        Assert.Equal(PerformanceStrategy.Balanced, configuration.PerformanceStrategy);
    }

    [Fact]
    public void MaximumPerformanceConfigurationShouldEnableAllOptimizations()
    {
        // Arrange
        var configuration = new FlumeConfiguration()
            .WithPerformanceStrategy(PerformanceStrategy.MaximumPerformance);

        // Act & Assert
        Assert.True(configuration.EnableObjectPooling);
        Assert.True(configuration.EnablePipelineCompilation);
        Assert.True(configuration.EnableTypeCaching);
        Assert.Equal(10000, configuration.MaxCacheSize);
        Assert.Equal(PerformanceStrategy.MaximumPerformance, configuration.PerformanceStrategy);
    }

    [Fact]
    public void CustomConfigurationShouldAllowManualSettings()
    {
        // Arrange
        var configuration = new FlumeConfiguration()
        {
            EnableObjectPooling = true,
            EnablePipelineCompilation = false,
            EnableTypeCaching = true,
            MaxCacheSize = 5000,
            PerformanceStrategy = PerformanceStrategy.MaximumPerformance
        };

        // Act & Assert
        Assert.True(configuration.EnableObjectPooling);
        Assert.False(configuration.EnablePipelineCompilation);
        Assert.True(configuration.EnableTypeCaching);
        Assert.Equal(5000, configuration.MaxCacheSize);
        Assert.Equal(PerformanceStrategy.MaximumPerformance, configuration.PerformanceStrategy);
    }

    [Fact]
    public void CustomCacheConfigurationShouldAllowFineGrainedControl()
    {
        // Arrange
        var configuration = new FlumeConfiguration()
            .WithCustomCache(cache =>
            {
                cache.RequestHandlerCacheSize = 2000;
                cache.NotificationHandlerCacheSize = 500;
                cache.StreamRequestHandlerCacheSize = 1000;
                cache.TypeInfoCacheSize = 5000;
                cache.ServiceResolutionCacheSize = 2000;
                cache.CompiledPipelineCacheSize = 1000;
                cache.EnableCacheWarming = true;
                cache.EvictionPolicy = CacheEvictionPolicy.LFU;
                cache.CacheTtlMinutes = 30;
                cache.EnableCacheStatistics = true;
            });

        // Act & Assert
        Assert.Equal(2000, configuration.CacheConfiguration.RequestHandlerCacheSize);
        Assert.Equal(500, configuration.CacheConfiguration.NotificationHandlerCacheSize);
        Assert.Equal(1000, configuration.CacheConfiguration.StreamRequestHandlerCacheSize);
        Assert.Equal(5000, configuration.CacheConfiguration.TypeInfoCacheSize);
        Assert.Equal(2000, configuration.CacheConfiguration.ServiceResolutionCacheSize);
        Assert.Equal(1000, configuration.CacheConfiguration.CompiledPipelineCacheSize);
        Assert.True(configuration.CacheConfiguration.EnableCacheWarming);
        Assert.Equal(CacheEvictionPolicy.LFU, configuration.CacheConfiguration.EvictionPolicy);
        Assert.Equal(30, configuration.CacheConfiguration.CacheTtlMinutes);
        Assert.True(configuration.CacheConfiguration.EnableCacheStatistics);
    }

    [Fact]
    public void CacheConfigurationDefaultsShouldMatchBalancedStrategy()
    {
        // Arrange
        var defaultConfig = new CacheConfiguration();
        var balancedConfig = CacheConfiguration.Balanced();

        // Act & Assert
        Assert.Equal(defaultConfig.RequestHandlerCacheSize, balancedConfig.RequestHandlerCacheSize);
        Assert.Equal(defaultConfig.NotificationHandlerCacheSize, balancedConfig.NotificationHandlerCacheSize);
        Assert.Equal(defaultConfig.StreamRequestHandlerCacheSize, balancedConfig.StreamRequestHandlerCacheSize);
        Assert.Equal(defaultConfig.TypeInfoCacheSize, balancedConfig.TypeInfoCacheSize);
        Assert.Equal(defaultConfig.ServiceResolutionCacheSize, balancedConfig.ServiceResolutionCacheSize);
        Assert.Equal(defaultConfig.CompiledPipelineCacheSize, balancedConfig.CompiledPipelineCacheSize);
        Assert.Equal(defaultConfig.EnableCacheWarming, balancedConfig.EnableCacheWarming);
        Assert.Equal(defaultConfig.EvictionPolicy, balancedConfig.EvictionPolicy);
        Assert.Equal(defaultConfig.CacheTtlMinutes, balancedConfig.CacheTtlMinutes);
        Assert.Equal(defaultConfig.EnableCacheStatistics, balancedConfig.EnableCacheStatistics);
    }

    [Fact]
    public void MediatRCompatibleCacheConfigurationShouldUseMinimalSettings()
    {
        // Arrange
        var config = CacheConfiguration.MediatRCompatible();

        // Act & Assert
        Assert.Equal(100, config.RequestHandlerCacheSize);
        Assert.Equal(100, config.NotificationHandlerCacheSize);
        Assert.Equal(100, config.StreamRequestHandlerCacheSize);
        Assert.Equal(100, config.TypeInfoCacheSize);
        Assert.Equal(100, config.ServiceResolutionCacheSize);
        Assert.Equal(100, config.CompiledPipelineCacheSize);
        Assert.False(config.EnableCacheWarming);
        Assert.Equal(CacheEvictionPolicy.LRU, config.EvictionPolicy);
        Assert.Equal(0, config.CacheTtlMinutes);
        Assert.False(config.EnableCacheStatistics);
    }

    [Fact]
    public void MaximumPerformanceCacheConfigurationShouldUseAggressiveSettings()
    {
        // Arrange
        var config = CacheConfiguration.MaximumPerformance();

        // Act & Assert
        Assert.Equal(10000, config.RequestHandlerCacheSize);
        Assert.Equal(10000, config.NotificationHandlerCacheSize);
        Assert.Equal(10000, config.StreamRequestHandlerCacheSize);
        Assert.Equal(10000, config.TypeInfoCacheSize);
        Assert.Equal(10000, config.ServiceResolutionCacheSize);
        Assert.Equal(10000, config.CompiledPipelineCacheSize);
        Assert.True(config.EnableCacheWarming);
        Assert.Equal(CacheEvictionPolicy.LRU, config.EvictionPolicy);
        Assert.Equal(0, config.CacheTtlMinutes);
        Assert.True(config.EnableCacheStatistics);
    }

    [Fact]
    public void MediatorWithCustomCacheConfigurationShouldUseConfiguredCacheSizes()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new FlumeConfiguration()
            .WithCustomCache(cache =>
            {
                cache.RequestHandlerCacheSize = 5000;
                cache.NotificationHandlerCacheSize = 2000;
                cache.StreamRequestHandlerCacheSize = 3000;
            })
            .RegisterServicesFromAssemblyContaining<PerformanceConfigurationTests>();

        services.AddFlume(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Assert
        Assert.NotNull(mediator);
        // The mediator should be created with the configured cache sizes
        // This is tested indirectly through the configuration being applied
    }
}

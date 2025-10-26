# Flume Custom Cache Configuration

Flume now supports fine-grained cache configuration options that allow developers to customize caching behavior for specific use cases while maintaining full backward compatibility.

## Custom Cache Configuration

### CacheConfiguration Class

The `CacheConfiguration` class provides detailed control over all caching aspects:

```csharp
public class CacheConfiguration
{
    // Individual cache sizes
    public int RequestHandlerCacheSize { get; set; } = 1000;
    public int NotificationHandlerCacheSize { get; set; } = 1000;
    public int StreamRequestHandlerCacheSize { get; set; } = 1000;
    public int TypeInfoCacheSize { get; set; } = 1000;
    public int ServiceResolutionCacheSize { get; set; } = 1000;
    public int CompiledPipelineCacheSize { get; set; } = 1000;
    
    // Cache behavior options
    public bool EnableCacheWarming { get; set; } = false;
    public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.LRU;
    public int CacheTtlMinutes { get; set; } = 0;
    public bool EnableCacheStatistics { get; set; } = false;
}
```

### Cache Eviction Policies

```csharp
public enum CacheEvictionPolicy
{
    LRU,    // Least Recently Used (default)
    LFU,    // Least Frequently Used
    FIFO,   // First In First Out
    Random  // Random eviction
}
```

## Usage Examples

### 1. Basic Custom Cache Configuration

```csharp
services.AddFlume(cfg => cfg
    .WithCustomCache(cache =>
    {
        cache.RequestHandlerCacheSize = 2000;
        cache.NotificationHandlerCacheSize = 500;
        cache.EnableCacheWarming = true;
        cache.EvictionPolicy = CacheEvictionPolicy.LFU;
    })
    .RegisterServicesFromAssemblyContaining<MyHandler>());
```

### 2. Memory-Optimized Configuration

```csharp
services.AddFlume(cfg => cfg
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
    .RegisterServicesFromAssemblyContaining<MyHandler>());
```

### 3. High-Throughput Configuration

```csharp
services.AddFlume(cfg => cfg
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
    .RegisterServicesFromAssemblyContaining<MyHandler>());
```

### 4. Monitoring and Statistics

```csharp
services.AddFlume(cfg => cfg
    .WithCustomCache(cache =>
    {
        cache.EnableCacheStatistics = true;
        cache.CacheTtlMinutes = 30; // 30-minute TTL
        cache.EvictionPolicy = CacheEvictionPolicy.LRU;
    })
    .RegisterServicesFromAssemblyContaining<MyHandler>());
```

## Predefined Cache Configurations

### MediatR Compatible
```csharp
var config = CacheConfiguration.MediatRCompatible();
// All cache sizes = 100, minimal features enabled
```

### Balanced (Default)
```csharp
var config = CacheConfiguration.Balanced();
// All cache sizes = 1000, standard features enabled
```

### Maximum Performance
```csharp
var config = CacheConfiguration.MaximumPerformance();
// All cache sizes = 10000, all features enabled
```

## Configuration Options Reference

| Option | Description | Default | Range |
|--------|-------------|---------|-------|
| `RequestHandlerCacheSize` | Max size for request handler cache | 1000 | 1-100000 |
| `NotificationHandlerCacheSize` | Max size for notification handler cache | 1000 | 1-100000 |
| `StreamRequestHandlerCacheSize` | Max size for stream request handler cache | 1000 | 1-100000 |
| `TypeInfoCacheSize` | Max size for type information cache | 1000 | 1-100000 |
| `ServiceResolutionCacheSize` | Max size for service resolution cache | 1000 | 1-100000 |
| `CompiledPipelineCacheSize` | Max size for compiled pipeline cache | 1000 | 1-100000 |
| `EnableCacheWarming` | Enable cache warming during startup | false | true/false |
| `EvictionPolicy` | Cache eviction policy | LRU | LRU/LFU/FIFO/Random |
| `CacheTtlMinutes` | Time-to-live for cached items (0 = no expiration) | 0 | 0-1440 |
| `EnableCacheStatistics` | Enable cache statistics collection | false | true/false |

## Performance Impact

### Cache Sizes
- **Smaller caches**: Less memory usage, more cache misses
- **Larger caches**: More memory usage, better hit rates
- **Individual tuning**: Optimize each cache type based on usage patterns

### Eviction Policies
- **LRU**: Best for most applications, evicts least recently used items
- **LFU**: Good for stable workloads, evicts least frequently used items
- **FIFO**: Predictable memory usage, evicts oldest items first
- **Random**: Simple implementation, evicts items randomly

### Cache Warming
- **Enabled**: Pre-populates caches during startup for immediate performance
- **Disabled**: Caches populate on-demand, faster startup

### TTL (Time-To-Live)
- **0 (no expiration)**: Maximum performance, potential memory growth
- **Short TTL (5-30 min)**: Prevents memory leaks, slight performance impact
- **Long TTL (60+ min)**: Good balance for most applications

## Backward Compatibility

✅ **All existing code continues to work unchanged**
✅ **Default behavior remains identical**
✅ **New features are opt-in only**
✅ **No breaking changes**

## Migration Guide

### From Previous Versions
No changes required - existing applications get identical behavior.

### Adding Custom Cache Configuration
```csharp
// Before (still works)
services.AddFlume(cfg => cfg.RegisterServicesFromAssemblyContaining<MyHandler>());

// After (optional enhancement)
services.AddFlume(cfg => cfg
    .WithCustomCache(cache => cache.RequestHandlerCacheSize = 2000)
    .RegisterServicesFromAssemblyContaining<MyHandler>());
```

### Performance Strategy Integration
```csharp
// Use predefined strategy
services.AddFlume(cfg => cfg.WithPerformanceStrategy(PerformanceStrategy.MaximumPerformance));

// Override specific cache settings
services.AddFlume(cfg => cfg
    .WithPerformanceStrategy(PerformanceStrategy.MaximumPerformance)
    .WithCustomCache(cache => cache.RequestHandlerCacheSize = 5000));
```

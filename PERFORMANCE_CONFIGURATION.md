# Flume Performance Configuration

Flume now supports configurable performance strategies that allow you to tune the caching behavior to match your specific needs, from MediatR compatibility to maximum performance optimization.

## Performance Strategies

### MediatRCompatible
- **Purpose**: Minimal caching for compatibility with MediatR performance characteristics
- **Settings**:
  - `EnableObjectPooling = false`
  - `EnablePipelineCompilation = false`
  - `EnableTypeCaching = false`
  - `MaxCacheSize = 100`

### Balanced (Default)
- **Purpose**: Moderate caching with good performance and memory usage
- **Settings**:
  - `EnableObjectPooling = true`
  - `EnablePipelineCompilation = true`
  - `EnableTypeCaching = true`
  - `MaxCacheSize = 1000`

### MaximumPerformance
- **Purpose**: Aggressive caching, pre-compilation, and optimization
- **Settings**:
  - `EnableObjectPooling = true`
  - `EnablePipelineCompilation = true`
  - `EnableTypeCaching = true`
  - `MaxCacheSize = 10000`

## Usage Examples

### Using Performance Strategies

```csharp
// MediatR Compatible - minimal caching
services.AddFlume(cfg => cfg.WithPerformanceStrategy(PerformanceStrategy.MediatRCompatible));

// Balanced - moderate caching (default)
services.AddFlume(cfg => cfg.WithPerformanceStrategy(PerformanceStrategy.Balanced));

// Maximum Performance - aggressive caching
services.AddFlume(cfg => cfg.WithPerformanceStrategy(PerformanceStrategy.MaximumPerformance));
```

### Custom Configuration

```csharp
services.AddFlume(cfg => cfg
    .WithPerformanceStrategy(PerformanceStrategy.MaximumPerformance)
    .RegisterServicesFromAssemblyContaining<MyHandler>());
```

### Manual Configuration

```csharp
var configuration = new FlumeConfiguration()
{
    EnableObjectPooling = true,
    EnablePipelineCompilation = true,
    EnableTypeCaching = true,
    MaxCacheSize = 5000,
    PerformanceStrategy = PerformanceStrategy.MaximumPerformance
};

services.AddFlume(configuration);
```

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `EnableObjectPooling` | Enable object pooling for handler wrappers | `true` |
| `EnablePipelineCompilation` | Enable pre-compiled pipeline behaviors | `true` |
| `EnableTypeCaching` | Enable aggressive type caching | `true` |
| `MaxCacheSize` | Maximum cache size for handler wrappers | `1000` |
| `PerformanceStrategy` | Performance optimization strategy | `Balanced` |

## Performance Impact

The performance strategy affects:

1. **Memory Usage**: Higher cache sizes use more memory but provide better performance
2. **Startup Time**: More aggressive caching may increase startup time
3. **Runtime Performance**: Better caching improves runtime performance
4. **GC Pressure**: Object pooling reduces GC pressure

Choose the strategy that best fits your application's requirements:

- **MediatRCompatible**: Use when migrating from MediatR or when memory is constrained
- **Balanced**: Use for most applications as a good default
- **MaximumPerformance**: Use when performance is critical and memory is available

## Migration Guide

If you're upgrading from a previous version of Flume, the default behavior remains the same (Balanced strategy). No changes are required unless you want to optimize for specific scenarios.

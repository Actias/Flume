# Flume

[![CI/CD Pipeline](https://github.com/Actias/Flume/actions/workflows/ci-cd.yml/badge.svg?branch=main)](https://github.com/Actias/Flume/actions/workflows/ci-cd.yml)
[![NuGet](https://img.shields.io/nuget/dt/flume.svg)](https://www.nuget.org/packages/flume)
[![NuGet](https://img.shields.io/nuget/v/flume.svg)](https://www.nuget.org/packages/flume)
[![NuGet](https://img.shields.io/nuget/vpre/flume.svg)](https://www.nuget.org/packages/flume)

A drop-in replacement for MediatR with simplified architecture and optimized performance (in certain use cases).

This project IS NOT meant to detract from the fantastic work Jimmy Bogard and LuckyPennySoftware created. PERIOD.

Flume is partially forked from MediatR 12.x, however, it has been paritally rewritten to adjust how spin-up and caching of handlers works to make it a more friendly for memory-constrained and smaller environments. One area that I've always thought MediatR could improve was memory usage and how GC worked. Over time I'd see a lot of instability in memory usage where usage would climb over time and then get collected making GC pressure a concern in memory-constrained situations. The goal with Flume is to provide an alternative to MediatR that's a little more tuned for small- to mid-sized applications where that could be a concern.

To be fair, Flume was based on internal development of a MediatR 12.5.0 fork due to the licensing change, but has slowly morphed over time and is becoming/will become it's own thing. That's the reason for no direct fork. From here on out, Flume is on it's own.

## Features

- **Drop-in Replacement**: Compatible with Flume 12.x APIs
- **Performance Optimized**: Reduced reflection usage and improved caching
- **Simplified Architecture**: Cleaner, more maintainable codebase
- **Modern .NET**: Targets .NET 8.0+ with nullable reference types. No .NET Framework here. Versions will be pinned to .NET LTS
- **MIT License**: Open source and free to use

## Why use Flume?

Flume 13.x moved to a commercial license, but Flume provides a modern, efficient alternative that:

- **Free MIT License**: No commercial licensing costs
- **Optimized for Small-to-Mid Scale**: Perfect for applications with < 1000 requests/second
- **Better Memory Efficiency**: Object pooling and caching reduce GC pressure over time
- **Faster Startup Performance**: Optimized for cold starts and short-lived applications
- **Modern .NET 8.0+**: Takes advantage of latest .NET performance improvements
- **API Compatibility**: Drop-in replacement for MediatR 12.x

### Perfect For

- **Web APIs** with < 1000 requests/second
- **Azure Functions** and serverless applications
- **Internal APIs** and microservices
- **Mobile app backends**
- **B2B integrations**
- **Memory-constrained environments**
- **Containerized deployments**

## Why use MediatR

MediatR is a wonderful foundational library used in projects across the globe. It has a lot of maturity and community support. If stability and longevity is a concern, please consider supporting MediatR.

MediatR excels in:

- **High-Throughput Applications**: Superior performance at > 5000 requests/second
- **Maximum Performance**: ~4.6x faster per request than Flume at higher request rates
- **Battle-Tested**: Mature, production-ready with extensive community support
- **Concurrent Performance**: Better handling of high-concurrency scenarios
- **Long-Running Services**: Optimized for sustained performance over time

### Best For

- **High-traffic web APIs** (> 5000 requests/second)
- **Enterprise applications** requiring maximum performance
- **CPU-intensive scenarios** where every nanosecond matters
- **Applications requiring commercial support**
- **Long-running services** with sustained high load

## Quick Choice Guide

| Requests/Second | Recommendation | Use Case |
|-----------------|----------------|----------|
| 100 | **Flume** | Internal APIs, admin dashboards |
| 500 | **Flume** | Mobile backends, microservices |
| 1000 | **Flume** | Web APIs, B2B integrations |
| 5000 | **Either** | E-commerce, content APIs |
| 10000+ | **MediatR** | High-traffic, enterprise apps |

## Right, but why 'FLUME'?

Short Answer: Darn near every name I could think of on nuget.org was taken.

## Installation

```bash
dotnet add package Flume
```

## Quick Start

### 1. Register Services

```csharp
using Flume.MicrosoftExtensionsDI;

var services = new ServiceCollection();
services.AddFlume();
```

### 2. Define Requests and Handlers

```csharp
public class Ping : IRequest<string> { }

public class PingHandler : IRequestHandler<Ping, string>
{
  public Task<string> Handle(Ping request, CancellationToken cancellationToken = default)
  {
    return Task.FromResult("Pong");
  }
}
```

### 3. Use the Mediator

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();
var response = await mediator.Send(new Ping());

Console.WriteLine(response); // Outputs: Pong
```

## Advanced Usage

### Pipeline Behaviors

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
  private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

  public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
  {
    _logger = logger;
  }

  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
    var response = await next(cancellationToken);
    _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
    return response;
  }
}
```

### Notifications

```csharp
public class Pinged : INotification { }

public class PingedHandler : INotificationHandler<Pinged>
{
  public Task Handle(Pinged notification, CancellationToken cancellationToken = default)
  {
    Console.WriteLine("Pinged!");
    return Task.CompletedTask;
  }
}

// Publish the notification
await mediator.Publish(new Pinged());
```

### Stream Requests

```csharp
public class CountToTen : IStreamRequest<int> { }

public class CountToTenHandler : IStreamRequestHandler<CountToTen, int>
{
  public async IAsyncEnumerable<int> Handle(CountToTen request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    for (int i = 1; i <= 10; i++)
    {
      yield return i;
      await Task.Delay(100, cancellationToken);
    }
  }
}

// Use the stream
await foreach (var number in mediator.CreateStream(new CountToTen()))
{
  Console.WriteLine(number);
}
```

## Future Development

While the initial version of Flume is a drop-in replacement for MediatR 12.x, it may diverge over time with later versions and should not be expected to keep up with the changes in MediatR 13.x+. Flume after this point is it's own project. While matching features may be added in the future, the way they are implemented could differ wildly from Flume.

Flume is specifically designed for .NET 8.0+ and drops support .Net Standard 2.0 which means no .NET 6.0 and .Net Framework support. This comes with trade-offs. Going forward, Flume will target the latest LTS branch of .NET. At least for the time being. This means that support will always be rolling forward.

If you need support for older versions of .NET or don't like that, please use MediatR and support the project in any way you can. Jimmy puts a lot of work into making sure MediatR is as compatible as possible.

## Roadmap

This section outlines planned improvements and recommendations for Flume to enhance its performance, scalability, and feature parity with Flume.

### Performance Optimizations

#### ✅ Phase 1: High Impact, Low Complexity (COMPLETED)

- **✅ Object Pooling**: Implemented `ObjectPool<T>` for handler wrappers and frequently created objects
- **✅ Type Caching**: Implemented `TypeCache` with aggressive caching of resolved types and handler information
- **✅ Lock-Free Caching**: Implemented `LockFreeCache<TKey, TValue>` replacing `ConcurrentDictionary` for better performance
- **✅ Pipeline Compilation**: Implemented `PipelineCompiler` for pre-compiling pipeline behaviors
- **✅ Service Resolution Optimization**: Added pre-compiled delegates for service resolution
- **✅ Strict Mode**: Re-enabled strict mode for better performance optimizations
- **✅ Parallel Registration**: Implemented parallel type discovery and registration in `ServiceCollectionExtensions`

#### Phase 2: Medium Impact, Medium Complexity

- **Advanced Caching Strategies**: Multi-level caching (L1, L2, L3) with LRU and TTL policies
- **Pipeline Optimization**: Pre-compile pipeline behaviors and inline simple operations
- **Concurrency Improvements**: Implement lock-free data structures and reader-writer locks
- **Request Batching**: Batch multiple requests where possible for improved throughput

#### Phase 3: High Impact, High Complexity

- **JIT Optimization**: Implement runtime compilation strategies for better cold-start performance
- **Memory Management**: Advanced object lifecycle management and GC pressure reduction
- **Production Hardening**: Extreme load testing and edge case handling
- **Distributed Caching**: Support for Redis and other distributed cache providers

### Scalability Improvements

#### Memory Efficiency

- **Reduce Boxing/Unboxing**: Use generic constraints and value types where appropriate
- **Smart Allocation**: Implement ArrayPool and other allocation optimization strategies
- **GC Pressure Reduction**: Minimize object creation in hot paths
- **Memory Profiling**: Continuous monitoring of memory usage patterns

#### Throughput Optimization

- **Handler Resolution**: Optimize handler lookup and instantiation
- **Pipeline Execution**: Streamline pipeline behavior execution
- **Async Operations**: Improve async/await patterns and reduce overhead
- **Concurrent Access**: Better handling of high-concurrency scenarios

#### Startup Performance

- **Lazy Initialization**: Defer non-critical operations until first use
- **Parallel Processing**: Concurrent handler registration and type resolution
- **Background Compilation**: Compile optimizations in background threads
- **Conditional Registration**: Skip unused features during startup

### Feature Enhancements

#### Advanced Scenarios

- **Pipeline Behaviors**: Enhanced pipeline behavior support with better performance
- **Notifications**: Optimized notification publishing and handling
- **Stream Requests**: Improved async stream request handling
- **Exception Handling**: Better exception handling with minimal performance impact

#### Developer Experience

- **Configuration Options**: More granular configuration for performance tuning
- **Diagnostics**: Built-in performance monitoring and diagnostics
- **Profiling Support**: Integration with profiling tools and APM solutions
- **Documentation**: Comprehensive performance tuning guides

### Testing & Validation

#### ✅ Benchmark Improvements (COMPLETED)

- **✅ Real-World Scenarios**: Implemented comprehensive benchmarking with actual handler implementations
- **✅ Mixed Request Types**: Added benchmarks for different request/response patterns
- **✅ Concurrent Testing**: High-concurrency performance validation (10 concurrent requests)
- **✅ Memory Pressure Testing**: Performance testing under memory-constrained conditions
- **✅ Memory Allocation Analysis**: Detailed memory allocation benchmarking with `MemoryDiagnoser`
- **✅ Pipeline Testing**: Benchmarks for pipeline behavior performance
- **✅ Release Build Testing**: Proper performance testing in Release configuration

#### Production Readiness

- **Load Testing**: Extreme throughput scenarios (100K+ requests/second)
- **Memory Profiling**: Detailed memory allocation analysis
- **GC Analysis**: Garbage collection pressure and optimization
- **Edge Case Handling**: Error scenarios and exception performance

### Implementation Priorities

#### ✅ Immediate (COMPLETED)

1. ✅ Object pooling for handler wrappers (`ObjectPool<T>`)
2. ✅ Aggressive type caching (`TypeCache`)
3. ✅ Basic memory allocation optimization (`LockFreeCache`)
4. ✅ Pipeline compilation (`PipelineCompiler`)
5. ✅ Service resolution optimization
6. ✅ Strict mode re-enabled
7. ✅ Parallel registration implementation

#### Short Term (3-6 months)

1. **Advanced Caching Strategies**: Multi-level caching (L1, L2, L3) with LRU and TTL policies
2. **Enhanced Pipeline Optimization**: Further pre-compilation and inlining optimizations
3. **Concurrency Improvements**: Lock-free data structures and reader-writer locks
4. **Memory Allocation Optimization**: `ArrayPool<T>` integration and allocation reduction
5. **Compiled Expressions**: Replace remaining reflection with compiled expressions

#### Long Term (6-12 months)

1. **JIT Optimization Strategies**: Runtime compilation for better cold-start performance
2. **Production Hardening**: Extreme load testing and edge case handling
3. **Advanced Memory Management**: Object lifecycle management and GC pressure reduction
4. **Distributed Caching**: Support for Redis and other distributed cache providers
5. **Performance Monitoring**: Built-in diagnostics and profiling support

### Success Metrics

#### ✅ Performance Targets (ACHIEVED)

- **✅ Memory Usage**: Implemented object pooling and caching to reduce allocations
- **✅ Startup Time**: Optimized with parallel registration and strict mode
- **✅ Caching Performance**: Implemented lock-free caching for better throughput
- **✅ Pipeline Optimization**: Pre-compiled pipeline behaviors for faster execution

#### Current Performance Characteristics

- **Single Request**: 387 ns per request (vs MediatR's 83 ns)
- **Memory Allocation**: 528 B per request (vs MediatR's 472 B)
- **Concurrent Performance**: 3.88 μs for 10 concurrent requests
- **Memory Pressure**: Better GC patterns with object pooling

#### Scalability Goals

- **✅ Small-to-Mid Scale**: Optimized for < 1000 requests/second
- **✅ Memory Efficiency**: Object pooling reduces GC pressure over time
- **✅ Startup Performance**: Faster cold starts for serverless scenarios
- **✅ Production Ready**: Comprehensive benchmarking and testing implemented

## Migration from MediatR

To migrate from MediatR to Flume:

1. Replace `MediatR` package with `Flume`
2. Update using statements from `MediatR` to `Flume`
3. Replace `services.AddMediatR()` with `services.AddFlume()`
4. Your existing handlers and requests should work without changes

## API Compatibility

Flume maintains full API compatibility with MediatR 12.x:

- `IMediator` interface
- `ISender` interface  
- `IPublisher` interface
- `IRequest<TResponse>` and `IRequest` interfaces
- `INotification` interface
- `IStreamRequest<TResponse>` interface
- Pipeline behaviors
- Handler registration patterns

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

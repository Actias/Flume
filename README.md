# Flume

A drop-in replacement for MediatR with simplified architecture and optimized performance (in certain use cases).

![CI](https://github.com/actias/flume/workflows/CI%3ACD%20Pipeline/badge.svg)
[![NuGet](https://img.shields.io/nuget/dt/flume.svg)](https://www.nuget.org/packages/flume)
[![NuGet](https://img.shields.io/nuget/v/flume.svg)](https://www.nuget.org/packages/flume)
[![NuGet](https://img.shields.io/nuget/vpre/flume.svg)](https://www.nuget.org/packages/flume)

This project IS NOT meant to detract from the fantastic work Jimmy Bogard and LuckyPennySoftware created. PERIOD.

This project was unfortunately created out of necessity due to working with customers that either can't afford or have a hard time justifying the cost of more software.

## Features

- **Drop-in Replacement**: Compatible with MediatR 12.x APIs
- **Performance Optimized**: Reduced reflection usage and improved caching
- **Simplified Architecture**: Cleaner, more maintainable codebase
- **Modern .NET**: Targets .NET 8.0+ with nullable reference types
- **MIT License**: Open source and free to use

## Why Flume?

MediatR 13.x moved to a commercial license, but Flume provides a modern, efficient alternative that:

- Maintains API compatibility with MediatR 12.x.
- Improves performance through better caching strategies for small- to mid-scale scenarios
- Reduces memory allocations during execution
- Simplifies the internal architecture
- Removes unnecessary complexity
- It's not just a fork. It's it's own thing with it's own roadmap from here on out.

## Why MediatR?

MediatR is a wonderful foundational library used in projects across the globe. It has a lot of maturity and community support. If stability and longevity is a concern, please consider supporting MediatR.

MediatR still has a lot of benefits:

- Scales better for larger systems
- Smaller memory footprint overall. Flume takes a "let's use it!" memory approach. To be clear, the amount of memory use difference between the two in common scenarios is negligible but on large scale systems with 10k+ requests per second over hundreds of Handlers, MediatR is your choice. Flume will focus on caching and memory optimizations which means it will almost ALWAYS use more memory than MediatR. Again, this isn't just a fork.
- Shorter spin up time. If you're using AZ Functions or short-lived APIs, MediatR will perform better here. The reason is that Flume is designed to take the hits upfront vs during runtime. This has it's trade-offs.

## Right, but why 'FLUME'?

Short Answer: Darn near every name I could think of on nuget.org was taken.

## Future Development

While the initial version of Flume is a drop-in replacement for MediatR 12.x, it may diverge over time with later versions and should not be expected to keep up with the changes in MediatR 13.x+. Flume after this point is it's own project. While matching features may be added in the future, the way they are implemented could differ wildly from MediatR.

Flume is specifically designed for .NET 8.0+ and drops support .Net Standard 2.0 which means no .NET 6.0 and .Net Framework support. This comes with trade-offs. Going forward, Flume will target the latest LTS branch of .NET. At least for the time being. This means that support will always be rolling forward.

If you need support for older versions of .NET or don't like that, please use MediatR and support the project in any way you can. Jimmy puts a lot of work into making sure MediatR is as compatible as possible.

## CI/CD and Release Process

This project uses GitHub Actions for continuous integration and deployment:

- **Build & Test**: Runs on every push and pull request
- **Pre-releases**: Automatically published to NuGet from the `develop` branch
- **Production Releases**: Automatically published to NuGet when tags are pushed
- **Quality Gates**: All builds treat warnings as errors, code analysis runs on PRs

See [BRANCHING_STRATEGY.md](BRANCHING_STRATEGY.md) for detailed information about the release process and branching strategy.

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

## Roadmap

This section outlines planned improvements and recommendations for Flume to enhance its performance, scalability, and feature parity with MediatR.

### Performance Optimizations

#### Phase 1: High Impact, Low Complexity

- **Object Pooling**: Implement pooling for handler wrappers, request/response objects, and cancellation tokens
- **Type Caching**: Aggressive caching of resolved types and handler information
- **Reflection Reduction**: Replace reflection with compiled expressions and pre-compiled delegates
- **Memory Allocation Analysis**: Profile and optimize hot-path allocations

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

#### Benchmark Improvements

- **Real-World Scenarios**: Test with actual handler implementations and complex pipelines
- **Mixed Request Types**: Benchmark with diverse request/response patterns
- **Concurrent Testing**: High-concurrency performance validation
- **Memory Pressure Testing**: Performance under memory-constrained conditions

#### Production Readiness

- **Load Testing**: Extreme throughput scenarios (100K+ requests/second)
- **Memory Profiling**: Detailed memory allocation analysis
- **GC Analysis**: Garbage collection pressure and optimization
- **Edge Case Handling**: Error scenarios and exception performance

### Implementation Priorities

#### Immediate (Next Release)

1. Object pooling for handler wrappers
2. Aggressive type caching
3. Basic memory allocation optimization

#### Short Term (3-6 months)

1. Advanced caching strategies
2. Pipeline optimization
3. Concurrency improvements

#### Long Term (6-12 months)

1. JIT optimization strategies
2. Production hardening
3. Advanced memory management

### Success Metrics

#### Performance Targets

- **Memory Usage**: Reduce per-operation allocation by 20-30%
- **Throughput**: Improve sustained throughput by 15-25%
- **Startup Time**: Reduce cold-start time by 30-40%
- **GC Pressure**: Reduce GC frequency by 25-35%

#### Scalability Goals

- **High Load**: Handle 50K+ requests/second without degradation
- **Memory Efficiency**: Maintain performance under memory pressure
- **Concurrent Access**: Scale linearly with additional CPU cores
- **Production Ready**: Handle real-world production workloads

## Migration from MediatR

To migrate from MediatR to Flume:

1. Replace `MediatR` package with `Flume`
2. Update using statements from `MediatR` to `Flume`
3. Replace `services.AddMediatR()` with `services.AddFlume()`
4. Your existing handlers and requests will work without changes

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

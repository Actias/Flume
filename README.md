# Flume

[![CI/CD Pipeline](https://github.com/Actias/Flume/actions/workflows/ci-cd.yml/badge.svg?branch=main)](https://github.com/Actias/Flume/actions/workflows/ci-cd.yml)
[![NuGet](https://img.shields.io/nuget/dt/flume.svg)](https://www.nuget.org/packages/flume)
[![NuGet](https://img.shields.io/nuget/v/flume.svg)](https://www.nuget.org/packages/flume)

A drop-in replacement for MediatR with simplified architecture and optimized performance (in certain use cases).

This project IS NOT meant to detract from the fantastic work Jimmy Bogard and LuckyPennySoftware created. PERIOD.

Flume is partially forked from MediatR 12.x, however, it has been partially rewritten to adjust how spin-up and caching of handlers works to make it a more friendly for memory-constrained and smaller environments. One area that I've always thought MediatR could improve was memory usage and how GC worked. Over time I'd see a lot of instability in memory usage where usage would climb over time and then get collected making GC pressure a concern in memory-constrained situations. The goal with Flume is to provide an alternative to MediatR that's a little more tuned for small- to mid-sized applications where that could be a concern.

## Features

- **Drop-in Replacement**: Compatible with MediatR 12.x APIs (`MediatR` becomes `Flume`, `AddMediatR` becomes `AddFlume`)
- **Performance Optimized**: Static wrapper cache per request type. Handlers and behaviors are resolved per call
- **Simplified Architecture**: Cleaner, more maintainable codebase
- **Modern .NET**: Multi-targets `net10.0` (LTS) and `net11.0`. No .NET Framework
- **MIT License**: Open source and free to use

## Why use Flume?

Flume is aimed at focusing on minimizing app startup costs and GC pressure while maintaining compatibility with MediatR 12. in memory constrained environments.

- **Free MIT License**: No commercial licensing costs
- **Optimized for Small-to-Mid Scale**: Perfect for applications with < 1000 requests/second
- **Less per-request work**: The pipeline wrapper is cached per closed request type and does not retain scoped handlers
- **Faster Startup Performance**: Optimized for cold starts and short-lived applications
- **.NET 10 LTS and .NET 11**: `net10.0` is the LTS target. `net11.0` is included in the same package
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
- **Maximum Performance**: Tuned for high request rates. Measure your own workload; see the comparison project
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
| 1000 | **Either** | Web APIs, B2B integrations |
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
using Flume;

var services = new ServiceCollection();
services.AddFlume(cfg => cfg.RegisterServicesFromAssemblyContaining<PingHandler>());
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

Flume targets `net10.0` and `net11.0`. .NET 10 is the LTS line. .NET 8, .NET 9, .NET Standard, and .NET Framework are not targets. Support rolls forward with the LTS line.

If you need support for older versions of .NET or don't like that, please use MediatR and support the project in any way you can. Jimmy puts a lot of work into making sure MediatR is as compatible as possible.

## What the mediator does

- Caches one stateless wrapper per closed request, notification, and stream type.
- Resolves handlers and behaviors from the call's `IServiceProvider`. Scoped services are not cached.
- Runs processors and exception handlers once, as pipeline behaviors.
- Keeps `OrderAttribute` for notification handlers and caches the attribute lookup per handler type.
- Leaves Redis and result caching in `Flume.Behaviors`.

Object pooling, the old pipeline compiler, the lock-free cache, and the performance-strategy flags are not part of this library. Multi-level caches, request batching, background JIT, and a distributed cache inside the mediator are not planned there either.

## Performance

`examples/Flume.Comparison` on `net10.0` (.NET 10.0.12, BenchmarkDotNet 0.13.12 ShortRun: 1 launch, 3 warmups, 3 iterations) against MediatR 12.5.0. The fresh-mediator methods resolve a new mediator every iteration. The behavior methods register one pass-through `IPipelineBehavior`. The error column is wide because the job is short; it is not a ranking.

| Method | Mean | Error | StdDev | Allocated |
| --- | ---: | ---: | ---: | ---: |
| MediatRSend | 104.99 ns | 91.11 ns | 4.99 ns | 288 B |
| FlumeSend | 98.38 ns | 123.71 ns | 6.78 ns | 272 B |
| MediatRSendFreshMediator | 124.57 ns | 160.15 ns | 8.78 ns | 320 B |
| FlumeSendFreshMediator | 108.63 ns | 115.25 ns | 6.32 ns | 304 B |
| MediatRSendWithBehavior | 187.43 ns | 48.77 ns | 2.67 ns | 528 B |
| FlumeSendWithBehavior | 218.47 ns | 106.89 ns | 5.86 ns | 512 B |

## Migration from MediatR

To migrate from MediatR to Flume:

1. Replace `MediatR` package with `Flume`
2. Update using statements from `MediatR` to `Flume`
3. Replace `services.AddMediatR()` with `services.AddFlume()`
4. Handlers that implement `IRequestHandler<TRequest>` (no response) keep compiling. `IRequest` extends `IRequest<Unit>`
5. `IStreamRequest<T>` extends `IBaseRequest` only. `StreamHandlerDelegate` takes a `CancellationToken`

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

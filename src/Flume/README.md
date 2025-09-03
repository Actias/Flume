# Flume

A fast, lightweight, and simple mediator pattern implementation for .NET. A drop-in replacement for MediatR 12.x

## Features

- **Simple**: Easy to use mediator pattern implementation
- **Fast**: Optimized for performance with minimal overhead
- **Lightweight**: Small footprint with no external dependencies
- **Type-safe**: Full type safety with compile-time validation
- **Extensible**: Pipeline behaviors and custom handlers support

## Quick Start

### Installation

```bash
dotnet add package Flume
```

### Basic Usage

```csharp
// Register services
var services = new ServiceCollection();

services.AddFlume(Assembly);
services.AddScoped<IRequestHandler<Ping, Pong>, PingHandler>();

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();

// Send a request
var response = await mediator.Send(new Ping { Message = "Hello" });
```

## License

MIT License - see [LICENSE](LICENSE) file for details.

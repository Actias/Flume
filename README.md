# Flume

A fast, lightweight, and simple mediator pattern implementation for .NET.

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
services.AddFlume();
services.AddScoped<IRequestHandler<Ping, Pong>, PingHandler>();

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();

// Send a request
var response = await mediator.Send(new Ping { Message = "Hello" });
```

## Documentation

- [Branching Strategy](BRANCHING_STRATEGY.md) - Git workflow and release process
- [Performance Benchmarks](PERFORMANCE.md) - Performance comparison with other mediators

## Versioning Strategy

Flume follows a .NET version-aligned versioning scheme:

- **Major** - Aligns with the .NET version (e.g., v8.x.x for .NET 8.0)
- **Minor** - Current release within the major version (e.g., v8.1.x)
- **Patch** - Hotfixes and bug fixes (e.g., v8.1.1)

This ensures compatibility and makes it clear which .NET version each release supports.

## Development

This project follows a structured branching strategy:

- `main` - Production-ready releases
- `develop` - Integration branch for features
- `feature/*` - Individual feature development
- `bugfix/*` - Bug fixes
- `hotfix/*` - Critical production fixes

See [BRANCHING_STRATEGY.md](BRANCHING_STRATEGY.md) for detailed workflow information.

## License

MIT License - see [LICENSE](LICENSE) file for details.

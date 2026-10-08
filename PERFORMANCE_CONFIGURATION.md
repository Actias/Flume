# Performance configuration

The cache-size, eviction, pooling, and pipeline-compilation switches were removed. Nothing on the request path read them.

What runs instead:

- One wrapper instance is cached per closed request, notification, or stream type. The cache is static and lives for the process.
- Handlers and pipeline behaviors are resolved from the `IServiceProvider` passed into each call. Scoped services are not stored in the cache.
- `OrderAttribute` lookups are cached per handler type. Handlers without the attribute keep registration order.
- `Flume.Behaviors` is where result caching lives, including Redis. The mediator does not embed a distributed cache.

Register Flume the same way as MediatR 12:

```csharp
services.AddFlume(cfg => cfg.RegisterServicesFromAssemblyContaining<MyHandler>());
```

Handlers stay transient unless you register them yourself. The first behavior you register is the outermost. The default notification publisher is `ForeachAwaitPublisher`.
